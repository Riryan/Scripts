
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class DefaultVelocity : MonoBehaviour
{
    public Rigidbody rigidBody;
    public Vector3 velocity;

    void Start()
    {
        rigidBody.linearVelocity = velocity;
    }
}
