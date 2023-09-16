using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Scribe
{
    /// <summary>
    /// A dependency injection scope tied to a specific scene.
    /// </summary>
    [DefaultExecutionOrder(-999)]
    public class GameScope : ScriptableObject, IScope
    {

        protected Container Container = new Container(
            instantiationFunc: (g) =>
            {
                var go = Instantiate(g);
                DontDestroyOnLoad(go);
                return go;
            });

        /// <summary>
        /// <inheritdoc cref="IHierarchyScope.IsBound(Type)"/>
        /// </summary>
        public bool IsBound(Type type) => Container.IsBound(type);

        /// <summary>
        /// <inheritdoc cref="IHierarchyScope.IsBound(Type, string)"/>
        /// </summary>
        public bool IsBound(Type type, string id) => Container.IsBound(type, id);

        /// <summary>
        /// <inheritdoc cref="IHierarchyScope.Get(Type)"/>
        /// </summary>
        public object Get(Type type) => Container.Get(type);

        /// <summary>
        /// <inheritdoc cref="IHierarchyScope.Get(Type, string)"/>
        /// </summary>
        public object Get(Type type, string id) => Container.Get(type, id);
    }
}