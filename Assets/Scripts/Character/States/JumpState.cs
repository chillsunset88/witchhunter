using UnityEngine;

namespace WitchHunter.Character
{
    public class JumpState : CharacterState
    {
        private int jumpsUsed;

        public JumpState(PlayerMovement machine) : base(machine) { }

        public override void Enter()
        {
            jumpsUsed++;
            machine.ConsumeJumpInput();
            machine.SetAnimatorBool(machine.AnimatorJumpBool, true);
            machine.ApplyJump(machine.Stats.JumpForce);
            machine.IsFalling = false;
        }

        public override void Exit()
        {
            machine.SetAnimatorBool(machine.AnimatorJumpBool, false);
        }

        public override void Update()
        {
            if (machine.Rigidbody.velocity.y <= 0f)
            {
                machine.ChangeState(machine.FallingState);
            }
        }

        public override void PhysicsUpdate()
        {
            Vector3 airControl = machine.WorldMoveDirection * machine.Stats.AirSpeed;
            machine.MoveHorizontally(airControl, machine.Stats.Acceleration * 0.5f);
            machine.ApplyAdditionalGravity();
        }

        public void ResetJumpCounter()
        {
            jumpsUsed = 0;
        }

        public bool CanConsumeJump() => jumpsUsed < machine.Stats.MaxJumps;
    }
}
