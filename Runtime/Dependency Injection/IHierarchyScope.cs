using System;
using UnityEngine;

namespace Scribe
{
    public interface IHierarchyScope : IScope
    {
        /// <summary>
        /// The gameobject tied to this scope. Used to determine via object 
        /// hierachy which scope affects which gameobjects.
        /// </summary>
        public GameObject gameObject { get; }
    }
}
