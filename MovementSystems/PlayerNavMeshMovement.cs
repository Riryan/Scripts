using System;
using UnityEngine;
using Mirror;

[RequireComponent(typeof(NetworkNavMeshAgentRubberbanding))]
[DisallowMultipleComponent]
public class PlayerNavMeshMovement : NavMeshMovement
{
    public enum MovementMode
    {
        Classic,   // your original: WASD + Click-to-Move
        Action,    // WASD only, chase camera + mouse look
        ClickOnly  // Click-to-Move only (no WASD)
    }

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

    [Header("Control Mode")]
    [Tooltip("Classic: WASD + Click. Action: WASD only. ClickOnly: Click-only.")]
    public MovementMode movementMode = MovementMode.Classic;

    [Header("Action Camera")]
    [Tooltip("How fast the camera yaw chases the player yaw in Action mode when not rotating with the mouse.")]
    public float actionCameraFollowSpeed = 5f;
    [Tooltip("Lock and hide the cursor while rotating camera in Action mode.")]
    public bool actionModeLockCursor = true;

    [Header("Action Movement")]
    [Tooltip("Degrees per second the character turns when pressing A/D in Action mode.")]
    public float actionTurnSpeed = 180f;

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

        // safe client-side resets
        currentSpeed = 0f;
        isSprinting = false;
        sprintEndTime = 0f;
        nextSprintReadyTime = Time.time;
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

        // Classic & ClickOnly: keep default NavMeshAgent rotation.
        // Action: we control rotation ourselves.
        agent.updateRotation = (movementMode != MovementMode.Action);
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
            // sprint input + timers (same for all modes)
            UpdateSprint();

            // WASD only in Classic or Action
            if (player.IsMovementAllowed() &&
                (movementMode == MovementMode.Classic || movementMode == MovementMode.Action))
            {
                MoveWASD();
            }

            // Click-to-move in Classic and ClickOnly
            if ((movementMode == MovementMode.Classic || movementMode == MovementMode.ClickOnly) &&
                (player.IsMovementAllowed() || player.state == "CASTING" || player.state == "STUNNED"))
            {
                MoveClick();
            }
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

    // ----------------- MOVEMENT -----------------

    [Client]
    void MoveWASD()
    {
        switch (movementMode)
        {
            case MovementMode.Classic:
                MoveWASD_Classic();
                break;
            case MovementMode.Action:
                MoveWASD_Action();
                break;
            default:
                // ClickOnly: no WASD
                currentSpeed = 0f;
                agent.velocity = Vector3.zero;
                break;
        }
    }

    // === Classic WASD: EXACTLY your original implementation ===
    [Client]
    void MoveWASD_Classic()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        if (horizontal != 0 || vertical != 0)
        {
            Vector3 input = new Vector3(horizontal, 0, vertical);
            if (input.magnitude > 1) input = input.normalized;

            Vector3 angles = cam.transform.rotation.eulerAngles;
            angles.x = 0;
            Quaternion rot = Quaternion.Euler(angles);
            Vector3 direction = rot * input;

            Debug.DrawLine(transform.position, transform.position + direction, Color.green, 0, false);
            agent.ResetMovement();

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

    // === Action WASD: character-facing, A/D turn, W/S forward/back ===
    [Client]
    void MoveWASD_Action()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical   = Input.GetAxis("Vertical");

        // A/D: turn character yaw
        if (Mathf.Abs(horizontal) > 0.01f)
        {
            float yaw = transform.eulerAngles.y + horizontal * actionTurnSpeed * Time.deltaTime;
            transform.rotation = Quaternion.Euler(0, yaw, 0);
        }

        // W/S: move along facing (forward/back)
        Vector3 moveDir = transform.forward * vertical;

        if (moveDir.sqrMagnitude > 0.001f)
        {
            agent.ResetMovement();

            float targetSpeed = player.speed * (isSprinting ? sprintSpeedMultiplier : 1f);
            if (accelSeconds <= 0.001f)
                currentSpeed = targetSpeed;
            else
            {
                float accelPerSec = targetSpeed / Mathf.Max(0.001f, accelSeconds);
                currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, accelPerSec * Time.deltaTime);
            }

            // NavMeshAgent rotation is OFF in Action mode; we fully control facing.
            agent.velocity = moveDir.normalized * currentSpeed;

            // S = backpedal, always face forward; no spin / jitter
            LookAtY(transform.position + transform.forward);

            player.useSkillWhenCloser = -1;
        }
        else
        {
            currentSpeed = 0f;
            agent.velocity = Vector3.zero;
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

    // ----------------- CAMERA -----------------

    void LateUpdate()
    {
        if (!isLocalPlayer) return;

        cam = Camera.main;
        if (cam == null) return;

        Vector3 targetPos = transform.position + cameraOffset;

        if (!Utils.IsCursorOverUserInterface())
        {
            if (Input.mousePresent)
            {
                bool rotatingWithMouse = Input.GetMouseButton(mouseRotateButton);

                if (movementMode == MovementMode.Action)
                {
                    // Action: mouse-look + optional cursor lock + chase camera

                    if (actionModeLockCursor)
                    {
                        if (rotatingWithMouse)
                        {
                            Cursor.lockState = CursorLockMode.Locked;
                            Cursor.visible = false;
                        }
                        else
                        {
                            Cursor.lockState = CursorLockMode.None;
                            Cursor.visible = true;
                        }
                    }

                    if (!rotationInitialized)
                    {
                        rotation = cam.transform.eulerAngles;
                        rotationInitialized = true;
                    }

                    if (rotatingWithMouse)
                    {
                        float mouseX = Input.GetAxis("Mouse X");
                        float mouseY = Input.GetAxis("Mouse Y");

                        rotation.y += mouseX * rotationSpeed;
                        rotation.x -= mouseY * rotationSpeed;
                        rotation.x = Mathf.Clamp(rotation.x, xMinAngle, xMaxAngle);
                    }
                    else
                    {
                        // Only chase behind player if actually moving
                        if (agent.velocity.sqrMagnitude > 0.001f)
                        {
                            float targetYaw = transform.eulerAngles.y;
                            rotation.y = Mathf.LerpAngle(rotation.y, targetYaw,
                                actionCameraFollowSpeed * Time.deltaTime);
                        }
                    }

                    cam.transform.rotation = Quaternion.Euler(rotation.x, rotation.y, 0);
                }
                else
                {
                    // Classic / ClickOnly: ORIGINAL camera behavior
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
        else
        {
            // ensure cursor unlocked if UI is open in Action mode
            if (movementMode == MovementMode.Action && actionModeLockCursor)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
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
