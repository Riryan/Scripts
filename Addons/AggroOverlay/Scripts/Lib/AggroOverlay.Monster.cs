using Mirror;

// MONSTER
public partial class Monster
{
    public AggroOverlay aggroOverlay;
    private string _currentAggro;
    // -----------------------------------------------------------------------------------
    // OnClientAggro_AggroOverlay
    // -----------------------------------------------------------------------------------
#if _SERVER
    public override void OnStartServer()
    {
        onAggro.AddListener(EvAggro_Overlay);
    }

    private void EvAggro_Overlay(Entity entity)
    {
        OnAggro_overLay();
    }
#endif

#if _CLIENT
    [ClientCallback]
    public void OnClientAggro_AggroOverlay(Entity entity)
    {
        if (aggroOverlay == null) return;

        if (target != entity || !(entity is Player))
            aggroOverlay.Hide();
        else if (target == null || entity is Player)
            aggroOverlay.Show();
    }
#endif

    [ClientRpc]
    private void OnAggro_overLay()
    {
#if _CLIENT
        if (aggroOverlay)
        {
            if (state == "DEAD")
            {
                _currentAggro = null;
                aggroOverlay.Hide();
            }
            else if (target == null)
            {
                _currentAggro = null;
            }
            else
            {
                if (_currentAggro == null || _currentAggro != target.name)
                {
                    _currentAggro = (target != null) ? target.name : null;
                    aggroOverlay.Show();
                }
            }
        }
#endif
    }
    // -----------------------------------------------------------------------------------
}