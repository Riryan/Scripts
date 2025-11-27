using UnityEngine;
using Mirror;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
public class Area_InvisibleHint : MonoBehaviour
{
    [Tooltip("[Optional] Toggle this hint zone on/off without deleting it")]
    public bool isActive = true;

    [Tooltip("[Required] Text to display while in the area"), TextArea(1, 30)]
    public string textToDisplay = "";

    [Tooltip("[Optional] Auto-hide after X seconds (0 = only hide on exit)")]
    public float hideAfter = 0f;

#if !UNITY_SERVER || UNITY_EDITOR
    BoxCollider box;

    void Awake()
    {
        box = GetComponent<BoxCollider>();
        if (box != null) box.isTrigger = true;
    }

    void OnValidate()
    {
        var bc = GetComponent<BoxCollider>();
        if (bc != null) bc.isTrigger = true;
    }

    [ClientCallback]
    void OnTriggerEnter(Collider other)
    {
        if (!isActive || string.IsNullOrWhiteSpace(textToDisplay)) return;

        var player = other.GetComponentInParent<Player>();
        if (player != null && player == Player.localPlayer)
            player.InvisibleHint_Show(textToDisplay, hideAfter);
    }

    [ClientCallback]
    void OnTriggerExit(Collider other)
    {
        var player = other.GetComponentInParent<Player>();
        if (player != null && player == Player.localPlayer)
            player.InvisibleHint_Hide();
    }
#endif
}
