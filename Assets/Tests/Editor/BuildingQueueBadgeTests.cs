using System.Collections;
using System.Linq;
using NUnit.Framework;
using ResortEmpire.UI;
using ResortEmpire.World;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
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
        public IEnumerator UnstaffedBuilding_ShowsBadgeOnlyWhileThreeOrMoreGuestsWait()
        {
            var building = new GameObject("Unstaffed building").AddComponent<Building>();
            try
            {
                yield return null; // Let the building start with no guests and no worker.
                Assert.IsFalse(BadgeVisible(building));

                var guests = new GameObject[3];
                for (int i = 0; i < guests.Length; i++)
                {
                    guests[i] = JoinQueue(building);
                    Assert.AreEqual(i == 2, BadgeVisible(building), $"{i + 1} guests waiting");
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

        [UnityTest]
        public IEnumerator MovingWorker_TransfersWarningBetweenBuildingsWithWaitingGuests()
        {
            SceneManager.LoadScene("GameWorld");
            yield return null;
            yield return null;
            var system = Object.FindAnyObjectByType<BuildingSystem>();
            var donor = system.Buildings.First(b => b.HasWorker);
            var recipient = system.Buildings.Single(b => !b.HasWorker);

            for (int i = 0; i < 4; i++) JoinQueue(donor);
            Assert.IsFalse(BadgeVisible(donor));
            Assert.IsFalse(WarningShownFor(donor));

            JoinQueue(recipient);
            JoinQueue(recipient);
            Assert.IsFalse(WarningShownFor(recipient));
            JoinQueue(recipient);
            Assert.IsTrue(WarningShownFor(recipient));

            Assert.IsTrue(system.TryMoveWorker(donor, recipient));
            Assert.IsFalse(BadgeVisible(recipient));
            Assert.IsTrue(BadgeVisible(donor));
            yield return null; // The toast discards the old warning and shows the next eligible one.
            yield return null;
            Assert.IsFalse(WarningShownFor(recipient));
            Assert.IsTrue(WarningShownFor(donor));
        }

        private static GameObject JoinQueue(Building building)
        {
            var guest = new GameObject("Guest");
            guest.transform.SetParent(building.transform, false);
            guest.transform.position = building.EntrancePosition;
            building.JoinQueue(guest);
            return guest;
        }

        private static bool WarningShownFor(Building building)
        {
            var toast = Object.FindAnyObjectByType<NotificationToast>();
            return toast != null && toast.GetComponentsInChildren<TMP_Text>()
                .Any(label => label.text.Contains(building.DisplayName) && label.text.Contains("slow service"));
        }

        private static bool BadgeVisible(Building building) =>
            building.GetComponentInChildren<SlowServiceIndicator>() != null;
    }
}
