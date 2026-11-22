using System.Collections;
using NUnit.Framework;
using ResortEmpire.World;
using UnityEngine;
using UnityEngine.TestTools;

namespace ResortEmpire.Tests
{
    public class BuildingQueueBadgeTests
    {
        [UnitySetUp]
        public IEnumerator EnterPlay()
        {
            yield return new EnterPlayMode();
        }

        [UnityTearDown]
        public IEnumerator ExitPlay()
        {
            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator UnstaffedBuilding_ShowsBadgeOnlyWhileFiveOrMoreGuestsWait()
        {
            var building = new GameObject("Unstaffed building").AddComponent<Building>();
            try
            {
                yield return null; // Let the building start with no guests and no worker.
                Assert.IsFalse(BadgeVisible(building));

                var guests = new GameObject[5];
                for (int i = 0; i < guests.Length; i++)
                {
                    guests[i] = new GameObject("Guest");
                    guests[i].transform.SetParent(building.transform, false);
                    guests[i].transform.position = building.EntrancePosition;
                    building.JoinQueue(guests[i]);
                    Assert.AreEqual(i == 4, BadgeVisible(building), $"{i + 1} guests waiting");
                }

                Assert.IsTrue(building.TryEnter(guests[0]));
                Assert.IsFalse(BadgeVisible(building), "A guest being served does not count as waiting.");
                building.Leave(guests[0]);
                building.JoinQueue(guests[0]);
                Assert.IsTrue(BadgeVisible(building));
                building.Leave(guests[0]);
                Assert.IsFalse(BadgeVisible(building), "A guest who leaves the queue does not count either.");
            }
            finally
            {
                Object.Destroy(building.gameObject);
            }
        }

        private static bool BadgeVisible(Building building) =>
            building.GetComponentInChildren<SlowServiceIndicator>() != null;
    }
}
