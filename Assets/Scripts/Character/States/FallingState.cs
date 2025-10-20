using UnityEngine;

namespace WitchHunter.Character
{
    public class FallingState : CharacterState
    {
        public FallingState(PlayerMovement machine) : base(machine) { }

        public override void Enter()
        {
            machine.IsFalling = true;
            machine.SetAnimatorBool(machine.AnimatorFallingBool, true);
        }

        public override void Exit()
        {
            machine.IsFalling = false;
            machine.SetAnimatorBool(machine.AnimatorFallingBool, false);
        }

        public override void HandleInput()
        {
            if (machine.JumpPressed && machine.JumpState.CanConsumeJump())
            {
                machine.ChangeState(machine.JumpState);
            }
        }

        public override void Update()
        {
            if (machine.IsGrounded)
            {
                machine.JumpState.ResetJumpCounter();
                machine.ChangeState(machine.MoveInput.sqrMagnitude > machine.MoveInputDeadZone * machine.MoveInputDeadZone
                    ? machine.LocomotionState
                    : machine.IdleState);
            }
        }

        public override void PhysicsUpdate()
        {
            Vector3 airControl = machine.WorldMoveDirection * machine.Stats.AirSpeed;
            machine.MoveHorizontally(airControl, machine.Stats.Acceleration * 0.4f);
            machine.ApplyAdditionalGravity();
        }
    }
}
