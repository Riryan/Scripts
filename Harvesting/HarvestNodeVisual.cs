using Mirror;
using UnityEngine;

public class HarvestNodeVisual : MonoBehaviour
{
    public int cellX, cellY, prototypeId, localIndex;
    public GameObject whole, stump;

    public void SetState(bool harvested)
    {
        if (whole) whole.SetActive(!harvested);
        if (stump) stump.SetActive(harvested);
    }

    // Simple click-to-harvest
    void OnMouseDown()
    {
        if (!NetworkClient.active) return;
        NetworkClient.Send(new HarvestInteractRequest {
            cellX = cellX, cellY = cellY, prototypeId = prototypeId, localIndex = localIndex
        });
    }
}
