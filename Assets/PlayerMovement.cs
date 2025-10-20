using UnityEngine;

namespace WitchHunter.Character
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Animator))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private Stats stats;
        [SerializeField] private Transform groundCheck;
        [SerializeField] private LayerMask groundLayers = Physics.DefaultRaycastLayers;

        [Header("Movement configuration")]
        [SerializeField, Range(0f, 1f)] private float moveInputDeadZone = 0.1f;
        [SerializeField, Range(1f, 3f)] private float sprintMultiplier = 1.75f;
        [SerializeField, Range(0f, 2f)] private float slideDuration = 0.65f;
        [SerializeField, Range(0f, 5f)] private float attackLockDuration = 0.85f;
        [SerializeField, Range(0f, 1f)] private float attackExitNormalizedTime = 0.85f;
        [SerializeField] private string attackStateTag = "Attack";
        [SerializeField, Range(0f, 3f)] private float aboutToLandDistance = 0.75f;
        [SerializeField, Range(0f, 0.5f)] private float groundCheckRadius = 0.2f;
        [SerializeField, Range(0f, 1.5f)] private float groundCheckDistance = 0.15f;

        [Header("Animator parameters")]
        [SerializeField] private string horizontalFloat = "horInput";
        [SerializeField] private string verticalFloat = "vertInput";
        [SerializeField] private string inputMagnitudeFloat = "inputMagnitude";
        [SerializeField] private string groundVelocityFloat = "groundVelocity";
        [SerializeField] private string fallingBool = "isFalling";
        [SerializeField] private string aboutToLandBool = "isAboutToLand";
        [SerializeField] private string crouchingBool = "crouching";
        [SerializeField] private string unstoppableBool = "unsloppable";
        [SerializeField] private string slidingBool = "sliding";
        [SerializeField] private string attackBool = "attacking";
        [SerializeField] private string comboAttackBool = "comboAttack";
        [SerializeField] private string fastAttackBool = "fastAttack";
        [SerializeField] private string strongAttackBool = "strongAttack";
        [SerializeField] private string jumpBool = "jump";

        [Header("Debug")]
        [SerializeField] private bool showGroundingGizmos = true;

        private Rigidbody rb;
        private Animator animator;
        private Camera mainCamera;

        private CharacterState currentState;
        public IdleState IdleState { get; private set; }
        public LocomotionState LocomotionState { get; private set; }
        public JumpState JumpState { get; private set; }
        public FallingState FallingState { get; private set; }
        public SlidingState SlidingState { get; private set; }
        public CrouchingState CrouchingState { get; private set; }
        public AttackState AttackState { get; private set; }

        public Stats Stats => stats;
        public Rigidbody Rigidbody => rb;
        public Animator Animator => animator;

        public Vector2 MoveInput { get; private set; }
        public Vector3 DesiredVelocity { get; set; }
        public bool IsGrounded { get; private set; }
        public bool IsFalling { get; set; }
        public bool IsSliding { get; set; }
        public bool IsCrouching { get; set; }
        public bool IsAttacking { get; set; }
        public bool IsSprinting { get; set; }

        public bool JumpPressed { get; private set; }
        public bool SlidePressed { get; private set; }
        public bool SprintHeld { get; private set; }
        public bool CrouchHeld { get; private set; }
        public bool FastAttackPressed { get; private set; }
        public bool StrongAttackPressed { get; private set; }
        public bool ComboAttackPressed { get; private set; }

        public bool CanJump => JumpState.CanConsumeJump();
        public bool CanSlide => IsGrounded && !IsSliding && CurrentGroundSpeed > stats.Speed * 0.5f;
        public bool CanCrouch => IsGrounded && !IsSliding && !IsAttacking;
        public bool AttackPressed => FastAttackPressed || StrongAttackPressed || ComboAttackPressed;

        public float CurrentGroundSpeed => new Vector3(rb.velocity.x, 0f, rb.velocity.z).magnitude;
        public float MoveInputDeadZone => moveInputDeadZone;
        public float SprintMultiplier => sprintMultiplier;
        public float SlideDuration => slideDuration;
        public float AttackLockDuration => attackLockDuration;
        public float AttackExitNormalizedTime => attackExitNormalizedTime;
        public string AttackTag => attackStateTag;

        public string AnimatorCrouchingBool => crouchingBool;
        public string AnimatorUnstoppableBool => unstoppableBool;
        public string AnimatorSlidingBool => slidingBool;
        public string AnimatorAttackBool => attackBool;
        public string AnimatorComboAttackBool => comboAttackBool;
        public string AnimatorFastAttackBool => fastAttackBool;
        public string AnimatorStrongAttackBool => strongAttackBool;
        public string AnimatorJumpBool => jumpBool;
        public string AnimatorFallingBool => fallingBool;

        private readonly int defaultAnimatorLayer = 0;
        private bool hasHorizontalFloat;
        private bool hasVerticalFloat;
        private bool hasInputMagnitudeFloat;
        private bool hasGroundVelocityFloat;
        private bool hasFallingBool;
        private bool hasAboutToLandBool;
        private bool hasCrouchingBool;
        private bool hasUnstoppableBool;
        private bool hasSlidingBool;
        private bool hasAttackBool;
        private bool hasComboAttackBool;
        private bool hasFastAttackBool;
        private bool hasStrongAttackBool;
        private bool hasJumpBool;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            animator = GetComponent<Animator>();
            mainCamera = Camera.main;

            if (stats == null)
            {
                stats = GetComponent<Stats>();
                if (stats == null)
                {
                    stats = gameObject.AddComponent<Stats>();
                    Debug.LogWarning($"[{nameof(PlayerMovement)}] Stats component was not assigned; a default instance was added at runtime.");
                }
            }

            IdleState = new IdleState(this);
            LocomotionState = new LocomotionState(this);
            JumpState = new JumpState(this);
            FallingState = new FallingState(this);
            SlidingState = new SlidingState(this);
            CrouchingState = new CrouchingState(this);
            AttackState = new AttackState(this);

            CacheAnimatorParameters();

            currentState = IdleState;
            currentState.Enter();

            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }

        private void Update()
        {
            if (mainCamera == null && Camera.main != null)
            {
                mainCamera = Camera.main;
            }

            CacheInput();
            UpdateGrounded();

            currentState.HandleInput();
            currentState.Update();

            UpdateAnimatorParameters();
            ResetTransientInputs();
        }

        private void FixedUpdate()
        {
            currentState.PhysicsUpdate();
        }

        public void ChangeState(CharacterState newState)
        {
            if (newState == null || newState == currentState)
                return;

            currentState.Exit();
            currentState = newState;
            currentState.Enter();
        }

        private void CacheInput()
        {
            MoveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            MoveInput = Vector2.ClampMagnitude(MoveInput, 1f);

            SprintHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            JumpPressed |= Input.GetKeyDown(KeyCode.Space);
            SlidePressed |= Input.GetKeyDown(KeyCode.LeftControl);
            CrouchHeld = Input.GetKey(KeyCode.C);
            FastAttackPressed |= Input.GetMouseButtonDown(0);
            StrongAttackPressed |= Input.GetMouseButtonDown(1);
            ComboAttackPressed |= Input.GetKeyDown(KeyCode.E);
        }

        private void ResetTransientInputs()
        {
            JumpPressed = false;
            SlidePressed = false;
            FastAttackPressed = false;
            StrongAttackPressed = false;
            ComboAttackPressed = false;
        }

        private void UpdateGrounded()
        {
            Vector3 origin = groundCheck != null ? groundCheck.position : transform.position + Vector3.up * 0.1f;
            float radius = groundCheckRadius;

            IsGrounded = Physics.CheckSphere(origin, radius + groundCheckDistance, groundLayers, QueryTriggerInteraction.Ignore);

            if (IsGrounded)
            {
                JumpState.ResetJumpCounter();
            }
        }

        public void MoveHorizontally(Vector3 targetVelocity, float acceleration, bool instant = false)
        {
            if (rb == null) return;

            Vector3 current = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
            Vector3 next = instant ? targetVelocity : Vector3.MoveTowards(current, targetVelocity, acceleration * Time.fixedDeltaTime);
            rb.velocity = new Vector3(next.x, Mathf.Max(rb.velocity.y, stats.MaxDownVelocity), next.z);
        }

        public void ApplyJump(float jumpForce)
        {
            Vector3 velocity = rb.velocity;
            velocity.y = 0f;
            rb.velocity = velocity;
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }

        public void ApplyAdditionalGravity()
        {
            Vector3 velocity = rb.velocity;
            velocity += Vector3.down * stats.AdditionalGravityForce * Time.fixedDeltaTime;
            velocity.y = Mathf.Max(velocity.y, stats.MaxDownVelocity);
            rb.velocity = velocity;
        }

        public void FaceTowards(Vector3 direction, float rotationSpeed)
        {
            if (direction.sqrMagnitude < 0.001f) return;

            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        public Vector3 WorldMoveDirection
        {
            get
            {
                if (MoveInput.sqrMagnitude < moveInputDeadZone * moveInputDeadZone)
                    return Vector3.zero;

                Vector3 forward = Vector3.forward;
                Vector3 right = Vector3.right;

                if (mainCamera != null)
                {
                    forward = Vector3.ProjectOnPlane(mainCamera.transform.forward, Vector3.up).normalized;
                    right = Vector3.ProjectOnPlane(mainCamera.transform.right, Vector3.up).normalized;
                }

                Vector3 direction = forward * MoveInput.y + right * MoveInput.x;
                if (direction.sqrMagnitude > 1f) direction.Normalize();
                return direction;
            }
        }

        public bool AnimatorIsInTaggedState(string tag, out AnimatorStateInfo stateInfo)
        {
            stateInfo = animator.GetCurrentAnimatorStateInfo(defaultAnimatorLayer);
            return !animator.IsInTransition(defaultAnimatorLayer) && stateInfo.IsTag(tag);
        }

        private void UpdateAnimatorParameters()
        {
            if (animator == null) return;

            Vector3 localVelocity = transform.InverseTransformDirection(new Vector3(rb.velocity.x, 0f, rb.velocity.z));
            float magnitude = MoveInput.magnitude;

            if (hasHorizontalFloat)
            {
                animator.SetFloat(horizontalFloat, MoveInput.x);
            }

            if (hasVerticalFloat)
            {
                animator.SetFloat(verticalFloat, MoveInput.y);
            }

            if (hasInputMagnitudeFloat)
            {
                animator.SetFloat(inputMagnitudeFloat, magnitude);
            }

            if (hasGroundVelocityFloat)
            {
                animator.SetFloat(groundVelocityFloat, localVelocity.z);
            }

            if (hasFallingBool)
            {
                animator.SetBool(fallingBool, IsFalling);
            }

            if (hasAboutToLandBool)
            {
                animator.SetBool(aboutToLandBool, CheckAboutToLand());
            }

            if (hasCrouchingBool)
            {
                animator.SetBool(crouchingBool, IsCrouching);
            }

            if (hasUnstoppableBool)
            {
                animator.SetBool(unstoppableBool, IsSliding);
            }

            if (hasSlidingBool)
            {
                animator.SetBool(slidingBool, IsSliding);
            }

            if (hasAttackBool)
            {
                animator.SetBool(attackBool, IsAttacking);
            }
        }

        private bool CheckAboutToLand()
        {
            if (IsGrounded || rb.velocity.y >= 0f) return false;

            Vector3 origin = transform.position + Vector3.up * 0.1f;
            float distance = aboutToLandDistance;

            return Physics.Raycast(origin, Vector3.down, distance, groundLayers, QueryTriggerInteraction.Ignore);
        }

        public void SetAnimatorBool(string name, bool value)
        {
            if (string.IsNullOrEmpty(name) || animator == null) return;
            if (!HasParameter(name, AnimatorControllerParameterType.Bool)) return;
            animator.SetBool(name, value);
        }

        public void ConsumeJumpInput()
        {
            JumpPressed = false;
        }

        public void ClearAttackInputs()
        {
            FastAttackPressed = false;
            StrongAttackPressed = false;
            ComboAttackPressed = false;
        }

        private void CacheAnimatorParameters()
        {
            hasHorizontalFloat = HasParameter(horizontalFloat, AnimatorControllerParameterType.Float);
            hasVerticalFloat = HasParameter(verticalFloat, AnimatorControllerParameterType.Float);
            hasInputMagnitudeFloat = HasParameter(inputMagnitudeFloat, AnimatorControllerParameterType.Float);
            hasGroundVelocityFloat = HasParameter(groundVelocityFloat, AnimatorControllerParameterType.Float);
            hasFallingBool = HasParameter(fallingBool, AnimatorControllerParameterType.Bool);
            hasAboutToLandBool = HasParameter(aboutToLandBool, AnimatorControllerParameterType.Bool);
            hasCrouchingBool = HasParameter(crouchingBool, AnimatorControllerParameterType.Bool);
            hasUnstoppableBool = HasParameter(unstoppableBool, AnimatorControllerParameterType.Bool);
            hasSlidingBool = HasParameter(slidingBool, AnimatorControllerParameterType.Bool);
            hasAttackBool = HasParameter(attackBool, AnimatorControllerParameterType.Bool);
            hasComboAttackBool = HasParameter(comboAttackBool, AnimatorControllerParameterType.Bool);
            hasFastAttackBool = HasParameter(fastAttackBool, AnimatorControllerParameterType.Bool);
            hasStrongAttackBool = HasParameter(strongAttackBool, AnimatorControllerParameterType.Bool);
            hasJumpBool = HasParameter(jumpBool, AnimatorControllerParameterType.Bool);

            if (hasComboAttackBool) animator.SetBool(comboAttackBool, false);
            if (hasFastAttackBool) animator.SetBool(fastAttackBool, false);
            if (hasStrongAttackBool) animator.SetBool(strongAttackBool, false);
            if (hasJumpBool) animator.SetBool(jumpBool, false);
        }

        private bool HasParameter(string parameter, AnimatorControllerParameterType type)
        {
            if (animator == null || string.IsNullOrEmpty(parameter))
                return false;

            foreach (var p in animator.parameters)
            {
                if (p.type == type && p.name == parameter)
                {
                    return true;
                }
            }

            return false;
        }

        private void OnDrawGizmosSelected()
        {
            if (!showGroundingGizmos) return;

            Gizmos.color = Color.cyan;
            Vector3 origin = groundCheck != null ? groundCheck.position : transform.position + Vector3.up * 0.1f;
            Gizmos.DrawWireSphere(origin, groundCheckRadius + groundCheckDistance);
        }
    }
}
