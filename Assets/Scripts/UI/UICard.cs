using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ResortEmpire.UI
{
    /// <summary>
    /// Card from the shared UI kit (Prefabs/UI/Card): round avatar with a corner badge, title,
    /// subtitle, a status <see cref="UITag"/> and one action button. Parts without content hide
    /// themselves, so the same card fits staff, upgrades or shop items.
    /// </summary>
    public sealed class UICard : MonoBehaviour
    {
        [SerializeField] private Image avatar;
        [SerializeField] private Image badge;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text subtitle;
        [SerializeField] private UITag statusTag;
        [SerializeField] private Button action;
        [SerializeField] private TMP_Text actionLabel;
        [SerializeField] private CanvasGroup group;
        [SerializeField, Range(0f, 1f)] private float dimmedAlpha = 0.55f;

        private Action _onAction;

        private void Awake()
        {
            action.onClick.AddListener(InvokeAction);
        }

        private void OnDestroy()
        {
            action.onClick.RemoveListener(InvokeAction);
        }

        public void SetAvatar(Sprite sprite) => avatar.sprite = sprite;

        /// <summary>Small icon on the avatar's corner; null hides it.</summary>
        public void SetBadge(Sprite icon)
        {
            badge.sprite = icon;
            badge.gameObject.SetActive(icon != null);
        }

        public void SetTitle(string text) => title.text = text;

        /// <summary>Line under the title; null or empty hides it.</summary>
        public void SetSubtitle(string text)
        {
            subtitle.text = text;
            subtitle.gameObject.SetActive(!string.IsNullOrEmpty(text));
        }

        /// <summary>Status tag in the corner; null or empty hides it.</summary>
        public void SetTag(string text, Color color)
        {
            statusTag.gameObject.SetActive(!string.IsNullOrEmpty(text));
            statusTag.Set(text, color);
        }

        /// <summary>Action button; a null <paramref name="onClick"/> hides it.</summary>
        public void SetAction(string label, Action onClick)
        {
            _onAction = onClick;
            actionLabel.text = label;
            action.gameObject.SetActive(onClick != null);
        }

        /// <summary>Fades the card, e.g. for options that are not available right now.</summary>
        public void SetDimmed(bool dimmed) => group.alpha = dimmed ? dimmedAlpha : 1f;

        private void InvokeAction() => _onAction?.Invoke();
    }
}
