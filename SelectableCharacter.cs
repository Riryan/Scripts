
using UnityEngine;
using Mirror;

[RequireComponent(typeof(PlayerIndicator))]
[DisallowMultipleComponent]
public class SelectableCharacter : MonoBehaviour
{
    public int index = -1;
    void OnMouseDown()
    {
        ((NetworkManagerMMO)NetworkManager.singleton).selection = index;
        GetComponent<PlayerIndicator>().SetViaParent(transform);
    }

    void Update()
    {
        if (((NetworkManagerMMO)NetworkManager.singleton).selection != index)
        {
            GetComponent<PlayerIndicator>().Clear();
        }
    }
}
