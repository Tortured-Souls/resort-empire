using System;
using System.Collections.Generic;

namespace ResortEmpire.World
{
    /// <summary>How well one need was met during a wave, as shown in the report breakdown.</summary>
    public readonly struct NeedResult
    {
        public NeedResult(string needName, float satisfaction)
        {
            NeedName = needName;
            Satisfaction = satisfaction;
        }

        /// <summary>Name shown in the UI, e.g. "Food".</summary>
        public string NeedName { get; }

        /// <summary>Average satisfaction of the need in the 0–1 range.</summary>
        public float Satisfaction { get; }

        /// <summary>Satisfaction as a whole percentage.</summary>
        public int Percent => WaveRating.ToPercent(Satisfaction);
    }

    /// <summary>
    /// Results of a finished wave. The wave manager fills it in, including the rewards, and
    /// hands it to <see cref="ResortEmpire.UI.WaveReportPanel"/>; the rating and the breakdown
    /// are derived from these values.
    /// </summary>
    public sealed class WaveReport
    {
        /// <summary>Lowest percentage at which a need counts as "What went well".</summary>
        public const int WentWellPercent = 70;

        /// <summary>Number of the finished wave, 1-based.</summary>
        public int Wave { get; set; }

        /// <summary>Guests whose needs were served during the wave.</summary>
        public int GuestsServed { get; set; }

        /// <summary>Guests who arrived with the wave.</summary>
        public int GuestsTotal { get; set; }

        /// <summary>Final wave satisfaction in the 0–1 range.</summary>
        public float Satisfaction { get; set; }

        /// <summary>Coins the player gets after claiming.</summary>
        public int CoinsEarned { get; set; }

        /// <summary>Energy the player gets after claiming.</summary>
        public int EnergyEarned { get; set; }

        /// <summary>How much the resort value grew.</summary>
        public int ResortValueGained { get; set; }

        /// <summary>Results per need, in the order they are listed in the breakdown.</summary>
        public IReadOnlyList<NeedResult> NeedResults { get; set; } = Array.Empty<NeedResult>();

        /// <summary>Wave satisfaction as a whole percentage.</summary>
        public int SatisfactionPercent => WaveRating.ToPercent(Satisfaction);

        /// <summary>Stars earned, from 1 to 5.</summary>
        public int Stars => WaveRating.StarsFor(SatisfactionPercent);

        /// <summary>True for the Perfect tier above five stars.</summary>
        public bool IsPerfect => WaveRating.IsPerfect(SatisfactionPercent);

        /// <summary>Needs that reached <see cref="WentWellPercent"/>.</summary>
        public IReadOnlyList<NeedResult> WentWell => FilterNeeds(wentWell: true);

        /// <summary>Needs below <see cref="WentWellPercent"/>.</summary>
        public IReadOnlyList<NeedResult> NeedsImprovement => FilterNeeds(wentWell: false);

        private List<NeedResult> FilterNeeds(bool wentWell)
        {
            var filtered = new List<NeedResult>();
            foreach (var result in NeedResults)
            {
                if (result.Percent >= WentWellPercent == wentWell) filtered.Add(result);
            }

            return filtered;
        }
    }
}
