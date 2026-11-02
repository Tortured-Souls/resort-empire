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

        internal void Wait(float deltaTime) => waitingSeconds += deltaTime;
        internal void Complete() => isCompleted = true;
    }
}
