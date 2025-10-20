using UnityEngine;

namespace WitchHunter.Character
{
    public class IdleState : GroundedState
    {
        public IdleState(PlayerMovement machine) : base(machine) { }

        public override void Enter()
        {
            machine.DesiredVelocity = Vector3.zero;
            machine.SetAnimatorBool(machine.AnimatorCrouchingBool, false);
        }

        public override void HandleInput()
        {
            base.HandleInput();

            if (machine.MoveInput.sqrMagnitude > machine.MoveInputDeadZone * machine.MoveInputDeadZone)
            {
                machine.ChangeState(machine.LocomotionState);
            }
        }

        public override void PhysicsUpdate()
        {
            machine.MoveHorizontally(Vector3.zero, machine.Stats.Acceleration);
        }
    }
}
