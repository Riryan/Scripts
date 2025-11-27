using System;
using System.Collections.Generic;
using UnityEngine;

public static class HarvestShared
{
    public const float CellSize = 64f;

    public struct Node
    {
        public Vector3 pos;
        public Quaternion rot;
        public int localIndex; // 0..N-1 within (cell, prototype)
    }

    // 64-bit SplitMix RNG (deterministic, fast, no alloc)
    public struct SplitMix64
    {
        ulong state;
        public SplitMix64(ulong seed) { state = seed; }
        public ulong NextU64() {
            ulong z = (state += 0x9E3779B97F4A7C15UL);
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }
        public float Next01() => (NextU64() >> 11) * (1.0f / 9007199254740992f); // [0,1)
    }

    public static ulong Hash64(uint a, uint b, uint c, uint d, uint e = 0)
    {
        ulong x = 0x9E3779B97F4A7C15UL;
        void mix(uint v) {
            x ^= v; x *= 0xBF58476D1CE4E5B9UL; x ^= x >> 33; x *= 0x94D049BB133111EBUL; x ^= x >> 33;
        }
        mix(a); mix(b); mix(c); mix(d); mix(e);
        return x;
    }

    public static Vector2Int WorldToCell(Vector3 world)
        => new Vector2Int(Mathf.FloorToInt(world.x / CellSize), Mathf.FloorToInt(world.z / CellSize));

    public static Vector3 CellOrigin(int cx, int cy) => new Vector3(cx * CellSize, 0, cy * CellSize);

    // Expected count via Poisson(mean). For speed, we approximate: floor(mean) + (rand < frac ? 1 : 0)
    static int SampleCount(float mean, ref SplitMix64 rng)
    {
        int c = Mathf.FloorToInt(mean);
        float frac = mean - c;
        if (rng.Next01() < frac) c++;
        return Mathf.Max(0, c);
    }

    // Simple dart-throw within cell (XZ plane). Good enough for Phase 1.
    public static List<Node> GenerateNodes(int zoneId, int cellX, int cellY, HarvestRule rule)
    {
        var seed = Hash64((uint)zoneId, (uint)cellX, (uint)cellY, (uint)rule.PrototypeId);
        var rng = new SplitMix64(seed);
        int want = SampleCount(rule.averageCountPerCell, ref rng);

        var list = new List<Node>(want);
        var origin = CellOrigin(cellX, cellY);
        float s2 = rule.minSpacing * rule.minSpacing;

        for (int tries = 0; list.Count < want && tries < want * 12; tries++)
        {
            float x = origin.x + rng.Next01() * CellSize;
            float z = origin.z + rng.Next01() * CellSize;
            var p = new Vector3(x, 0f, z);

            bool ok = true;
            for (int i = 0; i < list.Count; i++)
                if ((p - list[i].pos).sqrMagnitude < s2) { ok = false; break; }

            if (!ok) continue;

            var yaw = rng.Next01() * 360f;
            list.Add(new Node { pos = p, rot = Quaternion.Euler(0, yaw, 0), localIndex = list.Count });
        }
        return list;
    }

    public static ulong NodeId(int zoneId, int cellX, int cellY, int prototypeId, int localIndex)
        => Hash64((uint)zoneId, (uint)cellX, (uint)cellY, (uint)prototypeId, (uint)localIndex);
}
