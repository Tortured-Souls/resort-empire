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
    /// seats and workers, and a <see cref="UICard"/> for every other building. Each building has
    /// one worker slot and exactly one building is without a worker, so a move always goes from a
    /// staffed building to the empty one; the player picks which building to leave empty. One tap
    /// moves, without confirmation, and raises <see cref="MoveClicked"/>.
    /// </summary>
    [RequireComponent(typeof(UISheet))]
    public sealed class WorkerAllocationPanel : MonoBehaviour
    {
        [SerializeField] private UITag status;
        [SerializeField] private Image buildingArt;
        [SerializeField] private UIPill seats;
        [SerializeField] private UIPill workers;
        [SerializeField] private TMP_Text section;
        [SerializeField] private RectTransform cards;
        [SerializeField] private UICard cardPrefab;
        [SerializeField] private NeedIcons needIcons;
        [SerializeField] private Sprite workerAvatar;

        private readonly List<UICard> _cards = new();
        private UISheet _sheet;
        private IReadOnlyList<Building> _buildings;
        private Building _target;

        /// <summary>Raised with the building that loses its worker and the one that gets it.</summary>
        public event Action<Building, Building> MoveClicked;

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

        private void Fill()
        {
            bool staffed = _target.HasWorker;
            Sheet.Title.text = _target.DisplayName;
            status.Set(staffed ? "Worker" : "No worker", staffed ? UIPalette.Positive : UIPalette.Warning);
            var art = _target.TryGetComponent(out SpriteRenderer body) ? body.sprite : null;
            buildingArt.sprite = art;
            buildingArt.gameObject.SetActive(art != null);
            seats.SetValue(_target.Capacity.ToString());
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

        private UICard NextCard(int index)
        {
            while (_cards.Count <= index) _cards.Add(Instantiate(cardPrefab, cards));
            var card = _cards[index];
            card.gameObject.SetActive(true);
            return card;
        }
    }
}
