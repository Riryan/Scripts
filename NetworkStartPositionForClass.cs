// Simple script that inherits from NetworkStartPosition to make class based
// spawns.
using Mirror;

namespace uMMORPG
{
    public class NetworkStartPositionForClass : NetworkStartPosition
    {
        public Player playerPrefab;
    }
}