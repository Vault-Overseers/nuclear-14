﻿using Content.Shared.Ensnaring.Components;
using Content.Shared.Movement.Systems;

namespace Content.Shared.Ensnaring;

public abstract class SharedEnsnareableSystem : EntitySystem
{
    [Dependency] private readonly MovementSpeedModifierSystem _speedModifier = default!;
    [Dependency] protected readonly SharedAppearanceSystem Appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SharedEnsnareableComponent, RefreshMovementSpeedModifiersEvent>(MovementSpeedModify);
        SubscribeLocalEvent<SharedEnsnareableComponent, EnsnareEvent>(OnEnsnare);
        SubscribeLocalEvent<SharedEnsnareableComponent, EnsnareRemoveEvent>(OnEnsnareRemove);
        SubscribeLocalEvent<SharedEnsnareableComponent, EnsnaredChangedEvent>(OnEnsnareChange);
    }

    private void OnEnsnare(EntityUid uid, SharedEnsnareableComponent component, EnsnareEvent args)
    {
        component.WalkSpeed = args.WalkSpeed;
        component.SprintSpeed = args.SprintSpeed;

        _speedModifier.RefreshMovementSpeedModifiers(uid);

        var ev = new EnsnaredChangedEvent(component.IsEnsnared);
        RaiseLocalEvent(uid, ev);
    }

    private void OnEnsnareRemove(EntityUid uid, SharedEnsnareableComponent component, EnsnareRemoveEvent args)
    {
        _speedModifier.RefreshMovementSpeedModifiers(uid);

        var ev = new EnsnaredChangedEvent(component.IsEnsnared);
        RaiseLocalEvent(uid, ev);
    }

    private void OnEnsnareChange(EntityUid uid, SharedEnsnareableComponent component, EnsnaredChangedEvent args)
    {
        UpdateAppearance(uid, component);
    }

    private void UpdateAppearance(EntityUid uid, SharedEnsnareableComponent component, AppearanceComponent? appearance = null)
    {
        Appearance.SetData(uid, EnsnareableVisuals.IsEnsnared, component.IsEnsnared, appearance);
    }

    private void MovementSpeedModify(EntityUid uid, SharedEnsnareableComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        if (!component.IsEnsnared)
            return;

        args.ModifySpeed(component.WalkSpeed, component.SprintSpeed);
    }
}
