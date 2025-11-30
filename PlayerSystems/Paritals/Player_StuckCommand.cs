using UnityEngine;
using Mirror;

public partial class Player
{
    [Header("Stuck Command")]
    public float stuckCooldown = 300f; // 5 minutes

    [HideInInspector] public double lastStuckTime = -9999;

    // --------------------------
    // /stuck command
    // --------------------------
    [Command]
    public void CmdStuck()
    {
        // cooldown check (server authoritative)
        if (NetworkTime.time - lastStuckTime < stuckCooldown)
        {
            double remain = stuckCooldown - (NetworkTime.time - lastStuckTime);

            // send message back to this player only
            if (chat != null)
                chat.TargetMsgInfo($"You must wait {(int)remain}s before using /stuck again.");

            return;
        }

        lastStuckTime = NetworkTime.time;

        if (chat != null)
            chat.TargetMsgInfo("You collapsed and will awaken at your bindpoint.");

        // trigger normal death flow; existing Player.OnDeath + DEAD state
        // will handle movement reset and respawn/bindpoint logic.
        health.current = 0;
    }
}
