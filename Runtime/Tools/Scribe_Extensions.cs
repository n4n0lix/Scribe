using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public static class Scribe_Extensions
{
    #region Array
        public static T Random<T>(this T[] self) => self[UnityEngine.Random.Range(0, self.Length)];

        public static T GetOrLast<T>(this T[] self, int index) => self[Mathf.Clamp(index, 0, self.Length - 1)];

        public static T GetOrDefault<T>(this T[] self, int index, T _default = default) => (index >= self.Length) ? _default : self[index];
    #endregion
    
    #region Behavior
    public static bool HasComponent<T>(this Behaviour self) => self.GetComponent<T>() != null;

    public static T GetOrAddComponent<T>(this Behaviour self) where T : Component => self.gameObject.GetOrAddComponent<T>();
    #endregion

    #region Component
    public static bool HasComponent<T>(this Component self) => self.GetComponent<T>() != null;

        public static T GetOrAddComponent<T>(this Component self) where T : Component => self.gameObject.GetOrAddComponent<T>();
    #endregion

    #region GameObject
    public static bool HasComponent<T>(this GameObject self) => self.GetComponent<T>() != null;

        public static T GetOrAddComponent<T>(this GameObject self) where T : Component
        {
            var component = self.GetComponent<T>();
            if (component == null)
                return self.AddComponent<T>();
                
            return component;
        }
        #endregion

    #region Graphic
        public static void SetAlpha(this Graphic self, float alpha)
        {
            var color = self.color;
            color.a = alpha;
            self.color = color;
        }
        #endregion

    #region IEnumerator
        public static IEnumerator<T> GetEnumerator<T>(this IEnumerator<T> enumerator) => enumerator;
    #endregion

    #region MonoBehaviour
    public static bool HasComponent<T>(this MonoBehaviour self) => self.GetComponent<T>() != null;

    public static T GetOrAddComponent<T>(this MonoBehaviour self) where T : Component => self.gameObject.GetOrAddComponent<T>();
    #endregion
   
    #region Queue
    public static void EnqueueAll<T>(this Queue<T> pSelf, IEnumerable<T> pRange)
    {
        foreach (T t in pRange)
            pSelf.Enqueue(t);
    }
    #endregion

    #region RectTransform
    public static void SetXAnchor(this RectTransform self, float minX, float maxX)
        {
            self.SetAnchorMinX(minX);
            self.SetAnchorMaxX(maxX);
        }

        public static void SetYAnchor(this RectTransform self, float minY, float maxY)
        {
            self.SetAnchorMinY(minY);
            self.SetAnchorMaxY(maxY);
        }

        public static void SetAnchorMinX(this RectTransform self, float minX)
        {
            var minAnchor = self.anchorMin;
            minAnchor.x = minX;
            self.anchorMin = minAnchor;
        }

        public static void SetAnchorMaxX(this RectTransform self, float maxX)
        {
            var maxAnchor = self.anchorMax;
            maxAnchor.x = maxX;
            self.anchorMax = maxAnchor;
        }

        public static void SetAnchorMinY(this RectTransform self, float minY)
        {
            var minAnchor = self.anchorMin;
            minAnchor.y = minY;
            self.anchorMin = minAnchor;
        }

        public static void SetAnchorMaxY(this RectTransform self, float maxY)
        {
            var maxAnchor = self.anchorMax;
            maxAnchor.y = maxY;
            self.anchorMax = maxAnchor;
        }
    #endregion

    #region Transform
    public static void DestroyAllChildren(this Transform t)
    {
        for (int i = 0; i < t.childCount; i++)
            MonoBehaviour.Destroy(t.GetChild(i).gameObject);
    }
    #endregion
}

