using UnityEngine;

namespace WitchHunter.Character
{
    public class SlidingState : GroundedState
    {
        private float slideTime;

        public SlidingState(PlayerMovement machine) : base(machine) { }

        public override void Enter()
        {
            slideTime = 0f;
            machine.IsSliding = true;
            machine.SetAnimatorBool(machine.AnimatorSlidingBool, true);
            machine.SetAnimatorBool(machine.AnimatorUnstoppableBool, true);
            machine.FaceTowards(machine.WorldMoveDirection.sqrMagnitude > 0.001f
                ? machine.WorldMoveDirection
                : machine.transform.forward, machine.Stats.RotationSpeed * 1.2f);
        }

        public override void Exit()
        {
            machine.IsSliding = false;
            machine.SetAnimatorBool(machine.AnimatorSlidingBool, false);
            machine.SetAnimatorBool(machine.AnimatorUnstoppableBool, false);
        }

        public override void HandleInput()
        {
            base.HandleInput();

            if (slideTime >= machine.SlideDuration)
            {
                machine.ChangeState(machine.MoveInput.sqrMagnitude > machine.MoveInputDeadZone * machine.MoveInputDeadZone
                    ? machine.LocomotionState
                    : machine.IdleState);
                return;
            }

            if (machine.MoveInput.sqrMagnitude < machine.MoveInputDeadZone * machine.MoveInputDeadZone)
            {
                machine.ChangeState(machine.IdleState);
            }
        }

        public override void Update()
        {
            base.Update();
            slideTime += Time.deltaTime;
        }

        public override void PhysicsUpdate()
        {
            Vector3 forward = machine.transform.forward * machine.Stats.SlidingSpeed;
            machine.MoveHorizontally(forward, machine.Stats.Acceleration * 2f, true);
        }
    }
}
