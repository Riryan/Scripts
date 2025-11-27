
public class NpcRevive : NpcOffer
{
    public override bool HasOffer(Player player) => true;

    public override string GetOfferName() => "Revive";

    public override void OnSelect(Player player)
    {
        UINpcRevive.singleton.panel.SetActive(true);
        UIInventory.singleton.panel.SetActive(true); 
        UINpcDialogue.singleton.panel.SetActive(false);
    }
}
