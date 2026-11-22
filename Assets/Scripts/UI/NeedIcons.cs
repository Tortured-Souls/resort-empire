using System;
using ResortEmpire.NPC;
using UnityEngine;

namespace ResortEmpire.UI
{
    /// <summary>One icon per <see cref="NeedType"/>, shared by every screen that shows needs or services.</summary>
    [CreateAssetMenu(fileName = "NeedIcons", menuName = "Resort Empire/Need Icons")]
    public sealed class NeedIcons : ScriptableObject
    {
        [SerializeField] private Sprite rooms;
        [SerializeField] private Sprite drinks;
        [SerializeField] private Sprite food;
        [SerializeField] private Sprite relax;
        [SerializeField] private Sprite oceanView;

        public Sprite For(NeedType need) => need switch
        {
            NeedType.Rooms => rooms,
            NeedType.Drinks => drinks,
            NeedType.Food => food,
            NeedType.Relax => relax,
            NeedType.OceanView => oceanView,
            _ => throw new ArgumentOutOfRangeException(nameof(need), need, null)
        };
    }
}
