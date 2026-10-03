using System.Text;
using Fusion;
using Game.Scripts.Battle;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.Dungeon
{
    /// In-dungeon HUD: health, timer, swarm warning, interaction ring, skills, spells, belt and status effects.
    public sealed class DungeonHudView : DisplayableView
    {
        [System.Serializable]
        private sealed class AbilitySlot
        {
            public GameObject Root;
            public TMP_Text Glyph;
            public TMP_Text Key;
            public TMP_Text Charges;
            public Image Cooldown;
        }

        [System.Serializable]
        private sealed class BeltSlot
        {
            public GameObject Root;
            public TMP_Text Glyph;
            public TMP_Text Count;
        }

        [SerializeField]
        private DungeonContext _context;

        [SerializeField]
        private Image _healthFill;

        [SerializeField]
        private TMP_Text _healthText;

        [SerializeField]
        private TMP_Text _nameText;

        [SerializeField]
        private TMP_Text _timerText;

        [SerializeField]
        private TMP_Text _floorText;

        [SerializeField]
        private TMP_Text _swarmText;

        [SerializeField]
        private TMP_Text _weaponText;

        [SerializeField]
        private TMP_Text _effectsText;

        [SerializeField]
        private TMP_Text _promptText;

        [SerializeField]
        private Image _promptRing;

        [SerializeField]
        private Image _castBar;

        [SerializeField]
        private TMP_Text _castText;

        [SerializeField]
        private GameObject _castRoot;

        [SerializeField]
        private AbilitySlot[] _skills;

        [SerializeField]
        private AbilitySlot[] _spells;

        [SerializeField]
        private BeltSlot[] _belt;

        [SerializeField]
        private Image _vignette;

        [SerializeField]
        private TMP_Text _killText;

        [SerializeField]
        private TMP_Text _readiedText;

        [SerializeField]
        private Image _bossFill;

        [SerializeField]
        private GameObject _bossRoot;

        [SerializeField]
        private TMP_Text _bossText;

        [SerializeField]
        private TMP_Text[] _weaponSlotLabels;

        private readonly StringBuilder _builder = new();

        private void Update()
        {
            AdventurerComponent adventurer = _context.LocalAdventurer;

            if (adventurer == null || adventurer.Object == null || !adventurer.Object.IsValid)
                return;

            UpdateHealth(adventurer);
            UpdateMatch(adventurer);
            UpdateCombat(adventurer);
            UpdatePrompt(adventurer);
            UpdateAbilities(adventurer);
            UpdateBelt(adventurer);
            UpdateEffects(adventurer);
            UpdateReadied(adventurer);
            UpdateBoss(adventurer);
        }

        private void UpdateReadied(AdventurerComponent adventurer)
        {
            AbilityConfig spell = adventurer.ReadiedSpellConfig;

            if (adventurer.Form != ShapeshiftForm.None)
            {
                _readiedText.text = $"<color=#9c6>{adventurer.Form} form</color>  <size=70%>press the form key again to return</size>";

                return;
            }

            if (spell == null)
            {
                _readiedText.text = string.Empty;

                return;
            }

            int index = adventurer.SkillCount + adventurer.ReadiedSpell;
            string charges = spell.IsCooldownBased ? $"{Mathf.CeilToInt(adventurer.GetCooldownLeft(index))}s" : $"{adventurer.GetCharges(index)}/{spell.Charges}";
            _readiedText.text = adventurer.HasFocus
                ? $"<color=#{ColorUtility.ToHtmlStringRGB(spell.Color)}>{spell.DisplayName}</color>  [RMB] cast   {charges}"
                : $"<color=#f66>{spell.DisplayName}: requires a {(adventurer.Class.Focus == CastFocus.Instrument ? "instrument" : "magical focus")} in hand</color>";
        }

        private void UpdateBoss(AdventurerComponent adventurer)
        {
            MonsterComponent boss = null;
            float best = 22f * 22f;

            foreach (MonsterComponent monster in MonsterComponent.All)
            {
                if (!monster.IsBoss || monster.Fighter.Health.IsDead || monster.Floor != adventurer.Floor)
                    continue;

                float distance = (monster.transform.position - adventurer.transform.position).sqrMagnitude;

                if (distance < best)
                {
                    best = distance;
                    boss = monster;
                }
            }

            _bossRoot.SetActive(boss != null);

            if (boss == null)
                return;

            _bossFill.fillAmount = boss.Fighter.Health.Progress;
            _bossText.text = boss.Config.DisplayName;
        }

        private void UpdateHealth(AdventurerComponent adventurer)
        {
            HealthComponent health = adventurer.Fighter.Health;
            _healthFill.fillAmount = health.Progress;
            _healthText.text = $"{health.CurrentHealth} / {health.MaxHealth}";
            PlayerSessionComponent session = adventurer.Session;
            _nameText.text = session != null ? $"{session.DisplayName}  ·  {adventurer.Class.DisplayName} {session.Level}" : adventurer.Class.DisplayName;
            _killText.text = $"Kills {adventurer.Kills}   XP +{adventurer.RunExperience}";

            float danger = adventurer.IsInSwarm ? 0.55f : Mathf.Lerp(0.45f, 0f, health.Progress * 2f);
            _vignette.color = new Color(adventurer.IsInSwarm ? 0.1f : 0.5f, 0f, adventurer.IsInSwarm ? 0.2f : 0f, danger);
        }

        private void UpdateMatch(AdventurerComponent adventurer)
        {
            MatchComponent match = _context.Match;
            _floorText.text = adventurer.Floor == 1 ? "The Crypts · Upper floor" : "The Crypts · Lower floor";

            if (match == null || !match.IsRunning)
            {
                _timerText.text = "--:--";
                _swarmText.text = string.Empty;

                return;
            }

            float left = match.TimeLeft;
            _timerText.text = $"{Mathf.FloorToInt(left / 60f):00}:{Mathf.FloorToInt(left % 60f):00}";
            _timerText.color = left < 60f ? new Color(1f, 0.3f, 0.2f) : Color.white;

            Vector3 delta = adventurer.transform.position - match.GetSwarmCenter(adventurer.Floor);
            delta.y = 0f;
            float distance = delta.magnitude - match.GetSafeRadius(adventurer.Floor);
            float next = match.GetSwarmTimeToNextStage();

            _swarmText.text = adventurer.IsInSwarm
                ? $"<color=#f55>DARK SWARM  ·  safe zone {distance:0}m away</color>"
                : next > 0f ? $"Swarm closes in {Mathf.CeilToInt(next)}s" : "Swarm is closing";
        }

        private void UpdateCombat(AdventurerComponent adventurer)
        {
            CombatComponent combat = adventurer.Fighter.Combat;
            string state = combat.State switch
            {
                CombatState.Idle => string.Empty,
                CombatState.Attack => $"Attack {combat.AttackIndex + 1}",
                CombatState.Busy => adventurer.Pending.ToString(),
                _ => combat.State.ToString()
            };
            _weaponText.text = $"{combat.Weapon.DisplayName}  <size=70%>{state}</size>";

            for (int i = 0; i < _weaponSlotLabels.Length; i++)
                _weaponSlotLabels[i].color = i == combat.WeaponSlot ? new Color(1f, 0.85f, 0.4f) : new Color(0.55f, 0.5f, 0.42f);

            bool isCasting = combat.State == CombatState.Busy && adventurer.Pending is PendingAction.Ability or PendingAction.Consumable or PendingAction.Utility;
            _castRoot.SetActive(isCasting || combat.State == CombatState.Draw);

            if (adventurer.IsHoldingCast)
            {
                AbilityConfig spell = adventurer.ReadiedSpellConfig;
                float duration = spell != null ? spell.CastTime / adventurer.Stats.CastSpeed : 1f;
                _castBar.fillAmount = Mathf.Clamp01(combat.StateTime / Mathf.Max(0.1f, duration));
                _castText.text = spell != null ? spell.DisplayName : string.Empty;
            }
            else
            {
                _castBar.fillAmount = combat.State == CombatState.Draw ? combat.DrawPower : combat.BusyProgress;
                _castText.text = adventurer.Pending switch
                {
                    PendingAction.Ability when adventurer.Pending == PendingAction.Ability => "Casting",
                    PendingAction.Consumable => "Using item",
                    PendingAction.Utility => "Using item",
                    _ => string.Empty
                };
            }
        }

        private void UpdatePrompt(AdventurerComponent adventurer)
        {
            InteractableComponent target = adventurer.LookTarget;
            bool isInteracting = adventurer.Pending == PendingAction.Interact;

            if (target == null && !isInteracting)
            {
                _promptText.text = string.Empty;
                _promptRing.fillAmount = 0f;

                return;
            }

            _promptText.text = target != null ? (target.IsAvailable ? $"[F] {target.Prompt}" : target.Prompt) : "[F] Cancel";
            _promptRing.fillAmount = adventurer.InteractProgress;
        }

        private void UpdateAbilities(AdventurerComponent adventurer)
        {
            int skillCount = adventurer.SkillCount;

            for (int i = 0; i < _skills.Length; i++)
                UpdateAbility(_skills[i], adventurer, i, i < skillCount);

            for (int i = 0; i < _spells.Length; i++)
                _spells[i].Root.SetActive(false);
        }

        private static void UpdateAbility(AbilitySlot slot, AdventurerComponent adventurer, int index, bool isUsed)
        {
            slot.Root.SetActive(isUsed);

            if (!isUsed)
                return;

            AbilityConfig ability = adventurer.Abilities[index];
            float cooldown = adventurer.GetCooldownLeft(index);
            slot.Glyph.text = ability.Glyph;
            slot.Glyph.color = ability.Color;
            slot.Cooldown.fillAmount = ability.Cooldown > 0f ? cooldown / ability.Cooldown : 0f;
            slot.Charges.text = ability.IsSpell && !ability.IsCooldownBased ? adventurer.GetCharges(index).ToString() : cooldown > 0f ? Mathf.CeilToInt(cooldown).ToString() : string.Empty;
        }

        private void UpdateBelt(AdventurerComponent adventurer)
        {
            InventoryComponent inventory = adventurer.Inventory;

            for (int i = 0; i < _belt.Length; i++)
            {
                ItemStack stack = inventory.GetEquipped((EquipSlot)((int)EquipSlot.Utility1 + i));
                ItemConfig config = inventory.GetConfig(stack);
                _belt[i].Glyph.text = config != null ? config.IconGlyph : string.Empty;
                _belt[i].Glyph.color = config != null ? config.IconColor : Color.white;
                _belt[i].Count.text = config != null && stack.Count > 1 ? stack.Count.ToString() : string.Empty;
            }
        }

        private void UpdateEffects(AdventurerComponent adventurer)
        {
            _builder.Clear();

            for (int i = 0; i < StatusEffectComponent.Capacity; i++)
            {
                StatusEffect effect = adventurer.Effects.Effects[i];

                if (!effect.IsActive)
                    continue;

                _builder.AppendLine($"{effect.Kind} {Mathf.CeilToInt(effect.Remaining)}s");
            }

            if (adventurer.IsResting)
                _builder.AppendLine("Resting...");

            _effectsText.text = _builder.ToString();
        }
    }
}
