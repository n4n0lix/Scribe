#if HAS_UNITASK
using Cysharp.Threading.Tasks;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Scribe
{
    /// <summary>
    /// Dependency Injection class for managing scopes and injecting dependencies.
    /// </summary>
    public class DI
    {
        /// <summary>
        /// Gets all scopes for a <see cref="MonoBehaviour"/>
        /// <list type="bullet">
        ///     <item>All scopes in parent <see cref="GameObject"/>s</item>
        ///     <item>All scopes in scene of the given <see cref="MonoBehaviour"/></item>
        ///     <item>All global scopes</item>
        /// </list>
        /// The order is first scopes close in hierachy, then scopes higher up in hierachy and lastly scene scopes (unordered).
        /// </summary>
        /// <param name="self">The <see cref="MonoBehaviour"/></param>
        /// <returns>All applying scopes of the given <see cref="MonoBehaviour"/></returns>
        public static List<IScope> GetOrderedScopes(MonoBehaviour self)
        {
            List<IScope> scopes = new List<IScope>();

            // #1 Search in local hierachy
            // TODO: Maybe we can optimize this and already check if the wanted instance
            // exists so we dont have to check every parent? For now we assume hierachies will
            // not be so deep so its negilible
            MonoBehaviour current = (MonoBehaviour)self.GetComponentInParent<IScope>();
            while (current != null)
            {
                scopes.Add((IScope)current);
                if (current.transform.parent != null)
                    current = (MonoBehaviour)current.transform.parent.GetComponentInParent<IScope>();
                else
                    current = null;
            }

            // #2 Check in scene
            if (sceneScopes.ContainsKey(self.gameObject.scene))
                scopes.AddRange(sceneScopes[self.gameObject.scene]);

            // #3 Check globally
            scopes.AddRange(globalScopes);
            scopes.AddRange(gameScopes);

            return scopes;
        }

        /// <summary>
        /// Injects dependencies from applying scopes into fields marked with <see cref="Scribe.InjectAttribute"/>.
        /// See <see cref="GetOrderedScopes"/> for what scopes apply.
        /// </summary>
        /// <param name="self">The <see cref="MonoBehaviour"/></param>
        /// <returns>All applying scopes of the given <see cref="MonoBehaviour"/></returns>
        public static void InjectInto(MonoBehaviour self)
        {
            if (self == null) return;

            // Find scopes
            var scopes = GetOrderedScopes(self);
            var fields = self.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            // Inject into fields
            foreach (var field in fields)
            {
                var injectAttribute = Attribute.GetCustomAttribute(field, typeof(InjectAttribute)) as InjectAttribute;

                if (injectAttribute == null)
                    continue;

                Type fieldType = field.FieldType;

                bool resolved;
                object resolvedObject;

                if (string.IsNullOrEmpty(injectAttribute.id))
                    resolved = Resolve(scopes, fieldType, out resolvedObject);
                else
                    resolved = ResolveById(scopes, fieldType, injectAttribute.id, out resolvedObject);

                if (!resolved && !injectAttribute.optional)
                {
                    if (injectAttribute.id == null)
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
        /// Resolve a dependency for a given type and a collection of scopes.
        /// </summary>
        /// <param name="scopes">A collection of scopes</param>
        /// <param name="type">A type</param>
        /// <param name="result"></param>
        /// <param name="muteFailureLog"></param>
        /// <returns>An object bound for the given type or null</returns>
        public static bool Resolve(IEnumerable<IScope> scopes, Type type, out object result,
            bool muteFailureLog = false)
        {
            foreach (var scope in scopes)
                if (scope.IsBound(type))
                {
                    result = scope.Get(type);
                    return true;
                }

#if UNITY_EDITOR
            if (!muteFailureLog)
            {
                var scopeNames = string.Join(", ", scopes.Select(s => s.ToString()));
                Debug.LogError($"Failed to resolve {type} in {scopes.Count()} scopes `{scopeNames}`");
            }
#endif
            result = null;
            return false;
        }

        /// <summary>
        /// Resolve a dependency for a given type, given id and a collection of scopes.
        /// </summary>
        /// <param name="scopes">A collection of scopes</param>
        /// <param name="type">A type</param>
        /// <returns>An object bound for the given type or null</returns>
        public static bool ResolveById(IEnumerable<IScope> scopes, Type type, string id, out object result)
        {
            foreach (var scope in scopes)
                if (scope.IsBound(type, id))
                {
                    result = scope.Get(type, id);
                    return true;
                }

//#if UNITY_EDITOR
//            Debug.LogError($"Failed to resolve {type} for id `{id}`");
//#endif
            result = null;
            return false;
        }

        public static T Get<T>(MonoBehaviour self) => _Get<T>(self);

        private static T _Get<T>(MonoBehaviour self, bool muteFailureLog = false)
        {
            Resolve(GetOrderedScopes(self), typeof(T), out var result, muteFailureLog);
            return (T)result;
        }

        public static T Get<T>(MonoBehaviour self, string id)
        {
            ResolveById(GetOrderedScopes(self), typeof(T), id, out var result);
            return (T)result;
        }

#if HAS_UNITASK
        public static async UniTask<T> WaitToGetValue<T>(MonoBehaviour self,
            CancellationToken cancellationToken = default) where T : struct
        {
            Nullable<T> result = null;
            await UniTask.WaitUntil(() =>
            {
                result = Get<T>(self);
                return result.HasValue;
            }, cancellationToken: cancellationToken);

            return result.Value;
        }

        public static async UniTask<T> WaitToGet<T>(MonoBehaviour self)
            where T : class
        {
            return await WaitToGet<T>(self, self.destroyCancellationToken);
        }

        public static async UniTask<T> WaitToGet<T>(MonoBehaviour self, CancellationToken cancellationToken)
            where T : class
        {
            T result = _Get<T>(self, muteFailureLog: true);

            if (result == null)
            {
                await UniTask.WaitUntil(() =>
                {
                    result = _Get<T>(self, muteFailureLog: true);
                    return result != null;
                }, cancellationToken: cancellationToken);
            }

            return result;
        }

        public static async UniTask<T> WaitUntil<T>(MonoBehaviour self, Func<T, bool> predicateFunc)
            where T : class
        {
            return await WaitUntil<T>(self, predicateFunc, self.destroyCancellationToken);
        }

        public static async UniTask<T> WaitUntil<T>(MonoBehaviour self, Func<T, bool> predicateFunc,
            CancellationToken cancellationToken)
            where T : class
        {
            return await WaitToGet<T>(self, cancellationToken).ContinueWith(async (t) =>
            {
                await UniTask.WaitUntil(
                    () => predicateFunc(t),
                    cancellationToken: cancellationToken,
                    cancelImmediately: true);
                return t;
            });
        }
#endif

        public static bool HasBound<T>(MonoBehaviour self)
        {
            return Resolve(GetOrderedScopes(self), typeof(T), out var _);
        }

        #region Scene Scopes

        private static Dictionary<Scene, List<IHierarchyScope>> sceneScopes =
            new Dictionary<Scene, List<IHierarchyScope>>();

        /// <summary>
        /// Register a scope to a scene.
        /// </summary>
        /// <param name="scope">The scope</param>
        public static void RegisterSceneScope(IHierarchyScope scope)
        {
            if (!sceneScopes.ContainsKey(scope.scene))
                sceneScopes[scope.scene] = new List<IHierarchyScope>();

            sceneScopes[scope.scene].Add(scope);
        }

        /// <summary>
        /// Unregister a scope from a scene.
        /// </summary>
        /// <param name="scene">The scene</param>
        /// <param name="scope">The scope</param>
        public static void UnregisterSceneScope(IHierarchyScope scope)
        {
            if (!sceneScopes.ContainsKey(scope.scene))
                return;

            sceneScopes[scope.scene].Remove(scope);
        }

        #endregion

        #region Scene Scopes

        private static List<IHierarchyScope> globalScopes = new();

        /// <summary>
        /// Register a scope to a scene.
        /// </summary>
        /// <param name="scope">The scope</param>
        public static void RegisterGlobalScope(IHierarchyScope scope)
        {
            if (globalScopes.Contains(scope))
                return;

            globalScopes.Add(scope);
        }

        /// <summary>
        /// Unregister a scope from a scene.
        /// </summary>
        /// <param name="scene">The scene</param>
        /// <param name="scope">The scope</param>
        public static void UnregisterGlobalScope(IHierarchyScope scope)
        {
            if (!globalScopes.Contains(scope))
                return;

            globalScopes.Remove(scope);
        }

        #endregion

        #region Game Scopes

        private static List<GameScope> gameScopes = new List<GameScope>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            if (!Application.isPlaying) return;

            var gameScopes = Resources.LoadAll<GameScope>("");
            foreach (var scope in gameScopes)
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
            Debug.Log($"added game-scope `{gameScope}`");
        }

        public static void ClearGameScope()
        {
            var allGameScopeNames = gameScopes.Select(s => s.name).ToArray();

            Debug.Log($"removed all game-scopes `{string.Join(", ", allGameScopeNames)}`");
            foreach (var scope in gameScopes)
                UnityEngine.Object.Destroy(scope);

            gameScopes.Clear();
        }

        #endregion
    }
}
