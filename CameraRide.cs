using UnityEngine;
using Mirror;

public class CameraRide : MonoBehaviour
{
    public float speed = 0.1f;

    void Update()
    {
        
        if (((NetworkManagerMMO)NetworkManager.singleton).state != NetworkState.Offline)
            Destroy(this);

        
        transform.position -= transform.forward * speed * Time.deltaTime;
    }
}
