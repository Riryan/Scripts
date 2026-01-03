using UnityEngine;
using System.Collections.Generic;

namespace uMMORPG
{
    public partial class UIBuffs : MonoBehaviour
    {
        public GameObject panel;
        public UIBuffSlot slotPrefab;

        // internal pooling (no public API change)
        readonly List<UIBuffSlot> slots = new List<UIBuffSlot>(16);
        readonly Stack<UIBuffSlot> pool = new Stack<UIBuffSlot>(16);

        void Update()
        {
            Player player = Player.localPlayer;
            if (player)
            {
                panel.SetActive(true);

                int buffCount = player.skills.buffs.Count;

                // ensure enough slots (reuse first, instantiate only if needed)
                while (slots.Count < buffCount)
                {
                    UIBuffSlot slot = GetSlot();
                    slot.transform.SetParent(panel.transform, false);
                    slots.Add(slot);
                }

                // disable extra slots (no destroy)
                for (int i = buffCount; i < slots.Count; ++i)
                {
                    slots[i].gameObject.SetActive(false);
                    pool.Push(slots[i]);
                }
                if (slots.Count > buffCount)
                    slots.RemoveRange(buffCount, slots.Count - buffCount);

                // refresh active buffs (unchanged behavior)
                for (int i = 0; i < buffCount; ++i)
                {
                    Buff buff = player.skills.buffs[i];
                    UIBuffSlot slot = slots[i];
                    slot.gameObject.SetActive(true);

                    slot.image.color = Color.white;
                    slot.image.sprite = buff.image;

                    // tooltip optimization preserved
                    if (slot.tooltip.IsVisible())
                        slot.tooltip.text = buff.ToolTip();

                    slot.slider.maxValue = buff.buffTime;
                    slot.slider.value = buff.BuffTimeRemaining();
                }
            }
            else
            {
                panel.SetActive(false);
            }
        }

        UIBuffSlot GetSlot()
        {
            if (pool.Count > 0)
                return pool.Pop();

            return Instantiate(slotPrefab);
        }
    }
}
