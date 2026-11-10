using System;
using System.Collections.Generic;
using System.Text;
using ResortEmpire.World;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ResortEmpire.UI
{
    /// <summary>
    /// Full-screen report shown at the end of a wave. The wave manager calls
    /// <see cref="Show"/>; the canvas is loaded from <c>Resources/WaveReportPanel</c> on first
    /// use, and its content is built from the shared UI prefabs.
    /// </summary>
    public sealed class WaveReportPanel : MonoBehaviour
    {
        private const string ResourceName = "WaveReportPanel";
        private const string ClaimLabel = "Claim Rewards";
        private const string PerfectLabel = "PERFECT STAY";
        private const string WentWellHeader = "What went well";
        private const string NeedsImprovementHeader = "Needs improvement";
        private const string NothingWentWell = "Nothing this time";
        private const string NothingToImprove = "Nothing, great wave!";
        private const int ContentPadding = 40;
        private const float ContentSpacing = 14f;
        private const float StarSpacing = 10f;
        private const float BreakdownSpacing = 48f;

        private static readonly Color BackgroundColor = new(0.07f, 0.18f, 0.24f, 1f);
        private static readonly Color PerfectColor = new(1f, 0.78f, 0.34f, 1f);
        private static readonly Color EarnedStarColor = Color.white;
        private static readonly Color MissingStarColor = new(1f, 1f, 1f, 0.16f);

        private static WaveReportPanel _instance;

        [SerializeField] private Image panelPrefab;
        [SerializeField] private TMP_Text headingPrefab;
        [SerializeField] private TMP_Text textPrefab;
        [SerializeField] private Button buttonPrefab;
        [SerializeField] private Sprite starSprite;
        [SerializeField, Min(1f)] private float starSize = 64f;
        [SerializeField] private Vector2 claimButtonSize = new(280f, 64f);

        private readonly Image[] _stars = new Image[WaveRating.MaxStars];
        private TMP_Text _title;
        private TMP_Text _perfect;
        private TMP_Text _guests;
        private TMP_Text _satisfaction;
        private TMP_Text _rewards;
        private TMP_Text _wentWell;
        private TMP_Text _needsImprovement;
        private Action _claimed;

        /// <summary>
        /// Opens the report on top of the game. <paramref name="claimed"/> runs once, after the
        /// player presses Claim Rewards; pay out the rewards and start the next wave there.
        /// </summary>
        public static void Show(WaveReport report, Action claimed)
        {
            if (_instance == null) _instance = Instantiate(Resources.Load<WaveReportPanel>(ResourceName));
            _instance.Display(report, claimed);
        }

        private void Awake()
        {
            var panel = Instantiate(panelPrefab, transform);
            panel.sprite = null;
            panel.color = BackgroundColor;
            Stretch((RectTransform)panel.transform);

            var content = panel.GetComponent<VerticalLayoutGroup>();
            content.padding = new RectOffset(ContentPadding, ContentPadding, ContentPadding, ContentPadding);
            content.spacing = ContentSpacing;
            content.childAlignment = TextAnchor.MiddleCenter;
            content.childForceExpandWidth = false;

            _title = AddText(headingPrefab, content.transform);
            AddStars(content.transform);
            _perfect = AddText(headingPrefab, content.transform);
            _perfect.text = PerfectLabel;
            _perfect.color = PerfectColor;
            _guests = AddText(textPrefab, content.transform);
            _satisfaction = AddText(textPrefab, content.transform);
            _rewards = AddText(textPrefab, content.transform);
            AddBreakdown(content.transform);
            AddClaimButton(content.transform);
        }

        private void Display(WaveReport report, Action claimed)
        {
            _claimed = claimed;
            _title.text = $"Wave {report.Wave} Report";
            _perfect.gameObject.SetActive(report.IsPerfect);
            _guests.text = $"Guests served: <b>{report.GuestsServed}/{report.GuestsTotal}</b>";
            _satisfaction.text = $"Wave Satisfaction: <b>{report.SatisfactionPercent}%</b>";
            _rewards.text = $"+{report.CoinsEarned:N0} coins  •  +{report.EnergyEarned:N0} energy  •  +{report.ResortValueGained:N0} resort value";
            _wentWell.text = FormatBreakdown(WentWellHeader, report.WentWell, NothingWentWell);
            _needsImprovement.text = FormatBreakdown(NeedsImprovementHeader, report.NeedsImprovement, NothingToImprove);

            for (int i = 0; i < _stars.Length; i++)
            {
                _stars[i].color = i < report.Stars ? EarnedStarColor : MissingStarColor;
            }

            transform.SetAsLastSibling();
            gameObject.SetActive(true);
        }

        private void Claim()
        {
            var claimed = _claimed;
            _claimed = null;
            gameObject.SetActive(false);
            claimed?.Invoke();
        }

        private void AddStars(Transform parent)
        {
            var row = AddRow("Stars", parent);
            row.childControlWidth = false;
            row.childControlHeight = false;
            row.childForceExpandWidth = false;
            row.spacing = StarSpacing;

            for (int i = 0; i < _stars.Length; i++)
            {
                var star = new GameObject($"Star{i + 1}", typeof(RectTransform), typeof(Image));
                var rect = (RectTransform)star.transform;
                rect.SetParent(row.transform, false);
                rect.sizeDelta = new Vector2(starSize, starSize);
                _stars[i] = star.GetComponent<Image>();
                _stars[i].sprite = starSprite;
                _stars[i].raycastTarget = false;
            }
        }

        private void AddBreakdown(Transform parent)
        {
            var row = AddRow("Breakdown", parent);
            row.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = true;
            row.childAlignment = TextAnchor.UpperCenter;
            row.spacing = BreakdownSpacing;

            _wentWell = AddBreakdownColumn(row.transform);
            _needsImprovement = AddBreakdownColumn(row.transform);
        }

        private TMP_Text AddBreakdownColumn(Transform parent)
        {
            var column = AddText(textPrefab, parent);
            var element = column.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = 0f;
            element.flexibleWidth = 1f;
            return column;
        }

        private void AddClaimButton(Transform parent)
        {
            var button = Instantiate(buttonPrefab, parent);
            button.GetComponentInChildren<TMP_Text>().text = ClaimLabel;
            button.onClick.AddListener(Claim);

            var element = button.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = claimButtonSize.x;
            element.preferredHeight = claimButtonSize.y;
        }

        private static HorizontalLayoutGroup AddRow(string rowName, Transform parent)
        {
            var row = new GameObject(rowName, typeof(RectTransform), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(parent, false);

            var group = row.GetComponent<HorizontalLayoutGroup>();
            group.childAlignment = TextAnchor.MiddleCenter;
            group.childForceExpandHeight = false;
            return group;
        }

        private static TMP_Text AddText(TMP_Text prefab, Transform parent)
        {
            var label = Instantiate(prefab, parent);
            label.alignment = TextAlignmentOptions.Top;
            label.raycastTarget = false;
            return label;
        }

        private static string FormatBreakdown(string header, IReadOnlyList<NeedResult> results, string emptyText)
        {
            var text = new StringBuilder($"<b>{header}</b>");
            if (results.Count == 0) text.Append('\n').Append(emptyText);

            foreach (var result in results)
            {
                text.Append('\n').Append(result.NeedName).Append(": ").Append(result.Percent).Append('%');
            }

            return text.ToString();
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
