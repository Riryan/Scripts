using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace uMMORPG
{
    public class FakeCCUSpawner : MonoBehaviour
    {
        public Player playerPrefab;
        public int targetCCU = 150;
        public float spawnInterval = 0.1f;

        readonly List<Player> fakePlayers = new();

        IEnumerator Start()
        {
            for (int i = 0; i < targetCCU; i++)
            {
                SpawnFakePlayer(i);
                yield return new WaitForSeconds(spawnInterval);
            }
        }

        void SpawnFakePlayer(int index)
        {
            Player p = Instantiate(playerPrefab);
            p.name = $"[FAKE]{index:D3}";

            // register like a real online player
            Player.onlinePlayers[p.name] = p;

            // disable network-driven logic
            p.enabled = false;

            p.gameObject.AddComponent<FakePlayerDriver>();
            fakePlayers.Add(p);
        }
    }
}
