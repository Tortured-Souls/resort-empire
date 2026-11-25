using NUnit.Framework;
using ResortEmpire.UI;
using Offer = ResortEmpire.UI.WorkerAllocationPanel.UpgradeOffer;

namespace ResortEmpire.Tests
{
    public class WorkerAllocationPanelTextTests
    {
        [TestCase(0, 2, "0/2")]
        [TestCase(3, 4, "3/4")]
        [TestCase(-1, 4, "0/4")]
        public void Seats_ShowGuestsInsideOutOfCapacity(int guests, int capacity, string expected)
        {
            Assert.AreEqual(expected, WorkerAllocationPanel.SeatsText(guests, capacity));
        }

        [TestCase(Offer.Available)]
        [TestCase(Offer.NotAffordable)]
        public void UpgradeButton_ShowsNextLevelAndPrice(Offer offer)
        {
            string text = WorkerAllocationPanel.UpgradeText(offer, 1, 500);

            StringAssert.Contains("Level 2", text);
            StringAssert.Contains(500.ToString("N0"), text);
        }

        [Test]
        public void UpgradeButton_AtMaxLevel_ShowsNoPrice()
        {
            Assert.AreEqual("Max level 3", WorkerAllocationPanel.UpgradeText(Offer.MaxLevel, 3, 0));
        }
    }
}
