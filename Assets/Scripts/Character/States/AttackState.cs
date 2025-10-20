using UnityEngine;

namespace WitchHunter.Character
{
    public class AttackState : CharacterState
    {
        private float attackTimer;
        private float currentLockDuration;

        public AttackState(PlayerMovement machine) : base(machine) { }

        public override void Enter()
        {
            attackTimer = 0f;
            currentLockDuration = machine.AttackLockDuration;
            machine.IsAttacking = true;
            machine.DesiredVelocity = Vector3.zero;
            machine.MoveHorizontally(Vector3.zero, machine.Stats.Acceleration, true);
            machine.SetAnimatorBool(machine.AnimatorAttackBool, true);

            if (machine.FastAttackPressed)
            {
                machine.SetAnimatorBool(machine.AnimatorFastAttackBool, true);
            }
            else if (machine.StrongAttackPressed)
            {
                machine.SetAnimatorBool(machine.AnimatorStrongAttackBool, true);
                currentLockDuration *= 1.15f;
            }
            else if (machine.ComboAttackPressed)
            {
                machine.SetAnimatorBool(machine.AnimatorComboAttackBool, true);
                currentLockDuration *= 1.3f;
            }

            machine.ClearAttackInputs();
        }

        public override void Exit()
        {
            machine.IsAttacking = false;
            machine.SetAnimatorBool(machine.AnimatorAttackBool, false);
            machine.SetAnimatorBool(machine.AnimatorFastAttackBool, false);
            machine.SetAnimatorBool(machine.AnimatorStrongAttackBool, false);
            machine.SetAnimatorBool(machine.AnimatorComboAttackBool, false);
        }

        public override void HandleInput()
        {
            // lock movement until animation finishes
        }

        public override void Update()
        {
            attackTimer += Time.deltaTime;

            if (machine.AnimatorIsInTaggedState(machine.AttackTag, out AnimatorStateInfo stateInfo))
            {
                if (stateInfo.normalizedTime >= machine.AttackExitNormalizedTime)
                {
                    machine.ChangeState(machine.IsGrounded
                        ? (machine.MoveInput.sqrMagnitude > machine.MoveInputDeadZone * machine.MoveInputDeadZone ? machine.LocomotionState : machine.IdleState)
                        : machine.FallingState);
                }
            }
            else if (attackTimer >= currentLockDuration)
            {
                machine.ChangeState(machine.IsGrounded
                    ? (machine.MoveInput.sqrMagnitude > machine.MoveInputDeadZone * machine.MoveInputDeadZone ? machine.LocomotionState : machine.IdleState)
                    : machine.FallingState);
            }
        }

        public override void PhysicsUpdate()
        {
            machine.MoveHorizontally(Vector3.zero, machine.Stats.Acceleration * 2f, true);
        }
    }
}
