using Scribe;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.SocialPlatforms;

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
        ///     <item>All <see cref="Scribe.IHierarchyScope"></see> in parent <see cref="GameObject"/>s</item>
        ///     <item>All <see cref="Scribe.ISceneScope"></see> registered to the scene of the given <see cref="MonoBehaviour"/></item>
        ///     <item>All <see cref="Scribe.GameScope"></see> registered to the game (global)</item>
        /// </list>
        /// The order is first scopes close in hierachy, then scopes higher up in hierachy and lastly scene scopes (unordered).
        /// </summary>
        /// <param name="self">The <see cref="MonoBehaviour"/></param>
        /// <returns>All applying scopes of the given <see cref="MonoBehaviour"/></returns>
        public static List<IScope> GetScopes(MonoBehaviour self)
        {
            List<IScope> scopes = new List<IScope>();

            MonoBehaviour current = (MonoBehaviour)self.GetComponentInParent<IScope>();
            while (current != null)
            {
                scopes.Add((IScope)current);
                if (current.transform.parent != null)
                    current = (MonoBehaviour)current.transform.parent.GetComponentInParent<IScope>();
                else
                    current = null;
            }

            if (sceneScopes.ContainsKey(self.gameObject.scene))
                scopes.AddRange(sceneScopes[self.gameObject.scene]);

            scopes.AddRange(gameScopes);

            return scopes;
        }

        /// <summary>
        /// Injects dependencies from applying scopes into fields marked with <see cref="Scribe.InjectAttribute"/>.
        /// See <see cref="DI.GetScopes(MonoBehaviour)"/> for what scopes apply.
        /// </summary>
        /// <param name="self">The <see cref="MonoBehaviour"/></param>
        /// <returns>All applying scopes of the given <see cref="MonoBehaviour"/></returns>
        public static void InjectInto(MonoBehaviour self)
        {
            if (self == null) return;

            // Find scopes
            var scopes = GetScopes(self);
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
                        Debug.LogError($"failed to inject required field: {fieldType.Name} {self.GetType().Name}.{field.Name}");
                    else
                        Debug.LogError($"failed to inject required field with id `{injectAttribute.id}`: {fieldType.Name} {self.GetType().Name}.{field.Name}");

                    return;
                }

                field.SetValue(self, resolvedObject);
            }
        }

        /// <summary>
        /// Resolve a dependency for a given type and a collection of scopes.
        /// </summary>
        /// <param name="scopes">A collection of scopes</param>
        /// <param name="type">A type</param>
        /// <returns>An object bound for the given type or null</returns>
        public static bool Resolve(IEnumerable<IScope> scopes, Type type, out object result)
        {
            foreach (var scope in scopes)
                if (scope.IsBound(type))
                {
                    result = scope.Get(type);
                    return true;
                }

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
                    result =scope.Get(type, id);
                    return true;
                }

            result = null;
            return false;
        }

        public static T Get<T>(MonoBehaviour self)
        {
            Resolve(GetScopes(self), typeof(T), out var result);
            return (T)result;
        }

        #region Scene Scopes
        private static Dictionary<Scene, List<IHierarchyScope>> sceneScopes = new Dictionary<Scene, List<IHierarchyScope>>();

        /// <summary>
        /// Register a scope to a scene.
        /// </summary>
        /// <param name="scene">The scene</param>
        /// <param name="scope">The scope</param>
        public static void RegisterSceneScope(Scene scene, IHierarchyScope scope)
        {
            if (!sceneScopes.ContainsKey(scene))
                sceneScopes[scene] = new List<IHierarchyScope>();

            sceneScopes[scene].Add(scope);
        }

        /// <summary>
        /// Unregister a scope from a scene.
        /// </summary>
        /// <param name="scene">The scene</param>
        /// <param name="scope">The scope</param>
        public static void UnregisterSceneScope(Scene scene, IHierarchyScope scope)
        {
            if (!sceneScopes.ContainsKey(scene))
                return;

            sceneScopes[scene].Remove(scope);
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
                scope.OnRegister();
                AddGameScope(scope);
            }
        }

        public static void AddGameScope(GameScope gameScope)
        {
            gameScopes.Add(gameScope);
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
