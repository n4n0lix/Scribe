using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Scribe
{
    public sealed class InstanceFactory<T> where T : Component
    {
        readonly Transform  content;
        readonly GameObject prefab;
        readonly bool       poolInsteadOfDestroy;

        readonly List<T> instances = new List<T>();

        public int Count => instances.Count;
        public T this[int index] => instances[index];

        public InstanceFactory(
            Transform content,
            GameObject prefab,
            bool initialScanExistingChildren = true,
            bool poolInsteadOfDestroy = true)
        {
            this.content = content ? content : throw new ArgumentNullException(nameof(content));
            this.prefab = prefab ? prefab : throw new ArgumentNullException(nameof(prefab));
            this.poolInsteadOfDestroy = poolInsteadOfDestroy;

            if (this.prefab.GetComponent<T>() == null &&
                this.prefab.GetComponentInChildren<T>(true) == null)
            {
                throw new InvalidOperationException(
                    $"InstanceFactory<{typeof(T).Name}>: Prefab '{this.prefab.name}' has no {typeof(T).Name} component.");
            }

            if (initialScanExistingChildren)
            {
                for(var i = 0; i < this.content.childCount; i++)
                {
                    var child = this.content.GetChild(i);
                    var instance = child.GetComponent<T>() ?? child.GetComponentInChildren<T>(true);
                    if (instance != null)
                        instances.Add(instance);
                }
            }
        }

        public void EnsureCount(int count)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            while (instances.Count < count)
                instances.Add(CreateOne());
        }

        public T GetOne()
        {
            if (poolInsteadOfDestroy)
            {
                for(var i = 0; i < instances.Count; i++)
                {
                    var inst = instances[i];
                    if (inst == null)
                        continue;

                    var container = GetContainer(inst);
                    if (!container.activeSelf)
                    {
                        // ensure correct parenting
                        if (container.transform.parent != content)
                            container.transform.SetParent(content, false);

                        container.SetActive(true);
                        return inst;
                    }
                }
            }

            var created = CreateOne();
            instances.Add(created);
            return created;
        }

        public void ReleaseOne(T instance)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            var index = instances.IndexOf(instance);
            if (index < 0)
                throw new InvalidOperationException(
                    $"InstanceFactory<{typeof(T).Name}>: Tried to release an instance that is not tracked by this factory.");

            if (poolInsteadOfDestroy)
            {
                var container = GetContainer(instance);

                // keep pooled items under the expected parent for reuse
                if (container.transform.parent != content)
                    container.transform.SetParent(content, false);

                container.SetActive(false);
                return;
            }

            instances.RemoveAt(index);

            var go = GetContainer(instance);
            if (go != null)
                Object.Destroy(go);
        }

        public T FindOne(Predicate<T> match) => instances.Find(match);

        public void Clear()
        {
            if (poolInsteadOfDestroy)
            {
                foreach (var t in instances)
                {
                    if (t == null) continue;
                    GetContainer(t).SetActive(false);
                }
                return;
            }

            for(var i = instances.Count - 1; i >= 0; i--)
            {
                var inst = instances[i];
                instances.RemoveAt(i);

                if (inst == null) continue;

                var container = GetContainer(inst);
                Object.Destroy(container);
            }
        }

        T CreateOne()
        {
            var go = Object.Instantiate(prefab, content);
            if (!go)
                throw new InvalidOperationException("Instantiate returned null.");

            var instance = go.GetComponent<T>() ?? go.GetComponentInChildren<T>(true);

            if (instance == null)
            {
                Object.Destroy(go);
                throw new InvalidOperationException(
                    $"Spawned object '{prefab.name}' has no {typeof(T).Name} component.");
            }

            // activate the whole entry container (the instantiated root)
            go.SetActive(true);
            return instance;
        }

        GameObject GetContainer(T inst)
        {
            // We want the topmost object that is a direct child of `content`.
            // This makes pooling robust even if T is on a nested child.
            var tr = inst.transform;

            while (tr.parent != null && tr.parent != content)
                tr = tr.parent;

            // If it's not under content (shouldn't happen), fall back to the component's GO.
            return tr.parent == content ? tr.gameObject : inst.gameObject;
        }
    }
}