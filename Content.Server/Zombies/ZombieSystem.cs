using System.Linq;
using Robust.Shared.Random;
using Content.Server.Body.Systems;
using Content.Server.Disease.Components;
using Content.Server.Drone.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.MobState.Components;
using Content.Server.Disease;
using Content.Shared.Inventory;
using Content.Shared.MobState;
using Content.Server.Inventory;
using Robust.Shared.Prototypes;
using Content.Server.Speech;
using Content.Server.Chat.Systems;
using Content.Server.Weapons.Melee.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Damage;
using Content.Shared.Zombies;

namespace Content.Server.Zombies
{
    public sealed class ZombieSystem : SharedZombieSystem
    {
        [Dependency] private readonly DiseaseSystem _disease = default!;
        [Dependency] private readonly BloodstreamSystem _bloodstream = default!;
        [Dependency] private readonly ZombifyOnDeathSystem _zombify = default!;
        [Dependency] private readonly ServerInventorySystem _inv = default!;
        [Dependency] private readonly VocalSystem _vocal = default!;
        [Dependency] private readonly ChatSystem _chat = default!;
        [Dependency] private readonly IPrototypeManager _protoManager = default!;
        [Dependency] private readonly IRobustRandom _robustRandom = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ZombieComponent, MeleeHitEvent>(OnMeleeHit);
            SubscribeLocalEvent<ZombieComponent, MobStateChangedEvent>(OnMobState);
            SubscribeLocalEvent<ActiveZombieComponent, DamageChangedEvent>(OnDamage);

        }

        private void OnMobState(EntityUid uid, ZombieComponent component, MobStateChangedEvent args)
        {
            if (args.CurrentMobState == DamageState.Alive)
                EnsureComp<ActiveZombieComponent>(uid);
            else
                RemComp<ActiveZombieComponent>(uid);
        }

        private void OnDamage(EntityUid uid, ActiveZombieComponent component, DamageChangedEvent args)
        {
            if (args.DamageIncreased)
                DoGroan(uid, component);
        }

        private float GetZombieInfectionChance(EntityUid uid, ZombieComponent component)
        {
            var baseChance = component.MaxZombieInfectionChance;

            if (!TryComp<InventoryComponent>(uid, out var inventoryComponent))
                return baseChance;

            var enumerator =
                new InventorySystem.ContainerSlotEnumerator(uid, inventoryComponent.TemplateId, _protoManager, _inv,
                    SlotFlags.FEET |
                    SlotFlags.HEAD |
                    SlotFlags.EYES |
                    SlotFlags.GLOVES |
                    SlotFlags.MASK |
                    SlotFlags.NECK |
                    SlotFlags.INNERCLOTHING |
                    SlotFlags.OUTERCLOTHING);

            var items = 0f;
            var total = 0f;
            while (enumerator.MoveNext(out var con))
            {
                total++;

                if (con.ContainedEntity != null)
                    items++;
            }

            var max = component.MaxZombieInfectionChance;
            var min = component.MinZombieInfectionChance;
            //gets a value between the max and min based on how many items the entity is wearing
            var chance = (max-min) * ((total - items)/total) + min;
            return chance;
        }

        private void OnMeleeHit(EntityUid uid, ZombieComponent component, MeleeHitEvent args)
        {
            if (!EntityManager.TryGetComponent<ZombieComponent>(args.User, out var zombieComp))
                return;

            if (!args.HitEntities.Any())
                return;

            foreach (var entity in args.HitEntities)
            {
                if (args.User == entity)
                    continue;

                if (!TryComp<MobStateComponent>(entity, out var mobState) || HasComp<DroneComponent>(entity))
                    continue;

                if (HasComp<DiseaseCarrierComponent>(entity) && _robustRandom.Prob(GetZombieInfectionChance(entity, component)))
                    _disease.TryAddDisease(entity, "ActiveZombieVirus");

                if (HasComp<ZombieComponent>(entity))
                    args.BonusDamage = -args.BaseDamage * zombieComp.OtherZombieDamageCoefficient;

                if ((mobState.CurrentState == DamageState.Dead || mobState.CurrentState == DamageState.Critical)
                    && !HasComp<ZombieComponent>(entity))
                {
                    _zombify.ZombifyEntity(entity);
                    args.BonusDamage = -args.BaseDamage;
                }
                else if (mobState.CurrentState == DamageState.Alive) //heals when zombies bite live entities
                {
                    var healingSolution = new Solution();
                    healingSolution.AddReagent("Bicaridine", 1.00); //if OP, reduce/change chem
                    _bloodstream.TryAddToChemicals(args.User, healingSolution);
                }
            }
        }

        private void DoGroan(EntityUid uid, ActiveZombieComponent component)
        {
            if (component.LastDamageGroanCooldown > 0)
                return;

            if (_robustRandom.Prob(0.5f)) //this message is never seen by players so it just says this for admins
                _chat.TrySendInGameICMessage(uid, "[automated zombie groan]", InGameICChatType.Speak, false);
            else
                _vocal.TryScream(uid);

            component.LastDamageGroanCooldown = component.GroanCooldown;
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var zombiecomp in EntityQuery<ActiveZombieComponent>())
            {
                zombiecomp.Accumulator += frameTime;
                zombiecomp.LastDamageGroanCooldown -= frameTime;

                if (zombiecomp.Accumulator < zombiecomp.RandomGroanAttempt)
                    continue;
                zombiecomp.Accumulator -= zombiecomp.RandomGroanAttempt;

                if (!_robustRandom.Prob(zombiecomp.GroanChance))
                    continue;

                //either do a random accent line or scream
                DoGroan(zombiecomp.Owner, zombiecomp);
            }
        }
    }
}
