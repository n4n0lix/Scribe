using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.EnhancedTouch;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Scribe.Tooltip
{

    public class Tooltip : MonoBehaviour
    {

        [SerializeField] RectTransform content;
        [SerializeField] TextMeshProUGUI textField;
        [SerializeField] TextMeshProUGUI buttonText;
        [SerializeField] Button button;
        [SerializeField] LineRenderer arrow;
        [SerializeField] float distanceToTarget;

        [SerializeField] Vector2 maxSize = new Vector2(579, 9999);
        [SerializeField] Overflow defaultMargin = new Overflow();
        [SerializeField] VerticalLayoutGroup layout;
        [SerializeField] RectTransform contentButtons;

        public static RectTransform Target { get; private set; }
        Vector3 lastTargetPos;
        bool autoHideOnInput;
        Coroutine autoHideEnabler;
        Action onHide;
        RectTransform bounds;
        Overflow boundsMargin;

        public Options On(RectTransform target) => new Options(this).On(target);
        public Options On(Transform target) => On(target as RectTransform);
        public Options On(GameObject gameObject) => On(gameObject.transform);
        public Options On(Component component) => On(component.transform);

        void Show(Options options)
        {
            if (autoHideEnabler != null)
            {
                StopCoroutine(autoHideEnabler);
                autoHideOnInput = false;
            }

            if (options.showButton)
                ShowButton(options.buttonAction, options.buttonText, options.buttonSprite);
            else
                HideButton();

            if (options.bounds != null)
            {
                bounds = options.bounds;
                boundsMargin = options.boundsMargin;
            }
            else
            {
                bounds = transform as RectTransform;
                boundsMargin = defaultMargin;
            }

            Target = options.target;
            textField.text = options.text;
            onHide = options.onHideAction;

            UpdatePositionAndSize();
            PlayShowAnimation();

            content.gameObject.SetActive(true);
            arrow.gameObject.SetActive(true);

            autoHideEnabler = StartCoroutine(SetAutoHideWithDelay(0.1f, options.autoHideOnInput));
        }

        private void Awake()
        {
#if SCRIBE_INPUTSYSTEM
            EnhancedTouchSupport.Enable();
#endif
        }

        private void Start()
        {
            content.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (Target == null) return;

            if (lastTargetPos != Target.position)
            {
                lastTargetPos = Target.position;
                UpdatePositionAndSize();
            }

            HandleAutoHiding();
        }

        private IEnumerator SetAutoHideWithDelay(float delay, bool isAutoHide)
        {
            autoHideOnInput = false;
            yield return null;// new WaitForSeconds(delay);
            autoHideOnInput = isAutoHide;
        }

        private void HandleAutoHiding()
        {
            if (!autoHideOnInput) return;
            if (!content.gameObject.activeInHierarchy) return;
            if (!IsGlobalInput(out Vector2 clickScreenPos)) return;

            var results = RaycastUiByScreenPosition(clickScreenPos);
            var clickedTransform = results.Count > 0 ? results[0].gameObject.transform : null;
            if (clickedTransform != null && clickedTransform.IsChildOf(content))
                return;

            Hide();
        }

        private void UpdatePositionAndSize()
        {
            // wXXX -> world-space
            // lXXX -> local-space of this gameobject
            var wTargetRect = GetWorldRect(Target);
            var wBoundsRect = GetWorldRect(bounds);
            var wPreferredPosition = wTargetRect.center;
            var lPreferredSize = CalculateLocalTargetSize();
            var wPreferredSize = ToWorld(new Rect(Vector2.zero, lPreferredSize)).size;
            var wDistanceToTarget = ToWorld(distanceToTarget);

            wBoundsRect.xMin += ToWorld(boundsMargin.left);
            wBoundsRect.xMax -= ToWorld(boundsMargin.right);
            wBoundsRect.yMin += ToWorld(boundsMargin.bottom);
            wBoundsRect.yMax -= ToWorld(boundsMargin.top);

            // Check if there is enough space to show above
            var wAbovePos = wPreferredPosition;
            wAbovePos.y += wPreferredSize.y / 2 + wTargetRect.height / 2 + wDistanceToTarget;
            Rect wAboveRect = new Rect(wAbovePos - wPreferredSize / 2, wPreferredSize);
            var wAboveOverflow = GetOverflow(wAboveRect, wBoundsRect);

            bool showAbove = wAboveOverflow.top <= 0;
            Rect wPreferredRect;
            if (showAbove)
            {
                wPreferredPosition = wAbovePos;
                wPreferredRect = wAboveRect;
            }
            else
            {
                var wBelowPos = wPreferredPosition;
                wBelowPos.y -= wPreferredSize.y / 2 + wTargetRect.height / 2 + wDistanceToTarget;
                Rect wBelowRect = new Rect(wBelowPos - wPreferredSize / 2, wPreferredSize);

                wPreferredPosition = wBelowPos;
                wPreferredRect = wBelowRect;
            }

            // #2 Move tooltip inside defined limits (could be screen, canvas size, ...)
            var wOverflow = GetOverflow(wPreferredRect, wBoundsRect);
            wPreferredPosition += new Vector2(wOverflow.left, wOverflow.bottom);
            wPreferredPosition -= new Vector2(wOverflow.right, wOverflow.top);

            content.sizeDelta = lPreferredSize;
            content.position = new Vector3(wPreferredPosition.x, wPreferredPosition.y, 0);

            // #3 Set arrow start and end
            var start = wTargetRect.center;
            if (showAbove)
                start.y += wTargetRect.height / 2 + wDistanceToTarget * 2;
            else
                start.y -= wTargetRect.height / 2 + wDistanceToTarget * 2;

            start = GetClosestPointOnRect(wPreferredRect, start); // Make sure arrow really starts inside tooltip rect
            var end = GetClosestPointOnRect(wTargetRect, start);
            end.x = start.x; // Make arrow go vertically

            arrow.SetPosition(0, start);
            arrow.SetPosition(1, end);
        }

        public virtual Task PlayHideAnimation()   => Task.CompletedTask;
        public virtual Task PlayShowAnimation() => Task.CompletedTask;

        public async void Hide(Action onComplete = null)
        {
            onHide?.Invoke();
            onHide = null;
            Target = null;


            await PlayHideAnimation();

            arrow.gameObject.SetActive(false);
            content.gameObject.SetActive(false);
            onComplete?.Invoke();
        }

        void ShowButton(Action buttonAction, string buttonText, Sprite buttonSprite)
        {
            button.gameObject.SetActive(true);
            button.onClick.RemoveAllListeners();

            this.buttonText.text = buttonText;
            if (buttonAction != null)
                button.onClick.AddListener(buttonAction.Invoke);
            if (buttonSprite != null)
                button.image.sprite = buttonSprite;
        }

        void HideButton()
        {
            button.gameObject.SetActive(false);
        }

        // Converts a distance from localSpace to worldSpace
        private float ToWorld(float distance)
        {
            return transform.TransformPoint(new Vector3(distance, 0, 0)).x;
        }

        // Converts a Rect from localSpace to worldSpace
        private Rect ToWorld(Rect lRect)
        {
            Vector3[] corners = new Vector3[4];
            corners[0] = transform.TransformPoint(new Vector3(lRect.x, lRect.y));
            corners[1] = transform.TransformPoint(new Vector3(lRect.x + lRect.width, lRect.y));
            corners[2] = transform.TransformPoint(new Vector3(lRect.x, lRect.y + lRect.height));
            corners[3] = transform.TransformPoint(new Vector3(lRect.x + lRect.width, lRect.y + lRect.height));

            // Find the minimum and maximum local space coordinates to construct the Rect
            float minX = Mathf.Min(corners[0].x, corners[1].x, corners[2].x, corners[3].x);
            float maxX = Mathf.Max(corners[0].x, corners[1].x, corners[2].x, corners[3].x);
            float minY = Mathf.Min(corners[0].y, corners[1].y, corners[2].y, corners[3].y);
            float maxY = Mathf.Max(corners[0].y, corners[1].y, corners[2].y, corners[3].y);

            // Calculate the width and height of the Rect
            float width = maxX - minX;
            float height = maxY - minY;

            // Create and return the Rect
            return new Rect(minX, minY, width, height);
        }

        private Vector2 CalculateLocalTargetSize()
        {
            Vector2 size = Vector2.zero;

            var textSize = textField.GetPreferredValues(
                textField.text,
                maxSize.x - layout.padding.left - layout.padding.right,
                maxSize.y - layout.padding.top - layout.padding.bottom
            );
            size.x = Mathf.Max(size.x, textSize.x);
            size.y += textSize.y;

            if (button.gameObject.activeSelf && button.enabled)
            {
                var buttonSize = GetLocalButtonSize();
                size.x = Mathf.Max(size.x, buttonSize.x);
                size.y += buttonSize.y;
            }

            size.x += layout.padding.left + layout.padding.right;
            size.y += layout.padding.top + layout.padding.bottom;

            size.x = Mathf.Min(size.x, maxSize.x);
            size.y = Mathf.Min(size.y, maxSize.y);

            return size;
        }

        private Vector2 GetLocalButtonSize()
        {
            var size = (button.transform as RectTransform).rect.size;

            var layoutElement = button.GetComponent<LayoutElement>();
            if (layoutElement != null)
            {
                if (layoutElement.preferredWidth > 0) size.x = Mathf.Max(size.x, layoutElement.preferredWidth);
                if (layoutElement.preferredHeight > 0) size.y = Mathf.Max(size.y, layoutElement.preferredHeight);
                if (layoutElement.minWidth > 0) size.x = Mathf.Max(size.x, layoutElement.minWidth);
                if (layoutElement.minHeight > 0) size.y = Mathf.Max(size.y, layoutElement.minHeight);
            }

            return size;
        }

        // Returns how far self is outside of the canvas towards all directions
        // - left:   How much our left edge is outside of the canvas
        // - right:  How much our right edge is outside of the canvas
        // - top:    How much our top edge is outside of the canvas
        // - bottom: How much our bottom edge is outside of the canvas
        private Overflow GetOverflow(Rect inner, Rect outer)
        {
            Overflow offset = new Overflow();

            if (inner.xMin < outer.xMin)
                offset.left = outer.xMin - inner.xMin;

            if (inner.xMax > outer.xMax)
                offset.right = inner.xMax - outer.xMax;

            if (inner.yMin < outer.yMin)
                offset.bottom = outer.yMin - inner.yMin;

            if (inner.yMax > outer.yMax)
                offset.top = inner.yMax - outer.yMax;

            return offset;
        }

        private Rect GetWorldRect(RectTransform rect)
        {
            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);

            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;

            foreach (Vector3 corner in corners)
            {
                if (corner.x < minX)
                    minX = corner.x;
                if (corner.x > maxX)
                    maxX = corner.x;
                if (corner.y < minY)
                    minY = corner.y;
                if (corner.y > maxY)
                    maxY = corner.y;
            }

            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }

        public static Rect ToWorldSpace(Rect localRect, Transform transform)
        {
            // Calculate world space corners of the local rect
            Vector3[] corners = new Vector3[4];
            corners[0] = transform.TransformPoint(new Vector3(localRect.xMin, localRect.yMin));
            corners[1] = transform.TransformPoint(new Vector3(localRect.xMax, localRect.yMin));
            corners[2] = transform.TransformPoint(new Vector3(localRect.xMin, localRect.yMax));
            corners[3] = transform.TransformPoint(new Vector3(localRect.xMax, localRect.yMax));

            // Find the min and max points in world space
            Vector3 minPoint = corners[0];
            Vector3 maxPoint = corners[0];
            for (int i = 1; i < 4; i++)
            {
                minPoint = Vector3.Min(minPoint, corners[i]);
                maxPoint = Vector3.Max(maxPoint, corners[i]);
            }

            // Calculate the size and position of the world space rect
            Vector2 size = new Vector2(maxPoint.x - minPoint.x, maxPoint.y - minPoint.y);
            Vector2 position = new Vector2(minPoint.x, minPoint.y);

            return new Rect(position, size);
        }

        public static Rect GetWorldRect2(RectTransform transform)
        {
            var min = transform.InverseTransformVector(transform.rect.min);
            var max = transform.InverseTransformVector(transform.rect.max);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private Vector2 GetClosestPointOnRect(Rect rect, Vector2 point)
        {
            float closestX = Mathf.Clamp(point.x, rect.xMin, rect.xMax);
            float closestY = Mathf.Clamp(point.y, rect.yMin, rect.yMax);

            return new Vector2(closestX, closestY);
        }

        private bool IsGlobalInput(out Vector2 clickScreenPos)
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current.IsPressed(0))
            {
                clickScreenPos = Input.mousePosition;
                return true;
            }

            foreach(var touch in UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches)
            {
                if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
                {
                    clickScreenPos = touch.finger.screenPosition;
                    return true;
                }
            }
#else
            if (Input.GetMouseButtonDown(0))
            {
                clickScreenPos = Input.mousePosition;
                return true;
            }

            for (int i = 0; i < Input.touchCount; i++)
            {
                var touch = Input.GetTouch(i);
                if (touch.phase == TouchPhase.Began)
                {
                    clickScreenPos = touch.position;
                    return true;
                }
            }
#endif

            clickScreenPos = Vector2.zero;
            return false;
        }

        public static List<RaycastResult> RaycastUiByScreenPosition(Vector2 pPosition)
        {
            var @event = new PointerEventData(EventSystem.current)
            {
                position = pPosition
            };

            List<RaycastResult> raycastResults = new List<RaycastResult>();
            EventSystem.current.RaycastAll(@event, raycastResults);

            // Sort results top first, background last
            return raycastResults
                      .OrderBy(r => -r.sortingLayer)
                      .ThenBy(r => -r.sortingOrder)
                      .ToList();
        }

        [Serializable]
        public class Overflow
        {
            public float left;
            public float right;
            public float top;
            public float bottom;

            public override string ToString()
            {
                return $"Overflow(left:{left}, right:{right}, top:{top}, bottom:{bottom})";
            }
        }

        public class Options
        {
            public Tooltip source;

            public RectTransform target;
            public string text = "";

            public bool showButton = false;
            public string buttonText = "";
            public Action buttonAction = null;
            public Sprite buttonSprite = null;

            public bool autoHideOnInput = true;
            public Action onHideAction = null;

            public RectTransform bounds = null;
            public Overflow boundsMargin = null;

            public Options(Tooltip source)
            {
                this.source = source;
            }

            public Options On(RectTransform target)
            {
                this.target = target;
                return this;
            }

            public Options WithText(string text)
            {
                this.text = text;
                return this;
            }

            public Options WithButton(string buttonText, Action buttonAction, Sprite buttonSprite = null)
            {
                showButton = true;
                this.buttonText = buttonText;
                this.buttonAction = buttonAction;
                this.buttonSprite = buttonSprite;
                return this;
            }

            public Options WithAutoHideOnInput(bool enabled = true)
            {
                this.autoHideOnInput = enabled;
                return this;
            }

            public Options WithBounds(RectTransform bounds, Overflow margin = null)
            {
                this.bounds = bounds;
                this.boundsMargin = margin;
                return this;
            }

            public Options OnHide(Action action)
            {
                this.onHideAction = action;
                return this;
            }

            public void Show()
            {
                source.Show(this);
            }
        }
    }

}
