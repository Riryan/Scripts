using System.Collections.Generic;

namespace uMMORPG.Storage
{
    public struct StorageSlot
    {
        public int itemId;
        public int amount;
    }

    public class StorageContainer
    {
        public readonly List<StorageSlot> slots;
        public long gold;

        public int MaxSlots { get; }
        public long MaxGold { get; }

        // versioned storage
        public int Version { get; set; }

        public StorageContainer(int maxSlots, long maxGold, int version = 1)
        {
            MaxSlots = maxSlots;
            MaxGold = maxGold;
            Version = version;

            slots = new List<StorageSlot>(maxSlots);
            gold = 0;
        }
    }
}
