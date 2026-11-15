using System;
using System.Collections.Generic;
using ResortEmpire.NPC;
using UnityEngine;

namespace ResortEmpire.World
{
    /// <summary>Tunable values of the satisfaction model (build document §4.4, §4.8).</summary>
    [Serializable]
    public struct SatisfactionSettings
    {
        [Tooltip("patience_grace: waiting up to this many seconds still gives 100 %.")]
        [Min(0f)] public float patienceGrace;

        [Tooltip("patience_floor_after: from this many seconds on a unit only gives unit_floor.")]
        [Min(0f)] public float patienceFloorAfter;

        [Tooltip("unit_floor: the lowest contribution of a single unit, 0–1.")]
        [Range(0f, 1f)] public float unitFloor;

        [Tooltip("wave_floor: the lowest Wave Satisfaction, 0–1. Must be above unit_floor (WR2).")]
        [Range(0f, 1f)] public float waveFloor;

        /// <summary>The prototype defaults: 10 s, 30 s, 25 %, 40 %.</summary>
        public static SatisfactionSettings Default => new()
        {
            patienceGrace = 10f,
            patienceFloorAfter = 30f,
            unitFloor = 0.25f,
            waveFloor = 0.40f,
        };
    }

    /// <summary>
    /// Turns the waiting history of a wave into Wave Satisfaction (build document §4.4). Every unit
    /// of a need contributes by how long it waited between arrival and being seated; the wave value
    /// is the weighted average of those contributions, never below wave_floor. Stars come from
    /// <see cref="WaveRating"/>.
    /// </summary>
    public static class WaveSatisfaction
    {
        /// <summary>
        /// Contribution of one unit that waited <paramref name="waitingSeconds"/>: 100 % up to
        /// patience_grace, then falling linearly to unit_floor at patience_floor_after, and
        /// unit_floor after that.
        /// </summary>
        public static float UnitContribution(float waitingSeconds, SatisfactionSettings settings)
        {
            float wait = Mathf.Max(0f, waitingSeconds);
            if (wait <= settings.patienceGrace) return 1f;
            if (wait >= settings.patienceFloorAfter) return settings.unitFloor;

            float t = (wait - settings.patienceGrace) / (settings.patienceFloorAfter - settings.patienceGrace);
            return Mathf.Lerp(1f, settings.unitFloor, t);
        }

        /// <summary>
        /// Wave Satisfaction (0–1) from the waiting times and weights of all units of the wave.
        /// A wave without any unit counts as fully satisfied: nobody had to wait.
        /// </summary>
        public static float Calculate(IEnumerable<(float waitingSeconds, float weight)> units, SatisfactionSettings settings)
        {
            if (units == null) throw new ArgumentNullException(nameof(units));

            float weightedSum = 0f;
            float weightTotal = 0f;
            foreach (var (waitingSeconds, weight) in units)
            {
                if (weight <= 0f) continue;
                weightedSum += UnitContribution(waitingSeconds, settings) * weight;
                weightTotal += weight;
            }

            if (weightTotal <= 0f) return 1f;
            return Mathf.Max(settings.waveFloor, weightedSum / weightTotal);
        }

        /// <summary>Wave Satisfaction (0–1) from the needs the guests of the wave carried.</summary>
        public static float Calculate(IEnumerable<Need> needs, SatisfactionSettings settings)
        {
            if (needs == null) throw new ArgumentNullException(nameof(needs));
            return Calculate(ToUnits(needs), settings);
        }

        /// <summary>Stars (1–5) for a Wave Satisfaction in the 0–1 range.</summary>
        public static int Stars(float satisfaction) => WaveRating.StarsFor(WaveRating.ToPercent(satisfaction));

        private static IEnumerable<(float, float)> ToUnits(IEnumerable<Need> needs)
        {
            foreach (var need in needs)
            {
                if (need != null) yield return (need.WaitingSeconds, need.Weight);
            }
        }
    }
}
