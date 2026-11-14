using NUnit.Framework;
using ResortEmpire.World;

namespace ResortEmpire.Tests
{
    /// <summary>Edit Mode tests for the Wave Satisfaction model (SCRUM-147, build document §4.4).</summary>
    public class WaveSatisfactionTests
    {
        private const float Tolerance = 0.0001f;
        private static readonly SatisfactionSettings Settings = SatisfactionSettings.Default;

        [TestCase(0f, 1f)]
        [TestCase(5f, 1f)]
        [TestCase(10f, 1f)]
        [TestCase(20f, 0.625f)]
        [TestCase(30f, 0.25f)]
        [TestCase(60f, 0.25f)]
        public void UnitContribution_FollowsTheWaitingCurve(float waitingSeconds, float expected)
        {
            Assert.AreEqual(expected, WaveSatisfaction.UnitContribution(waitingSeconds, Settings), Tolerance);
        }

        [Test]
        public void ShortWaits_GivePerfectWave()
        {
            float satisfaction = WaveSatisfaction.Calculate(new[] { (5f, 1f), (5f, 1f), (8f, 1f) }, Settings);

            Assert.AreEqual(1f, satisfaction, Tolerance);
            Assert.AreEqual(5, WaveSatisfaction.Stars(satisfaction));
            Assert.IsTrue(WaveRating.IsPerfect(WaveRating.ToPercent(satisfaction)));
        }

        [Test]
        public void LongWaits_StopAtWaveFloor()
        {
            float satisfaction = WaveSatisfaction.Calculate(new[] { (60f, 1f), (90f, 1f) }, Settings);

            Assert.AreEqual(0.40f, satisfaction, Tolerance);
            Assert.AreEqual(1, WaveSatisfaction.Stars(satisfaction));
        }

        [Test]
        public void MixedWaits_AreAveraged()
        {
            // (1 + 1 + 0.625 + 0.25) / 4 = 0.71875 -> 72 % -> 3 stars
            float satisfaction = WaveSatisfaction.Calculate(new[] { (5f, 1f), (5f, 1f), (20f, 1f), (60f, 1f) }, Settings);

            Assert.AreEqual(0.71875f, satisfaction, Tolerance);
            Assert.AreEqual(3, WaveSatisfaction.Stars(satisfaction));
        }

        [Test]
        public void Weights_ChangeTheAverage()
        {
            // (1 * 1 + 0.25 * 3) / 4 = 0.4375
            float satisfaction = WaveSatisfaction.Calculate(new[] { (5f, 1f), (60f, 3f) }, Settings);

            Assert.AreEqual(0.4375f, satisfaction, Tolerance);
        }

        [Test]
        public void EmptyWave_IsFullySatisfied()
        {
            float satisfaction = WaveSatisfaction.Calculate(new (float, float)[0], Settings);

            Assert.AreEqual(1f, satisfaction, Tolerance);
        }

        [Test]
        public void DefaultFloors_KeepTheRuleUnitFloorBelowWaveFloor()
        {
            // WR2: unit_floor < wave_floor, otherwise the lowest stars could never be reached.
            Assert.Less(Settings.unitFloor, Settings.waveFloor);
        }
    }
}
