// PlayerItemMall.cs — DISABLED STUB
// Drop-in replacement to remove Item Mall functionality without breaking call sites.
// Keeps public fields/methods that other scripts might reference.

using Mirror;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerItemMall : NetworkBehaviour
{
    [Header("Disabled Item Mall (stub)")]
    // Kept so other scripts accessing these refs/fields still compile.
    public Player player;              // optional reference; unused
    public PlayerChat chat;            // optional reference; unused
    public PlayerInventory inventory;  // optional reference; unused

    // Keep this field for any UI/scripts that still read player.itemMall.coins.
    // Not a SyncVar on purpose (no bandwidth). Always 0 in the disabled build.
    public long coins = 0;

    // Some projects referenced a cooldown on coupon entry; safe to keep.
    public float couponWaitSeconds = 0f;

    // Mall is globally disabled.
    public bool IsEnabled => false;

    // --- Lifecycle (no scheduling, no DB, no network traffic) ---
    public override void OnStartServer() { /* no-op */ }
    public override void OnStartClient() { /* no-op */ }

    // --- Commands kept for compatibility; all are no-ops now. ---

    // Called by client when entering coupons in old flow.
    [Command]
    public void CmdEnterCoupon(string coupon)
    {
        // Intentionally no-op.
    }

    // Called by client to unlock/buy an item in old flow.
    [Command]
    public void CmdUnlockItem(int categoryIndex, int itemIndex)
    {
        // Intentionally no-op.
    }

    // Old server-side order processor; kept so any external calls won't break.
    [Server]
    public void ProcessCoinOrders()
    {
        // Intentionally no-op.
    }
}
