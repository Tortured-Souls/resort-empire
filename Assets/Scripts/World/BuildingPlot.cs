using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ResortEmpire.World
{
    /// <summary>State of a <see cref="BuildingPlot"/> from the player's point of view.</summary>
    public enum PlotState
    {
        /// <summary>The unlock wave has not been completed yet.</summary>
        Locked,

        /// <summary>Unlocked by waves and the player has enough coins.</summary>
        Available,

        /// <summary>Unlocked by waves but the player does not have enough coins.</summary>
        NotAffordable,

        /// <summary>Bought, the building stands on the spot.</summary>
        Unlocked
    }

    /// <summary>
    /// Purchasable plot for one fixed building (Pool, Bungalows, ...). Sits next to a
    /// <see cref="BuildingSpot"/>, becomes available after <see cref="UnlockAfterWave"/> and is
    /// bought for <see cref="Price"/> coins. A tap on the plot raises <see cref="Clicked"/>;
    /// <see cref="BuildingSystem"/> decides whether the purchase panel opens.
    /// </summary>
    [RequireComponent(typeof(BuildingSpot), typeof(Collider2D))]
    public sealed class BuildingPlot : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private string displayName = "Building";
        [SerializeField] private Building buildingPrefab;
        [SerializeField, Min(0)] private int unlockAfterWave = 1;
        [SerializeField, Min(0)] private int price = 1000;

        [Header("Visuals")]
        [SerializeField] private SpriteRenderer ground;
        [SerializeField] private SpriteRenderer signBoard;
        [SerializeField] private TMP_Text sign;
        [SerializeField] private Transform marker;
        [SerializeField] private Color lockedTint = new(0.55f, 0.55f, 0.55f, 1f);
        [SerializeField] private Color notAffordableTint = new(1f, 0.7f, 0.7f, 1f);
        [SerializeField, Min(0f)] private float markerBobHeight = 0.12f;
        [SerializeField, Min(0f)] private float markerBobSpeed = 3f;

        private BuildingSpot _spot;
        private Vector3 _markerBase;
        private PlotState _state = PlotState.Locked;

        /// <summary>Raised when the state changes.</summary>
        public event Action<BuildingPlot> StateChanged;

        /// <summary>Raised when the player taps the plot.</summary>
        public event Action<BuildingPlot> Clicked;

        /// <summary>Name of the building sold on this plot, e.g. "Pool".</summary>
        public string DisplayName => displayName;

        /// <summary>Building placed on the spot after the purchase.</summary>
        public Building BuildingPrefab => buildingPrefab;

        /// <summary>Wave that has to be completed before the plot can be bought.</summary>
        public int UnlockAfterWave => unlockAfterWave;

        /// <summary>Price in coins.</summary>
        public int Price => price;

        /// <summary>Current state.</summary>
        public PlotState State => _state;

        /// <summary>True in <see cref="PlotState.Available"/> and <see cref="PlotState.NotAffordable"/>.</summary>
        public bool IsForSale => _state is PlotState.Available or PlotState.NotAffordable;

        /// <summary>The spot the building is placed on.</summary>
        public BuildingSpot Spot => _spot != null ? _spot : _spot = GetComponent<BuildingSpot>();

        private void Awake()
        {
            if (marker != null) _markerBase = marker.localPosition;
            ApplyVisuals();
        }

        private void Update()
        {
            if (marker == null || !marker.gameObject.activeSelf) return;
            float offset = Mathf.Abs(Mathf.Sin(Time.time * markerBobSpeed)) * markerBobHeight;
            marker.localPosition = _markerBase + Vector3.up * offset;
        }

        /// <summary>Recomputes the state from progress and coins. Bought plots stay unlocked.</summary>
        public void Refresh(int completedWaves, int coins)
        {
            if (_state == PlotState.Unlocked) return;

            PlotState next = completedWaves < unlockAfterWave
                ? PlotState.Locked
                : coins >= price ? PlotState.Available : PlotState.NotAffordable;
            SetState(next);
        }

        /// <summary>Places the building and marks the plot as bought. Paying is up to the caller.</summary>
        public bool TryBuild(out Building building)
        {
            building = null;
            if (_state == PlotState.Unlocked || buildingPrefab == null) return false;
            if (!Spot.TryPlace(buildingPrefab.gameObject, out var placed)) return false;

            building = placed.GetComponent<Building>();
            SetState(PlotState.Unlocked);
            return true;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left) Clicked?.Invoke(this);
        }

        private void SetState(PlotState next)
        {
            if (next == _state) return;

            _state = next;
            ApplyVisuals();
            StateChanged?.Invoke(this);
        }

        private void ApplyVisuals()
        {
            bool unlocked = _state == PlotState.Unlocked;
            Color tint = _state switch
            {
                PlotState.Locked => lockedTint,
                PlotState.NotAffordable => notAffordableTint,
                _ => Color.white
            };

            if (ground != null)
            {
                ground.gameObject.SetActive(!unlocked);
                ground.color = tint;
            }

            if (signBoard != null)
            {
                signBoard.gameObject.SetActive(!unlocked);
                signBoard.color = tint;
            }

            if (sign != null)
            {
                sign.gameObject.SetActive(!unlocked);
                sign.text = _state == PlotState.Locked
                    ? $"{displayName}\n<size=70%>after wave {unlockAfterWave}</size>"
                    : $"{displayName}\n<size=70%>{price:N0} coins</size>";
            }

            if (marker != null)
            {
                marker.gameObject.SetActive(IsForSale);
                marker.localPosition = _markerBase;
            }
        }
    }
}
