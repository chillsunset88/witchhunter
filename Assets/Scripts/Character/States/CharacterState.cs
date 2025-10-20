using UnityEngine;

namespace WitchHunter.Character
{
    public abstract class CharacterState
    {
        protected readonly PlayerMovement machine;

        protected CharacterState(PlayerMovement machine)
        {
            this.machine = machine;
        }

        public virtual void Enter() { }
        public virtual void Exit() { }
        public virtual void HandleInput() { }
        public virtual void Update() { }
        public virtual void PhysicsUpdate() { }
    }
}
