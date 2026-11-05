using System;
using ResortEmpire.World;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ResortEmpire.UI
{
    /// <summary>
    /// Popup for buying a <see cref="BuildingPlot"/>: shows the building name and price and
    /// raises <see cref="BuyClicked"/>. The Buy button is disabled while the player cannot afford it.
    /// </summary>
    public sealed class BuildingPurchasePanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text body;
        [SerializeField] private Button buyButton;
        [SerializeField] private Button closeButton;

        private BuildingPlot _plot;

        /// <summary>Raised with the shown plot when the player confirms the purchase.</summary>
        public event Action<BuildingPlot> BuyClicked;

        /// <summary>Plot currently shown, or null when the panel is hidden.</summary>
        public BuildingPlot Plot => _plot;

        /// <summary>True while the panel is shown.</summary>
        public bool IsOpen => gameObject.activeSelf;

        private void Awake()
        {
            buyButton.onClick.AddListener(OnBuy);
            closeButton.onClick.AddListener(Hide);
        }

        private void OnDestroy()
        {
            buyButton.onClick.RemoveListener(OnBuy);
            closeButton.onClick.RemoveListener(Hide);
        }

        /// <summary>Opens the panel for <paramref name="plot"/>.</summary>
        public void Show(BuildingPlot plot)
        {
            if (plot == null) return;

            _plot = plot;
            gameObject.SetActive(true);
            Refresh();
        }

        /// <summary>Updates texts and the Buy button, e.g. after the coin balance changed.</summary>
        public void Refresh()
        {
            if (_plot == null) return;

            bool affordable = _plot.State == PlotState.Available;
            title.text = _plot.DisplayName;
            body.text = affordable
                ? $"Buy for <b>{_plot.Price:N0}</b> coins?\nYou also get 1 new worker."
                : $"You need <b>{_plot.Price:N0}</b> coins.";
            buyButton.interactable = affordable;
        }

        public void Hide()
        {
            _plot = null;
            gameObject.SetActive(false);
        }

        private void OnBuy()
        {
            if (_plot != null) BuyClicked?.Invoke(_plot);
        }
    }
}
