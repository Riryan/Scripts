using Mirror;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_ImprovedTarget : MonoBehaviour
{
    [Header("Target")]
    public GameObject panel;
    public Slider healthSlider;
    public TMP_Text nameText;
    public TMP_Text distanceText;
    public TMP_Text healthText;
    public TMP_Text levelText;
    public Transform buffsPanel;
    public UIBuffSlot buffSlotPrefab;
    public Button tradeButton, guildInviteButton, partyInviteButton;

    [Header("Target of target")]
    public GameObject TargetPanel;
    public Slider targetHealthSlider;
    public TMP_Text targetNameText;
    public TMP_Text TagetHealthText;
    public Button selectTargetButton;

    [Header("[-=-[ Improved Target ]-=-]")]
    public GameObject challengeObject;
    public GameObject eliteObject;
    public GameObject bossObject;
    public bool nameColoring = false;


    public Color bossColor = new Color(143, 0, 254, 1);
    // Définir une liste de paires de différences et de couleurs
    public List<DiffColorPair> diffColorPairs = new List<DiffColorPair>
    {
        new DiffColorPair { diff = -2, color = Color.grey },
        new DiffColorPair { diff = -1, color = Color.green },
        new DiffColorPair { diff = 0, color = Color.white },
        new DiffColorPair { diff = 1, color = Color.blue },
        new DiffColorPair { diff = 2, color = Color.yellow },
        new DiffColorPair { diff = 3, color = new Color(1.0f, 0.64f, 0.0f) }, // Orange
        new DiffColorPair { diff = 4, color = Color.red }
        // Ajoutez d'autres paires de différences et de couleurs ici
    };

    private void Update()
    {
        Player player = Player.localPlayer;
        if (player != null)
        {
            // show nextTarget > target
            Entity target = player.nextTarget ?? player.target;
            if (target != null && target != player)
            {
                float distance = Utils.ClosestDistance(player, target);

                if (!(target is Player) && target.health.current > 0 && distance < 50) SetupTarget(player, target, distance);
                else if (target is Player && distance < 50) SetupTarget(player, target, distance);
                else panel.SetActive(false);
            }
            else panel.SetActive(false);
        }
        else panel.SetActive(false);
    }

    // Performs all required setup for our target.
    private void SetupTarget(Player player, Entity target, float distance)
    {
        // name and health
        panel.SetActive(true);
        nameText.text = target.name;
        healthSlider.value = target.health.Percent();
        if (target.target == null)
        {
            TargetPanel.SetActive(false);
        }
        else
        {
            TargetPanel.SetActive(true);
            targetHealthSlider.value = target.target.health.Percent();
            TagetHealthText.text = target.target.health.current.ToString() + " / " + target.target.health.max.ToString();
            targetNameText.text = target.target.name.ToString();
        }
        healthText.text = target.health.current.ToString() + " / " + target.health.max.ToString();
        levelText.text = target.level.current.ToString();
        distanceText.text = ((int)distance).ToString() + "m";

        BuffControl(target);
        ButtonControl(player, target, distance);
        TargetControl(player, target);
    }

    // Controls all functionality for buffs on target.
    private void BuffControl(Entity target)
    {
        // target buffs
        UIUtils.BalancePrefabs(buffSlotPrefab.gameObject, target.skills.buffs.Count, buffsPanel);
        for (int i = 0; i < target.skills.buffs.Count; ++i)
        {
            UIBuffSlot slot = buffsPanel.GetChild(i).GetComponent<UIBuffSlot>();

            // refresh
            slot.image.color = Color.white;
            slot.image.sprite = target.skills.buffs[i].image;
            slot.tooltip.text = target.skills.buffs[i].ToolTip();
            slot.slider.maxValue = target.skills.buffs[i].buffTime;
            slot.slider.value = target.skills.buffs[i].BuffTimeRemaining();
        }
    }

    // Controls all functionality for buttons on target.
    private void ButtonControl(Player player, Entity target, float distance)
    {
        // trade button
        if (target is Player
#if _iMMOPVP
            && ((Player)target).Tools_SameRealm(player)
#endif
        )
        {
            tradeButton.gameObject.SetActive(true);
            tradeButton.interactable = player.trading.CanStartTradeWith(target);
            tradeButton.onClick.SetListener(() =>
            {
                player.trading.CmdSendRequest();
            });
        }
        else tradeButton.gameObject.SetActive(false);

        // guild invite button
        if (target is Player && player.guild.InGuild()
#if _iMMOPVP
            && ((Player)target).Tools_SameRealm(player)
#endif
        )
        {
            guildInviteButton.gameObject.SetActive(true);
            guildInviteButton.interactable = !((Player)target).guild.InGuild() &&
#if _iMMOGUILDUPGRADES
                                             player.playerGuildUpgrades.GuildCapacity_CanInvite() &&
#endif
                                             player.guild.guild.CanInvite(player.name, target.name) &&
                                             NetworkTime.time >= player.nextRiskyActionTime &&
                                             distance <= player.interactionRange;
            guildInviteButton.onClick.SetListener(() =>
            {
                player.guild.CmdInviteTarget();
            });
        }
        else guildInviteButton.gameObject.SetActive(false);

        // party invite button
        if (target is Player
#if _iMMOPVP
            && ((Player)target).Tools_SameRealm(player)
#endif
        )
        {
            partyInviteButton.gameObject.SetActive(true);
            partyInviteButton.interactable = (!player.party.InParty() || !player.party.party.IsFull()) &&
                                             !((Player)target).party.InParty() &&
                                             NetworkTime.time >= player.nextRiskyActionTime &&
                                             distance <= player.interactionRange;
            partyInviteButton.onClick.SetListener(() =>
            {
                player.party.CmdInvite(target.name);
            });
        }
        else partyInviteButton.gameObject.SetActive(false);

        selectTargetButton.onClick.SetListener(() =>
        {
            player.CmdSetTarget(target.target.netIdentity);
        });
    }

    // Controls all functionality for improved target.
    private void TargetControl(Player player, Entity target)
    {
        // Setup Elite
        if (target.isElite) eliteObject.SetActive(true);
        else eliteObject.SetActive(false);

        // Setup Boss
        if (target.isBoss) bossObject.SetActive(true);
        else bossObject.SetActive(false);

        // Setup Level Info
        levelText.gameObject.SetActive(true);
        challengeObject.SetActive(false);

        //si le niveau de la cible  et inférieur ou égal  
        int diff = (target.level.current - player.level.current);
        Color diffColor = GetColorForDiff(diff);

        levelText.color = diffColor;
        nameText.color = diffColor;

        if (target.isBoss) { levelText.color = bossColor; nameText.color = bossColor; } // violet
    }

    // Méthode pour obtenir la couleur correspondant à une différence donnée
    public Color GetColorForDiff(int diff)
    {
        // Initialiser une couleur par défaut au cas où aucune correspondance ne serait trouvée
        Color closestColor = Color.white;
        int minDifference = int.MaxValue; // Initialiser la différence minimale à la plus grande valeur possible

        // Parcourir la liste de paires de différences et de couleurs
        foreach (var pair in diffColorPairs)
        {
            // Si la différence correspond exactement à celle de la paire, retourner immédiatement la couleur de la paire
            if (diff == pair.diff)
            {
                return pair.color;
            }
            // Sinon, calculer la différence absolue entre la valeur demandée et celle de la paire
            int absoluteDiff = Mathf.Abs(diff - pair.diff);
            // Si cette différence absolue est inférieure à la plus petite différence enregistrée jusqu'à présent
            if (absoluteDiff < minDifference)
            {
                // Mettre à jour la différence minimale et la couleur correspondante
                minDifference = absoluteDiff;
                closestColor = pair.color;
            }
        }

        // Retourner la couleur correspondant à la différence la plus proche
        return closestColor;
    }


    [System.Serializable]
    public class DiffColorPair
    {
        public int diff;
        public Color color;
    }
}
