using UnityEngine;

/// <summary>
/// Drop this on a camera to get a third-person orbit that feels similar to Cinemachine's FreeLook.
/// Assign a target (usually the player root), tweak limits/smoothing, and you're good to go.
/// </summary>
public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Object the camera keeps in frame.")]
    public Transform target;
    [Tooltip("Local-space offset from target origin (e.g. eye height).")]
    public Vector3 targetOffset = new Vector3(0f, 1.6f, 0f);
    [Tooltip("Time it takes to center back on the target (0 for instant).")]
    [Range(0f, 0.5f)] public float followSmoothTime = 0.05f;

    [Header("Orbit")]
    [Tooltip("Mouse axis used for horizontal orbit.")]
    public string horizontalAxis = "Mouse X";
    [Tooltip("Mouse axis used for vertical orbit.")]
    public string verticalAxis = "Mouse Y";
    [Tooltip("Degrees per second multiplied by mouse input.")]
    public float rotationSpeed = 180f;
    [Tooltip("Clamp for vertical tilt (in degrees).")]
    public Vector2 verticalAngleLimits = new Vector2(-35f, 70f);
    [Tooltip("Invert vertical input.")]
    public bool invertY;

    [Header("Zoom")]
    [Tooltip("Starting distance from target.")]
    public float distance = 4f;
    public float minDistance = 1.5f;
    public float maxDistance = 6f;
    [Tooltip("How fast scroll wheel adjusts distance.")]
    public float zoomSpeed = 3f;
    [Tooltip("Smoothing for distance changes (0 = instant).")]
    [Range(0f, 0.25f)] public float zoomSmoothTime = 0.1f;

    [Header("Camera Response")]
    [Tooltip("Extra smoothing applied to camera position (0 = snappy).")]
    [Range(0f, 0.5f)] public float positionSmoothTime = 0.05f;
    [Tooltip("Smoothing for rotation interpolation (0 = instant).")]
    [Range(0f, 0.5f)] public float rotationSmoothTime = 0.05f;

    [Header("Collision")]
    [Tooltip("Layers considered solid for camera collision.")]
    public LayerMask collisionLayers = ~0;
    [Tooltip("Radius for collision probe. Increase if camera clips through walls.")]
    public float collisionRadius = 0.25f;
    [Tooltip("Keep the camera a small offset away from obstacles.")]
    public float collisionBuffer = 0.2f;
    [Tooltip("Draw gizmos to preview camera collision checks.")]
    public bool drawDebugGizmos;

    Vector3 focusPoint;
    Vector3 focusVelocity;
    float yaw;
    float pitch;

    float currentDistance;
    float desiredDistance;
    float distanceVelocity;

    void Awake()
    {
        if (target != null)
        {
            focusPoint = target.position + target.TransformVector(targetOffset);
        }
        else
        {
            focusPoint = transform.position + transform.forward * distance;
        }

        Vector3 euler = transform.eulerAngles;
        yaw = euler.y;
        pitch = NormalizeAngle(euler.x);

        desiredDistance = Mathf.Clamp(distance, minDistance, maxDistance);
        currentDistance = desiredDistance;
    }

    void LateUpdate()
    {
        if (target == null) return;

        HandleInput();
        UpdateFocusPoint();

        Quaternion orbitRotation = Quaternion.Euler(pitch, yaw, 0f);
        float camDistance = ResolveCollision(orbitRotation);

        Vector3 desiredPosition = focusPoint - (orbitRotation * Vector3.forward * camDistance);
        float posLerp = ComputeSmoothingFactor(positionSmoothTime);
        float rotLerp = ComputeSmoothingFactor(rotationSmoothTime);

        if (positionSmoothTime <= 0f)
        {
            transform.position = desiredPosition;
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, desiredPosition, posLerp);
        }

        if (rotationSmoothTime <= 0f)
        {
            transform.rotation = orbitRotation;
        }
        else
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, orbitRotation, rotLerp);
        }
    }

    void HandleInput()
    {
        float deltaTime = Time.deltaTime;
        if (deltaTime <= 0f) return;

        float mouseX = Input.GetAxisRaw(horizontalAxis);
        float mouseY = Input.GetAxisRaw(verticalAxis);

        yaw += mouseX * rotationSpeed * deltaTime;

        float invert = invertY ? 1f : -1f;
        pitch += mouseY * rotationSpeed * deltaTime * invert;
        pitch = Mathf.Clamp(pitch, verticalAngleLimits.x, verticalAngleLimits.y);

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > Mathf.Epsilon)
        {
            desiredDistance -= scroll * zoomSpeed;
            desiredDistance = Mathf.Clamp(desiredDistance, minDistance, maxDistance);
        }

        if (zoomSmoothTime <= 0f)
        {
            currentDistance = desiredDistance;
        }
        else
        {
            currentDistance = Mathf.SmoothDamp(currentDistance, desiredDistance, ref distanceVelocity, zoomSmoothTime);
        }

        currentDistance = Mathf.Clamp(currentDistance, minDistance, maxDistance);
    }

    void UpdateFocusPoint()
    {
        Vector3 targetPos = target.position + target.TransformVector(targetOffset);
        if (followSmoothTime <= 0f)
        {
            focusPoint = targetPos;
        }
        else
        {
            focusPoint = Vector3.SmoothDamp(focusPoint, targetPos, ref focusVelocity, followSmoothTime);
        }
    }

    float ResolveCollision(Quaternion desiredRotation)
    {
        Vector3 castDirection = desiredRotation * Vector3.back;
        Vector3 toCamera = -castDirection * currentDistance;
        float distanceCheck = toCamera.magnitude;
        if (distanceCheck <= 0.001f)
            return currentDistance;

        Vector3 direction = toCamera / distanceCheck;
        bool hitSomething = Physics.SphereCast(
            focusPoint,
            collisionRadius,
            direction,
            out RaycastHit hit,
            distanceCheck + collisionBuffer,
            collisionLayers,
            QueryTriggerInteraction.Ignore);

        if (!hitSomething) return currentDistance;

        float adjustedDistance = Mathf.Clamp(hit.distance - collisionBuffer, minDistance, currentDistance);
        return Mathf.Max(minDistance, adjustedDistance);
    }

    float ComputeSmoothingFactor(float smoothTime)
    {
        if (smoothTime <= 0f) return 1f;
        return 1f - Mathf.Exp(-Time.deltaTime / smoothTime);
    }

    static float NormalizeAngle(float angle)
    {
        angle %= 360f;
        if (angle > 180f) angle -= 360f;
        return angle;
    }

    void OnDrawGizmosSelected()
    {
        if (!drawDebugGizmos) return;

        Gizmos.color = Color.cyan;
        Vector3 origin = target != null
            ? target.position + target.TransformVector(targetOffset)
            : transform.position + transform.forward;

        Gizmos.DrawWireSphere(origin, collisionRadius);
        Gizmos.DrawLine(origin, transform.position);
        Gizmos.DrawWireSphere(transform.position, collisionRadius);
    }
}
