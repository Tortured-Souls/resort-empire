using System;
using System.Collections.Generic;

namespace ResortEmpire.Save
{
    /// <summary>Level of the building standing on one building spot.</summary>
    [Serializable]
    public struct SavedBuilding
    {
        public int spotIndex;
        public int level;

        public SavedBuilding(int spotIndex, int level)
        {
            this.spotIndex = spotIndex;
            this.level = level;
        }
    }

    /// <summary>Everything a resort needs to continue after the game is closed.</summary>
    [Serializable]
    public sealed class SaveData
    {
        public int coins;
        public int gems;
        public int energy;
        public int completedWaves;
        public List<int> boughtPlots = new();
        public List<SavedBuilding> buildings = new();
    }
}
