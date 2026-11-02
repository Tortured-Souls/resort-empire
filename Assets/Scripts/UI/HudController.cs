using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ResortEmpire.UI
{
    /// <summary>
    /// HUD layer of the GameWorld scene. Exposes the empty slots that the individual HUD
    /// widgets (coins, gems, reputation, ...) are placed into. The corner containers use
    /// layout groups, so widgets only need a LayoutElement with their preferred size.
    /// </summary>
    public sealed class HudController : MonoBehaviour
    {
        [Header("Top left")]
        [SerializeField] private RectTransform reputationSlot;
        [SerializeField] private RectTransform resortValueSlot;

        [Header("Top right")]
        [SerializeField] private RectTransform coinsSlot;
        [SerializeField] private RectTransform gemsSlot;
        [SerializeField] private RectTransform displaySlot;

        [Header("Icons")]
        [SerializeField] private Sprite coinIcon;
        [SerializeField] private Sprite gemIcon;

        [SerializeField] private int coins;
        private TextMeshProUGUI coinCountText;
        private TextMeshProUGUI gemCountText;
        private GemWallet gemWallet;

        private void Awake()
        {
            if (coinsSlot != null)
            {
                coinCountText = CreateCounter(coinsSlot, "CoinCounter", "CoinIcon", coinIcon);
                SetCoins(coins);
            }

            if (gemsSlot != null)
            {
                gemCountText = CreateCounter(gemsSlot, "GemCounter", "GemIcon", gemIcon);
                gemWallet = GetComponent<GemWallet>();
                if (gemWallet == null)
                    gemWallet = gameObject.AddComponent<GemWallet>();

                gemWallet.BalanceChanged += UpdateGemCount;
                UpdateGemCount(gemWallet.Balance);
            }
        }

#if UNITY_EDITOR
        private const string CoinIconPath = "Assets/Art/UI/CoinIcon.png";
        private const string GemIconPath = "Assets/Art/UI/GemIcon.png";

        // Fills in missing icon references so the counters never fall back to a blank white Image.
        private void OnValidate()
        {
            if (coinIcon == null)
                coinIcon = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(CoinIconPath);
            if (gemIcon == null)
                gemIcon = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(GemIconPath);
        }
#endif

        private static TextMeshProUGUI CreateCounter(RectTransform slot, string counterName, string iconName, Sprite icon)
        {
            var counter = new GameObject(counterName, typeof(RectTransform), typeof(Image));
            var counterRect = (RectTransform)counter.transform;
            counterRect.SetParent(slot, false);
            counterRect.sizeDelta = new Vector2(180f, 56f);
            counter.GetComponent<Image>().color = new Color(0.07f, 0.12f, 0.16f, 0.92f);

            var iconObject = new GameObject(iconName, typeof(RectTransform), typeof(Image));
            var iconRect = (RectTransform)iconObject.transform;
            iconRect.SetParent(counterRect, false);
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.anchoredPosition = new Vector2(12f, 0f);
            iconRect.sizeDelta = new Vector2(36f, 36f);
            var iconImage = iconObject.GetComponent<Image>();
            iconImage.sprite = icon;
            iconImage.preserveAspect = true;

            var countObject = new GameObject(counterName.Replace("Counter", "Count"), typeof(RectTransform), typeof(TextMeshProUGUI));
            var countRect = (RectTransform)countObject.transform;
            countRect.SetParent(counterRect, false);
            countRect.anchorMin = Vector2.zero;
            countRect.anchorMax = Vector2.one;
            countRect.offsetMin = new Vector2(58f, 0f);
            countRect.offsetMax = new Vector2(-10f, 0f);
            var countText = countObject.GetComponent<TextMeshProUGUI>();
            countText.font = TMP_Settings.defaultFontAsset;
            countText.fontSize = 25f;
            countText.fontStyle = FontStyles.Bold;
            countText.color = new Color(1f, 0.96f, 0.83f);
            countText.alignment = TextAlignmentOptions.MidlineLeft;
            return countText;
        }

        private void OnDestroy()
        {
            if (gemWallet != null)
                gemWallet.BalanceChanged -= UpdateGemCount;
        }

        /// <summary>Updates the number displayed in the coin counter.</summary>
        public void SetCoins(int amount)
        {
            coins = Mathf.Max(0, amount);
            if (coinCountText != null)
                coinCountText.text = coins.ToString("N0");
        }

        /// <summary>Updates the gem counter when the wallet balance changes.</summary>
        private void UpdateGemCount(int amount)
        {
            if (gemCountText != null)
                gemCountText.text = Mathf.Max(0, amount).ToString("N0");
        }

        /// <summary>The wallet used by gameplay systems to award or spend gems.</summary>
        public GemWallet Wallet => gemWallet;

        /// <summary>Reputation widget slot (SCRUM-81).</summary>
        public RectTransform ReputationSlot => reputationSlot;

        /// <summary>Resort value widget slot (SCRUM-82).</summary>
        public RectTransform ResortValueSlot => resortValueSlot;

        /// <summary>Coin counter slot (SCRUM-65).</summary>
        public RectTransform CoinsSlot => coinsSlot;

        /// <summary>Gems widget slot (SCRUM-79).</summary>
        public RectTransform GemsSlot => gemsSlot;

        /// <summary>Top right display slot (SCRUM-66).</summary>
        public RectTransform DisplaySlot => displaySlot;

        /// <summary>Shows or hides the whole HUD, e.g. while a full-screen menu is open.</summary>
        public void SetVisible(bool visible) => gameObject.SetActive(visible);
    }
}
