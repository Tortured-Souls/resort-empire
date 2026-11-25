using NUnit.Framework;
using ResortEmpire.World;
using UnityEngine;

namespace ResortEmpire.Tests
{
    public class BuildingVisibleQueueTests
    {
        private Building _building;
        private GameObject[] _guests;

        [SetUp]
        public void CreateBuildingWithSixWaitingGuests()
        {
            _building = new GameObject("Building").AddComponent<Building>();
            _guests = new GameObject[6];
            for (int i = 0; i < _guests.Length; i++)
            {
                _guests[i] = new GameObject($"Guest {i}");
                _building.JoinQueue(_guests[i]);
            }
        }

        [TearDown]
        public void DestroyAll()
        {
            foreach (var guest in _guests) Object.DestroyImmediate(guest);
            Object.DestroyImmediate(_building.gameObject);
        }

        [Test]
        public void OnlyTheFirstGuestsStandInTheQueue_TheRestWaitOutOfSight()
        {
            int visible = _building.VisibleQueue;
            for (int i = 0; i < _guests.Length; i++)
                Assert.AreEqual(i < visible, _building.IsShownInQueue(_guests[i]), $"guest {i}");

            Assert.AreEqual(_guests.Length - visible, _building.HiddenQueueCount);
        }

        [Test]
        public void HiddenGuestsWaitOnTheLastVisibleSlot_SoTheQueueNeverGrowsLonger()
        {
            var lastVisible = _building.QueuePosition(_guests[_building.VisibleQueue - 1]);
            Assert.AreEqual(lastVisible, _building.QueuePosition(_guests[_guests.Length - 1]));
        }

        [Test]
        public void LimitIsOnlyVisual_HiddenGuestsMoveUpWhenTheQueueAdvances()
        {
            int visible = _building.VisibleQueue;
            Assert.IsFalse(_building.IsShownInQueue(_guests[visible]));

            Assert.IsTrue(_building.TryEnter(_guests[0]));

            Assert.AreEqual(_guests.Length - 1, _building.QueueCount, "Hidden guests keep their place.");
            Assert.IsTrue(_building.IsShownInQueue(_guests[visible]));
        }

        [TestCase(3, 4, 0)]
        [TestCase(4, 4, 0)]
        [TestCase(9, 4, 5)]
        public void HiddenCount_IsTheQueueBeyondTheVisibleSlots(int queued, int visible, int hidden)
        {
            Assert.AreEqual(hidden, Building.HiddenInQueue(queued, visible));
        }

        [Test]
        public void OverflowLabel_CountsTheHiddenGuests()
        {
            Assert.AreEqual("+5", QueueOverflowLabel.Text(5));
        }
    }
}
