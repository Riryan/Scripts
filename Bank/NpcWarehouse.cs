using UnityEngine;

[DisallowMultipleComponent]
public class NpcWarehouse : MonoBehaviour, IPlayerInteractable
{
    public void OnInteractServer(Player player)
    {
        //player.TargetShowWarehouseUI();
        player.ServerOpenWarehouse();
    }

    public void OnInteractClient(Player player) {}
}
