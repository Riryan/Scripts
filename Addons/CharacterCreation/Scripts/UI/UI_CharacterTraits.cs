using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public partial class UI_CharacterTraits : MonoBehaviour
{
#if _iMMOTRAITS

    
    public UI_CharacterCreation parentPanel;
    public Text traitPointsText;
    public GameObject slotPrefab;
    public Transform content;

    protected List<TraitTemplate> traitPool;
    protected int traitPoints;
    protected int classIndex;
    [HideInInspector] public List<TraitTemplate> currentTraits;


    // -----------------------------------------------------------------------------------
    // Show
    // -----------------------------------------------------------------------------------
    public void Show()
    {
        Player player = parentPanel.currentPlayer;
        if (!player) return;

        currentTraits.Clear();
        currentTraits = new List<TraitTemplate>();
        traitPool = new List<TraitTemplate>();
        traitPool.AddRange(TraitTemplate.dict.Values.ToList());

        if (player.playerTraits) 
        { 
            traitPoints = player.playerTraits.TraitPoints;

            traitPointsText.text = traitPoints.ToString() + "/" + player.playerTraits.TraitPoints.ToString();
        }
        gameObject.SetActive(true);
    }

    // -----------------------------------------------------------------------------------
    // Update
    // -----------------------------------------------------------------------------------
    private void Update()
    {
        if (gameObject.activeSelf && parentPanel.gameObject.activeSelf)
        {
            Player player = parentPanel.currentPlayer;
            if (!player) return;

            if (player.playerTraits)
            {
                // -----
                traitPointsText.text = traitPoints.ToString() + "/" + player.playerTraits.TraitPoints.ToString();

                // ----- update trait list
                UIUtils.BalancePrefabs(slotPrefab.gameObject, traitPool.Count, content);

                for (int i = 0; i < traitPool.Count; ++i)
                {
                    int icopy = i;

                    UI_TraitSlot slot = content.GetChild(icopy).GetComponent<UI_TraitSlot>();
                    TraitTemplate tmpl = traitPool[icopy];

                    bool bAllowed = true;

                    // -- can afford
                    if (traitPoints < tmpl.traitCost) bAllowed = false;

                    // -- not unique, not taken?
                    if (currentTraits.Any(x => x.uniqueGroup == tmpl.uniqueGroup)) bAllowed = false;

                    // -- not limited by class?
                    if (tmpl.allowedClasses.Length > 0 && !tmpl.allowedClasses.Any(x => x.name == player.gameObject.name)) bAllowed = false;

                    // -- enable/disable button add/remove trait
                    slot.button.interactable = ((currentTraits.Any(x => x == tmpl)) || bAllowed);

                    // -- trait exists already?
                    slot.image.color = ((currentTraits.Any(x => x == tmpl)) ? Color.black : Color.white);

                    // -- set/unset trait per click
                    slot.button.onClick.SetListener(() =>
                    {
                        if (currentTraits.Any(x => x == tmpl))
                        {
                            currentTraits.Remove(tmpl);
                            traitPoints += tmpl.traitCost;
                        }
                        else
                        {
                            currentTraits.Add(tmpl);
                            traitPoints -= tmpl.traitCost;
                        }
                    });

                    slot.image.sprite = tmpl.image;
                    slot.tooltip.enabled = true;
                    slot.tooltip.text = tmpl.ToolTip();

                    slot.gameObject.SetActive(((currentTraits.Any(x => x == tmpl)) || bAllowed));
                }

            }
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    // -----------------------------------------------------------------------------------
    // Hide
    // -----------------------------------------------------------------------------------
    public void Hide()
    {
        gameObject.SetActive(false);
    }

    // -----------------------------------------------------------------------------------
#endif
}