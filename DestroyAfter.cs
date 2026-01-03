// Destroys the GameObject after a certain time.
using UnityEngine;

namespace uMMORPG
{
    public class DestroyAfter : MonoBehaviour
    {
        public float time = 1;

        void Start()
        {
            Destroy(gameObject, time);
        }
    }
}