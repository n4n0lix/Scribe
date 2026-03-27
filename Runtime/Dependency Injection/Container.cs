using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;
namespace Scribe
{
    public class Container
    {
        public Func<GameObject, GameObject> instantiateFunc;

        public Container(Func<GameObject, GameObject> instantiationFunc = null)
        {
            instantiateFunc = GameObject.Instantiate;
            if (instantiationFunc != null)
                instantiateFunc = instantiationFunc;
        }


        #region Non-Id Binding
        readonly Dictionary<Type, object> instanceBindings = new Dictionary<Type, object>();

        readonly Dictionary<Type, GameObject> prefabBindings = new Dictionary<Type, GameObject>();

        public void Bind<T>(T instance)
        {
            var type = typeof(T);
            if (IsNull(instance))
            {
                Debug.LogError($"Failed to bind {type}: object is null, if you want to unbind use `Unbind()`");
                return;
            }

            Debug.Log($"Bound {typeof(T).Name} => {instance}");
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

            Debug.Log($"Bound prefab {typeof(T).Name} => {prefab}");
            prefabBindings[type] = prefab;
        }

        public void BindFromResources<T>(string resourcePath) where T : Object
        {
            Bind(Resources.Load<T>(resourcePath));
        }

        public void InstantiateAndBindResource<T>(string resourcePath) where T : Object
        {
            var obj = Resources.Load<T>(resourcePath);
            Bind(Object.Instantiate(obj));
        }

        public bool IsBound(Type type)
        {
            if (instanceBindings.TryGetValue(type, out var instance) && !IsNull(instance))
                return true;

            if (prefabBindings.TryGetValue(type, out var prefab) && !IsNull(prefab))
                return true;

            return false;
        }

        public bool IsBound(Type type, string id)
        {
            var key = Tuple.Create(type, id);
            return idBindings.TryGetValue(key, out var instance) && !IsNull(instance);
        }

        public object Get(Type type)
        {
            // #1 Handle bound instance
            var (instanceFound, instance) = _GetInstance(type);
            if (instanceFound)
            {
                return instance;
            }

            // #2 Handle bound prefabs
            var (prefabFound, prefab) = _GetPrefab(type);
            if (prefabFound)
            {
                // #2.1 Create an inactive version of the prefab
                var cachedActive = prefab.activeSelf;
                prefab.SetActive(false);
                var go = instantiateFunc(prefab);
                prefab.SetActive(cachedActive);

                instance = go.GetComponentInChildren(type, true);
                if (instance == null)
                {
                    Object.Destroy(go);
                    return null;
                }

                go.SetActive(true);
                instanceBindings.Add(type, instance);

                return instance;
            }

            return Default(type);
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

        (bool, object) _GetInstance(Type type)
        {
            if (!instanceBindings.TryGetValue(type, out var instance))
                return (false, null);

            if (IsNull(instance))
            {
                instanceBindings.Remove(type);
                return (false, null);
            }

            if (!type.IsInstanceOfType(instance))
            {
                var actualType = instance.GetType();
                instanceBindings.Remove(type);
                throw new InvalidCastException(
                    $"Type mismatch: object of type {actualType.Name} was bound as {type.Name}.");
            }

            return (true, instance);
        }

        (bool, GameObject) _GetPrefab(Type type)
        {
            // #1 No binding exists
            if (!prefabBindings.ContainsKey(type))
                return (false, null);


            return (true, prefabBindings[type]);
        }
        #endregion

        #region Id Binding
        readonly Dictionary<Tuple<Type, string>, object> idBindings =
            new Dictionary<Tuple<Type, string>, object>();

        public void Bind<T>(string id, T instance)
        {
            var type = typeof(T);

            if (IsNull(instance))
            {
                Debug.LogError(
                    $"Failed to bind {type} with id `{id}`: object is null, if you want to unbind use `Unbind()`");
                return;
            }

            Debug.Log($"Bound prefab {typeof(T).Name} [{id}] => {instance}");

            // This inheritantly unbinds any previous bound instance
            idBindings[Tuple.Create(type, id)] = instance;
        }

        public object Get(Type type, string id)
        {
            var (found, instance) = _Get(type, id);

            if (!found)
                return Default(type);

            return instance;
        }

        public void Unbind<T>(string id)
        {
            var tupel = Tuple.Create(typeof(T), id);
            idBindings.Remove(tupel);
        }

        /// <summary>
        ///     Unbinds "id" if the current bound instance is `instance`.
        /// </summary>
        /// <typeparam name="T">The binding type</typeparam>
        /// <param name="id">The binding id</param>
        /// <param name="instance">The instance to unbind</param>
        public void Unbind<T>(string id, T instance)
        {
            var tupel = Tuple.Create(typeof(T), id);

            var (found, obj) = _Get(typeof(T), id);
            if (!found) return;
            if (!obj.Equals(instance)) return;

            idBindings.Remove(tupel);
        }

        /// <summary>
        /// </summary>
        /// <param name="type"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <exception cref="InvalidCastException"></exception>
        (bool, object) _Get(Type type, string id)
        {
            var key = Tuple.Create(type, id);

            // #1 No binding exists
            if (!idBindings.ContainsKey(key))
                return (false, null);

            // #2 Ensure the bound instance meets expectations (type, not null, ...)
            object instance;

            // #2.1 Type check
            try
            {
                instance = Convert.ChangeType(idBindings[key], type);
            }
            catch (InvalidCastException e)
            {
                idBindings.Remove(key);
                throw new InvalidCastException(
                    $"type mismatch: object ({idBindings[key].GetType().Name}) was bound as type {type.Name}!", e);
            }

            // #2.2 Null check
            if (IsNull(instance))
            {
                idBindings.Remove(key);
                return (false, null);
            }

            return (true, instance);
        }
        #endregion

        /// <summary>
        ///     Returns if the given object is null. If the given object is a UnityEngine.Object
        ///     cast it to it, and then execute the null-check to cover the overloaded null comparison.
        /// </summary>
        static bool IsNull(object obj)
        {
            if (obj is Object unityObject)
                return unityObject == null;

            return obj == null;
        }

        // TODO: Remove bindings from "from" container
        public static void MoveBindings(Container from, Container to, bool overrideExistingBindings = true)
        {
            // #1 Move normal bindings
            foreach (var x in from.instanceBindings)
            {
                if (!overrideExistingBindings && to.instanceBindings.ContainsKey(x.Key))
                    continue;

                to.instanceBindings[x.Key] = x.Value;
            }

            // #2 Move prefab bindings
            foreach (var x in from.prefabBindings)
            {
                if (!overrideExistingBindings && to.prefabBindings.ContainsKey(x.Key))
                    continue;

                to.prefabBindings[x.Key] = x.Value;
            }

            // #3 Move id bindings
            foreach (var x in from.idBindings)
            {
                if (!overrideExistingBindings && to.idBindings.ContainsKey(x.Key))
                    continue;

                to.idBindings[x.Key] = x.Value;
            }
        }

        public static object Default(Type type)
        {
            if (type.IsValueType)
                return Activator.CreateInstance(type);

            return null;
        }

        class GetResult
        {
            public GetResult(bool found, object obj)
            {
                this.found = found;
                this.obj = obj;
            }

            public object obj;
            public bool   found;
        }
    }
}