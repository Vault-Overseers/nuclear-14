using Content.Shared.Actions;
using Content.Shared.Actions.Events;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared._NC.FollowDistance.Components;
using Content.Shared._NC.CameraFollow.Components;
using Content.Shared.Interaction;
using Content.Shared.Verbs;
using Content.Shared.IdentityManagement;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared._N14.Weapons.Attachments;

public sealed class WeaponAttachmentSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedContainerSystem _containers = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedGunSystem _gun = default!;

    private static readonly VerbCategory ModsCategory = new("verb-categories-mods");

    public override void Initialize()
    {
        SubscribeLocalEvent<WeaponAttachmentHolderComponent, MapInitEvent>(OnHolderInit);
        SubscribeLocalEvent<WeaponAttachmentHolderComponent, ComponentShutdown>(OnHolderShutdown);
        SubscribeLocalEvent<WeaponAttachmentHolderComponent, InteractUsingEvent>(OnHolderInteractUsing);
        SubscribeLocalEvent<WeaponAttachmentHolderComponent, GetVerbsEvent<AlternativeVerb>>(OnHolderGetVerbs);
        SubscribeLocalEvent<WeaponAttachmentHolderComponent, GetItemActionsEvent>(OnHolderGetItemActions);
        SubscribeLocalEvent<GrenadeLauncherActionEvent>(OnGrenadeLaunch);
        SubscribeLocalEvent<MasterkeyActionEvent>(OnMasterkeyFire);
        SubscribeLocalEvent<BipodToggleActionEvent>(OnBipodToggle);
        SubscribeLocalEvent<WeaponAttachmentHolderComponent, GunRefreshModifiersEvent>(OnGunRefreshModifiers);
    }

    private void OnHolderInit(EntityUid uid, WeaponAttachmentHolderComponent comp, MapInitEvent args)
    {
        foreach (var (id, slot) in comp.Slots)
        {
            if (slot.StartingAttachment == null)
                continue;

            var container = _containers.EnsureContainer<ContainerSlot>(uid, id);
            var ent = Spawn(slot.StartingAttachment, Transform(uid).Coordinates);
            _containers.Insert(ent, container);
            SetupAttachment(uid, ent);
        }
    }

    private void OnHolderShutdown(EntityUid uid, WeaponAttachmentHolderComponent comp, ComponentShutdown args)
    {
        foreach (var (id, slot) in comp.Slots)
        {
            if (_containers.TryGetContainer(uid, id, out var container))
            {
                foreach (var ent in container.ContainedEntities)
                    CleanupAttachment(uid, ent);
            }
        }
    }

    private void OnHolderInteractUsing(EntityUid uid, WeaponAttachmentHolderComponent comp, InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (TryComp<WeaponAttachmentComponent>(args.Used, out var attach))
            args.Handled = TryAttach(uid, args.Used);
    }

    private void OnHolderGetVerbs(EntityUid uid, WeaponAttachmentHolderComponent comp, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        foreach (var (id, slot) in comp.Slots)
        {
            if (!_containers.TryGetContainer(uid, id, out var container) || container.ContainedEntities.Count == 0)
                continue;

            var ent = container.ContainedEntities[0];
            var verb = new AlternativeVerb
            {
                Text = Identity.Name(ent, EntityManager),
                Category = ModsCategory,
                Act = () => TryDetach(uid, id)
            };

            args.Verbs.Add(verb);
        }
    }

    private void OnHolderGetItemActions(EntityUid uid, WeaponAttachmentHolderComponent comp, GetItemActionsEvent args)
    {
        foreach (var (id, slot) in comp.Slots)
        {
            if (!_containers.TryGetContainer(uid, id, out var container))
                continue;

            foreach (var ent in container.ContainedEntities)
            {
                if (TryComp<GrenadeLauncherAttachmentComponent>(ent, out var gl))
                    args.AddAction(ref gl.ActionEntity, gl.ActionPrototype);

                if (TryComp<ScopeAttachmentComponent>(ent, out var scope) && scope.ActionPrototype is { } proto)
                    args.AddAction(ref scope.ActionEntity, proto);
            }
        }
    }

    private void SetupAttachment(EntityUid holder, EntityUid attachment)
    {
        if (TryComp<GrenadeLauncherAttachmentComponent>(attachment, out var gl))
        {
            gl.GunEntity = Spawn(gl.GunPrototype);
            _actions.AddAction(holder, ref gl.ActionEntity, gl.ActionPrototype);
        }

        if (TryComp<MasterkeyAttachmentComponent>(attachment, out var mk))
        {
            mk.GunEntity = Spawn(mk.GunPrototype);
            _actions.AddAction(holder, ref mk.ActionEntity, mk.ActionPrototype);
        }

        if (TryComp<BipodAttachmentComponent>(attachment, out var bipod))
        {
            _actions.AddAction(holder, ref bipod.ActionEntity, bipod.ActionPrototype);
        }

        if (HasComp<SilencerAttachmentComponent>(attachment) &&
            HasComp<GunComponent>(holder))
        {
            _gun.RefreshModifiers(holder);
        }

        if (TryComp<ScopeAttachmentComponent>(attachment, out var scope) &&
            TryComp<FollowDistanceComponent>(attachment, out var scopeFollow) &&
            TryComp<FollowDistanceComponent>(holder, out var holderFollow))
        {
            scope.OriginalBackStrength = holderFollow.BackStrength;
            scope.OriginalMaxDistance = holderFollow.MaxDistance;
            holderFollow.BackStrength = scopeFollow.BackStrength;
            holderFollow.MaxDistance = scopeFollow.MaxDistance;

            var camera = EnsureComp<CameraFollowComponent>(holder);
            if (scope.ActionPrototype is { } actionProto)
                _actions.AddAction(holder, ref scope.ActionEntity, actionProto);
        }
    }

    private void CleanupAttachment(EntityUid holder, EntityUid attachment)
    {
        if (TryComp<GrenadeLauncherAttachmentComponent>(attachment, out var gl))
        {
            if (gl.GunEntity != null)
                QueueDel(gl.GunEntity.Value);
            if (gl.ActionEntity != null)
                _actions.RemoveAction(holder, gl.ActionEntity.Value);
        }

        if (TryComp<MasterkeyAttachmentComponent>(attachment, out var mk))
        {
            if (mk.GunEntity != null)
                QueueDel(mk.GunEntity.Value);
            if (mk.ActionEntity != null)
                _actions.RemoveAction(holder, mk.ActionEntity.Value);
        }

        if (TryComp<ScopeAttachmentComponent>(attachment, out var scope) &&
            TryComp<FollowDistanceComponent>(holder, out var holderFollow))
        {
            if (scope.OriginalBackStrength != null)
                holderFollow.BackStrength = scope.OriginalBackStrength.Value;
            if (scope.OriginalMaxDistance != null)
                holderFollow.MaxDistance = scope.OriginalMaxDistance.Value;

            if (scope.ActionEntity != null)
                _actions.RemoveAction(holder, scope.ActionEntity.Value);

            if (HasComp<CameraFollowComponent>(holder))
                RemCompDeferred<CameraFollowComponent>(holder);
        }

        if (TryComp<BipodAttachmentComponent>(attachment, out var bipod))
        {
            if (bipod.ActionEntity != null)
                _actions.RemoveAction(holder, bipod.ActionEntity.Value);
        }

        if (HasComp<SilencerAttachmentComponent>(attachment) &&
            HasComp<GunComponent>(holder))
        {
            _gun.RefreshModifiers(holder);
        }
    }

    public bool TryAttach(EntityUid holder, EntityUid attachment)
    {
        if (!TryComp<WeaponAttachmentHolderComponent>(holder, out var holderComp) ||
            !TryComp<WeaponAttachmentComponent>(attachment, out var attachComp))
            return false;

        if (!holderComp.Slots.TryGetValue(attachComp.Slot, out var slot))
            return false;

        var container = _containers.EnsureContainer<ContainerSlot>(holder, attachComp.Slot);
        if (!_containers.Insert(attachment, container))
            return false;

        SetupAttachment(holder, attachment);
        return true;
    }

    public bool TryDetach(EntityUid holder, string slotId)
    {
        if (!TryComp<WeaponAttachmentHolderComponent>(holder, out var holderComp))
            return false;

        if (!_containers.TryGetContainer(holder, slotId, out var container) || container.ContainedEntities.Count == 0)
            return false;

        var ent = container.ContainedEntities[0];
        _containers.Remove(ent, container);
        CleanupAttachment(holder, ent);
        return true;
    }

    private void OnGrenadeLaunch(GrenadeLauncherActionEvent ev)
    {
        if (!TryComp<GrenadeLauncherAttachmentComponent>(ev.Performer, out var gl) || gl.GunEntity == null)
            return;

        if (TryComp<GunComponent>(gl.GunEntity.Value, out var gun))
            _gun.AttemptShoot(ev.Performer, gl.GunEntity.Value, gun, ev.Target);
    }

    private void OnMasterkeyFire(MasterkeyActionEvent ev)
    {
        if (!TryComp<MasterkeyAttachmentComponent>(ev.Performer, out var mk) || mk.GunEntity == null)
            return;

        if (TryComp<GunComponent>(mk.GunEntity.Value, out var gun))
            _gun.AttemptShoot(ev.Performer, mk.GunEntity.Value, gun, ev.Target);
    }

    private void OnBipodToggle(BipodToggleActionEvent ev)
    {
        if (!TryComp<BipodAttachmentComponent>(ev.Performer, out var bipod))
            return;

        bipod.Deployed = !bipod.Deployed;

        if (bipod.ActionEntity != null)
            _actions.SetToggled(bipod.ActionEntity.Value, bipod.Deployed);

        if (TryComp<GunComponent>(ev.Performer, out _))
            _gun.RefreshModifiers(ev.Performer);
    }

    private void OnGunRefreshModifiers(EntityUid uid, WeaponAttachmentHolderComponent comp, ref GunRefreshModifiersEvent args)
    {
        foreach (var (id, _) in comp.Slots)
        {
            if (!_containers.TryGetContainer(uid, id, out var container))
                continue;

            foreach (var ent in container.ContainedEntities)
            {
                if (TryComp<SilencerAttachmentComponent>(ent, out var silencer))
                {
                    args.SoundGunshot = silencer.Sound;
                }

                if (TryComp<BipodAttachmentComponent>(ent, out var bipod) && bipod.Deployed)
                {
                    args.CameraRecoilScalar *= 0.75f;
                    args.AngleIncrease = Angle.FromDegrees(args.AngleIncrease.Degrees * 0.5f);
                    args.AngleDecay = Angle.FromDegrees(args.AngleDecay.Degrees * 0.5f);
                    args.MaxAngle = Angle.FromDegrees(args.MaxAngle.Degrees * 0.5f);
                    args.MinAngle = Angle.FromDegrees(args.MinAngle.Degrees * 0.5f);
                }
            }
        }
    }
}
