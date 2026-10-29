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

        [SerializeField] private int coins;
        private TextMeshProUGUI coinCountText;
        private TextMeshProUGUI gemCountText;
        private GemWallet gemWallet;

        private void Awake()
        {
            if (coinsSlot != null)
            {
                coinCountText = CreateCounter(coinsSlot, "CoinCounter", "CoinIcon", CreateCoinSprite());
                SetCoins(coins);
            }

            if (gemsSlot != null)
            {
                gemCountText = CreateCounter(gemsSlot, "GemCounter", "GemIcon", CreateGemSprite());
                gemWallet = GetComponent<GemWallet>();
                if (gemWallet == null)
                    gemWallet = gameObject.AddComponent<GemWallet>();

                gemWallet.BalanceChanged += UpdateGemCount;
                UpdateGemCount(gemWallet.Balance);
            }
        }

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
            iconObject.GetComponent<Image>().sprite = icon;

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

        private static Sprite CreateCoinSprite()
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "GeneratedCoinIcon";
            texture.filterMode = FilterMode.Bilinear;
            var pixels = new Color32[size * size];
            var center = (size - 1) * 0.5f;

            for (var y = 0; y < size; y++)
                for (var x = 0; x < size; x++)
                {
                    var dx = x - center;
                    var dy = y - center;
                    var radius = Mathf.Sqrt(dx * dx + dy * dy);
                    Color32 color = new Color32(0, 0, 0, 0);
                    if (radius <= 29f)
                    {
                        color = radius >= 25f
                            ? new Color32(174, 104, 27, 255)
                            : radius >= 22f
                                ? new Color32(255, 218, 112, 255)
                                : new Color32(242, 173, 51, 255);

                        if (x < 25 && y > 35 && radius < 21f)
                            color = new Color32(255, 226, 133, 255);
                        if (Mathf.Abs(dx) < 2f && Mathf.Abs(dy) < 13f && radius < 19f)
                            color = new Color32(194, 119, 31, 255);
                    }

                    pixels[y * size + x] = color;
                }

            texture.SetPixels32(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private static Sprite CreateGemSprite()
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "GeneratedGemIcon";
            texture.filterMode = FilterMode.Bilinear;
            var pixels = new Color32[size * size];
            var outer = new[]
            {
                new Vector2(32f, 2f), new Vector2(59f, 24f), new Vector2(43f, 61f),
                new Vector2(21f, 61f), new Vector2(5f, 24f)
            };
            var inner = new[]
            {
                new Vector2(32f, 8f), new Vector2(51f, 25f), new Vector2(39f, 54f),
                new Vector2(25f, 54f), new Vector2(13f, 25f)
            };

            for (var y = 0; y < size; y++)
                for (var x = 0; x < size; x++)
                {
                    var point = new Vector2(x, y);
                    var color = new Color32(0, 0, 0, 0);
                    if (ContainsPoint(outer, point))
                    {
                        color = new Color32(13, 83, 126, 255);
                        if (ContainsPoint(inner, point))
                        {
                            if (y < 25f)
                                color = x < 32f
                                    ? new Color32(138, 248, 255, 255)
                                    : new Color32(54, 192, 238, 255);
                            else if (x < 27f)
                                color = new Color32(64, 211, 244, 255);
                            else if (x < 34f)
                                color = new Color32(162, 250, 255, 255);
                            else
                                color = new Color32(34, 157, 216, 255);
                        }
                    }

                    pixels[y * size + x] = color;
                }

            texture.SetPixels32(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private static bool ContainsPoint(Vector2[] polygon, Vector2 point)
        {
            var inside = false;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                var a = polygon[i];
                var b = polygon[j];
                if ((a.y > point.y) != (b.y > point.y)
                    && point.x < (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x)
                    inside = !inside;
            }

            return inside;
        }
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
