using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ResortEmpire.UI
{
    /// <summary>
    /// Toast notification that slides down from the top edge of its canvas, stays for
    /// <see cref="displaySeconds"/>, then slides back up and deactivates itself.
    /// Expects a top-anchored RectTransform with its pivot on the top edge.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class NotificationToast : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text messageLabel;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button moveWorkerButton;
        [SerializeField, Min(0f)] private float displaySeconds = 5f;
        [SerializeField, Min(0.01f)] private float slideSeconds = 0.25f;

        private readonly struct Notice
        {
            public readonly string Title, Message;
            public readonly Func<bool> StillRelevant, CanMove;
            public readonly Action Move;

            public Notice(string title, string message, Func<bool> stillRelevant = null,
                Func<bool> canMove = null, Action move = null)
            {
                Title = title;
                Message = message;
                StillRelevant = stillRelevant;
                CanMove = canMove;
                Move = move;
            }
        }

        private readonly Queue<Notice> _waitingWarnings = new();
        private Notice _current;
        private Notice? _deferredMessage;
        private RectTransform _rect;
        private CanvasGroup _group;
        private Vector2 _shownPosition;
        private float _visibility; // 0 = hidden above the top edge, 1 = fully shown.
        private Coroutine _routine;

        private void Awake()
        {
            _rect = (RectTransform)transform;
            _group = GetComponent<CanvasGroup>();
            _shownPosition = _rect.anchoredPosition;
            closeButton.onClick.AddListener(Dismiss);
            moveWorkerButton.onClick.AddListener(MoveWorker);
            ApplyVisibility();
        }

        private void OnDisable()
        {
            _current = default;
            _waitingWarnings.Clear();
            _deferredMessage = null;
            _routine = null;
            _visibility = 0f;
            ApplyVisibility();
        }

        /// <summary>Replaces an ordinary toast, or saves the latest message until all warnings finish.</summary>
        public void Show(string title, string message)
        {
            var notice = new Notice(title, message);
            if (_current.StillRelevant != null)
                _deferredMessage = notice;
            else
                Present(notice);
        }

        internal void ShowQueueWarning(string title, string message, Func<bool> stillRelevant,
            Func<bool> canMove, Action move)
        {
            var notice = new Notice(title, message, stillRelevant, canMove, move);
            if (_current.StillRelevant != null)
                _waitingWarnings.Enqueue(notice);
            else
                Present(notice);
        }

        private void Present(Notice notice)
        {
            _current = notice;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            titleLabel.text = notice.Title;
            messageLabel.text = notice.Message;
            moveWorkerButton.interactable = notice.CanMove != null && notice.CanMove();
            moveWorkerButton.gameObject.SetActive(notice.Move != null);
            LayoutRebuilder.ForceRebuildLayoutImmediate(_rect);

            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(ShowThenHide());
        }

        private void Update()
        {
            if (_current.StillRelevant == null) return;
            if (!_current.StillRelevant())
                Dismiss();
            else
                moveWorkerButton.interactable = _current.CanMove();
        }

        private void MoveWorker()
        {
            if (_current.Move == null) return;
            if (!_current.StillRelevant())
            {
                Dismiss();
                return;
            }
            if (!_current.CanMove())
            {
                moveWorkerButton.interactable = false;
                return;
            }

            Action move = _current.Move;
            Dismiss(); // The move itself may publish another toast.
            move();
        }

        /// <summary>Advances to the next live notice, or slides the toast out if none remain.</summary>
        public void Dismiss()
        {
            if (!isActiveAndEnabled) return;
            _current = default;
            while (_waitingWarnings.Count > 0)
            {
                Notice next = _waitingWarnings.Dequeue();
                if (!next.StillRelevant()) continue;
                Present(next);
                return;
            }
            if (_deferredMessage is Notice message)
            {
                _deferredMessage = null;
                Present(message);
                return;
            }

            moveWorkerButton.interactable = false;

            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(Hide());
        }

        private IEnumerator ShowThenHide()
        {
            yield return AnimateTo(1f);
            yield return new WaitForSecondsRealtime(displaySeconds);
            _routine = null;
            Dismiss();
        }

        private IEnumerator Hide()
        {
            yield return AnimateTo(0f);
            gameObject.SetActive(false);
        }

        private IEnumerator AnimateTo(float target)
        {
            while (_visibility != target)
            {
                yield return null; // Step on frame time, not on the delta of the frame that called Show.
                _visibility = Mathf.MoveTowards(_visibility, target, Time.unscaledDeltaTime / slideSeconds);
                ApplyVisibility();
            }
        }

        private void ApplyVisibility()
        {
            float eased = 1f - Mathf.Pow(1f - _visibility, 3f); // Ease-out in, ease-in out.
            float hiddenY = _rect.rect.height; // Pivot on the top edge: bottom edge rests on the canvas top.
            _rect.anchoredPosition = new Vector2(_shownPosition.x, Mathf.LerpUnclamped(hiddenY, _shownPosition.y, eased));
            _group.alpha = eased;
        }
    }
}
