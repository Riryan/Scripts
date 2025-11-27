
using UnityEngine;

public class DestroyAfter : MonoBehaviour
{
    public float time = 1;

    void Start()
    {
        Destroy(gameObject, time);
    }
}
