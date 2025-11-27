




using Mirror;

public abstract class NpcOffer : NetworkBehaviour
{
    
    
    public abstract bool HasOffer(Player player);

    
    public abstract string GetOfferName();

    
    public abstract void OnSelect(Player player);
}
