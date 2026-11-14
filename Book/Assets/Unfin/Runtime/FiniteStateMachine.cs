using System;
using System.Collections.Generic;

namespace Unfin
{
    // Shared, allocation-free state clock. Orthogonal channels (movement/combat)
    // use separate instances so steering never restarts an active strike.
    public sealed class FiniteStateMachine<T> where T : struct, Enum
    {
        static readonly EqualityComparer<T> Comparer = EqualityComparer<T>.Default;
        public T State { get; private set; }
        public float Time { get; set; }
        public FiniteStateMachine(T initial) { State = initial; }
        public bool Change(T next, bool restart = false)
        {
            if (!restart && Comparer.Equals(State, next)) return false;
            State = next;
            Time = 0;
            return true;
        }
        public void Advance(float delta) { Time += delta; }
    }

    public enum PlayerLocomotionState
    {
        Idle, RunStart, Run, RunStop, Jump, MovingJump, JumpLand, MovingLand,
        Fall, FallLand, Dash, WallHold, WallClimb, Death, Revive
    }
    public enum PlayerCombatState { Ready, GroundAttack, AirAttack }
    public enum EnemyAnimationState { Move, Attack, Death }
}
