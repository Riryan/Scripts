


using UnityEngine;
using Mirror;

[DisallowMultipleComponent]
public class PlayerMountControl : NetworkBehaviour
{
    [Header("Mount")]
    public Transform meshToOffsetWhenMounted;
    public float seatOffsetY = -1;

    
    [SyncVar, HideInInspector] public Mount activeMount;

    void LateUpdate()
    {
        
        
        ApplyMountSeatOffset();
    }

    public bool IsMounted()
    {
        return activeMount != null && activeMount.health.current > 0;
    }

    void ApplyMountSeatOffset()
    {
        if (meshToOffsetWhenMounted != null)
        {
            
            if (activeMount != null && activeMount.health.current > 0)
                meshToOffsetWhenMounted.transform.position = activeMount.seat.position + Vector3.up * seatOffsetY;
            else
                meshToOffsetWhenMounted.transform.localPosition = Vector3.zero;
        }
    }
}
