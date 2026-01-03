using UnityEngine;
using Mirror;

namespace uMMORPG
{
    [DisallowMultipleComponent]
    public class Level : NetworkBehaviour
    {
        [SyncVar] public int current = 1;
        public int max = 1;

        protected override void OnValidate()
        {
            base.OnValidate();
            current = Mathf.Clamp(current, 1, max);
        }
    }
}