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
        [SerializeField] private Sprite counterBackground;

        [SerializeField] private int coins;
        [Tooltip("Wallet the coin counter shows. Found in the scene when left empty.")]
        [SerializeField] private CoinWallet coinWallet;
        private Counter coinCounter;
        private Counter gemCounter;
        private GemWallet gemWallet;

        // Counter layout, in canvas units. The icon overlaps the left end of the pill.
        private const float CounterHeight = 56f;
        private const float IconSize = 60f;
        private const float PillInset = 22f;
        private const float TextLeft = 64f;
        private const float TextRight = 20f;
        private const float FontSize = 28f;
        private const float MinFontSize = 14f;
        private const float MaxCounterWidth = 240f;
        // The counter is always at least wide enough for this amount and grows past it.
        private const int MinCapacity = 1000;

        private sealed class Counter
        {
            public TextMeshProUGUI Text;
            public LayoutElement Layout;
        }

        private void Awake()
        {
            if (coinsSlot != null)
            {
                coinCounter = CreateCounter(coinsSlot, "CoinCounter", "CoinIcon", coinIcon);
                if (coinWallet == null)
                    coinWallet = FindAnyObjectByType<CoinWallet>();

                if (coinWallet != null)
                {
                    coinWallet.BalanceChanged += UpdateCoinCount;
                    UpdateCoinCount(coinWallet.Balance);
                }
                else
                {
                    SetCount(coinCounter, coins);
                }
            }

            if (gemsSlot != null)
            {
                gemCounter = CreateCounter(gemsSlot, "GemCounter", "GemIcon", gemIcon);
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
        private const string CounterBackgroundPath = "Assets/Art/UI/CounterBackground.png";

        // Fills in missing icon references so the counters never fall back to a blank white Image.
        private void OnValidate()
        {
            if (coinIcon == null)
                coinIcon = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(CoinIconPath);
            if (gemIcon == null)
                gemIcon = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(GemIconPath);
            if (counterBackground == null)
                counterBackground = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(CounterBackgroundPath);
        }
#endif

        private Counter CreateCounter(RectTransform slot, string counterName, string iconName, Sprite icon)
        {
            var counter = new GameObject(counterName, typeof(RectTransform));
            var counterRect = (RectTransform)counter.transform;
            counterRect.SetParent(slot, false);
            Stretch(counterRect);

            var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            var backgroundRect = (RectTransform)background.transform;
            backgroundRect.SetParent(counterRect, false);
            Stretch(backgroundRect);
            backgroundRect.offsetMin = new Vector2(PillInset, 0f);
            var backgroundImage = background.GetComponent<Image>();
            backgroundImage.sprite = counterBackground;
            backgroundImage.type = Image.Type.Sliced;
            // The sprite is drawn at 2x so its rounded ends stay crisp at the 56 unit counter height.
            backgroundImage.pixelsPerUnitMultiplier = counterBackground != null
                ? counterBackground.rect.height / CounterHeight
                : 1f;

            var iconObject = new GameObject(iconName, typeof(RectTransform), typeof(Image));
            var iconRect = (RectTransform)iconObject.transform;
            iconRect.SetParent(counterRect, false);
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.anchoredPosition = Vector2.zero;
            iconRect.sizeDelta = new Vector2(IconSize, IconSize);
            var iconImage = iconObject.GetComponent<Image>();
            iconImage.sprite = icon;
            iconImage.preserveAspect = true;

            var countObject = new GameObject(counterName.Replace("Counter", "Count"), typeof(RectTransform), typeof(TextMeshProUGUI));
            var countRect = (RectTransform)countObject.transform;
            countRect.SetParent(counterRect, false);
            Stretch(countRect);
            countRect.offsetMin = new Vector2(TextLeft, 0f);
            countRect.offsetMax = new Vector2(-TextRight, 0f);
            var countText = countObject.GetComponent<TextMeshProUGUI>();
            countText.font = TMP_Settings.defaultFontAsset;
            countText.fontSize = FontSize;
            countText.fontSizeMin = MinFontSize;
            countText.fontSizeMax = FontSize;
            countText.fontStyle = FontStyles.Bold;
            countText.color = new Color(0.36f, 0.18f, 0.04f);
            countText.alignment = TextAlignmentOptions.Midline;
            countText.textWrappingMode = TextWrappingModes.NoWrap;

            var layout = slot.GetComponent<LayoutElement>();
            if (layout == null)
                layout = slot.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = CounterHeight;

            return new Counter { Text = countText, Layout = layout };
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// Shows the amount and resizes the counter: it fits at least "1,000", widens for larger
        /// numbers up to MaxCounterWidth, and past that shrinks the font instead.
        /// </summary>
        private static void SetCount(Counter counter, int amount)
        {
            if (counter == null)
                return;

            var text = counter.Text;
            text.text = Mathf.Max(0, amount).ToString("N0");

            // Measure at full size; auto sizing would otherwise report the shrunken width.
            text.enableAutoSizing = false;
            text.fontSize = FontSize;
            var textWidth = Mathf.Max(text.GetPreferredValues(text.text).x, text.GetPreferredValues(MinCapacity.ToString("N0")).x);
            var width = TextLeft + textWidth + TextRight;

            text.enableAutoSizing = width > MaxCounterWidth;
            counter.Layout.preferredWidth = Mathf.Min(width, MaxCounterWidth);
        }

        private void OnDestroy()
        {
            if (gemWallet != null)
                gemWallet.BalanceChanged -= UpdateGemCount;
            if (coinWallet != null)
                coinWallet.BalanceChanged -= UpdateCoinCount;
        }

        /// <summary>
        /// Sets the coin amount. With a <see cref="CoinWallet"/> this sets the wallet balance and
        /// the counter follows it; without one only the counter changes.
        /// </summary>
        public void SetCoins(int amount)
        {
            coins = Mathf.Max(0, amount);
            if (coinWallet != null)
                coinWallet.SetBalance(coins);
            else
                SetCount(coinCounter, coins);
        }

        /// <summary>Updates the coin counter when the wallet balance changes (SCRUM-145).</summary>
        private void UpdateCoinCount(int amount)
        {
            coins = amount;
            SetCount(coinCounter, amount);
        }

        /// <summary>The wallet the coin counter shows.</summary>
        public CoinWallet CoinWallet => coinWallet;

        /// <summary>Updates the gem counter when the wallet balance changes.</summary>
        private void UpdateGemCount(int amount)
        {
            SetCount(gemCounter, amount);
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
