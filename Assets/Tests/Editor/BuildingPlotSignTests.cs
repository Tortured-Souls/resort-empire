using NUnit.Framework;
using ResortEmpire.World;

namespace ResortEmpire.Tests
{
    public class BuildingPlotSignTests
    {
        [Test]
        public void LockedPlot_SaysItIsLockedAndWhenItOpens()
        {
            string text = BuildingPlot.SignText(PlotState.Locked, "Pool", 3, 1200);

            StringAssert.StartsWith("Pool\n", text);
            StringAssert.Contains("Locked", text);
            StringAssert.Contains("wave 3", text);
            StringAssert.DoesNotContain("Build", text);
        }

        [TestCase(PlotState.Available)]
        [TestCase(PlotState.NotAffordable)]
        public void OpenPlot_SaysThePlayerCanBuildThereAndThePrice(PlotState state)
        {
            string text = BuildingPlot.SignText(state, "Pool", 3, 1200);

            StringAssert.StartsWith("Pool\n", text);
            StringAssert.Contains("Build here", text);
            StringAssert.Contains(1200.ToString("N0"), text);
            StringAssert.DoesNotContain("Locked", text);
        }

        [Test]
        public void BoughtPlot_ShowsOnlyTheName()
        {
            Assert.AreEqual("Pool", BuildingPlot.SignText(PlotState.Unlocked, "Pool", 3, 1200));
        }
    }
}
