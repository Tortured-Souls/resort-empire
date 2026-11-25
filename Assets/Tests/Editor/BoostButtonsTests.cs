using NUnit.Framework;
using ResortEmpire.UI;
using Offer = ResortEmpire.World.BoostSystem.Offer;

namespace ResortEmpire.Tests
{
    public class BoostButtonsTests
    {
        [Test]
        public void ReadyBuilding_OffersSpeedUpWithItsPrice()
        {
            Assert.AreEqual("Speed Up ⚡3", BoostButtons.Label(Offer.Ready, 3, 0));
        }

        [Test]
        public void BoostedBuilding_CountsDownTheSecondsLeft()
        {
            Assert.AreEqual("⚡ 12 s", BoostButtons.Label(Offer.Boosted, 3, 12));
        }

        [Test]
        public void WithoutEnergy_OffersAnAd()
        {
            StringAssert.StartsWith("Watch ad", BoostButtons.Label(Offer.NeedEnergy, 3, 0));
        }

        [TestCase(Offer.Hidden, false)]
        [TestCase(Offer.Used, false)]
        [TestCase(Offer.Ready, true)]
        [TestCase(Offer.Boosted, true)]
        [TestCase(Offer.NeedEnergy, true)]
        public void ButtonIsShownOnlyWhileThereIsSomethingToDo(Offer offer, bool shown)
        {
            Assert.AreEqual(shown, BoostButtons.IsShown(offer));
        }
    }
}
