using UnityEngine;

namespace uMMORPG
{
    public class FakePlayerDriver : MonoBehaviour
    {
        Player player;
        float nextAction;

        void Awake()
        {
            player = GetComponent<Player>();
            nextAction = Time.time + Random.Range(0.5f, 2f);
        }

        void Update()
        {
            if (Time.time < nextAction)
                return;

            nextAction = Time.time + Random.Range(0.2f, 2f);

            SimulateMovement();
            SimulateCombat();
        }

        void SimulateMovement()
        {
            Vector3 dir = Random.insideUnitSphere;
            dir.y = 0;
            player.transform.position += dir * Random.Range(0.1f, 1.5f);
        }

        void SimulateCombat()
        {
            if (Random.value < 0.15f)
            {
                // lightweight server-side pressure
                player.experience.current += Random.Range(1, 5);
            }
        }
    }
}
