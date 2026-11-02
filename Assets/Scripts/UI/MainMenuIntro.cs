using System.Collections;
using UnityEngine;

namespace ResortEmpire.UI
{
    /// <summary>
    /// Main menu entrance: the logo drops in with a small overshoot and then floats gently,
    /// while the menu items rise and fade in one after another. Items ignore clicks until
    /// they are in place. Expects items positioned freely (not driven by a layout group).
    /// </summary>
    public sealed class MainMenuIntro : MonoBehaviour
    {
        [SerializeField] private RectTransform logo;
        [SerializeField] private RectTransform[] items;
        [SerializeField, Min(0f)] private float logoDropHeight = 140f;
        [SerializeField, Min(0.01f)] private float logoSeconds = 0.7f;
        [SerializeField, Min(0f)] private float itemRise = 40f;
        [SerializeField, Min(0.01f)] private float itemSeconds = 0.35f;
        [SerializeField, Min(0f)] private float itemStagger = 0.1f;
        [SerializeField, Min(0f)] private float floatAmplitude = 6f;
        [SerializeField, Min(0.01f)] private float floatPeriod = 3.5f;

        private Vector2 _logoRest;
        private float _floatStartTime = float.PositiveInfinity;

        private void Start()
        {
            _logoRest = logo.anchoredPosition;
            StartCoroutine(DropLogo(GetOrAddGroup(logo)));
            for (int i = 0; i < items.Length; i++)
            {
                StartCoroutine(RiseItem(items[i], GetOrAddGroup(items[i]), logoSeconds * 0.5f + i * itemStagger));
            }
        }

        private void Update()
        {
            float t = Time.unscaledTime - _floatStartTime;
            if (t < 0f) return;
            logo.anchoredPosition = _logoRest + Vector2.up * (Mathf.Sin(t * 2f * Mathf.PI / floatPeriod) * floatAmplitude);
        }

        // Unscaled time throughout so the intro also plays if the game was left paused.
        private IEnumerator DropLogo(CanvasGroup group)
        {
            for (float t = 0f; t < logoSeconds; t += Time.unscaledDeltaTime)
            {
                float x = t / logoSeconds;
                logo.anchoredPosition = _logoRest + Vector2.up * (logoDropHeight * (1f - EaseOutBack(x)));
                group.alpha = Mathf.Clamp01(x * 2f);
                yield return null;
            }

            logo.anchoredPosition = _logoRest;
            group.alpha = 1f;
            _floatStartTime = Time.unscaledTime;
        }

        private IEnumerator RiseItem(RectTransform item, CanvasGroup group, float delay)
        {
            Vector2 rest = item.anchoredPosition;
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            item.anchoredPosition = rest + Vector2.down * itemRise;

            yield return new WaitForSecondsRealtime(delay);
            for (float t = 0f; t < itemSeconds; t += Time.unscaledDeltaTime)
            {
                float eased = 1f - Mathf.Pow(1f - t / itemSeconds, 3f);
                item.anchoredPosition = rest + Vector2.down * (itemRise * (1f - eased));
                group.alpha = eased;
                yield return null;
            }

            item.anchoredPosition = rest;
            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;
        }

        private static CanvasGroup GetOrAddGroup(Component target)
        {
            return target.TryGetComponent(out CanvasGroup group) ? group : target.gameObject.AddComponent<CanvasGroup>();
        }

        private static float EaseOutBack(float x)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
        }
    }
}
