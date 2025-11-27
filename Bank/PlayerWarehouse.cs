using Mirror;

public partial class Player
{
   // [SyncVar] public bool warehouseOpen;

    [TargetRpc]
    public void TargetShowWarehouseUI()
    {
        UI_PlayerWarehouse ui = FindObjectOfType<UI_PlayerWarehouse>();
        if (ui) ui.Show();
    }
   
}
