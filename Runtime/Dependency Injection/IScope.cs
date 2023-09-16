using System;
using UnityEngine;

namespace Scribe
{
    public interface IScope
    {
        /// <summary>
        /// If an object is bound to this scope for the given type.
        /// </summary>
        public bool IsBound(Type type);

        /// <summary>
        /// If an object is bound to this scope for the given type and id.
        /// </summary>
        public bool IsBound(Type type, string id);

        /// <summary>
        /// Get the object that is bound to this scope for the given type. If 
        /// no object is bound an error is logged and the default returned.
        /// </summary>
        public object Get(Type type);

        /// <summary>
        /// Get the object that is bound to this scope for the given type and
        /// id. If no object is bound an error is logged and the default returned.
        /// </summary>
        public object Get(Type type, string id);
    }
}
