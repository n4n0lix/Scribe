using Scribe;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

namespace Scribe
{
    public class Container
    {

        public Func<GameObject, GameObject> instantiateFunc;

        public Container(Func<GameObject, GameObject> instantiationFunc=null)
        {
            instantiateFunc = GameObject.Instantiate;
            if (instantiationFunc != null)
                instantiateFunc = instantiationFunc;
        }

        #region Non-Id Binding
        [SerializeField, HideInInspector]
        private readonly Dictionary<Type, object> instanceBindings = new Dictionary<Type, object>();

        [SerializeField, HideInInspector]
        private readonly Dictionary<Type, GameObject> prefabBindings = new Dictionary<Type, GameObject>();

        public void Bind<T>(T instance)
        {
            var type = typeof(T);
            if (IsNull(instance))
            {
                Debug.LogError($"Failed to bind {type}: object is null, if you want to unbind use `Unbind()`");
                return;
            }

            instanceBindings[type] = instance;
        }

        public void BindFromPrefab<T>(GameObject prefab)
        {
            var type = typeof(T);
            if (IsNull(prefab))
            {
                Debug.LogError($"Failed to bind {type}: object is null, if you want to unbind use `Unbind()`");
                return;
            }

            prefabBindings[type] = prefab;
        }

        public void BindFromResources<T>(string resourcePath) where T : UnityEngine.Object
        {
            Bind(Resources.Load<T>(resourcePath));
        }

        public bool IsBound(Type type) => instanceBindings.ContainsKey(type) || prefabBindings.ContainsKey(type);

        public object Get(Type type)
        {
            // #1 Instance bindings
            var instance = instanceBindings.GetValueOrDefault(type);
            if (instance != null)
                return instance;

            // #2 Prefab bindings
            if (!prefabBindings.ContainsKey(type))
                return null;

            var prefab = prefabBindings[type];
            var go = instantiateFunc(prefab);
            instance = go.GetComponentInChildren(type, true);
            if (instance == null)
            {
                UnityEngine.Object.Destroy(go);
                return null;
            }

            instanceBindings.Add(type, instance);
            return instance;
        }

        public void Unbind<T>()
        {
            var type = typeof(T);
            if (!instanceBindings.ContainsKey(type)) return;

            instanceBindings.Remove(type);
        }

        public void Unbind<T>(T instance)
        {
            var type = typeof(T);
            if (!instanceBindings.ContainsKey(type)) return;
            if (!instanceBindings[type].Equals(instance)) return;

            instanceBindings.Remove(type);
        }
        #endregion

        #region Id Binding
        [SerializeField, HideInInspector]
        private readonly Dictionary<Tuple<Type, string>, object> idBindings = new Dictionary<Tuple<Type, string>, object>();

        public void Bind<T>(string id, T instance)
        {
            var type = typeof(T);

            if (IsNull(instance))
            {
                Debug.LogError($"Failed to bind {type} with id `{id}`: object is null, if you want to unbind use `Unbind()`");
                return;
            }

            idBindings[Tuple.Create(type, id)] = instance;
        }

        public bool IsBound(Type type, string id) => idBindings.ContainsKey(Tuple.Create(type, id));
        public object Get(Type type, string id) => idBindings.GetValueOrDefault(Tuple.Create(type, id));

        public void Unbind<T>(string id)
        {
            var tupel = Tuple.Create(typeof(T), id);
            if (!idBindings.ContainsKey(tupel)) return;

            idBindings.Remove(tupel);
        }

        public void Unbind<T>(string id, T instance)
        {
            var tupel = Tuple.Create(typeof(T), id);
            if (!idBindings.ContainsKey(tupel)) return;
            if (idBindings[tupel].Equals(instance)) return;

            idBindings.Remove(tupel);
        }
        #endregion

        /// <summary>
        /// Returns if the given object is null. If the given object is a UnityEngine.Object
        /// cast it to it, and then execute the null-check to cover the overloaded null comparison.
        /// </summary>
        private static bool IsNull(object obj)
        {
            if (obj is UnityEngine.Object unityObject)
                return unityObject == null;

            return obj == null;
        }
    }
}