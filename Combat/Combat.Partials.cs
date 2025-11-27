// Combat.Partials.cs
// Drop this alongside your existing Combat.cs (which is partial).
// This file is SAFE: it compiles and is behavior-neutral (no gameplay changes yet).

using Mirror;
using System;
using System.Runtime.InteropServices;
using UnityEngine;

public partial class Combat
{
    // ---------- POD structs / enums for future use ----------
    public enum HitQuality : byte { Miss, Dodge, Glance, Normal, Crit, Block }
    public enum CCType : byte { None, Stun, Root, Slow, Silence, Knockdown }

    [StructLayout(LayoutKind.Sequential)]
    public readonly struct FinalStats {
        public readonly int level;
        public readonly int armor;
        public readonly float resistPhysical, resistFire, resistIce, resistPoison, resistArcane;
        public readonly float critChance, critMult;
        public readonly float blockChance, blockMitigation;
        public readonly float penetrationPct, lifestealPct, tenacityPct;
        public FinalStats(int level, int armor,
                          float resistPhysical, float resistFire, float resistIce, float resistPoison, float resistArcane,
                          float critChance, float critMult, float blockChance, float blockMitigation,
                          float penetrationPct, float lifestealPct, float tenacityPct)
        {
            this.level = level;
            this.armor = armor;
            this.resistPhysical = resistPhysical;
            this.resistFire = resistFire;
            this.resistIce = resistIce;
            this.resistPoison = resistPoison;
            this.resistArcane = resistArcane;
            this.critChance = critChance;
            this.critMult = critMult;
            this.blockChance = blockChance;
            this.blockMitigation = blockMitigation;
            this.penetrationPct = penetrationPct;
            this.lifestealPct = lifestealPct;
            this.tenacityPct = tenacityPct;
        }
    }

    public struct HitCtx {
        public Combat attacker;
        public Combat victim;
        public DamageType type;
        public int baseAmount;
        public ushort skillId;
        public bool canCrit, canBlock;
        public bool applyCC;
        public CCType ccType;
        public float ccSeconds;
        public bool isPvP;
        public bool fromBehind;
        public uint rngSeed;
        public double now;
    }

    public struct HitResult {
        public int finalDeltaHp;        // negative = damage, positive = heal
        public HitQuality quality;
        public int absorbed;
        public int lifestealAmount;
        public bool ccApplied;
        public CCType ccType;
        public float ccAppliedSeconds;
        public float threat;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HitVis {
        public uint netId;      // victim
        public short deltaHp;   // clamped visual delta
        public byte damageType; // cast from DamageType
        public byte quality;    // HitQuality
        public uint tickId;     // de-dupe on client
    }

    // ---------- Stats cache flags (no behavior until wired) ----------
    bool _statsDirty;
    void MarkStatsDirty() { _statsDirty = true; }
    void RebuildStatsIfDirty() { if (_statsDirty) { _statsDirty = false; /* fill when we wire aggregator */ } }

    // ---------- Server resolver stubs (neutral behavior) ----------
#if UNITY_SERVER || UNITY_EDITOR
    int ResolveAndApply(ref HitCtx ctx, in FinalStats atk, in FinalStats def, out HitResult result)
    {
        // Basic, behavior-neutral pass-through: just use baseAmount as damage.
        // We DO NOT change HP here; your existing Combat.cs keeps doing what it already does.
        result = new HitResult {
            finalDeltaHp = -Mathf.Max(1, ctx.baseAmount),
            quality = HitQuality.Normal,
            absorbed = 0,
            lifestealAmount = 0,
            ccApplied = false,
            ccType = CCType.None,
            ccAppliedSeconds = 0f,
            threat = Mathf.Max(1, ctx.baseAmount)
        };
        return -result.finalDeltaHp; // return positive damage amount
    }

    float ArmorMitigation(int armor, int attackerLevel)
    {
        // Placeholder (no mitigation). Wire A/(A+K(level)) later.
        return 0f;
    }

    float ApplyResistances(float amount, DamageType type, in FinalStats def)
    {
        // Placeholder (no resist). Wire per-type later.
        return amount;
    }

    bool TryApplyCCWithDR(Combat victim, CCType type, float baseSeconds, double now, out float appliedSeconds)
    {
        // Placeholder: don't apply any CC yet.
        appliedSeconds = 0f;
        return false;
    }

    // ---------- RNG (deterministic, tiny) ----------
    struct RngState { public uint s; }
    RngState RngFrom(uint seed) { return new RngState { s = seed == 0 ? 0xA3C59AC3u : seed }; }
    uint NextU32(ref RngState r)
    {
        // xorshift32
        uint x = r.s;
        x ^= x << 13;
        x ^= x >> 17;
        x ^= x << 5;
        r.s = x == 0 ? 0xA3C59AC3u : x;
        return r.s;
    }
    float Next01(ref RngState r) => (NextU32(ref r) & 0xFFFFFF) / (float)0x1000000;
#endif

    // ---------- Timed effects / batching stubs (no-ops for now) ----------
#if UNITY_SERVER || UNITY_EDITOR
    enum EffectKind : byte { DoT, HoT, Shield }
    struct TimedEffect {
        public EffectKind kind;
        public DamageType type;
        public int magnitude;
        public float period;
        public double nextTick;
        public double endTime;
        public int tagHash;
        public uint sourceNetId;
        public byte stacks;
    }

    void TickTimedEffects(float dt) { /* no-op until wired */ }

    void EnqueueHitForObservers(in HitVis vis) { /* batching disabled for now */ }

    void FlushBatchesIfDue(double now) { /* no-op until wired */ }
#endif

    // ---------- Back-compat hook: immediate feedback to the victim ----------
    // This just relays to your existing UnityEvent (damage popup) so current UI keeps working.
    [TargetRpc]
    void TargetRpcOnReceivedDamaged(NetworkConnectionToClient conn, int amount, DamageType type)
    {
        // Relay to the same event your existing Rpc uses
        onClientReceivedDamage?.Invoke(amount, type);
    }

    // ---------- Lifecycle hooks (empty: safe to have even if not called) ----------
    partial void OnAwake_Server() { }
    partial void OnStartServer_Combat() { }
    partial void OnStopServer_Combat() { }
    partial void OnUpdate_Server(float dt) { }
    partial void OnUpdate_Client(float dt) { }

    // ---------- Client batch parse (not used yet) ----------
    void OnCombatBatchMessage(ReadOnlySpan<byte> payload) { /* no-op until wired */ }

    // ---------- Misc helpers (no-ops) ----------
    static void AddThreat(Combat victimAI, Combat attacker, float value) { }
    int ClampVisualDelta(int rawDelta) => Mathf.Clamp(rawDelta, -32760, 32760);
}
