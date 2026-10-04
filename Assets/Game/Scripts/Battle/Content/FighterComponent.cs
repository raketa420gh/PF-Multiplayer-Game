using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace Game.Scripts.Battle
{
    public sealed class FighterComponent : NetworkBehaviour
    {
        public interface IInputSource
        {
            PlayerInputData GetInput();
        }

        public static IReadOnlyList<FighterComponent> All => s_all;

        /// Raised on every peer when the fighter dies (state change) and on the state authority before input is processed.
        public event Action OnDied;
        public event Action<NetworkButtons, NetworkButtons> OnSimulateInput;

        public HealthComponent Health => _health;
        public FighterMoveComponent Move => _move;
        public FighterBodyComponent Body => _body;
        public CombatComponent Combat => _combat;
        public DamageReceiverComponent Receiver => _receiver;
        public bool IsBot => _inputSource != null;
        public float RespawnTimeLeft => _respawnTimer.RemainingTime(Runner) ?? 0f;
        public NetworkButtons PreviousButtons => _previousButtons;
        public Vector2 Look => _look;
        public bool IsInputBlocked => _isInputBlocked;

        [SerializeField]
        private HealthComponent _health;

        [SerializeField]
        private FighterMoveComponent _move;

        [SerializeField]
        private FighterBodyComponent _body;

        [SerializeField]
        private CombatComponent _combat;

        [SerializeField]
        private DamageReceiverComponent _receiver;

        [SerializeField]
        private float _respawnDelay = 4f;

        [Networked]
        private NetworkButtons _previousButtons { get; set; }

        [Networked]
        private Vector2 _look { get; set; }

        [Networked]
        private TickTimer _respawnTimer { get; set; }

        [Networked]
        private Vector3 _spawnPosition { get; set; }

        [Networked]
        private NetworkBool _isInputBlocked { get; set; }

        [Networked]
        private NetworkBool _isCrouchForced { get; set; }

        private static readonly List<FighterComponent> s_all = new();
        private IInputSource _inputSource;
        private ICombatStats _stats;
        private bool _wasAlive = true;

        public override void Spawned()
        {
            s_all.Add(this);

            if (HasStateAuthority)
            {
                _spawnPosition = transform.position;
                _look = new Vector2(0f, transform.eulerAngles.y);
            }

            if (HasInputAuthority && BattleContext.Instance != null)
                BattleContext.Instance.SetLocalFighter(this);
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            s_all.Remove(this);

            if (BattleContext.Instance != null && BattleContext.Instance.LocalFighter == this)
                BattleContext.Instance.SetLocalFighter(null);
        }

        public override void FixedUpdateNetwork()
        {
            if (!TryGetInput(out PlayerInputData input))
                return;

            NetworkButtons buttons = input.Buttons;

            if (_isInputBlocked)
            {
                input.MoveDirection = Vector2.zero;
                buttons = default;
                input.Buttons = buttons;
            }

            _look = input.LookRotation;

            if (_health.IsAlive)
            {
                SimulateAlive(input, buttons);
            }
            else
            {
                SimulateDead();
            }

            if (HasStateAuthority && !IsBot && buttons.WasPressed(_previousButtons, PlayerInputButtons.BotMode))
                BotBrainComponent.CycleModes();

            OnSimulateInput?.Invoke(buttons, _previousButtons);
            _previousButtons = buttons;
        }

        public override void Render()
        {
            bool isAlive = _health.IsAlive;

            if (_wasAlive && !isAlive)
                OnDied?.Invoke();

            // A body on the floor is not in the way of the living.
            if (_wasAlive != isAlive)
                _move.SetBlocking(isAlive);

            _wasAlive = isAlive;
        }

        public void SetInputSource(IInputSource inputSource, int team)
        {
            _inputSource = inputSource;
            _receiver.SetTeam(team);
        }

        public void SetStats(ICombatStats stats)
        {
            _stats = stats;
            _combat.SetStats(stats);
        }

        public void SetInputBlocked(bool isBlocked)
        {
            _isInputBlocked = isBlocked;
        }

        /// Keeps the fighter crouched whatever the crouch button says. State authority only.
        public void SetCrouchForced(bool isForced)
        {
            _isCrouchForced = isForced;
        }

        public void SetLook(Vector2 look)
        {
            _look = look;
        }

        public void Revive(Vector3 position, float yaw)
        {
            _respawnTimer = TickTimer.None;
            _receiver.HitboxRoot.HitboxRootActive = true;
            _health.Restore(_health.MaxHealth);
            _combat.ResetState();
            _move.Teleport(position, yaw);
            _look = new Vector2(0f, yaw);
        }

        private bool TryGetInput(out PlayerInputData input)
        {
            if (_inputSource != null)
            {
                input = HasStateAuthority ? _inputSource.GetInput() : default;

                return HasStateAuthority;
            }

            if (!GetInput(out input))
            {
                input.LookRotation = _look;
                input.Buttons = _previousButtons;
            }

            return true;
        }

        private void SimulateAlive(PlayerInputData input, NetworkButtons buttons)
        {
            bool isWalk = buttons.IsSet(PlayerInputButtons.Sprint);
            bool isJump = buttons.WasPressed(_previousButtons, PlayerInputButtons.Jump);
            float speed = _combat.MoveMultiplier * (_stats?.MoveSpeedMultiplier ?? 1f);

            _move.Simulate(input.MoveDirection, input.LookRotation, isWalk,
                buttons.IsSet(PlayerInputButtons.Crouch) || _isCrouchForced, isJump, speed);
            _body.UpdateHitboxes();
            _combat.Simulate(buttons, _previousButtons);
        }

        private void SimulateDead()
        {
            if (!_respawnTimer.IsRunning && _respawnDelay > 0f)
            {
                _combat.ResetState();
                _receiver.HitboxRoot.HitboxRootActive = false;
                _respawnTimer = TickTimer.CreateFromSeconds(Runner, _respawnDelay);
            }
            else if (_respawnDelay <= 0f && _receiver.HitboxRoot.HitboxRootActive)
            {
                _combat.ResetState();
                _receiver.HitboxRoot.HitboxRootActive = false;
            }

            _move.Simulate(Vector2.zero, _look, false, false, false, 0f);

            if (HasStateAuthority && _respawnDelay > 0f && _respawnTimer.Expired(Runner))
                Revive(_spawnPosition, _look.y);
        }
    }
}
