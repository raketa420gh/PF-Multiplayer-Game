using Fusion;
using Game.Scripts.Battle;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// Class perks and the status effects that change a hit: outgoing bonuses of the striker, reductions of the struck, effects
    /// that one hit leaves on the other, auras around the adventurer and what a kill brings. Host only, except the perk values.
    public sealed partial class AdventurerComponent
    {
        private const float PerkAuraRadius = 6f;
        private const float PoisonPerSecond = 3f;
        private const float RetaliationTime = 2f;
        private const float OnslaughtTime = 2.5f;
        private const int OnslaughtMax = 5;
        private const float WrathCooldown = 5f;
        private const float WrathTime = 6f;
        private const float MarkTime = 5f;
        private const float CausticTime = 4f;
        private const float ShroudTime = 4f;
        private const float ExecuteHealth = 0.4f;
        private const float LowHealth = 0.3f;
        private const float LongShotRange = 20f;
        private const float FuneralRadius = 2f;
        private const float DeadRootTime = 6f;
        private const float DeadRootSlow = 20f;
        private const float GenerousRadius = 4f;
        private const float BulwarkAngle = 75f;

        private readonly float[] _perks = new float[(int)PerkEffect.Count];
        private DamageReceiverComponent _onslaughtVictim;
        private int _onslaughtStacks;
        private int _wrathReadyTick;
        private int _perkAuraTick;
        private DamageReceiverComponent _drainTarget;
        private float _drainLeft;
        private float _drainPerPulse;
        private float _drainSteal;
        private int _drainTick;
        /// Side effect a mixture leaves when its buff runs out.
        private StatusEffectKind _sideEffect;
        private float _sideMagnitude;
        private float _sideDuration;

        /// Summed value of the equipped perks of that kind, 0 when none is taken.
        public float Perk(PerkEffect effect)
        {
            return _perks[(int)effect];
        }

        public bool IsAllyOf(AdventurerComponent other)
        {
            return other == this || (other != null && other.Team == Team);
        }

        /// A status effect this adventurer puts on someone: his perks make slows colder, burns longer, poisons and curses worse.
        public void ApplyEffectTo(StatusEffectComponent target, StatusEffectKind kind, float magnitude, float duration, AbilityFlags flags = AbilityFlags.None)
        {
            if (target == null || kind == StatusEffectKind.None)
                return;

            switch (kind)
            {
                case StatusEffectKind.Slow or StatusEffectKind.Root:
                    if (kind == StatusEffectKind.Slow)
                        magnitude *= 1f + Perk(PerkEffect.DeepCold);

                    if (Perk(PerkEffect.Brittle) > 0f)
                        target.Add(StatusEffectKind.Vulnerable, Perk(PerkEffect.Brittle), duration);
                    break;
                case StatusEffectKind.Burn:
                    float smoulder = Perk(PerkEffect.LongSmoulder);
                    magnitude *= (duration + smoulder) / Mathf.Max(0.1f, duration);
                    duration += smoulder;
                    break;
                case StatusEffectKind.Poison:
                    Poison(target, magnitude, duration);

                    return;
                case StatusEffectKind.Cursed:
                    if (Perk(PerkEffect.Rot) > 0f)
                        target.Add(StatusEffectKind.Vulnerable, Perk(PerkEffect.Rot), duration);
                    break;
            }

            target.Add(kind, magnitude, duration);
        }

        /// Healing given by this adventurer: warm hands and last rites make it stronger, pure blood cleanses poison, a shroud guards the healed.
        public void HealTarget(AdventurerComponent target, float amount, bool isPercent, bool isOverTime, float duration = 0f, float scale = 1f)
        {
            HealthComponent health = target._fighter.Health;
            float heal = (isPercent ? health.MaxHealth * amount / 100f : amount) * scale;
            heal *= 1f + Perk(PerkEffect.WarmHands) + (health.Progress < LowHealth ? Perk(PerkEffect.LastRites) : 0f);

            if (Perk(PerkEffect.PureBlood) > 0f)
                target._effects.Remove(StatusEffectKind.Poison);

            if (Perk(PerkEffect.Shroud) > 0f && target._effects.GetMagnitude(StatusEffectKind.Guard) < Perk(PerkEffect.Shroud))
                target._effects.Add(StatusEffectKind.Guard, Perk(PerkEffect.Shroud), ShroudTime);

            if (isOverTime && duration > 0f)
                target._effects.Add(StatusEffectKind.HealOverTime, heal, duration);
            else
                health.Restore(target._effects.ScaleHealing(Mathf.Max(1, Mathf.RoundToInt(heal))));
        }

        /// A blow of a zone this adventurer left: it counts as his, so his perks change it and his own fire hurts him less.
        public void ZoneHit(DamageReceiverComponent receiver, float damage, DamageType type, AbilityFlags flags, float stagger, Vector3 from)
        {
            Vector3 point = BodyPoint(receiver);
            receiver.ApplyHit(new HitRequest
            {
                BaseDamage = Mathf.Max(1, _fighter.Combat.ScaleDamage(Mathf.RoundToInt(damage), type)),
                BodyRays = 1,
                Zone = HitZone.Torso,
                Point = point,
                Normal = (point - from).normalized,
                AttackerPosition = from,
                StaggerDuration = stagger,
                DamageType = type,
                Attacker = _fighter.Receiver,
                Tag = TagOf(flags)
            });
        }

        public static DamageTag TagOf(AbilityFlags flags)
        {
            DamageTag tag = DamageTag.Spell;

            if ((flags & AbilityFlags.Fire) != 0)
                tag |= DamageTag.Fire;

            if ((flags & AbilityFlags.Frost) != 0)
                tag |= DamageTag.Frost;

            if ((flags & AbilityFlags.Lightning) != 0)
                tag |= DamageTag.Lightning;

            if ((flags & AbilityFlags.Holy) != 0)
                tag |= DamageTag.Holy;

            if ((flags & AbilityFlags.Dark) != 0)
                tag |= DamageTag.Dark;

            if ((flags & AbilityFlags.Poison) != 0)
                tag |= DamageTag.Poison;

            if ((flags & AbilityFlags.Flask) != 0)
                tag = (tag & ~DamageTag.Spell) | DamageTag.Flask;

            return tag;
        }

        /// Venom of this adventurer: the magnitude is the damage over the whole duration; his perks thicken it and add weakness and rot.
        private void Poison(StatusEffectComponent target, float damage, float duration)
        {
            float extra = Perk(PerkEffect.ThickVenom);
            damage *= (duration + extra) / Mathf.Max(0.1f, duration) * (1f + Perk(PerkEffect.CausticVenom));
            duration += extra;
            target.Add(StatusEffectKind.Poison, damage, duration);

            if (Perk(PerkEffect.Weakness) > 0f)
                target.Add(StatusEffectKind.Weakened, Perk(PerkEffect.Weakness), duration);

            if (Perk(PerkEffect.RotBlood) > 0f)
                target.Add(StatusEffectKind.Festering, Perk(PerkEffect.RotBlood), duration);
        }

        private void PoisonFor(StatusEffectComponent target, float seconds)
        {
            Poison(target, PoisonPerSecond * seconds, seconds);
        }

        private bool IsMixtureActive => _effects.Has(StatusEffectKind.BullBlood) || _effects.Has(StatusEffectKind.Mercury);

        private bool IsHolding(WeaponClass weaponClass)
        {
            return HoldsWeaponClass(weaponClass);
        }

        /// Striker side: perks and buffs of this adventurer on a hit about to land. Next-hit buffs of the weapon are spent here.
        void DamageReceiverComponent.IHitModifier.ModifyOutgoingHit(DamageReceiverComponent victim, ref HitRequest request)
        {
            if (!HasStateAuthority)
                return;

            DamageTag tag = request.Tag;
            bool isWeapon = (tag & DamageTag.Melee) != 0 && !_isBonusHit;
            bool isPhysical = (tag & (DamageTag.Melee | DamageTag.Ranged | DamageTag.Thrown)) != 0;
            HealthComponent health = victim.Health;
            bool isMonster = victim.TryGetComponent(out MonsterComponent monster);
            victim.TryGetComponent(out StatusEffectComponent effects);
            float bonus = _effects.GetMagnitude(StatusEffectKind.DamageBonus) / 100f - _effects.GetMagnitude(StatusEffectKind.Weakened) / 100f;

            if (isWeapon && IsHolding(WeaponClass.Mace))
                bonus += Perk(PerkEffect.HeavyHand);

            if (isWeapon && IsHolding(WeaponClass.Dagger))
                bonus += Perk(PerkEffect.ShortBlade);

            if ((tag & DamageTag.Ranged) != 0)
                bonus += Perk(PerkEffect.LongShot) * Mathf.Clamp01(Vector3.Distance(victim.transform.position, transform.position) / LongShotRange);

            if ((tag & DamageTag.Fire) != 0)
                bonus += Perk(PerkEffect.FireMastery);

            if ((tag & DamageTag.Lightning) != 0)
                bonus += Perk(PerkEffect.DryAir) + (IsPlated(victim) ? Perk(PerkEffect.IronCalls) : 0f);

            if ((tag & DamageTag.Flask) != 0)
                bonus += Perk(PerkEffect.StrongCharge);

            if ((tag & DamageTag.Spell) != 0 && _fighter.Health.Progress < 0.5f)
                bonus += Perk(PerkEffect.DarkVoice);

            if (isMonster)
                bonus += Perk(PerkEffect.BeastHabit) + (monster.Config.IsUndead ? Perk(PerkEffect.Requiem) : 0f);

            if (health.Progress < LowHealth)
                bonus += Perk(PerkEffect.FinishOff);

            if (effects != null && (effects.Has(StatusEffectKind.Root) || effects.Has(StatusEffectKind.Slow)))
                bonus += Perk(PerkEffect.SnaredPrey);

            if (isPhysical && health.CurrentHealth >= health.MaxHealth)
                bonus += Perk(PerkEffect.FirstBlood);

            if (request.Zone == HitZone.Head)
                bonus += Perk(PerkEffect.Throat);

            float multiplier = 1f;

            if (isWeapon)
                multiplier = SpendWeaponBuffs(victim, ref request, ref bonus);

            request.BaseDamage = Mathf.Max(0, Mathf.RoundToInt(request.BaseDamage * Mathf.Max(0f, 1f + bonus) * multiplier));
        }

        /// The weapon buffs waiting for the next hit: a heavy blow, an opened guard, a finishing strike, righteous wrath and a sentence.
        private float SpendWeaponBuffs(DamageReceiverComponent victim, ref HitRequest request, ref float bonus)
        {
            float multiplier = 1f;

            if (Take(StatusEffectKind.HeavyBlow, out float heavy))
                bonus += heavy / 100f;

            if (Take(StatusEffectKind.Wrath, out float wrath))
                bonus += wrath / 100f;

            if (Take(StatusEffectKind.Expose, out float expose))
            {
                bonus += expose / 100f;
                request.IgnoreArmor = true;
            }

            float execute = _effects.GetMagnitude(StatusEffectKind.Execute);

            if (execute > 0f && victim.Health.Progress < ExecuteHealth)
            {
                _effects.Remove(StatusEffectKind.Execute);
                multiplier = execute;
            }

            if (Take(StatusEffectKind.Judgement, out float stun))
                request.StaggerDuration = Mathf.Max(request.StaggerDuration, stun);

            return multiplier;
        }

        private bool Take(StatusEffectKind kind, out float magnitude)
        {
            magnitude = _effects.GetMagnitude(kind);

            if (magnitude <= 0f)
                return false;

            _effects.Remove(kind);

            return true;
        }

        private static bool IsPlated(DamageReceiverComponent receiver)
        {
            return receiver.TryGetComponent(out AdventurerComponent adventurer) && adventurer._inventory.GetEquippedConfig<ArmorItemConfig>(EquipSlot.Chest) is { ArmorType: ArmorType.Plate };
        }

        float DamageReceiverComponent.IHitModifier.ModifyBlockMitigation(float mitigation)
        {
            return mitigation * (1f + Perk(PerkEffect.ShieldTraining));
        }

        /// Struck side: armor (unless the hit ignores it), guards and weaknesses, a wall in front, the habits of the alchemist and the tracker.
        int DamageReceiverComponent.IHitModifier.ModifyIncomingDamage(int damage, in HitRequest request)
        {
            if (_effects.Has(StatusEffectKind.Bulwark))
            {
                Vector3 toAttacker = Vector3.ProjectOnPlane(request.AttackerPosition - transform.position, Vector3.up);

                if (toAttacker.sqrMagnitude > 0.01f && Vector3.Angle(transform.forward, toAttacker) < BulwarkAngle)
                    return 0;
            }

            float reduced = request.IgnoreArmor ? damage : _stats.ModifyIncomingDamage(damage, request.DamageType, request.Zone);
            float guard = Mathf.Clamp(_effects.GetMagnitude(StatusEffectKind.Guard), 0f, 80f) / 100f;
            float taken = StatusDamage.Taken(_effects) - guard - (IsMixtureActive ? Perk(PerkEffect.Buzz) : 0f);

            if (request.Attacker == _fighter.Receiver && (request.Tag & (DamageTag.Flask | DamageTag.Fire)) != 0)
                taken *= 1f - Perk(PerkEffect.BlastHabit);

            if ((request.Tag & DamageTag.Trap) != 0)
                taken *= 1f - Perk(PerkEffect.TrapSense);

            return _effects.Absorb(Mathf.RoundToInt(reduced * Mathf.Max(0f, taken)));
        }

        /// What a landed hit leaves: marks, venom, healing tithe, a quickening onslaught, caustic splashes; and what a kill brings.
        private void OnDamageDealt(DamageReceiverComponent victim, in HitRequest request, StatusEffectComponent effects, bool isMelee)
        {
            DamageTag tag = request.Tag;
            bool isPhysical = (tag & (DamageTag.Melee | DamageTag.Ranged | DamageTag.Thrown)) != 0;

            if (effects != null && victim.IsAlive)
            {
                if (isPhysical && Perk(PerkEffect.HuntersMark) > 0f)
                    effects.Add(StatusEffectKind.Marked, Perk(PerkEffect.HuntersMark), MarkTime);

                float venom = _effects.GetMagnitude(StatusEffectKind.VenomBlade);

                if (venom > 0f && (isMelee || ((tag & DamageTag.Thrown) != 0 && Perk(PerkEffect.OiledBlades) > 0f)))
                    PoisonFor(effects, venom);

                if ((tag & DamageTag.Flask) != 0 && Perk(PerkEffect.CausticMix) > 0f)
                    effects.Add(StatusEffectKind.Vulnerable, Perk(PerkEffect.CausticMix), CausticTime);

                if (isMelee && Take(StatusEffectKind.DeadRoot, out float root))
                {
                    Poison(effects, root, DeadRootTime);
                    ApplyEffectTo(effects, StatusEffectKind.Slow, DeadRootSlow, DeadRootTime);
                }
            }

            if (isMelee)
            {
                if (Perk(PerkEffect.BloodTithe) > 0f)
                    _fighter.Health.Restore(Mathf.Max(1, Mathf.RoundToInt(_fighter.Health.MaxHealth * Perk(PerkEffect.BloodTithe) / 100f)));

                if (Perk(PerkEffect.Onslaught) > 0f)
                {
                    _onslaughtStacks = victim == _onslaughtVictim ? Mathf.Min(_onslaughtStacks + 1, OnslaughtMax) : 1;
                    _onslaughtVictim = victim;
                    _effects.Add(StatusEffectKind.Onslaught, Perk(PerkEffect.Onslaught) * _onslaughtStacks, OnslaughtTime);
                }
            }

            if (!victim.IsAlive)
                OnKill(victim, effects, tag);
        }

        private void OnKill(DamageReceiverComponent victim, StatusEffectComponent effects, DamageTag tag)
        {
            if (Perk(PerkEffect.CleanWork) > 0f)
            {
                for (int i = 0; i < _skillCount; i++)
                {
                    float left = GetCooldownLeft(i) - Perk(PerkEffect.CleanWork);
                    _cooldowns.Set(i, left > 0f ? TickTimer.CreateFromSeconds(Runner, left) : TickTimer.None);
                }
            }

            if (effects != null && effects.Has(StatusEffectKind.Cursed) && Perk(PerkEffect.TakeBack) > 0f)
                _fighter.Health.Restore(Mathf.RoundToInt(_fighter.Health.MaxHealth * Perk(PerkEffect.TakeBack) / 100f));

            if ((tag & DamageTag.Fire) == 0 || Perk(PerkEffect.FuneralHeat) <= 0f)
                return;

            Vector3 center = BodyPoint(victim);
            RpcBurst(VfxStyle.Fire, center, FuneralRadius, new Color(1f, 0.45f, 0.12f));
            GatherBodies(center, FuneralRadius);
            _isBonusHit = true;

            foreach (DamageReceiverComponent receiver in s_struck.ToArray())
                ZoneHit(receiver, Perk(PerkEffect.FuneralHeat), DamageType.Magical, AbilityFlags.Fire, 0.2f, center);

            _isBonusHit = false;
        }

        /// A blocked hit quickens the counter; a hit taken stirs righteous wrath at most once in a while.
        private void OnHitTaken(HitResult result, int damage, HitRequest request)
        {
            if (!HasStateAuthority)
                return;

            GainResource(result == HitResult.Hit ? ResourceSource.DamageTaken : ResourceSource.BlockedHit);

            if (result != HitResult.Hit && Perk(PerkEffect.Retaliation) > 0f)
                _effects.Add(StatusEffectKind.Retaliation, Perk(PerkEffect.Retaliation), RetaliationTime);

            if (result == HitResult.Hit && damage > 0 && request.Attacker != null && Perk(PerkEffect.RighteousWrath) > 0f && Runner.Tick >= _wrathReadyTick)
            {
                _effects.Add(StatusEffectKind.Wrath, Perk(PerkEffect.RighteousWrath), WrathTime);
                _wrathReadyTick = Runner.Tick + Mathf.CeilToInt(WrathCooldown / Runner.DeltaTime);
            }
        }

        /// A mixture's buff ran out: the body pays its side effect.
        private void OnEffectExpired(StatusEffectKind kind, float magnitude)
        {
            if (kind != StatusEffectKind.BullBlood || _sideEffect == StatusEffectKind.None || State != AdventurerState.Alive)
                return;

            _effects.Add(_sideEffect, _sideMagnitude, _sideDuration);
            _sideEffect = StatusEffectKind.None;
        }

        /// Banner, example, battle order, a healthy spirit and calm hands reach every ally around, the adventurer himself not included.
        private void SimulatePerkAuras()
        {
            if (Runner.Tick < _perkAuraTick)
                return;

            _perkAuraTick = Runner.Tick + Mathf.CeilToInt(AuraPulse / Runner.DeltaTime);
            float armor = Perk(PerkEffect.BannerAura);
            float example = Perk(PerkEffect.ExampleAura);
            float order = Perk(PerkEffect.FormationAura);
            float vitality = Perk(PerkEffect.VitalityAura);
            float calm = Perk(PerkEffect.CalmHandsAura);

            if (armor + example + order + vitality + calm <= 0f)
                return;

            foreach (FighterComponent fighter in FighterComponent.All)
            {
                if ((fighter.transform.position - transform.position).sqrMagnitude > PerkAuraRadius * PerkAuraRadius || !fighter.TryGetComponent(out AdventurerComponent ally)
                    || ally == this || !IsAllyOf(ally) || ally.State != AdventurerState.Alive)
                    continue;

                AddAura(ally._effects, StatusEffectKind.Armor, armor);
                AddAura(ally._effects, StatusEffectKind.Haste, example);
                AddAura(ally._effects, StatusEffectKind.DamageBonus, order);
                AddAura(ally._effects, StatusEffectKind.Vitality, vitality);
                AddAura(ally._effects, StatusEffectKind.UseSpeed, calm);
            }
        }

        /// An aura never overrides a stronger effect of the same kind.
        private static void AddAura(StatusEffectComponent effects, StatusEffectKind kind, float magnitude)
        {
            if (magnitude > 0f && effects.GetMagnitude(kind) <= magnitude)
                effects.Add(kind, magnitude, AuraPulse * 2.5f);
        }

        /// Wither: the target loses health every pulse and the caster drinks a share of it.
        private void StartDrain(DamageReceiverComponent target, AbilityConfig ability, float damage)
        {
            int pulses = Mathf.Max(1, Mathf.RoundToInt(ability.Duration / AuraPulse));
            _drainTarget = target;
            _drainLeft = pulses;
            _drainPerPulse = damage / pulses;
            _drainSteal = ability.LifeSteal;
            _drainTick = Runner.Tick;
        }

        private void SimulateDrain()
        {
            if (_drainTarget == null || Runner.Tick < _drainTick)
                return;

            if (_drainLeft <= 0f || !_drainTarget.IsAlive || _drainTarget.Object == null)
            {
                _drainTarget = null;

                return;
            }

            _drainLeft--;
            _drainTick = Runner.Tick + Mathf.CeilToInt(AuraPulse / Runner.DeltaTime);
            int before = _drainTarget.Health.CurrentHealth;
            Vector3 point = BodyPoint(_drainTarget);
            _drainTarget.ApplyHit(new HitRequest
            {
                BaseDamage = Mathf.Max(1, Mathf.RoundToInt(_drainPerPulse)), BodyRays = 1, Zone = HitZone.Torso, Point = point, Normal = (HandPoint - point).normalized,
                AttackerPosition = transform.position, DamageType = DamageType.Magical, Attacker = _fighter.Receiver, Tag = DamageTag.Spell | DamageTag.Dark
            });
            int dealt = before - Mathf.Max(0, _drainTarget.Health.CurrentHealth);

            if (dealt > 0)
                _fighter.Health.Restore(Mathf.Max(1, Mathf.RoundToInt(dealt * _drainSteal)));

            RpcBeam(VfxStyle.Dark, point, HandPoint, new Color(0.6f, 0.15f, 0.25f));
        }

        /// Bigger stacks of potions and throwing weapons for the provident and the knife bundle.
        private int StackBonus(ItemConfig item)
        {
            return item switch
            {
                ConsumableItemConfig { IsDrink: true } => Mathf.RoundToInt(Perk(PerkEffect.PotionStack)),
                UtilityItemConfig { UtilityKind: UtilityKind.ThrowingWeapon } => Mathf.RoundToInt(Perk(PerkEffect.KnifeBundle)),
                _ => 0
            };
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RpcBurst(VfxStyle style, Vector3 position, float radius, Color color)
        {
            AbilityVfx.Burst(style, position, radius, color);
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RpcBeam(VfxStyle style, Vector3 from, Vector3 to, Color color)
        {
            AbilityVfx.Beam(style, from, to, color);
        }
    }
}
