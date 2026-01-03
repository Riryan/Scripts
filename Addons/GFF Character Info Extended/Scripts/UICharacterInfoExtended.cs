using UnityEngine;
using UnityEngine.UI;

namespace uMMORPG
{
    public enum Attributes { none, original, extended }

    public partial class UICharacterInfoExtended : MonoBehaviour
    {
        [Header("Settings: general info")]
        [SerializeField] private bool useRaceClassGenderAddon;

        [Header("Settings: status info")]
        [SerializeField] private bool useDamageMinMax = false;
        [SerializeField] private bool useAccuracyAndDodge = false;
        [SerializeField] private bool useStunChance = false;
        //[SerializeField] private bool useVampirism = false;

        [Header("Settings: Attributes")]
        [SerializeField] private Attributes attributes;

        [Header("UI Elements")]
        [SerializeField] private GameObject panel;

        [Header("Panels : general info")]
        [SerializeField] private GameObject StatsContent;
        [SerializeField] private Text levelText;
        [SerializeField] private Text currentExperienceText;
        [SerializeField] private Text maximumExperienceText;
        [SerializeField] private Text skillExperienceText;
        [SerializeField] private Text skillExperienceTextValue;

        [SerializeField] private GameObject panelCharacterCreateExtended;
        [SerializeField] private Text raceText;
        [SerializeField] private Text raceTextValue;
        [SerializeField] private Text classText;
        [SerializeField] private Text classTextValue;
        [SerializeField] private Text genderText;
        [SerializeField] private Text genderTextValue;
        [SerializeField] private Text specializationText;
        [SerializeField] private Text specializationTextValue;

        [Header("Panels : status info")]
        [SerializeField] private Text damageText;
        [SerializeField] private Text defenseText;

        [SerializeField] private Text accuracyText;
        [SerializeField] private Text accuracyTextValue;
        [SerializeField] private Text dodgeText;
        [SerializeField] private Text dodgeTextValue;

        [SerializeField] private Text moveSpeedTextValue;
        [SerializeField] private Text attackRangeText;
        [SerializeField] private Text attackRangeTextValue;
        [SerializeField] private Text attackSpeedText;
        [SerializeField] private Text attackSpeedTextValue;

        [SerializeField] private Text healthText;
        [SerializeField] private Text manaText;
        [SerializeField] private Text staminaText;
        [SerializeField] private Text staminaTextValue;

        [SerializeField] private Text blockChanceValue;
        [SerializeField] private Text criticalChanceValue;
        [SerializeField] private Text stunChance;
        [SerializeField] private Text stunChanceValue;

        [SerializeField] private Text reduceBlockChance;
        [SerializeField] private Text reduceBlockChanceValue;
        [SerializeField] private Text reduceCriticalChance;
        [SerializeField] private Text reduceCriticalChanceValue;
        [SerializeField] private Text reduceStunChance;
        [SerializeField] private Text reduceStunChanceValue;

        //[SerializeField] private Text textVampirism;
        //[SerializeField] private Text textVampirismValue;

        [SerializeField] private Text textWeight;
        [SerializeField] private Text textWeightValue;

        [Header("Panels : Attributes")]
        [SerializeField] private GameObject panelAttributes;
        [SerializeField] private GameObject attributesContent;
        [SerializeField] private Button buttonResetAttributes;
        [SerializeField] private Text textAttributesAvailable;
        [SerializeField] private GameObject attributesContentForPrefabs;
        [SerializeField] private GameObject attributesPrefab;

        [Header("Panels : Combat Skills")]
        [SerializeField] private GameObject panelCombatSkills;
        [SerializeField] private GameObject combatSkillsContent;
        [SerializeField] private Transform combatSkillsContentForPrefabs;
        [SerializeField] private GameObject combatSkillsPrefab;

        [Header("Panels : Gathering Skills")]
        [SerializeField] private GameObject panelGatheringSkills;
        [SerializeField] private GameObject gatheringSkillsContent;
        [SerializeField] private Text textGatheringTime;
        [SerializeField] private Transform gatheringSkillsContentForPrefabs;
        [SerializeField] private GameObject gatheringSkillsPrefab;

        [Header("Panels : Crafting Skills")]
        [SerializeField] private GameObject panelCraftingSkills;
        [SerializeField] private GameObject craftingSkillsContent;
        [SerializeField] private Transform craftingSkillsContentForPrefabs;
        [SerializeField] private GameObject craftingSkillsPrefab;

        [Header("Panels : Elemental Resistance")]
        [SerializeField] private GameObject panelElementalResistance;
        [SerializeField] private GameObject elementalResistanceContent;
        [SerializeField] private Transform elementalResistanceContentForPrefabs;
        [SerializeField] private GameObject elementalResistancePrefab;

        [Header("Panels : Race Rank")]
        [SerializeField] private GameObject panelRaceRank;
        [SerializeField] private GameObject raceRankContent;
        [SerializeField] private Text textRaceRankValue;
        [SerializeField] private Text textRankPointsValue;
        [SerializeField] private Text textRankBonusesValue;

        [Header("Panels : Race Leader")]
        [SerializeField] private GameObject panelRaceLeader;
        [SerializeField] private GameObject RaceLeaderContent;
        [SerializeField] private Text raceLeaderValue;
        [SerializeField] private Text LeaderBonusesValue;

        [Header("Panels : Mounts")]
        [SerializeField] private GameObject panelMounts;
        [SerializeField] private GameObject MountsContent;
        [SerializeField] private Text mountName;
        [SerializeField] private Image imageGender;
        [SerializeField] private Slider sliderHP;
        [SerializeField] private Slider sliderSP;
        [SerializeField] private Slider sliderExp;
        [SerializeField] private Text sliderHPValue;
        [SerializeField] private Text sliderSPValue;
        [SerializeField] private Text sliderExpValue;
        [SerializeField] private Text mountLevel;
        [SerializeField] private Text mountRang;

        // remember default attributes header text so we can append "(remaining)"
        private string attributesTextDefault;

        private void Awake()
        {
            attributesTextDefault = textAttributesAvailable.text;
        }

        private void Start()
        {
            panelCharacterCreateExtended.SetActive(useRaceClassGenderAddon);

            //accuracy and dodge
            accuracyText.gameObject.SetActive(useAccuracyAndDodge);
            accuracyTextValue.gameObject.SetActive(useAccuracyAndDodge);
            dodgeText.gameObject.SetActive(useAccuracyAndDodge);
            dodgeTextValue.gameObject.SetActive(useAccuracyAndDodge);

            //use stun Chance
            stunChance.gameObject.SetActive(useStunChance);
            stunChanceValue.gameObject.SetActive(useStunChance);

#if GFF_Addons_Stamina
            staminaText.gameObject.SetActive(true);
            staminaTextValue.gameObject.SetActive(true);
#else
            staminaText.gameObject.SetActive(false);
            staminaTextValue.gameObject.SetActive(false);
#endif

            //attributes
            panelAttributes.SetActive(attributes != Attributes.none);

            // addon system hooks
            Utils.InvokeMany(typeof(UICharacterInfoExtended), this, "Start_");
        }

        private void Update()
        {
            // only refresh the panel while it's active
            if (panel.activeSelf)
            {
                Player player = Player.localPlayer;
                if (player)
                {
                    levelText.text = player.level.current.ToString();
                    currentExperienceText.text = player.experience.current.ToString();
                    maximumExperienceText.text = player.experience.max.ToString();
                    skillExperienceTextValue.text = ((PlayerSkills)player.skills).skillExperience.ToString();

                    healthText.text = player.health.max.ToString();
                    manaText.text = player.mana.max.ToString();

                    defenseText.text = player.combat.defense.ToString();

                    if (!useDamageMinMax) damageText.text = player.combat.damage.ToString();

                    moveSpeedTextValue.text = player.speed.ToString();

                    blockChanceValue.text = (player.combat.blockChance * 100).ToString("F0") + "%";
                    criticalChanceValue.text = (player.combat.criticalChance * 100).ToString("F0") + "%";

                    // addon system hooks
                    Utils.InvokeMany(typeof(UICharacterInfoExtended), this, "Update_", player);

                    if (attributes == Attributes.original)
                    {
                        // attributes (show spendable if >1 so it's more obvious)
                        // (each Attribute component has .PointsSpendable. can use any.)
                        int spendable = player.strength.PointsSpendable();
                        string suffix = "";
                        if (spendable > 0)
                            suffix = " (" + player.strength.PointsSpendable() + ")";
                        textAttributesAvailable.text = "Available " + attributesTextDefault + suffix;

                        // instantiate/destroy enough slots
                        UIUtils.BalancePrefabs(attributesPrefab, 2, attributesContentForPrefabs.transform);

                        //strength
                        UIAttributesExtendedSlot slot0 = attributesContentForPrefabs.transform.GetChild(0).GetComponent<UIAttributesExtendedSlot>();
                        slot0.textName.text = "Strength";
                        slot0.textValue.text = player.strength.value.ToString();
                        slot0.button.interactable = player.strength.PointsSpendable() > 0;
                        slot0.button.onClick.SetListener(() => { player.strength.CmdIncrease(); });

                        //intelligence
                        UIAttributesExtendedSlot slot1 = attributesContentForPrefabs.transform.GetChild(1).GetComponent<UIAttributesExtendedSlot>();
                        slot1.textName.text = "Intelligence";
                        slot1.textValue.text = player.intelligence.value.ToString();
                        slot1.button.interactable = player.intelligence.PointsSpendable() > 0;
                        slot1.button.onClick.SetListener(() => { player.intelligence.CmdIncrease(); });
                    }
                }
                else panel.SetActive(false);
            }
        }
    }
}

