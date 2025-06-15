using Content.KayMisaZlevels.Shared.Systems;
using Content.Shared._Shitmed.Targeting;
using Content.Shared.Administration.Logs;
using Content.Shared.Damage;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using JetBrains.Annotations;
using Robust.Shared.Physics.Systems;

namespace Content.Shared._KMZLevels.Falling;

[UsedImplicitly]
public sealed class FallingSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly DamageableSystem _damSystem = default!;
    [Dependency] private readonly SharedPhysicsSystem _phys = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FallingComponent, ZLevelDroppedEvent>(OnDropped);
    }

    private void OnDropped(Entity<FallingComponent> ent, ref ZLevelDroppedEvent args)
    {
        _stun.TryParalyze(ent, ent.Comp.LandingStunTime, true);
        if (!ent.Comp.IgnoreDamage)
        {
            _damSystem.TryChangeDamage(ent, ent.Comp.BaseDamage * args.Distance * ent.Comp.DamageModifier, ignoreResistances: true, targetPart: TargetBodyPart.LeftLeg);
            _damSystem.TryChangeDamage(ent, ent.Comp.BaseDamage * args.Distance * ent.Comp.DamageModifier, ignoreResistances: true, targetPart: TargetBodyPart.RightLeg);
        }

        foreach (var contact in _phys.GetCollidingEntities(Transform(ent.Owner).MapID, _entityLookup.GetWorldAABB(ent.Owner)))
        {
            if (contact.Owner == ent.Owner)
                continue;
            _stun.TryParalyze(contact.Owner, ent.Comp.LandingStunTime, true);
            _damSystem.TryChangeDamage(contact.Owner, ent.Comp.BaseDamage * args.Distance, ignoreResistances: true, targetPart: TargetBodyPart.Head);
            // Neck isn't defined in our TargetBodyPart enum so deal torso damage instead.
            _damSystem.TryChangeDamage(contact.Owner, ent.Comp.BaseDamage * args.Distance, ignoreResistances: true, targetPart: TargetBodyPart.Torso);
        }

        //if (TryComp<JumpComponent>(ent.Owner, out var jumpComp))
        //    jumpComp.IsFailed = false;
    }
}
