using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class DefaultVelocity : MonoBehaviour
{
    [Header("Initial Rigidbody velocity (world space)")]
    public Vector3 velocity;

    Rigidbody rigidBody;

    void Awake()
    {
        rigidBody = GetComponent<Rigidbody>();
    }

    void Start()
    {
        rigidBody.linearVelocity = velocity;
    }
}
