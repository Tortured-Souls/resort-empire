using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ResortEmpire.UI
{
    /// <summary>
    /// Bottom sheet from the shared UI kit (Prefabs/UI/Sheet): a title with a close button and a
    /// <c>Content</c> container. It slides up from the bottom edge on <see cref="Show"/> and back
    /// down on <see cref="Hide"/> or the close button. A concrete sheet puts its layout into
    /// <c>Content</c> and drives it from a component on the same object.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class UISheet : MonoBehaviour
    {
        [SerializeField] private TMP_Text title;
        [SerializeField] private Button closeButton;
        [SerializeField, Min(0.01f)] private float showSeconds = 0.3f;
        [SerializeField, Min(0.01f)] private float hideSeconds = 0.18f;

        private RectTransform _rect;
        private Vector2 _shownPosition;
        private Coroutine _routine;
        private bool _open;

        public TMP_Text Title => title;

        /// <summary>True from <see cref="Show"/> until <see cref="Hide"/>, including both slides.</summary>
        public bool IsOpen => _open;

        private void Awake()
        {
            _rect = (RectTransform)transform;
            _shownPosition = _rect.anchoredPosition;
            closeButton.onClick.AddListener(Hide);
        }

        private void OnDestroy()
        {
            closeButton.onClick.RemoveListener(Hide);
        }

        private void OnDisable()
        {
            // Rest where the sheet is shown, so the next Show starts from a known place.
            _routine = null;
            if (_rect != null) _rect.anchoredPosition = _shownPosition;
        }

        /// <summary>Slides the sheet in. Fill its content first, so it slides in at its final size.</summary>
        public void Show()
        {
            if (_open) return;

            _open = true;
            // A sheet still sliding out turns around where it is; a hidden one comes from below.
            bool sliding = gameObject.activeSelf;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_rect);
            Slide(sliding ? _rect.anchoredPosition : HiddenPosition, _shownPosition, showSeconds, EaseOutBack, hideAtEnd: false);
        }

        /// <summary>Slides the sheet out and deactivates it.</summary>
        public void Hide()
        {
            _open = false;
            if (!isActiveAndEnabled)
            {
                gameObject.SetActive(false);
                return;
            }

            Slide(_rect.anchoredPosition, HiddenPosition, hideSeconds, EaseInCubic, hideAtEnd: true);
        }

        // Just below the bottom edge; the sheet is anchored and pivoted at the bottom.
        private Vector2 HiddenPosition => new(_shownPosition.x, -_rect.rect.height);

        private void Slide(Vector2 from, Vector2 to, float seconds, Func<float, float> ease, bool hideAtEnd)
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(SlideRoutine(from, to, seconds, ease, hideAtEnd));
        }

        // Unscaled time so the sheet also works while the game is paused.
        private IEnumerator SlideRoutine(Vector2 from, Vector2 to, float seconds, Func<float, float> ease, bool hideAtEnd)
        {
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                _rect.anchoredPosition = Vector2.LerpUnclamped(from, to, ease(t / seconds));
                yield return null;
            }

            _rect.anchoredPosition = to;
            _routine = null;
            if (hideAtEnd) gameObject.SetActive(false);
        }

        private static float EaseOutBack(float t)
        {
            const float overshoot = 1.2f;
            float p = t - 1f;
            return 1f + (overshoot + 1f) * p * p * p + overshoot * p * p;
        }

        private static float EaseInCubic(float t) => t * t * t;
    }
}
