using System;
using UnityEngine;

namespace Scribe
{
    /// <summary>
    /// A dependency injection scope.
    /// </summary>
    [DefaultExecutionOrder(-999)]
    public class Scope : IScope
    {

        protected Container Container = new Container();

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
