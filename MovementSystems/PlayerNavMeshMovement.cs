using System;
using UnityEngine;
using Mirror;

[RequireComponent(typeof(NetworkNavMeshAgentRubberbanding))]
[DisallowMultipleComponent]
public class PlayerNavMeshMovement : NavMeshMovement
{
    public enum MovementMode
    {
        Classic,  // WASD + Click-to-Move
        Action,   // WASD only, chase camera + mouse look
        ClickOnly // Click-to-Move only (no WASD)
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
    [Tooltip("Base mouse look sensitivity (higher = faster camera rotation).")]
    public float rotationSpeed = 1.0f;
    public float xMinAngle = -40;
    public float xMaxAngle = 80;
    public Vector3 cameraOffset = Vector3.zero;
    public LayerMask viewBlockingLayers;

    [Header("Control Mode")]
    [Tooltip("Classic: WASD + Click-to-Move. Action: WASD only. ClickOnly: Click-to-Move only.")]
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
    public float accelSeconds = 0.5f;
    float currentSpeed = 0f;

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

    // -----------------------------------------
    // Core movement hooks
    // -----------------------------------------

    public override void Warp(Vector3 destination)
    {
        if (rubberbanding == null)
            rubberbanding = GetComponent<NetworkNavMeshAgentRubberbanding>();

        rubberbanding.RpcWarp(destination);
        agent.Warp(destination);
    }

    public override void OnStartLocalPlayer()
    {
        cam = Camera.main;

        // Classic + ClickOnly: let NavMeshAgent rotate as usual.
        // Action: this script fully controls rotation.
        agent.updateRotation =
            (movementMode == MovementMode.Classic || movementMode == MovementMode.ClickOnly);

        // initialize camera orientation so it doesn't start off in editor's old spot
        if (cam != null)
        {
            rotation = new Vector3(20f, transform.eulerAngles.y, 0f); // slight downward angle
            rotationInitialized = true;
            cameraDistance = Mathf.Clamp(cameraDistance, minDistance, maxDistance);
        }
    }

    // required by abstract Movement
    public override void Reset()
    {
        agent.ResetMovement();
        currentSpeed = 0f;

        isSprinting = false;
        sprintEndTime = 0f;
        nextSprintReadyTime = Time.time;
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
            UpdateSprint();

            // WASD only in Classic or Action modes
            if (player.IsMovementAllowed() &&
                (movementMode == MovementMode.Classic || movementMode == MovementMode.Action))
            {
                MoveWASD();
            }

            // Click-to-Move in Classic and ClickOnly
            if ((movementMode == MovementMode.Classic || movementMode == MovementMode.ClickOnly) &&
                (player.IsMovementAllowed() || player.state == "CASTING" || player.state == "STUNNED"))
            {
                MoveClick();
            }
        }

        UpdateAnimations();
    }

    // -----------------------------------------
    // Sprint
    // -----------------------------------------

    [Client]
    void UpdateSprint()
    {
        if (isSprinting && Time.time >= sprintEndTime)
            isSprinting = false;

        if (player.IsMovementAllowed() &&
            Input.GetKeyDown(sprintKey) &&
            Time.time >= nextSprintReadyTime)
        {
            isSprinting = true;
            sprintEndTime = Time.time + sprintDuration;
            nextSprintReadyTime = Time.time + sprintCooldown;
        }
    }

    // -----------------------------------------
    // WASD movement
    // -----------------------------------------

    [Client]
    void MoveWASD()
    {
        // No WASD for ClickOnly mode.
        if (movementMode == MovementMode.ClickOnly)
        {
            currentSpeed = 0f;
            agent.velocity = Vector3.zero;
            return;
        }

        float horizontal = Input.GetAxis("Horizontal");
        float vertical   = Input.GetAxis("Vertical");

        // ---------- ACTION MODE ----------
        if (movementMode == MovementMode.Action)
        {
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

                // velocity is along +/- forward; rotation is fully controlled by this script
                agent.velocity = moveDir.normalized * currentSpeed;

                // keep facing forward (S = backpedal, no spin)
                LookAtY(transform.position + transform.forward);

                player.useSkillWhenCloser = -1;
            }
            else
            {
                currentSpeed = 0f;
                agent.velocity = Vector3.zero;
            }

            return; // done for Action mode
        }

        // ---------- CLASSIC MODE ----------
        if (horizontal != 0 || vertical != 0)
        {
            Vector3 input = new Vector3(horizontal, 0, vertical);
            if (input.magnitude > 1) input = input.normalized;

            // camera-relative WASD (matches original behavior)
            Vector3 angles = cam.transform.rotation.eulerAngles;
            angles.x = 0;
            Quaternion camYaw = Quaternion.Euler(angles);
            Vector3 direction = camYaw * input;

            agent.ResetMovement();

            float targetSpeed = player.speed * (isSprinting ? sprintSpeedMultiplier : 1f);
            if (accelSeconds <= 0.001f)
                currentSpeed = targetSpeed;
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
            currentSpeed = 0f;
            agent.velocity = Vector3.zero;
        }
    }

    // -----------------------------------------
    // Click-to-move (Classic + ClickOnly)
    // -----------------------------------------

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

            if (cast && !hit.transform.GetComponent<Entity>())
            {
                Vector3 bestDestination = NearestValidDestination(hit.point);

                if (player.state == "CASTING" || player.state == "STUNNED")
                {
                    player.pendingDestination = bestDestination;
                    player.pendingDestinationValid = true;
                }
                else
                {
                    Navigate(bestDestination, 0);
                }
            }
        }
    }

    // -----------------------------------------
    // Camera
    // -----------------------------------------

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

                // cursor lock for Action mode mouse-look
                if (movementMode == MovementMode.Action && actionModeLockCursor)
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
                else if (movementMode == MovementMode.Action)
                {
                    // Only chase behind player if actually moving.
                    if (agent.velocity.sqrMagnitude > 0.001f)
                    {
                        float targetYaw = transform.eulerAngles.y;
                        rotation.y = Mathf.LerpAngle(
                            rotation.y,
                            targetYaw,
                            actionCameraFollowSpeed * Time.deltaTime
                        );
                    }
                }

                cam.transform.rotation = Quaternion.Euler(rotation.x, rotation.y, 0);
            }

            float speed = Input.mousePresent ? zoomSpeedMouse : zoomSpeedTouch;
            float step = Utils.GetZoomUniversal() * speed;

            const float camRadius = 0.40f;
            const float camPadding = 0.05f;
            float minAllowed = Mathf.Max(minDistance, camRadius + camPadding);
            cameraDistance = Mathf.Clamp(cameraDistance - step, minAllowed, maxDistance);
        }
        else
        {
            if (movementMode == MovementMode.Action && actionModeLockCursor)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        Vector3 desiredPos = targetPos - (cam.transform.rotation * Vector3.forward * cameraDistance);
        const float sphereRadius = 0.40f;
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
                if (Physics.SphereCast(cam.transform.position + dir * 0.01f, sphereRadius, -dir,
                                       out RaycastHit backHit, dist, viewBlockingLayers,
                                       QueryTriggerInteraction.Ignore))
                {
                    cam.transform.position = backHit.point + backHit.normal * (sphereRadius + spherePadding);
                }
                else
                {
                    cam.transform.position = desiredPos;
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
        {
            Debug.LogWarning(name + "'s NetworkNavMeshAgent... there might be WASD movement issues due to the Update order.");
        }
    }
}
