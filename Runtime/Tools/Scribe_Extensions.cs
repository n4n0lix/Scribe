using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if HAS_SPRITESHAPE
using UnityEngine.U2D;
#endif
#if HAS_UNITASK
#endif

public static class Scribe_Extensions
{

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

    #region Queue
    public static void EnqueueAll<T>(this Queue<T> pSelf, IEnumerable<T> pRange)
    {
        foreach (var t in pRange)
            pSelf.Enqueue(t);
    }
    #endregion

    #region Transform
    public static void DestroyAllChildren(this Transform t)
    {
        for(var i = 0; i < t.childCount; i++)
            MonoBehaviour.Destroy(t.GetChild(i).gameObject);
    }
    #endregion

    #region Dictionary
    public static Dictionary<V, K> Reverse<K, V>(this Dictionary<K, V> self)
    {
        var reverse = new Dictionary<V, K>();
        foreach (var kvp in self)
            reverse.Add(kvp.Value, kvp.Key);
        return reverse;
    }
    #endregion

    #region Array
    public static T Random<T>(this T[] self) => self[UnityEngine.Random.Range(0, self.Length)];

    public static T GetOrLast<T>(this T[] self, int index) => self[self.ClampIndex(index)];

    public static T GetOrDefault<T>(this T[] self, int index, T _default = default) => index >= self.Length ? _default : self[index];

    public static int ClampIndex<T>(this T[] self, int index) => Mathf.Clamp(index, 0, self.Length - 1);

    public static T GetOrLast<T>(this List<T> self, int index) => self[self.ClampIndex(index)];

    public static int ClampIndex<T>(this List<T> self, int index) => Mathf.Clamp(index, 0, self.Count - 1);
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

    public static void DestroyAllChildren(this GameObject g)
    {
        g.transform.DestroyAllChildren();
    }
    #endregion

    #region MonoBehaviour
    public static bool HasComponent<T>(this MonoBehaviour self) => self.GetComponent<T>() != null;

    public static T GetOrAddComponent<T>(this MonoBehaviour self) where T : Component => self.gameObject.GetOrAddComponent<T>();
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

    #region Spline
#if HAS_SPRITESHAPE

    /// <summary>
    ///     Get position along the spline based on percentage (0 to 1), respecting tangents and curve shape.
    /// </summary>
    public static Vector3 GetPositionAtPercentage(this Spline spline, float t)
    {
        var segmentCount = spline.GetPointCount() - 1;
        if (segmentCount < 1)
            return spline.GetPointCount() > 0 ? spline.GetPosition(0) : Vector3.zero;

        var totalT = Mathf.Clamp01(t) * segmentCount;
        var segmentIndex = Mathf.FloorToInt(totalT);
        var localT = totalT - segmentIndex;

        if (segmentIndex >= segmentCount)
            segmentIndex = segmentCount - 1;

        return EvaluateBezierOnSegment(spline, segmentIndex, localT);
    }

    /// <summary>
    ///     Get position along the spline based on world-space distance (0 to spline length), respecting tangents and curve
    ///     shape.
    /// </summary>
    public static Vector3 GetPositionAtDistance(this Spline spline, float distance, int resolution = 100)
    {
        var totalLength = spline.CalculateLength(resolution);
        var targetDistance = Mathf.Clamp(distance, 0f, totalLength);

        var accumulated = 0f;
        var prev = spline.GetPositionAtPercentage(0f);

        for(var i = 1; i <= resolution; i++)
        {
            var t = i / (float)resolution;
            var current = spline.GetPositionAtPercentage(t);
            var segmentLength = Vector3.Distance(prev, current);

            if (accumulated + segmentLength >= targetDistance)
            {
                var overshoot = targetDistance - accumulated;
                var segmentT = overshoot / segmentLength;
                return Vector3.Lerp(prev, current, segmentT);
            }

            accumulated += segmentLength;
            prev = current;
        }

        return spline.GetPositionAtPercentage(1f); // Fallback
    }

    /// <summary>
    ///     Approximate total spline length using Bezier segments.
    /// </summary>
    public static float CalculateLength(this Spline spline, int resolutionPerSegment = 10)
    {
        var segments = spline.GetPointCount() - 1;
        var length = 0f;

        for(var i = 0; i < segments; i++)
        {
            var prev = EvaluateBezierOnSegment(spline, i, 0f);
            for(var j = 1; j <= resolutionPerSegment; j++)
            {
                var t = j / (float)resolutionPerSegment;
                var current = EvaluateBezierOnSegment(spline, i, t);
                length += Vector3.Distance(prev, current);
                prev = current;
            }
        }

        return length;
    }

    static Vector3 EvaluateBezierOnSegment(Spline spline, int segmentIndex, float t)
    {
        SpriteShapeController c;
        var p0 = spline.GetPosition(segmentIndex);
        var p1 = p0 + spline.GetRightTangent(segmentIndex);
        var p3 = spline.GetPosition(segmentIndex + 1);
        var p2 = p3 + spline.GetLeftTangent(segmentIndex + 1);

        return BezierPoint(p0, p1, p2, p3, t);
    }

    static Vector3 BezierPoint(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        var u = 1f - t;
        var tt = t * t;
        var uu = u * u;

        return u * uu * p0 +
            3f * uu * t * p1 +
            3f * u * tt * p2 +
            tt * t * p3;
    }


#endif
    #endregion

    #region Vector3Int
    public static Vector2Int ToVec2(this Vector3Int self) => new Vector2Int(self.x, self.y);
    #endregion

    #region Vector2Int
    public static Vector3Int ToVec3(this Vector2Int self) => new Vector3Int(self.x, self.y, 0);
    public static int ManhattanDistance(this Vector2Int pStart, Vector2Int pGoal) => Mathf.Abs(pStart.x - pGoal.x) + Mathf.Abs(pStart.y - pGoal.y);
    #endregion

}