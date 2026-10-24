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
        private WeaponConfig _shownWeapon;
        private bool _wasActivePhase;

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

            if (slot != _shownSlot)
                ShowSlot(slot);

            WeaponConfig weapon = _combat.Weapon;

            if (weapon != _shownWeapon && _shownWeapon != null && BattleContext.Instance != null)
                BattleContext.Instance.Feedback.PlayEquip(transform.position + Vector3.up * 1.2f, weapon.Sounds);

            _shownWeapon = weapon;
            CombatState state = _combat.State;
            bool isActivePhase = _combat.Phase == AttackPhase.Active;
            bool isDrawn = state == CombatState.Draw;
            bool hasArrow = isDrawn || state == CombatState.Idle && (!weapon.Ranged.IsManualReload || _combat.IsLoaded);
            Vector3 drawHand = _sockets[(int)(weapon.IsMirrored ? WeaponSocket.LeftHand : WeaponSocket.RightHand)].position;

            // The book stays shut until the guard is down, as in the animation editor.
            bool isClosed = state is CombatState.BlockRaise or CombatState.Block or CombatState.BlockImpact or CombatState.Deflected ||
                            _anim != null && _anim.IsLoweringBlock;
            bool isAttack = state == CombatState.Attack;

            foreach (WeaponVisual visual in _visuals[slot])
            {
                visual.SetTrailActive(isActivePhase);
                visual.SetDraw(isDrawn, drawHand, hasArrow);
                visual.SetClosed(isClosed || isAttack && visual.IsShutInAttack(_combat.StateTime));
            }

            if (isActivePhase && !_wasActivePhase && BattleContext.Instance != null)
                BattleContext.Instance.Feedback.PlaySwing(transform.position + Vector3.up * 1.4f, weapon.Reach, weapon.Sounds);

            _wasActivePhase = isActivePhase;
        }

        private void ShowSlot(int slot)
        {
            if (_shownSlot >= 0)
                SetSlotActive(_shownSlot, false);

            SetSlotActive(slot, true);
            _shownSlot = slot;
        }

        private void SetSlotActive(int slot, bool isActive)
        {
            foreach (WeaponVisual visual in _visuals[slot])
                visual.gameObject.SetActive(isActive);
        }
    }
}
