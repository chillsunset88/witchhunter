using UnityEngine;

namespace WitchHunter.Character
{
    public class LocomotionState : GroundedState
    {
        public LocomotionState(PlayerMovement machine) : base(machine) { }

        public override void Enter()
        {
            machine.IsSprinting = false;
        }

        public override void HandleInput()
        {
            base.HandleInput();

            if (machine.MoveInput.sqrMagnitude <= machine.MoveInputDeadZone * machine.MoveInputDeadZone)
            {
                machine.ChangeState(machine.IdleState);
                return;
            }

            machine.IsSprinting = machine.SprintHeld;
        }

        public override void Update()
        {
            base.Update();

            Vector3 direction = machine.WorldMoveDirection;
            if (direction.sqrMagnitude > 0.0001f)
            {
                machine.FaceTowards(direction, machine.Stats.RotationSpeed);
            }

            float targetSpeed = machine.IsSprinting ? machine.Stats.Speed * machine.SprintMultiplier : machine.Stats.Speed;
            machine.DesiredVelocity = direction * targetSpeed;
        }

        public override void PhysicsUpdate()
        {
            float accel = machine.IsSprinting ? machine.Stats.Acceleration * 1.2f : machine.Stats.Acceleration;
            machine.MoveHorizontally(machine.DesiredVelocity, accel);
        }
    }
}
