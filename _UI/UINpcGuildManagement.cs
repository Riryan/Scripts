using UnityEngine;
using UnityEngine.UI;

public partial class UINpcGuildManagement : MonoBehaviour
{
    public static UINpcGuildManagement singleton;
    public GameObject panel;
    public Text createPriceText;
    public InputField createNameInput;
    public Button createButton;
    public Button terminateButton;

    public UINpcGuildManagement()
    {
        
        
        if (singleton == null) singleton = this;
    }

    void Update()
    {
        Player player = Player.localPlayer;

        
        if (player != null &&
            player.target != null && player.target is Npc &&
            Utils.ClosestDistance(player, player.target) <= player.interactionRange)
        {
            createNameInput.interactable = !player.guild.InGuild() &&
                                           player.gold >= GuildSystem.CreationPrice;
            createNameInput.characterLimit = GuildSystem.NameMaxLength;

            createPriceText.text = GuildSystem.CreationPrice.ToString();

            createButton.interactable = !player.guild.InGuild() && GuildSystem.IsValidGuildName(createNameInput.text);
            createButton.onClick.SetListener(() => {
                player.guild.CmdCreate(createNameInput.text);
                createNameInput.text = ""; 
            });

            terminateButton.interactable = player.guild.guild.CanTerminate(player.name);
            terminateButton.onClick.SetListener(() => {
                player.guild.CmdTerminate();
            });
        }
        else panel.SetActive(false);
    }
}
