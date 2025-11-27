using System;
using UnityEngine;
using Mirror;

[RequireComponent(typeof(NetworkNavMeshAgentRubberbanding))]
[DisallowMultipleComponent]
public class PlayerNavMeshMovement : NavMeshMovement
{
    [Header("Components")]
    public Player player;
    public NetworkNavMeshAgentRubberbanding rubberbanding;

    [Header("Camera")]
    public int mouseRotateButton = 1; 
    public float cameraDistance = 20;
    public float minDistance = 3;
    public float maxDistance = 20;
    public float zoomSpeedMouse = 1;
    public float zoomSpeedTouch = 0.2f;
    public float rotationSpeed = 2;
    public float xMinAngle = -40;
    public float xMaxAngle = 80;
    public Vector3 cameraOffset = Vector3.zero;
    public LayerMask viewBlockingLayers;
    
    [Header("Animation")]
    public float directionDampening = 0.05f;
    Vector3 rotation;
    bool rotationInitialized;
    Camera cam;

    [Header("Movement Feel")]
    [Tooltip("Time to ramp from 0 to full WASD speed.")]
    public float accelSeconds = 0.5f; // tweak 0.35–0.45 for 'a touch slower'
    float currentSpeed = 0f; // runtime
    [Header("Sprint")]
    public KeyCode sprintKey = KeyCode.LeftShift;

    [Tooltip("Speed multiplier while sprinting.")]
    public float sprintSpeedMultiplier = 1.5f;

    [Tooltip("How long sprint lasts (seconds).")]
    public float sprintDuration = 6f;

    [Tooltip("Cooldown between sprint uses (seconds).")]
    public float sprintCooldown = 90f;

    // runtime state
    bool isSprinting;
    float sprintEndTime;
    float nextSprintReadyTime;

    public override void Reset()
    {
        if (isServer)
            rubberbanding.ResetMovement();
        agent.ResetMovement();
    } 
    
    public override void Warp(Vector3 destination)
    {
        if (isServer)
            rubberbanding.RpcWarp(destination);
        agent.Warp(destination);
    }

    public override void OnStartLocalPlayer()
    {
        cam = Camera.main;
    }

    [ClientCallback]
    void UpdateAnimations()
    {
        foreach (Animator animator in GetComponentsInChildren<Animator>())
        {
            animator.SetFloat("DirZ", agent.velocity.magnitude, directionDampening, Time.deltaTime); 
            animator.SetBool("OnGround", true); 
        }
    }

    void Update()
    {
        if (isLocalPlayer)
        {
            // NEW: sprint input + timers
            UpdateSprint();

            if (player.IsMovementAllowed())
                MoveWASD();
            if (player.IsMovementAllowed() || player.state == "CASTING" || player.state == "STUNNED")
                MoveClick();
        }
        UpdateAnimations();
    }
    [Client]
    void UpdateSprint()
    {
        // expire current sprint
        if (isSprinting && Time.time >= sprintEndTime)
            isSprinting = false;

        // try to start a new sprint
        if (player.IsMovementAllowed() &&
            Input.GetKeyDown(sprintKey) &&
            Time.time >= nextSprintReadyTime)
        {
            isSprinting = true;
            sprintEndTime = Time.time + sprintDuration;

            // 90s between uses (from activation)
            nextSprintReadyTime = Time.time + sprintCooldown;
        }
    }

    [Client]
    void MoveWASD()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        if (horizontal != 0 || vertical != 0)
        {
            Vector3 input = new Vector3(horizontal, 0, vertical);
            if (input.magnitude > 1) input = input.normalized;
            Vector3 angles = cam.transform.rotation.eulerAngles;
            angles.x = 0;
            Quaternion rotation = Quaternion.Euler(angles);
            Vector3 direction = rotation * input;
            Debug.DrawLine(transform.position, transform.position + direction, Color.green, 0, false);
            agent.ResetMovement();

            //float targetSpeed = player.speed;
            float targetSpeed = player.speed * (isSprinting ? sprintSpeedMultiplier : 1f);

            
            if (accelSeconds <= 0.001f)
            {
                currentSpeed = targetSpeed; // effectively instant if set to ~0
            }
            else
            {
                float accelPerSec = targetSpeed / Mathf.Max(0.001f, accelSeconds);
                currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, accelPerSec * Time.deltaTime);
            }
            agent.velocity = direction * currentSpeed;
            LookAtY(transform.position + direction);
            player.useSkillWhenCloser = -1;
        }
        else
        {
            // keep instant stop (no decel for now)
            currentSpeed = 0f;
        }

    }

    [Client]
    void MoveClick()
    {
        if (Input.GetMouseButtonDown(0) &&
            !Utils.IsCursorOverUserInterface() &&
            Input.touchCount <= 1)
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            bool cast = player.localPlayerClickThrough
                        ? Utils.RaycastWithout(ray.origin, ray.direction, out hit, Mathf.Infinity, gameObject)
                        : Physics.Raycast(ray, out hit);
            if (cast)
            {
                if (!hit.transform.GetComponent<Entity>())
                {
                    Vector3 bestDestination = NearestValidDestination(hit.point);                
                    if (player.state == "CASTING" || player.state == "STUNNED")
                    {
                        player.pendingDestination = bestDestination;
                        player.pendingDestinationValid = true;
                    }
                    else Navigate(bestDestination, 0);
                }
            }
        }
    }

    void LateUpdate()
    {
        if (!isLocalPlayer) return;

        cam = Camera.main;
        Vector3 targetPos = transform.position + cameraOffset;

        if (!Utils.IsCursorOverUserInterface())
        {
            if (Input.mousePresent)
            {
                if (Input.GetMouseButton(mouseRotateButton))
                {
                    if (!rotationInitialized)
                    {
                        rotation = cam.transform.eulerAngles;
                        rotationInitialized = true;
                    }
                    rotation.y += Input.GetAxis("Mouse X") * rotationSpeed;
                    rotation.x -= Input.GetAxis("Mouse Y") * rotationSpeed;
                    rotation.x = Mathf.Clamp(rotation.x, xMinAngle, xMaxAngle);
                    cam.transform.rotation = Quaternion.Euler(rotation.x, rotation.y, 0);
                }
            }
            else
            {
                cam.transform.rotation = Quaternion.Euler(new Vector3(45, 0, 0));
            }

            float speed = Input.mousePresent ? zoomSpeedMouse : zoomSpeedTouch;
            float step = Utils.GetZoomUniversal() * speed;
            const float camRadius = 0.40f;  // camera collision radius
            const float camPadding = 0.05f;  // small air gap to avoid z-fighting
            float minAllowed = Mathf.Max(minDistance, camRadius + camPadding);
            cameraDistance = Mathf.Clamp(cameraDistance - step, minAllowed, maxDistance);
        }

        Vector3 desiredPos = targetPos - (cam.transform.rotation * Vector3.forward * cameraDistance);
        const float sphereRadius = 0.40f;  // keep in sync with camRadius above
        const float spherePadding = 0.05f;

        Vector3 toCam = desiredPos - targetPos;
        float dist = toCam.magnitude;

        if (dist > 0.001f)
        {
            Vector3 dir = toCam / dist;
            if (Physics.SphereCast(targetPos, sphereRadius, dir, out RaycastHit hit, dist,
                                   viewBlockingLayers, QueryTriggerInteraction.Ignore))
            {
                float safeDist = Mathf.Max(0f, hit.distance - sphereRadius - spherePadding);
                cam.transform.position = targetPos + dir * safeDist;
            }
            else
            {
                cam.transform.position = desiredPos;
            }

            if (Physics.CheckSphere(cam.transform.position, sphereRadius, viewBlockingLayers,
                                    QueryTriggerInteraction.Ignore))
            {
                if (Physics.SphereCast(cam.transform.position + dir * 0.01f, sphereRadius, -dir,
                                       out RaycastHit backHit, dist, viewBlockingLayers,
                                       QueryTriggerInteraction.Ignore))
                {
                    cam.transform.position = backHit.point + backHit.normal * (sphereRadius + spherePadding);
                }
            }
        }
        else
        {
            cam.transform.position = desiredPos;
        }
    }
    
    protected override void OnValidate()
    {
        base.OnValidate();
        Component[] components = GetComponents<Component>();
        if (Array.IndexOf(components, GetComponent<NetworkNavMeshAgentRubberbanding>()) >
            Array.IndexOf(components, this))
            Debug.LogWarning(name + "'s NetworkNavMeshAgentRubberbanding component is below the PlayerNavMeshMovement component. Please drag it above the Player component in the Inspector, otherwise there might be WASD movement issues due to the Update order.");
    }
}
