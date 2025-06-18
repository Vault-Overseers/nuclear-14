using Content.Server.Kitchen.Components;
using Content.Shared._Lavaland.Weapons;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Toggleable;
using Content.Shared.Light;
using Content.Shared.Light.Components;

namespace Content.Server._Lavaland.Weapons;

public sealed class WeaponAttachmentSystem : SharedWeaponAttachmentSystem
{
    [Dependency] private readonly SharedHandheldLightSystem _handheldLight = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<WeaponAttachmentComponent, ToggleActionEvent>(OnToggleLight);
    }

    protected override void AddSharp(EntityUid uid) => EnsureComp<SharpComponent>(uid);
    protected override void RemSharp(EntityUid uid) => RemCompDeferred<SharpComponent>(uid);

    protected override void AddScope(EntityUid uid, EntityUid scope)
    {
        if (!TryComp(scope, out FollowDistanceComponent? scopeComp))
            return;

        var follow = EnsureComp<FollowDistanceComponent>(uid);
        follow.MaxDistance = scopeComp.MaxDistance;
        follow.BackStrength = scopeComp.BackStrength;
    }

    protected override void RemScope(EntityUid uid)
    {
        RemCompDeferred<FollowDistanceComponent>(uid);
    }

    private void OnToggleLight(EntityUid uid, WeaponAttachmentComponent component, ToggleActionEvent args)
    {
        if (!component.LightAttached)
            return;

        component.LightOn = !component.LightOn;

        if (_itemSlots.TryGetSlot(uid, WeaponAttachmentComponent.LightSlotId, out var slot) &&
            slot.Item is EntityUid flashlight &&
            TryComp<HandheldLightComponent>(flashlight, out var lightComp))
            _handheldLight.SetActivated(flashlight, component.LightOn, lightComp);

        Dirty(uid, component);
    }
}
