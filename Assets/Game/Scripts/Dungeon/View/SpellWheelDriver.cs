using System.Collections.Generic;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// Q and E wheels of the local adventurer (memorized spells, forms), shared by the gameplay and the hub screens: held key
    /// opens the wheel, the cursor readies a spell, release closes it.
    public sealed class SpellWheelDriver
    {
        private readonly SpellWheelView _wheel;
        private readonly DungeonContext _context;
        private readonly List<int> _spells = new();
        private readonly List<(string, Sprite, string, Color, string)> _entries = new();
        private int _skill = -1;
        private int _readied = -1;

        public SpellWheelDriver(SpellWheelView wheel, DungeonContext context)
        {
            _wheel = wheel;
            _context = context;
        }

        public void Update(AdventurerComponent adventurer, bool isBlocked)
        {
            if (isBlocked || adventurer.State != AdventurerState.Alive)
            {
                Close(adventurer, false);

                return;
            }

            if (_skill < 0)
            {
                for (int i = 0; i < adventurer.SkillCount; i++)
                {
                    AbilityConfig skill = adventurer.GetSkill(i);

                    if (skill == null || skill.Kind is not (AbilityKind.SpellMemory or AbilityKind.Shapeshift) || !Input.GetKeyDown(i == 0 ? KeyCode.Q : KeyCode.E))
                        continue;

                    Open(adventurer, i, skill);

                    return;
                }

                return;
            }

            _wheel.Move(new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * 12f);
            ReadyHovered(adventurer);

            for (int i = 0; adventurer.GetSkill(_skill).Kind == AbilityKind.SpellMemory && i < _spells.Count; i++)
            {
                int index = adventurer.SkillCount + _spells[i];
                _wheel.SetFill(i, adventurer.Abilities[index].IsCooldownBased ? -1f : adventurer.GetChargeFill(index));
            }

            if (!Input.GetKey(_skill == 0 ? KeyCode.Q : KeyCode.E))
                Close(adventurer, true);
        }

        public void Close(AdventurerComponent adventurer, bool apply)
        {
            if (_skill < 0)
                return;

            int selected = _wheel.Selected;
            AbilityConfig skill = adventurer.GetSkill(_skill);
            _wheel.Hide();
            _context.Battle.Input.SetLookFrozen(false);
            _skill = -1;

            if (apply && selected >= 0 && skill.Kind == AbilityKind.Shapeshift)
                adventurer.RpcShapeshift((ShapeshiftForm)(selected + 1));
        }

        private void Open(AdventurerComponent adventurer, int skillIndex, AbilityConfig skill)
        {
            _skill = skillIndex;
            _entries.Clear();

            if (skill.Kind == AbilityKind.Shapeshift)
            {
                _entries.Add(("Rat", null, "Rt", new Color(0.6f, 0.6f, 0.6f), "tiny, fragile, sneaky"));
                _wheel.Open("Shapeshift", _entries);
            }
            else
            {
                AbilityConfig[] spells = adventurer.Class.Spells;
                _spells.Clear();

                for (int slot = skillIndex * ClassConfig.SpellWheelSize; slot < (skillIndex + 1) * ClassConfig.SpellWheelSize; slot++)
                {
                    int i = adventurer.GetSlotSpell(slot);

                    if (i < 0 || i >= spells.Length)
                        continue;

                    int index = adventurer.SkillCount + i;
                    string detail = spells[i].IsCooldownBased ? $"{spells[i].Cooldown:0}s cd" : $"{adventurer.GetCharges(index)}/{adventurer.GetMaxCharges(index)}";
                    _spells.Add(i);
                    _entries.Add((spells[i].DisplayName, spells[i].Icon, spells[i].Glyph, spells[i].Color, detail));
                }

                _wheel.Open(skill.DisplayName, _entries, "Weapon");
            }

            _readied = -1;
            _context.Battle.Input.SetLookFrozen(true);
            ReadyHovered(adventurer);
        }

        /// A spell is readied the moment the cursor enters its sector (the wheel opens on the centre); forms wait for the wheel to close.
        private void ReadyHovered(AdventurerComponent adventurer)
        {
            int selected = _wheel.Selected;

            if (selected == _readied || adventurer.GetSkill(_skill).Kind == AbilityKind.Shapeshift)
                return;

            _readied = selected;
            adventurer.RpcReadySpell(selected == SpellWheelView.Center ? AdventurerComponent.NoSpell : (byte)_spells[selected]);
        }
    }
}
