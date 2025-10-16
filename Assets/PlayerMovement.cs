// using Unity.Mathematics;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Input")]
    public float h;
    public float v;

    [Header("Movement")]
    public float speed = 2.0f;
    [Tooltip("Multiplier applied to speed while sprinting.")]
    public float sprintMultiplier = 1.8f;
    [Tooltip("Impulse force applied to Rigidbody when jumping.")]
    public float jumpForce = 5f;
    [SerializeField] float aimRotationSpeed = 15f;

    [Header("Ground")]
    [Range(0f, 1.5f)] public float distanceToGround = 0.1f;
    public LayerMask groundLayers;
    public Transform groundCheck; // optional, set to foot pivot

    [Header("Animator parameter names (case-sensitive)")]
    public string walkBool = "walk";    // prefer this if exists
    public string altWalkBool = "jalan";
    public string runBool = "lari";
    public string jumpBool = "lompat";
    public string speedFloat = "Speed";
    public string altSpeedFloat = "speed";

    [Header("Tuning")]
    public float animatorSmoothTime = 0.12f;
    public float walkThreshold = 0.05f;

    Animator anim;
    Rigidbody rb;
    Collider bodyCollider;

    bool isSprinting;
    bool isGrounded;
    bool jumpRequested;
    bool wasGrounded;
    Vector3 desiredVelocity = Vector3.zero;

    Quaternion targetRotation;

    void Awake()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        TryGetComponent(out bodyCollider);

        if (rb != null)
        {
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            // freeze X/Z rotation so model doesn't topple
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }

        if (anim != null)
        {
            anim.applyRootMotion = false;
            // debug info to help verify parameter names at runtime
            Debug.Log($"Animator found: {anim.name} controller: {(anim.runtimeAnimatorController ? anim.runtimeAnimatorController.name : "null")}");
            foreach (var p in anim.parameters) Debug.Log($"Animator param: {p.name} ({p.type})");
        }

        targetRotation = transform.rotation;
    }

    void Update()
    {
        isGrounded = CheckGrounded();

        // read input
        h = Input.GetAxis("Horizontal");
        v = Input.GetAxis("Vertical");

        bool isAiming = Input.GetMouseButton(1);
        if (anim != null && HasBoolParameter(anim, "aim")) anim.SetBool("aim", isAiming);

        bool isMovingInput = !Mathf.Approximately(h, 0f) || !Mathf.Approximately(v, 0f);

        // rotation: free-turn to camera direction when moving & not aiming; slerp when aiming
        if (!isAiming && isMovingInput && Camera.main != null)
        {
            Vector3 targetDirection = new Vector3(h, 0f, v);
            targetDirection = Camera.main.transform.TransformDirection(targetDirection);
            targetDirection.y = 0f;
            if (targetDirection.sqrMagnitude > 0.0001f)
                targetRotation = Quaternion.LookRotation(targetDirection, Vector3.up);
        }
        else if (isAiming && Camera.main != null)
        {
            Vector3 aimDirection = Camera.main.transform.forward;
            aimDirection.y = 0f;
            if (aimDirection.sqrMagnitude > 0.0001f)
                targetRotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(aimDirection.normalized, Vector3.up), aimRotationSpeed * Time.deltaTime);
        }

        float currentSpeed = HandleSprint(isMovingInput);

        // movement relative to camera
        Vector3 moveForward = Camera.main ? Vector3.ProjectOnPlane(Camera.main.transform.forward, Vector3.up).normalized : Vector3.forward;
        Vector3 moveRight = Camera.main ? Vector3.ProjectOnPlane(Camera.main.transform.right, Vector3.up).normalized : Vector3.right;
        Vector3 moveInput = (moveForward * v) + (moveRight * h);
        if (moveInput.sqrMagnitude > 1f) moveInput.Normalize();
        desiredVelocity = moveInput * currentSpeed;

        // jump input (mark for FixedUpdate)
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            jumpRequested = true;
            SetBoolIfExists(anim, jumpBool, true);
            // try trigger if they used a trigger instead
            if (anim != null && HasTriggerParameter(anim, "Jump")) anim.SetTrigger("Jump");
        }

        // reset jump bool when grounded and animation finished
        if (isGrounded && !jumpRequested)
        {
            SetBoolIfExists(anim, jumpBool, false);
        }

        // animator driving: prefer speed float normalized by base speed, otherwise use bools
        UpdateAnimatorParameters();
        wasGrounded = isGrounded;
    }

    void FixedUpdate()
    {
        if (rb == null) return;

        // apply horizontal velocity directly while preserving vertical velocity
        Vector3 horizontal = new Vector3(desiredVelocity.x, 0f, desiredVelocity.z);
        Vector3 nextVel = new Vector3(horizontal.x, rb.velocity.y, horizontal.z);
        rb.velocity = nextVel;

        // apply rotation using physics-friendly MoveRotation
        if (targetRotation != null)
        {
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, 10f * Time.fixedDeltaTime));
        }

        // jump impulse
        if (jumpRequested)
        {
            Vector3 vel = rb.velocity;
            vel.y = 0f;
            rb.velocity = vel;
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            jumpRequested = false;
            isGrounded = false;
        }
    }

    float HandleSprint(bool isMoving)
    {
        bool sprintKeyHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        isSprinting = isMoving && sprintKeyHeld;
        bool wantsWalk = isMoving && !isSprinting;

        SetBoolIfExists(anim, runBool, isSprinting);
        SetBoolIfExists(anim, walkBool, wantsWalk);

        return isSprinting ? speed * sprintMultiplier : speed;
    }

    void UpdateAnimatorParameters()
    {
        if (anim == null || rb == null) return;

        Vector3 horizontalVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        float actualSpeed = horizontalVel.magnitude;
        float normalized = Mathf.Clamp01(actualSpeed / Mathf.Max(0.0001f, speed));

        // choose which float param exists
        string usedSpeed = HasFloatParameter(anim, speedFloat) ? speedFloat
                            : HasFloatParameter(anim, altSpeedFloat) ? altSpeedFloat : null;
        if (!string.IsNullOrEmpty(usedSpeed))
            anim.SetFloat(usedSpeed, normalized, animatorSmoothTime, Time.deltaTime);

        // choose which walk bool exists
        string usedWalk = HasBoolParameter(anim, walkBool) ? walkBool
                            : HasBoolParameter(anim, altWalkBool) ? altWalkBool : null;

        bool walking = normalized > walkThreshold;
        if (!string.IsNullOrEmpty(usedWalk))
            anim.SetBool(usedWalk, walking);

        // debug: warn if parameter true but state hasn't changed (helps catch Exit Time)
        if (walking)
        {
            AnimatorStateInfo st = anim.GetCurrentAnimatorStateInfo(0);
            if (st.IsName("Idle"))
            {
                // only log occasionally to avoid spam
                if (Time.frameCount % 300 == 0)
                    Debug.LogWarning("walk=true but Animator still in Idle. Check Idle->Walking transition (Has Exit Time / conditions).");
            }
        }
    }

    bool CheckGrounded()
    {
        int mask = groundLayers == 0 ? Physics.DefaultRaycastLayers : groundLayers;

        Vector3 origin;
        float radius = 0.15f;
        float checkDist = Mathf.Max(distanceToGround, 0.05f);

        if (groundCheck != null)
        {
            origin = groundCheck.position;
        }
        else if (bodyCollider != null)
        {
            origin = bodyCollider.bounds.center + Vector3.down * (bodyCollider.bounds.extents.y - 0.01f);
            radius = Mathf.Max(radius, Mathf.Min(bodyCollider.bounds.extents.x, bodyCollider.bounds.extents.z) * 0.5f);
        }
        else
        {
            origin = transform.position + Vector3.up * 0.1f;
        }

        // Use CheckSphere for stability (better than single ray)
        return Physics.CheckSphere(origin, radius + checkDist, mask, QueryTriggerInteraction.Ignore);
    }

    // Helpers for animator parameters (safe checks)
    bool HasBoolParameter(Animator a, string name)
    {
        if (a == null || string.IsNullOrEmpty(name)) return false;
        foreach (var p in a.parameters) if (p.type == AnimatorControllerParameterType.Bool && p.name == name) return true;
        return false;
    }
    bool HasFloatParameter(Animator a, string name)
    {
        if (a == null || string.IsNullOrEmpty(name)) return false;
        foreach (var p in a.parameters) if (p.type == AnimatorControllerParameterType.Float && p.name == name) return true;
        return false;
    }
    bool HasTriggerParameter(Animator a, string name)
    {
        if (a == null || string.IsNullOrEmpty(name)) return false;
        foreach (var p in a.parameters) if (p.type == AnimatorControllerParameterType.Trigger && p.name == name) return true;
        return false;
    }
    void SetBoolIfExists(Animator a, string name, bool value)
    {
        if (a != null && HasBoolParameter(a, name)) a.SetBool(name, value);
    }

    // Animator IK (fix method name & safety)
    void OnAnimatorIK(int layerIndex)
    {
        if (anim == null) return;
        // simple left foot IK example: only run if clip uses IK positions and a foot ray hits ground
        if (bodyCollider == null) return;

        Vector3 leftFootPos = anim.GetIKPosition(AvatarIKGoal.LeftFoot);
        Ray ray = new Ray(leftFootPos + Vector3.up * 0.3f, Vector3.down);
        if (Physics.Raycast(ray, out RaycastHit hit, 1f + distanceToGround, groundLayers))
        {
            anim.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 1f);
            anim.SetIKRotationWeight(AvatarIKGoal.LeftFoot, 1f);
            anim.SetIKPosition(AvatarIKGoal.LeftFoot, hit.point + Vector3.up * distanceToGround);
            Quaternion footRot = Quaternion.FromToRotation(Vector3.up, hit.normal) * transform.rotation;
            anim.SetIKRotation(AvatarIKGoal.LeftFoot, footRot);
        }
        else
        {
            anim.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 0f);
            anim.SetIKRotationWeight(AvatarIKGoal.LeftFoot, 0f);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 origin;
        if (groundCheck != null) origin = groundCheck.position;
        else if (bodyCollider != null) origin = bodyCollider.bounds.center + Vector3.down * (bodyCollider.bounds.extents.y - 0.01f);
        else origin = transform.position + Vector3.up * 0.1f;
        Gizmos.DrawWireSphere(origin, Mathf.Max(0.15f, distanceToGround));
    }
}
