using System.Collections.Generic;
using ResortEmpire.World;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Offer = ResortEmpire.World.BoostSystem.Offer;

namespace ResortEmpire.UI
{
    /// <summary>
    /// Speed Up buttons for the Energy boost (SCRUM-168). During a wave every building shows a
    /// button below it: "Speed Up: 3 Energy" boosts it, a running boost counts down its seconds,
    /// and without enough Energy the button offers an ad instead. A building that had its boost
    /// this wave shows no button. Lives on a screen space overlay canvas and follows the buildings.
    /// </summary>
    public sealed class BoostButtons : MonoBehaviour
    {
        [SerializeField] private BoostSystem boostSystem;
        [SerializeField] private BuildingSystem buildingSystem;
        [SerializeField] private UIButton buttonPrefab;
        [SerializeField] private Vector2 buttonSize = new(190f, 44f);
        [Tooltip("Offset from the bottom center of the building sprite, in world units. The button hangs below this point.")]
        [SerializeField] private Vector2 worldOffset = new(0f, 0.05f);

        private sealed class Entry
        {
            public RectTransform Rect;
            public Button Button;
            public TMP_Text Label;
            public Offer Shown = Offer.Hidden;
            public int ShownSeconds = -1;
        }

        private readonly Dictionary<Building, Entry> _entries = new();
        private RectTransform _layer;
        private Camera _camera;

        private void LateUpdate()
        {
            if (boostSystem == null || buildingSystem == null || buttonPrefab == null) return;
            if (_camera == null) _camera = Camera.main;
            if (_camera == null) return;

            foreach (var building in buildingSystem.Buildings)
            {
                if (building != null) UpdateEntry(building);
            }
        }

        /// <summary>Button label for <paramref name="offer"/>; <paramref name="seconds"/> is the time left of a running boost.</summary>
        // Words instead of the ⚡ emoji: the emoji fades out on the orange button.
        public static string Label(Offer offer, int cost, int seconds) => offer switch
        {
            Offer.Ready => $"Speed Up: {cost} Energy",
            Offer.Boosted => $"Boosted: {Mathf.Max(0, seconds)} s",
            Offer.NeedEnergy => "Ad for Energy",
            _ => string.Empty
        };

        /// <summary>True when a building with <paramref name="offer"/> shows a button.</summary>
        public static bool IsShown(Offer offer) => offer is Offer.Ready or Offer.Boosted or Offer.NeedEnergy;

        private void UpdateEntry(Building building)
        {
            var offer = boostSystem.OfferFor(building);
            _entries.TryGetValue(building, out var entry);
            if (!IsShown(offer))
            {
                if (entry != null) entry.Rect.gameObject.SetActive(false);
                return;
            }

            entry ??= Create(building);
            entry.Rect.gameObject.SetActive(true);
            entry.Rect.position = _camera.WorldToScreenPoint(Anchor(building));

            int seconds = offer == Offer.Boosted ? Mathf.CeilToInt(building.BoostTimeLeft) : 0;
            if (offer == entry.Shown && seconds == entry.ShownSeconds) return;
            entry.Shown = offer;
            entry.ShownSeconds = seconds;
            entry.Label.text = Label(offer, boostSystem.EnergyCost, seconds);
            entry.Button.interactable = offer != Offer.Boosted;
        }

        // Bottom center of the building art, so the button does not cover the warning badge on the roof.
        private Vector3 Anchor(Building building)
        {
            var origin = building.transform.position;
            if (building.TryGetComponent(out SpriteRenderer body) && body.sprite != null)
                origin = new Vector3(body.bounds.center.x, body.bounds.min.y, origin.z);
            return origin + (Vector3)worldOffset;
        }

        private Entry Create(Building building)
        {
            if (_layer == null)
            {
                // One container behind the HUD counters, so they stay on top.
                var layer = new GameObject("Boost Buttons", typeof(RectTransform));
                _layer = (RectTransform)layer.transform;
                _layer.SetParent(transform, false);
                _layer.SetAsFirstSibling();
                _layer.anchorMin = Vector2.zero;
                _layer.anchorMax = Vector2.one;
                _layer.offsetMin = Vector2.zero;
                _layer.offsetMax = Vector2.zero;
            }

            var button = Instantiate(buttonPrefab, _layer);
            button.name = $"Boost {building.DisplayName}";
            var rect = (RectTransform)button.transform;
            // Hang below the building so the entrance and the queue stay visible.
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = buttonSize;

            var label = button.GetComponentInChildren<TMP_Text>(true);
            label.enableAutoSizing = true;
            label.fontSizeMax = label.fontSize;
            label.fontSizeMin = Mathf.Min(14f, label.fontSize);

            var entry = new Entry { Rect = rect, Button = button.GetComponent<Button>(), Label = label };
            entry.Button.onClick.AddListener(() => OnClicked(building));
            _entries[building] = entry;
            return entry;
        }

        private void OnClicked(Building building)
        {
            switch (boostSystem.OfferFor(building))
            {
                case Offer.Ready:
                    boostSystem.TryBoost(building);
                    break;
                case Offer.NeedEnergy:
                    boostSystem.WatchAdForEnergy();
                    break;
            }
        }
    }
}
