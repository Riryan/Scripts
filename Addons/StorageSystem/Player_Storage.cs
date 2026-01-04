using Mirror;
using uMMORPG.Storage;

namespace uMMORPG
{
    public partial class Player : IStorageOwner
    {
        // Server-authoritative storage container
        public StorageContainer Storage { get; private set; }

        // --------------------------------------------------
        // Initialization (SERVER ONLY)
        // --------------------------------------------------
        [Server]
        public void InitializeStorage(int maxSlots, long maxGold)
        {
            Storage = new StorageContainer(maxSlots, maxGold);
        }
[Server]
public void InitializeStorage()
{
    if (Storage != null)
        return;

    Storage = new StorageContainer(
        maxSlots: 60,        // DEFAULT ITEM CAP
        maxGold: 100000,
        version: 1
    );
}

        // --------------------------------------------------
        // Gold operations
        // --------------------------------------------------
        [Command]
        public void Cmd_DepositGold(long amount)
        {
            if (!isServer || Storage == null)
                return;

            if (amount <= 0 || gold < amount)
                return;

            long canStore = Storage.MaxGold - Storage.gold;
            long finalAmount = amount > canStore ? canStore : amount;

            gold -= finalAmount;
            Storage.gold += finalAmount;
        }

        [Command]
        public void Cmd_WithdrawGold(long amount)
        {
            if (!isServer || Storage == null)
                return;

            if (amount <= 0 || Storage.gold < amount)
                return;

            Storage.gold -= amount;
            gold += amount;
        }
    }
}
