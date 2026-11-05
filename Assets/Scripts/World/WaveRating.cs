using UnityEngine;

namespace ResortEmpire.World
{
    /// <summary>
    /// Turns wave satisfaction into the end-of-wave rating. Perfect (100 %) is a tier above
    /// five stars, and the rating never drops below one star. Everything is decided on the
    /// rounded percentage, so the stars always match the number shown to the player.
    /// </summary>
    public static class WaveRating
    {
        public const int MinStars = 1;
        public const int MaxStars = 5;
        public const int PerfectPercent = 100;

        private static readonly int[] PercentForStarAboveMinimum = { 55, 70, 80, 90 };

        /// <summary>Converts satisfaction in the 0–1 range to a whole percentage.</summary>
        public static int ToPercent(float satisfaction) => Mathf.RoundToInt(Mathf.Clamp01(satisfaction) * PerfectPercent);

        /// <summary>Stars earned for <paramref name="percent"/>, from 1 to 5.</summary>
        public static int StarsFor(int percent)
        {
            int stars = MinStars;
            foreach (int threshold in PercentForStarAboveMinimum)
            {
                if (percent >= threshold) stars++;
            }

            return stars;
        }

        /// <summary>True when <paramref name="percent"/> reaches the Perfect tier.</summary>
        public static bool IsPerfect(int percent) => percent >= PerfectPercent;
    }
}
