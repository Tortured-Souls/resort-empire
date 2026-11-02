using System;
using UnityEngine;

namespace ResortEmpire.NPC
{
    public sealed class NpcStateMachine : MonoBehaviour
    {
        [SerializeField] private NpcState initialState = NpcState.AwaitingService;

        private NpcState _current;
        private float _secondsInState;

        public event Action<NpcState, NpcState> StateChanged;

        public NpcState Current => _current;

        public float SecondsInState => _secondsInState;

        private void Awake()
        {
            _current = initialState;
            _secondsInState = 0f;
        }

        public bool Is(NpcState state) => _current == state;

        internal void Tick(float deltaTime) => _secondsInState += deltaTime;

        public void SetState(NpcState next)
        {
            if (next == _current) return;

            NpcState previous = _current;
            _current = next;
            _secondsInState = 0f;
            StateChanged?.Invoke(previous, next);
        }
    }
}
