using uMMORPG;
using UnityEngine;

namespace uMMORPG.Storage
{
    public class NpcStorageAccessRule : IStorageAccessRule
    {
        public bool CanAccess(Player player)
        {
            if (player == null)
                return false;

            if (player.target is not Npc npc)
                return false;

            return Utils.ClosestDistance(player, npc) <= player.interactionRange;
        }
    }
}