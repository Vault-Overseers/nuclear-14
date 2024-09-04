// License granted by JerryImMouse to Corvax Forge.
// Non-exclusive, non-transferable, perpetual license to use, distribute, and modify.
// All other rights reserved by JerryImMouse.
using System.Numerics;
using Content.Shared._NC.CameraFollow.Components;
using Content.Shared._NC.CameraFollow.Events;
using Content.Shared.Bed.Sleep;
using Content.Shared.Stunnable;

namespace Content.Shared._NC.CameraFollow.EntitySystems;

public sealed class CameraActionsSystem : EntitySystem
{
    [Dependency] private readonly SharedEyeSystem _eye = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CameraFollowComponent, ToggleCameraEvent>(OnToggleCamera);
        SubscribeLocalEvent<CameraFollowComponent, SleepStateChangedEvent>(OnSleeping);
    }

    private void OnSleeping(Entity<CameraFollowComponent> ent, ref SleepStateChangedEvent args)
    {
        SetCameraEnabled(ent, false);
        Dirty(ent);
    }

    private void OnToggleCamera(EntityUid uid, CameraFollowComponent component, ToggleCameraEvent args)
    {
        if (HasComp<SleepingComponent>(uid) || HasComp<StunnedComponent>(uid)) // Check if entity is sleeping right now(when sleeping entity has a shader without shadows, it can cause wallhacking)
        {
            args.Handled = true;
            return;
        }

        SetCameraEnabled((uid,component), !component.Enabled);
        Dirty(uid, component);
        args.Handled = true;
    }


    /// <summary>
    /// Sets the enabled state of the camera on server and client side
    /// </summary>
    /// <param name="component">CameraFollowComponent</param>
    /// <param name="enabled">Enabled boolean value</param>
    private void SetCameraEnabled(Entity<CameraFollowComponent> component, bool enabled)
    {
        component.Comp.Enabled = enabled;
        component.Comp.Offset = new Vector2(0, 0);
        _eye.SetOffset(component, component.Comp.Offset);
    }
}