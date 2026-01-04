using UnityEngine;
using Mirror;

namespace uMMORPG
{
    public partial class Player
    {
        [Header("[-=-=-[ Display your name ]-=-=-]")]
        public bool showName;
        [SerializeField] private GameObject nameOverlayPosition = null;

        [Header("[-=-=-[ Display your Guild ]-=-=-]")]
        public bool showGuild;
        [SerializeField] private GameObject guildOverlayPosition = null;

#if _iMMOTITLES
        [Header("[-=-=-[ Display your Title ]-=-=-]")]
        public bool showTitle;
        [SerializeField] private GameObject titleOverlayPosition = null;
#endif

#if _iMMOSTATSOVERLAY
        [Header("[-=-=-[ Display your healthbar ]-=-=-]")]
        public bool showHealbar;
        [SerializeField] private GameObject HealbarOverlayPosition = null;
#endif

        void InitDisplayOverlays()
        {
            if (nameOverlayPosition != null)
                nameOverlayPosition.SetActive(showName);

            if (guildOverlayPosition != null)
                guildOverlayPosition.SetActive(showGuild);

#if _iMMOTITLES
            if (titleOverlayPosition != null)
                titleOverlayPosition.SetActive(showTitle);
#endif

#if _iMMOSTATSOVERLAY
            if (HealbarOverlayPosition != null)
                HealbarOverlayPosition.SetActive(showHealbar);
#endif
        }
    }
}
