using UnityEngine;
using Mirror;

public class InteractionRaycastDebug : NetworkBehaviour
{
    public float range = 4f;

    void Update()
    {
        if (!isLocalPlayer) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, range, ~0, QueryTriggerInteraction.Collide))
        {
            Debug.DrawLine(ray.origin, hit.point, Color.green);
            Debug.Log($"[InteractionRaycastDebug] Hit: {hit.collider.name} on layer {LayerMask.LayerToName(hit.collider.gameObject.layer)}");
        }
        else
        {
            Debug.DrawRay(ray.origin, ray.direction * range, Color.red);
        }
    }
}
