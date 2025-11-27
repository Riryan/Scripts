




using Mirror;
using UnityEngine;

[RequireComponent(typeof(NavMeshMovement))]
[RequireComponent(typeof(NetworkNavMeshAgent))]

public partial class Npc : Entity
{
    [Header("Components")]
    public NpcGuildManagement guildManagement;
    public NpcQuests quests;
    public NpcRevive revive;
    public NpcTrading trading;
    public NpcTeleport teleport;

    [Header("Welcome Text")]
    [TextArea(1, 30)] public string welcome;

    
    [HideInInspector] public NpcOffer[] offers;

    void Awake()
    {
        offers = GetComponents<NpcOffer>();
    }

    
    [Server] protected override string UpdateServer() { return state; }
    [Client] protected override void UpdateClient() {}

    
    public override bool CanAttack(Entity entity) { return false; }

    
    protected override void OnInteract()
    {
        Player player = Player.localPlayer;

        
        
        if (health.current > 0 &&
            Utils.ClosestDistance(player, this) <= player.interactionRange)
        {
            UINpcDialogue.singleton.Show();
        }
        
        
        else
        {
            Vector3 destination = Utils.ClosestPoint(this, player.transform.position);
            player.movement.Navigate(destination, player.interactionRange);
        }
    }
}
