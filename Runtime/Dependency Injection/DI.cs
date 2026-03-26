using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
#if HAS_UNITASK
using Cysharp.Threading.Tasks;
#endif

namespace Scribe
{
    /// <summary>
    ///     Dependency Injection class for managing scopes and injecting dependencies.
    /// </summary>
    public class DI
    {
#if HAS_UNITASK
        /// <summary>
        ///     Global throttling settings for async polling (reduce main-thread pressure).
        /// </summary>
        public static int InitialPollDelayMs = 50; // first wait between checks
        public static int   MaxPollDelayMs        = 500; // cap for exponential backoff
        public static float DefaultTimeoutSeconds = -1f; // <=0 means no timeout
#endif

        /// <summary>
        ///     Gets all scopes for a <see cref="MonoBehaviour" />
        ///     <list type="bullet">
        ///         <item>All scopes in parent <see cref="GameObject" />s</item>
        ///         <item>All scopes in scene of the given <see cref="MonoBehaviour" /></item>
        ///         <item>All global scopes</item>
        ///     </list>
        ///     The order is first scopes close in hierarchy, then scopes higher up in hierarchy and lastly scene scopes
        ///     (unordered).
        /// </summary>
        /// <param name="self">The <see cref="MonoBehaviour" /></param>
        /// <returns>All applying scopes of the given <see cref="MonoBehaviour" /></returns>
        public static List<IScope> GetOrderedScopes(MonoBehaviour self)
        {
            var scopes = new List<IScope>();

            // #1 Search in local hierarchy
            // TODO: [Improve] Maybe we can optimize this and already check if the wanted instance
            // exists so we don't have to check every parent? For now we assume hierarchies will
            // not be so deep so it's negligible.
            var current = (MonoBehaviour)self.GetComponentInParent<IScope>();
            while (current != null)
            {
                scopes.Add((IScope)current);
                if (current.transform.parent != null)
                    current = (MonoBehaviour)current.transform.parent.GetComponentInParent<IScope>();
                else
                    current = null;
            }

            // #2 Check in scene
            if (sceneScopes.TryGetValue(self.gameObject.scene, out var list))
                scopes.AddRange(list.ToArray()); // copy to avoid concurrent modification during iteration

            // #3 Check globally
            scopes.AddRange(globalScopes.ToArray());
            scopes.AddRange(gameScopes.ToArray());

            return scopes;
        }

        /// <summary>
        ///     Injects dependencies from applying scopes into fields marked with <see cref="Scribe.InjectAttribute" />.
        ///     See <see cref="GetOrderedScopes" /> for what scopes apply.
        /// </summary>
        /// <param name="self">The <see cref="MonoBehaviour" /></param>
        public static void InjectInto(MonoBehaviour self)
        {
            if (self == null) return;

            // Find fields
            var fields = self.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            // Inject into fields
            foreach (var field in fields)
            {
                var injectAttribute = Attribute.GetCustomAttribute(field, typeof(InjectAttribute)) as InjectAttribute;

                if (injectAttribute == null)
                    continue;

                var fieldType = field.FieldType;

                bool resolved;
                object resolvedObject;

                if (string.IsNullOrEmpty(injectAttribute.id))
                    resolved = Resolve(GetOrderedScopes(self), fieldType, out resolvedObject);
                else
                    resolved = ResolveById(GetOrderedScopes(self), fieldType, injectAttribute.id, out resolvedObject);

                if (!resolved && !injectAttribute.optional)
                {
                    if (string.IsNullOrEmpty(injectAttribute.id))
                        Debug.LogError(
                            $"failed to inject required field: {fieldType.Name} {self.GetType().Name}.{field.Name}");
                    else
                        Debug.LogError(
                            $"failed to inject required field with id `{injectAttribute.id}`: {fieldType.Name} {self.GetType().Name}.{field.Name}");

                    continue;
                }

                field.SetValue(self, resolvedObject);
            }
        }

        /// <summary>
        ///     Resolve a dependency for a given type and a collection of scopes.
        /// </summary>
        /// <param name="scopes">A collection of scopes</param>
        /// <param name="type">A type</param>
        /// <param name="result">The resolved object if found</param>
        /// <param name="muteFailureLog">Mute failure logs (useful for polling)</param>
        /// <returns>True if resolved; otherwise false</returns>
        public static bool Resolve(IEnumerable<IScope> scopes, Type type, out object result,
            bool muteFailureLog = false)
        {
            foreach (var scope in scopes)
            {
                if (scope.IsBound(type))
                {
                    result = scope.Get(type);
                    return true;
                }
            }

#if UNITY_EDITOR
            if (!muteFailureLog)
            {
                var scopeList = scopes as IList<IScope> ?? scopes.ToList();
                var scopeNames = string.Join(", ", scopeList.Select(s => s.ToString()));
                Debug.LogError($"Failed to resolve {type} in {scopeList.Count} scopes `{scopeNames}`");
            }
#endif
            result = null;
            return false;
        }

        /// <summary>
        ///     Resolve a dependency for a given type, given id and a collection of scopes.
        /// </summary>
        /// <param name="scopes">A collection of scopes</param>
        /// <param name="type">A type</param>
        /// <param name="id">Binding identifier</param>
        /// <param name="result">The resolved object if found</param>
        /// <returns>True if resolved; otherwise false</returns>
        public static bool ResolveById(IEnumerable<IScope> scopes, Type type, string id, out object result)
        {
            foreach (var scope in scopes)
            {
                if (scope.IsBound(type, id))
                {
                    result = scope.Get(type, id);
                    return true;
                }
            }

            result = null;
            return false;
        }

        /// <summary>
        ///     Get a bound instance if available; returns default(T) when not found.
        /// </summary>
        public static T Get<T>(MonoBehaviour self) => _Get<T>(self);

        static T _Get<T>(MonoBehaviour self, bool muteFailureLog = false)
        {
            Resolve(GetOrderedScopes(self), typeof(T), out var boxed, muteFailureLog);
            if (boxed == null) return default;
            return (T)boxed;
        }

        public static T Get<T>(MonoBehaviour self, string id)
        {
            ResolveById(GetOrderedScopes(self), typeof(T), id, out var result);
            if (result == null) return default;
            return (T)result;
        }

        public static object Get(MonoBehaviour self, Type type)
        {
            Resolve(GetOrderedScopes(self), type, out var result);
            return result;
        }

        /// <summary>
        ///     Try-get helper for value types to avoid boxing null to T.
        /// </summary>
        public static bool TryGet<T>(MonoBehaviour self, out T value) where T : struct
        {
            if (Resolve(GetOrderedScopes(self), typeof(T), out var boxed, true) && boxed != null)
            {
                value = (T)boxed;
                return true;
            }

            value = default;
            return false;
        }

#if HAS_UNITASK
        /// <summary>
        ///     Throttled poller with exponential backoff to limit main-thread work.
        ///     Runs the predicate on the main thread; waits Initial->Max delay between attempts.
        /// </summary>
        static async UniTask<bool> PollAsync(Func<bool> trySatisfy, CancellationToken ct,
            int initialDelayMs, int maxDelayMs, float timeoutSeconds)
        {
            var delay = Mathf.Max(0, initialDelayMs);
            var maxDelay = Mathf.Max(delay, maxDelayMs);
            var start = Time.realtimeSinceStartup;

            while (!ct.IsCancellationRequested)
            {
                if (trySatisfy())
                    return true;

                if (timeoutSeconds > 0f && Time.realtimeSinceStartup - start >= timeoutSeconds)
                    return false;

                if (delay > 0)
                    await UniTask.Delay(delay, cancellationToken: ct);

                // exponential backoff, capped
                delay = Mathf.Min(delay * 2, maxDelay);
            }

            return false;
        }

        public static async UniTask Inject(MonoBehaviour self)
        {
            self.enabled = false;
            WaitToInjectInto(self).ContinueWith(() => self.enabled = true);
        }

        public static async UniTask WaitToInjectInto(MonoBehaviour self)
            => WaitToInjectInto(self, self.destroyCancellationToken);

        public static async UniTask WaitToInjectInto(MonoBehaviour self, CancellationToken cancellationToken)
        {
            if (self == null) return;

            var fields = self.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            // Inject into fields
            foreach (var field in fields)
            {
                var injectAttribute = Attribute.GetCustomAttribute(field, typeof(InjectAttribute)) as InjectAttribute;
                if (injectAttribute == null) continue;

                var fieldType = field.FieldType;
                var resolved = false;
                var resolvedObject = fieldType.IsValueType ? Activator.CreateInstance(fieldType) : null;

                var ok = await PollAsync(() =>
                    {
                        var scopes = GetOrderedScopes(self); // recompute on each poll so late scopes are visible
                        if (string.IsNullOrEmpty(injectAttribute.id))
                            resolved = Resolve(scopes, fieldType, out resolvedObject, true);
                        else
                            resolved = ResolveById(scopes, fieldType, injectAttribute.id, out resolvedObject);

                        return resolved || injectAttribute.optional;
                    },
                    cancellationToken,
                    InitialPollDelayMs,
                    MaxPollDelayMs,
                    DefaultTimeoutSeconds);

                if (!ok || !resolved && !injectAttribute.optional)
                {
                    if (string.IsNullOrEmpty(injectAttribute.id))
                        Debug.LogError(
                            $"failed to inject required field: {fieldType.Name} {self.GetType().Name}.{field.Name}");
                    else
                        Debug.LogError(
                            $"failed to inject required field with id `{injectAttribute.id}`: {fieldType.Name} {self.GetType().Name}.{field.Name}");
                    continue;
                }

                field.SetValue(self, resolvedObject);
            }
        }

        public static async UniTask<T> WaitToGetValue<T>(MonoBehaviour self,
            CancellationToken cancellationToken = default) where T : struct
        {
            T result = default;
            await PollAsync(() => TryGet(self, out result),
                cancellationToken,
                InitialPollDelayMs,
                MaxPollDelayMs,
                DefaultTimeoutSeconds);
            return result;
        }

        public static async UniTask<T> WaitToGet<T>(MonoBehaviour self)
            where T : class => await WaitToGet<T>(self, self.destroyCancellationToken);

        public static async UniTask<T> WaitToGet<T>(MonoBehaviour self, CancellationToken cancellationToken)
            where T : class
        {
            var result = _Get<T>(self, true);
            if (result != null) return result;

            await PollAsync(() =>
                {
                    result = _Get<T>(self, true);
                    return result != null;
                },
                cancellationToken,
                InitialPollDelayMs,
                MaxPollDelayMs,
                DefaultTimeoutSeconds);

            return result;
        }

        public static async UniTask<T> WaitUntil<T>(MonoBehaviour self, Func<T, bool> predicateFunc)
            where T : class => await WaitUntil(self, predicateFunc, self.destroyCancellationToken);

        public static async UniTask<T> WaitUntil<T>(MonoBehaviour self, Func<T, bool> predicateFunc,
            CancellationToken cancellationToken)
            where T : class
        {
            var t = await WaitToGet<T>(self, cancellationToken);
            await PollAsync(() => predicateFunc(t),
                cancellationToken,
                InitialPollDelayMs,
                MaxPollDelayMs,
                DefaultTimeoutSeconds);
            return t;
        }
#endif

        public static bool HasBound<T>(MonoBehaviour self) => Resolve(GetOrderedScopes(self), typeof(T), out _);

        #region Scene Scopes
        static readonly Dictionary<Scene, List<IHierarchyScope>> sceneScopes =
            new Dictionary<Scene, List<IHierarchyScope>>();

        /// <summary>
        ///     Register a scope to a scene.
        /// </summary>
        /// <param name="scope">The scope</param>
        public static void RegisterSceneScope(IHierarchyScope scope)
        {
            if (!sceneScopes.ContainsKey(scope.scene))
                sceneScopes[scope.scene] = new List<IHierarchyScope>();

            sceneScopes[scope.scene].Add(scope);
        }

        /// <summary>
        ///     Unregister a scope from a scene.
        /// </summary>
        /// <param name="scope">The scope</param>
        public static void UnregisterSceneScope(IHierarchyScope scope)
        {
            if (!sceneScopes.ContainsKey(scope.scene))
                return;

            sceneScopes[scope.scene].Remove(scope);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void WireSceneEvents()
        {
            // Ensure handler is not double-registered
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        static void OnSceneUnloaded(Scene scene)
        {
            if (sceneScopes.Remove(scene))
                Debug.Log($"Cleared DI scene scopes for unloaded scene `{scene.name}`");
        }
        #endregion

        #region Global Scopes
        static readonly List<IHierarchyScope> globalScopes = new List<IHierarchyScope>();

        /// <summary>
        ///     Register a global scope.
        /// </summary>
        /// <param name="scope">The scope</param>
        public static void RegisterGlobalScope(IHierarchyScope scope)
        {
            if (globalScopes.Contains(scope))
                return;

            globalScopes.Add(scope);
        }

        /// <summary>
        ///     Unregister a global scope.
        /// </summary>
        /// <param name="scope">The scope</param>
        public static void UnregisterGlobalScope(IHierarchyScope scope)
        {
            if (!globalScopes.Contains(scope))
                return;

            globalScopes.Remove(scope);
        }
        #endregion

        #region Game Scopes
        static readonly List<GameScope> gameScopes = new List<GameScope>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init()
        {
            if (!Application.isPlaying) return;

            var loadedGameScopes = Resources.LoadAll<GameScope>("");
            foreach (var scope in loadedGameScopes)
            {
                scope.RegisterScope();
                AddGameScope(scope);
            }
        }

        public static void AddGameScope(GameScope gameScope)
        {
            gameScopes.Add(gameScope);
            Debug.Log($"added game-scope `{gameScope}`");
        }

        public static void RemoveGameScope(GameScope gameScope)
        {
            gameScopes.Remove(gameScope);
            Debug.Log($"removed game-scope `{gameScope}`");
        }

        public static void ClearGameScope()
        {
            var allGameScopeNames = gameScopes.Select(s => s.name).ToArray();

            Debug.Log($"removed all game-scopes `{string.Join(", ", allGameScopeNames)}`");
            foreach (var scope in gameScopes)
                Object.Destroy(scope);

            gameScopes.Clear();
        }
        #endregion

    }
}