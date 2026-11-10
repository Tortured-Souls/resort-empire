using System;
using System.Collections.Generic;
using UnityEngine;

namespace ResortEmpire.NPC
{
    /// <summary>Ordered service requests carried by a visual guest; no guest satisfaction.</summary>
    public sealed class NpcNeeds : MonoBehaviour
    {
        [SerializeField] private List<Need> needs = new()
        {
            new Need(NeedType.Rooms),
            new Need(NeedType.Food)
        };

        public event Action<Need> NeedCompleted;

        public IReadOnlyList<Need> Needs => needs;

        public Need Current
        {
            get
            {
                foreach (var need in needs)
                {
                    if (!need.IsCompleted) return need;
                }

                return null;
            }
        }

        /// <summary>Configure before starting a visit. Two requests is the default, not a limit.</summary>
        public void SetNeeds(params NeedType[] types)
        {
            needs.Clear();
            foreach (var type in types) needs.Add(new Need(type));
        }

        internal void CompleteCurrent()
        {
            var need = Current;
            if (need == null) return;
            need.Complete();
            NeedCompleted?.Invoke(need);
        }
    }
}
