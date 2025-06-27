using Content.Shared.Actions;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared._NC.FollowDistance.Components;
using Content.Shared._NC.CameraFollow.Components;
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

    public override void Initialize()
    {
        SubscribeLocalEvent<WeaponAttachmentHolderComponent, MapInitEvent>(OnHolderInit);
        SubscribeLocalEvent<WeaponAttachmentHolderComponent, ComponentShutdown>(OnHolderShutdown);
        SubscribeLocalEvent<GrenadeLauncherActionEvent>(OnGrenadeLaunch);
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

    private void SetupAttachment(EntityUid holder, EntityUid attachment)
    {
        if (TryComp<GrenadeLauncherAttachmentComponent>(attachment, out var gl))
        {
            gl.GunEntity = Spawn(gl.GunPrototype);
            _actions.AddAction(holder, ref gl.ActionEntity, gl.ActionPrototype);
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
            if (scope.ActionPrototype != null)
                _actions.AddAction(holder, ref scope.ActionEntity, scope.ActionPrototype);
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
        _containers.Remove(ent);
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
}
