using UnityEngine;

namespace WitchHunter.Character
{
    public class CrouchingState : GroundedState
    {
        public CrouchingState(PlayerMovement machine) : base(machine) { }

        public override void Enter()
        {
            machine.IsCrouching = true;
            machine.SetAnimatorBool(machine.AnimatorCrouchingBool, true);
        }

        public override void Exit()
        {
            machine.IsCrouching = false;
            machine.SetAnimatorBool(machine.AnimatorCrouchingBool, false);
        }

        public override void HandleInput()
        {
            if (!machine.CrouchHeld)
            {
                machine.ChangeState(machine.IdleState);
                return;
            }

            base.HandleInput();
        }

        public override void Update()
        {
            base.Update();

            Vector3 direction = machine.WorldMoveDirection;
            if (direction.sqrMagnitude > 0.0001f)
            {
                machine.FaceTowards(direction, machine.Stats.RotationSpeed * 0.65f);
            }

            machine.DesiredVelocity = direction * machine.Stats.CrouchSpeed;
        }

        public override void PhysicsUpdate()
        {
            machine.MoveHorizontally(machine.DesiredVelocity, machine.Stats.Acceleration * 0.75f);
        }
    }
}
