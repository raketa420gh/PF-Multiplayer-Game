using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace Game.Scripts.Battle
{
    public sealed class WeaponViewComponent : NetworkBehaviour
    {
        [SerializeField]
        private CombatComponent _combat;

        [Tooltip("Indexed by WeaponSocket")]
        [SerializeField]
        private Transform[] _sockets;

        private FighterAnimComponent _anim;
        private List<WeaponVisual>[] _visuals;
        private int _shownSlot = -1;
        private bool _isShownOut = true;
        private WeaponConfig _shownWeapon;
        private bool _wasActivePhase;
        private CombatState _shownState;
        private bool _wasFullDraw;

        public override void Spawned()
        {
            WeaponConfig[] loadout = _combat.Catalog;
            _anim = GetComponent<FighterAnimComponent>();
            _visuals = new List<WeaponVisual>[loadout.Length];

            for (int i = 0; i < loadout.Length; i++)
            {
                _visuals[i] = new List<WeaponVisual>();

                foreach (WeaponAttachment attachment in loadout[i].Attachments)
                {
                    GameObject instance = Instantiate(attachment.Prefab, _sockets[(int)attachment.Socket], false);
                    instance.SetActive(false);
                    _visuals[i].Add(instance.GetComponent<WeaponVisual>());
                }
            }
        }

        public override void Render()
        {
            int slot = _combat.WeaponIndex;

            bool isOut = _combat.IsWeaponOut;

            if (slot != _shownSlot || isOut != _isShownOut)
                ShowSlot(slot, isOut);

            WeaponConfig weapon = _combat.Weapon;

            if (weapon != _shownWeapon && _shownWeapon != null && BattleContext.Instance != null)
                BattleContext.Instance.Feedback.PlayEquip(transform.position + Vector3.up * 1.2f, weapon.Sounds);

            _shownWeapon = weapon;
            CombatState state = _combat.State;
            bool isActivePhase = _combat.Phase == AttackPhase.Active;
            bool isDrawn = state == CombatState.Draw;
            bool hasArrow = isDrawn || state == CombatState.Idle && (weapon.Ranged.IsManualReload ? _combat.IsLoaded : _combat.QuiverArrows > 0);
            Vector3 drawHand = _sockets[(int)(weapon.IsMirrored ? WeaponSocket.LeftHand : WeaponSocket.RightHand)].position;

            // The book stays shut until the guard is down, as in the animation editor.
            bool isClosed = state is CombatState.BlockRaise or CombatState.Block or CombatState.BlockImpact or CombatState.Deflected ||
                            _anim != null && _anim.IsLoweringBlock;
            bool isAttack = state == CombatState.Attack;
            bool isEquip = state == CombatState.Equip;

            foreach (WeaponVisual visual in _visuals[slot])
            {
                visual.SetTrailActive(isActivePhase);
                visual.SetDraw(isDrawn, drawHand, hasArrow);
                visual.SetClosed(isClosed || isAttack && visual.IsShutInAttack(_combat.StateTime) ||
                                 isEquip && visual.IsShutInEquip(_combat.IsUnequipping, _combat.EquipClipTime));
            }

            if (isActivePhase && !_wasActivePhase && BattleContext.Instance != null)
                BattleContext.Instance.Feedback.PlaySwing(transform.position + Vector3.up * 1.4f, weapon.Reach, weapon.Sounds);

            _wasActivePhase = isActivePhase;
            bool isFullDraw = isDrawn && _combat.DrawPower >= 1f;

            if (weapon.Sounds != null && BattleContext.Instance != null)
                PlayRangedSound(weapon.Sounds, state, isFullDraw && !_wasFullDraw);

            _shownState = state;
            _wasFullDraw = isFullDraw;
        }

        /// The bow's own sounds: the draw, the hold once it is full, the next arrow nocked, the quiver restocked.
        private void PlayRangedSound(WeaponSoundConfig sounds, CombatState state, bool isFullNow)
        {
            BattleFeedback feedback = BattleContext.Instance.Feedback;
            Vector3 position = transform.position + Vector3.up * 1.4f;

            if (isFullNow)
                feedback.Play(sounds.Hold, position);

            if (state == _shownState)
                return;

            switch (state)
            {
                case CombatState.Draw:
                    feedback.Play(sounds.Draw, position);
                    feedback.Play(sounds.Creak, position);
                    break;
                case CombatState.Reload when !_combat.Weapon.Ranged.IsManualReload:
                    feedback.Play(sounds.Nock, position);
                    break;
                case CombatState.Restock:
                    feedback.Play(sounds.Restock, position);
                    break;
            }
        }

        private void ShowSlot(int slot, bool isOut)
        {
            if (_shownSlot >= 0)
                SetSlotActive(_shownSlot, false);

            SetSlotActive(slot, isOut);
            _shownSlot = slot;
            _isShownOut = isOut;
        }

        private void SetSlotActive(int slot, bool isActive)
        {
            foreach (WeaponVisual visual in _visuals[slot])
                visual.gameObject.SetActive(isActive);
        }
    }
}
