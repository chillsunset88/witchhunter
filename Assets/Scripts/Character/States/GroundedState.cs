namespace WitchHunter.Character
{
    public abstract class GroundedState : CharacterState
    {
        protected GroundedState(PlayerMovement machine) : base(machine) { }

        public override void HandleInput()
        {
            if (machine.AttackPressed)
            {
                machine.ChangeState(machine.AttackState);
                return;
            }

            if (machine.JumpPressed && machine.CanJump)
            {
                machine.ChangeState(machine.JumpState);
                return;
            }

            if (machine.SlidePressed && machine.CanSlide)
            {
                machine.ChangeState(machine.SlidingState);
                return;
            }

            if (machine.CrouchHeld && machine.CanCrouch)
            {
                machine.ChangeState(machine.CrouchingState);
                return;
            }
        }

        public override void Update()
        {
            base.Update();

            if (!machine.IsGrounded)
            {
                machine.ChangeState(machine.FallingState);
            }
        }
    }
}
