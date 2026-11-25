using System;
using System.Collections.Generic;
using ResortEmpire.World;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ResortEmpire.UI
{
    /// <summary>
    /// Worker Allocation (S3), a <see cref="UISheet"/> for the building the player tapped: its art,
    /// guests and seats, workers, an upgrade button and a <see cref="UICard"/> for every other
    /// building. Each building has one worker slot and exactly one building is without a worker,
    /// so a move always goes from a staffed building to the empty one; the player picks which
    /// building to leave empty. One tap moves, without confirmation, and raises
    /// <see cref="MoveClicked"/>. The upgrade button shows the price of the next level and
    /// raises <see cref="UpgradeClicked"/> (FPP-21).
    /// </summary>
    [RequireComponent(typeof(UISheet))]
    public sealed class WorkerAllocationPanel : MonoBehaviour
    {
        /// <summary>What the upgrade button offers for a building right now.</summary>
        public enum UpgradeOffer
        {
            /// <summary>No button, e.g. during a wave.</summary>
            Hidden,

            /// <summary>The player can pay for the next level.</summary>
            Available,

            /// <summary>The next level costs more coins than the player has; the button is disabled.</summary>
            NotAffordable,

            /// <summary>The building is at <see cref="Building.MaxLevel"/>; the button is disabled.</summary>
            MaxLevel
        }

        [SerializeField] private UITag status;
        [SerializeField] private Image buildingArt;
        [SerializeField] private UIPill seats;
        [SerializeField] private UIPill workers;
        [SerializeField] private TMP_Text section;
        [SerializeField] private RectTransform cards;
        [SerializeField] private UICard cardPrefab;
        [SerializeField] private NeedIcons needIcons;
        [SerializeField] private Sprite workerAvatar;
        [Tooltip("Button prefab for the upgrade, placed under the stats of the building.")]
        [SerializeField] private UIButton upgradeButtonPrefab;
        [SerializeField, Min(0f)] private float upgradeButtonHeight = 56f;

        private readonly List<UICard> _cards = new();
        private UISheet _sheet;
        private IReadOnlyList<Building> _buildings;
        private Building _target;
        private Button _upgradeButton;
        private TMP_Text _upgradeLabel;
        private string _shownSeats;
        private UpgradeOffer _shownOffer;
        private int _shownLevel;

        /// <summary>Raised with the building that loses its worker and the one that gets it.</summary>
        public event Action<Building, Building> MoveClicked;

        /// <summary>Raised with the shown building when the player taps an enabled upgrade button.</summary>
        public event Action<Building> UpgradeClicked;

        /// <summary>
        /// Decides the upgrade button of a building; set by the owner of the coins and the wave
        /// state. Without it the button stays hidden.
        /// </summary>
        public Func<Building, UpgradeOffer> UpgradeOfferFor { get; set; }

        public bool IsOpen => Sheet.IsOpen;

        private UISheet Sheet => _sheet != null ? _sheet : _sheet = GetComponent<UISheet>();

        /// <summary>Opens the sheet for <paramref name="target"/>, offering moves between it and <paramref name="buildings"/>.</summary>
        public void Show(Building target, IReadOnlyList<Building> buildings)
        {
            if (target == null || buildings == null) return;

            _target = target;
            _buildings = buildings;
            Fill();
            Sheet.Show();
        }

        /// <summary>Updates an open sheet, e.g. after a building was added.</summary>
        public void Refresh()
        {
            if (IsOpen) Fill();
        }

        public void Hide() => Sheet.Hide();

        // Guests come and go and coins change while the sheet is open, so both are polled.
        private void Update()
        {
            if (_target == null || !IsOpen) return;
            ShowSeats();
            ShowUpgrade();
        }

        private void Fill()
        {
            bool staffed = _target.HasWorker;
            Sheet.Title.text = _target.DisplayName;
            status.Set(staffed ? "Worker" : "No worker", staffed ? UIPalette.Positive : UIPalette.Warning);
            var art = _target.TryGetComponent(out SpriteRenderer body) ? body.sprite : null;
            buildingArt.sprite = art;
            buildingArt.gameObject.SetActive(art != null);
            _shownSeats = null;
            ShowSeats();
            _shownLevel = 0;
            ShowUpgrade();
            workers.SetValue($"{(staffed ? 1 : 0)}/{Building.WorkerSlots}");
            section.text = staffed ? "Send the worker to" : "Take a worker from";

            int used = 0;
            foreach (var building in _buildings)
            {
                if (building != _target) FillCard(NextCard(used++), building, staffed);
            }

            for (int i = used; i < _cards.Count; i++) _cards[i].gameObject.SetActive(false);
        }

        // A worker only moves from a staffed building to an empty one: swapping two workers would
        // change nothing, so those cards show where the workers are but offer no action.
        private void FillCard(UICard card, Building building, bool targetStaffed)
        {
            var target = _target;
            if (building.HasWorker)
            {
                card.SetAvatar(workerAvatar);
                card.SetBadge(needIcons.For(building.ServedNeed));
                card.SetTitle("Worker");
                card.SetSubtitle(building.DisplayName);
                card.SetTag(targetStaffed ? "Busy" : "Available", targetStaffed ? UIPalette.Neutral : UIPalette.Positive);
                card.SetAction("Select", targetStaffed ? null : () => MoveClicked?.Invoke(building, target));
                card.SetDimmed(targetStaffed);
                return;
            }

            card.SetAvatar(needIcons.For(building.ServedNeed));
            card.SetBadge(null);
            card.SetTitle(building.DisplayName);
            card.SetSubtitle("Serves slowly");
            card.SetTag("No worker", UIPalette.Warning);
            card.SetAction("Send", targetStaffed ? () => MoveClicked?.Invoke(target, building) : null);
            card.SetDimmed(!targetStaffed);
        }

        // Guests inside the building out of its seats, e.g. "3/4" (SCRUM-190).
        private void ShowSeats()
        {
            string text = SeatsText(_target.VisitorCount, _target.Capacity);
            if (text == _shownSeats) return;
            _shownSeats = text;
            seats.SetValue(text);
        }

        /// <summary>Text of the seats pill: guests inside out of the capacity.</summary>
        public static string SeatsText(int guests, int capacity) => $"{Mathf.Max(0, guests)}/{capacity}";

        private void ShowUpgrade()
        {
            var offer = UpgradeOfferFor != null ? UpgradeOfferFor(_target) : UpgradeOffer.Hidden;
            if (offer == _shownOffer && _target.Level == _shownLevel && _upgradeButton != null) return;
            _shownOffer = offer;
            _shownLevel = _target.Level;

            if (offer == UpgradeOffer.Hidden)
            {
                if (_upgradeButton != null) _upgradeButton.gameObject.SetActive(false);
                return;
            }

            if (!EnsureUpgradeButton()) return;
            _upgradeButton.gameObject.SetActive(true);
            _upgradeButton.interactable = offer == UpgradeOffer.Available;
            _upgradeLabel.text = UpgradeText(offer, _target.Level, _target.UpgradePrice);
        }

        /// <summary>Label of the upgrade button for <paramref name="offer"/> at <paramref name="level"/>.</summary>
        public static string UpgradeText(UpgradeOffer offer, int level, int price) => offer switch
        {
            UpgradeOffer.MaxLevel => $"Max level {level}",
            UpgradeOffer.Hidden => string.Empty,
            _ => $"Level {level + 1}: {price:N0} coins"
        };

        // The button sits under the stats of the building, in the left column of the sheet.
        private bool EnsureUpgradeButton()
        {
            if (_upgradeButton != null) return true;
            if (upgradeButtonPrefab == null || seats == null) return false;

            var column = seats.transform.parent != null ? seats.transform.parent.parent : null;
            if (column == null) return false;

            var instance = Instantiate(upgradeButtonPrefab, column);
            instance.name = "UpgradeButton";
            if (!instance.TryGetComponent(out LayoutElement layout)) layout = instance.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = upgradeButtonHeight;
            layout.preferredHeight = upgradeButtonHeight;
            // The prefab may not have run Awake yet when the sheet is still inactive.
            _upgradeButton = instance.GetComponent<Button>();
            _upgradeLabel = instance.GetComponentInChildren<TMP_Text>(true);
            if (_upgradeLabel != null)
            {
                // The column is narrower than a standalone button, so long prices shrink to fit.
                _upgradeLabel.enableAutoSizing = true;
                _upgradeLabel.fontSizeMax = _upgradeLabel.fontSize;
                _upgradeLabel.fontSizeMin = Mathf.Min(14f, _upgradeLabel.fontSize);
            }

            _upgradeButton.onClick.AddListener(OnUpgrade);
            return _upgradeLabel != null;
        }

        private void OnUpgrade()
        {
            if (_target != null && _shownOffer == UpgradeOffer.Available) UpgradeClicked?.Invoke(_target);
        }

        private UICard NextCard(int index)
        {
            while (_cards.Count <= index) _cards.Add(Instantiate(cardPrefab, cards));
            var card = _cards[index];
            card.gameObject.SetActive(true);
            return card;
        }
    }
}
