using System;
using UnityEngine;

namespace ResortEmpire.NPC
{
    public enum NeedType
    {
        Rooms,
        Drinks,
        Food,
        Relax,
        OceanView
    }

    /// <summary>One service request. Queue time is recorded per need, never per guest.</summary>
    [Serializable]
    public sealed class Need
    {
        [SerializeField] private NeedType type;
        [SerializeField] private float waitingSeconds;
        [SerializeField] private bool isCompleted;

        public NeedType Type => type;
        public float WaitingSeconds => waitingSeconds;
        public bool IsCompleted => isCompleted;

        public Need(NeedType type) => this.type = type;

        /// <summary>Weight of the need in the wave satisfaction formula; 1.0 for regular needs.</summary>
        public float Weight => 1f;

        /// <summary>First wave (1-based) in which guests have this need.</summary>
        public int ActiveFromWave => FirstWave(type);

        /// <summary>True when guests of <paramref name="wave"/> have this need.</summary>
        public bool IsActiveInWave(int wave) => wave >= ActiveFromWave;

        /// <summary>
        /// First wave of each need type. Relax comes with the pool (unlocked after wave 3),
        /// Ocean View with the bungalows (unlocked after wave 6).
        /// </summary>
        public static int FirstWave(NeedType type) => type switch
        {
            NeedType.Relax => 4,
            NeedType.OceanView => 7,
            _ => 1
        };

        internal void Wait(float deltaTime) => waitingSeconds += deltaTime;
        internal void Complete() => isCompleted = true;
    }
}
