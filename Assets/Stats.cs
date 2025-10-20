using UnityEngine;

namespace WitchHunter.Character
{
    [DisallowMultipleComponent]
    public class Stats : MonoBehaviour
    {
        [Header("Movement speed")]
        [SerializeField, Range(0f, 50f)] private float speed = 6f;
        [SerializeField, Range(0f, 50f)] private float acceleration = 6f;
        [SerializeField, Range(0f, 50f)] private float airSpeed = 9f;
        [SerializeField, Range(0f, 50f)] private float rotationSpeed = 10f;
        [SerializeField, Range(0f, 50f)] private float crouchSpeed = 4f;
        [SerializeField, Range(0f, 50f)] private float slidingSpeed = 11f;

        [Header("Jumping/falling")]
        [SerializeField, Range(0f, 50f)] private float jumpForce = 15f;
        [SerializeField, Range(0f, 50f)] private float additionalGravityForce = 2f;
        [SerializeField, Min(1)] private int maxJumps = 1;
        [SerializeField] private float maxDownVelocity = -20f;

        [Header("Armour")]
        [SerializeField] private float physicArmour = 0f;
        [SerializeField] private float magicArmour = 0f;
        [SerializeField] private float toxicArmour = 0f;

        [Header("Health")]
        [SerializeField, Min(0f)] private float health = 100f;
        [SerializeField, Min(0f)] private float headDamageMultiplier = 2f;
        [SerializeField, Min(0f)] private float bodyDamageMultiplier = 1f;

        public float Speed => speed;
        public float Acceleration => acceleration;
        public float AirSpeed => airSpeed;
        public float RotationSpeed => rotationSpeed;
        public float CrouchSpeed => crouchSpeed;
        public float SlidingSpeed => slidingSpeed;

        public float JumpForce => jumpForce;
        public float AdditionalGravityForce => additionalGravityForce;
        public int MaxJumps => maxJumps;
        public float MaxDownVelocity => maxDownVelocity;

        public float PhysicArmour => physicArmour;
        public float MagicArmour => magicArmour;
        public float ToxicArmour => toxicArmour;

        public float Health => health;
        public float HeadDamageMultiplier => headDamageMultiplier;
        public float BodyDamageMultiplier => bodyDamageMultiplier;
    }
}
