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

        public HealthComponent Health => _health;
        public FighterMoveComponent Move => _move;
        public FighterBodyComponent Body => _body;
        public CombatComponent Combat => _combat;
        public DamageReceiverComponent Receiver => _receiver;
        public bool IsBot => _inputSource != null;
        public float RespawnTimeLeft => _respawnTimer.RemainingTime(Runner) ?? 0f;

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

        private static readonly List<FighterComponent> s_all = new();
        private IInputSource _inputSource;

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
            _look = input.LookRotation;

            if (_health.IsAlive)
                SimulateAlive(input, buttons);
            else
                SimulateDead();

            if (HasStateAuthority && !IsBot && buttons.WasPressed(_previousButtons, PlayerInputButtons.BotMode))
                BotBrainComponent.CycleModes();

            _previousButtons = buttons;
        }

        public void SetInputSource(IInputSource inputSource, int team)
        {
            _inputSource = inputSource;
            _receiver.SetTeam(team);
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
            bool isSprint = buttons.IsSet(PlayerInputButtons.Sprint) && _combat.State == CombatState.Idle;
            bool isJump = buttons.WasPressed(_previousButtons, PlayerInputButtons.Jump);

            _move.Simulate(input.MoveDirection, input.LookRotation, isSprint,
                buttons.IsSet(PlayerInputButtons.Crouch), isJump, _combat.MoveMultiplier);
            _body.UpdateHitboxes();
            _combat.Simulate(buttons, _previousButtons);

        }

        private void SimulateDead()
        {
            if (!_respawnTimer.IsRunning)
            {
                _combat.ResetState();
                _receiver.HitboxRoot.HitboxRootActive = false;
                _respawnTimer = TickTimer.CreateFromSeconds(Runner, _respawnDelay);
            }

            _move.Simulate(Vector2.zero, _look, false, false, false, 0f);

            if (HasStateAuthority && _respawnTimer.Expired(Runner))
                Respawn();
        }

        private void Respawn()
        {
            _respawnTimer = TickTimer.None;
            _receiver.HitboxRoot.HitboxRootActive = true;
            _health.Restore(_health.MaxHealth);
            _move.Teleport(_spawnPosition, _look.y);
        }
    }
}
