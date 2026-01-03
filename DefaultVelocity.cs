// Sets the Rigidbody's velocity in Start().
using UnityEngine;

namespace uMMORPG
{
    [RequireComponent(typeof(Rigidbody))]
    public class DefaultVelocity : MonoBehaviour
    {
        public Rigidbody rigidBody;
        public Vector3 velocity;

        void Start()
        {
            rigidBody.velocity = velocity;
        }
    }
}