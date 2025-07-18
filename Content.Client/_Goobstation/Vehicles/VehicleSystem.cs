using System.Numerics;
using System.Collections.Generic;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.GameObjects;
using Content.Shared.Vehicles;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;

namespace Content.Client.Vehicles;

public sealed class VehicleSystem : SharedVehicleSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly SpriteSystem _sprites = default!;

    private const float FreezeRotationSpeed = 3f;
    private readonly Dictionary<EntityUid, Angle> _lastRotation = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<VehicleComponent, AppearanceChangeEvent>(OnAppearanceChange);
        SubscribeLocalEvent<VehicleComponent, MoveEvent>(OnMove);
        SubscribeLocalEvent<VehicleComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnAppearanceChange(EntityUid uid, VehicleComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!_appearance.TryGetData<bool>(uid, VehicleState.Animated, out var animated))
            return;

        if (!TryComp<SpriteComponent>(uid, out var spriteComp))
            return;

        SpritePos(uid, comp);
        spriteComp.LayerSetAutoAnimated(0, animated);
    }

    private void OnMove(EntityUid uid, VehicleComponent component, ref MoveEvent args)
    {
        SpritePos(uid, component);

        if (!TryComp<PhysicsComponent>(uid, out var physics))
            return;

        if (!TryComp<TransformComponent>(uid, out var xform))
            return;

        var speed = physics.LinearVelocity.Length();

        if (!_lastRotation.ContainsKey(uid))
            _lastRotation[uid] = xform.LocalRotation;

        if (speed >= FreezeRotationSpeed)
        {
            xform.LocalRotation = _lastRotation[uid];
        }
        else
        {
            _lastRotation[uid] = xform.LocalRotation;
        }
    }

    private void OnShutdown(EntityUid uid, VehicleComponent comp, ComponentShutdown args)
    {
        _lastRotation.Remove(uid);
    }

    private void SpritePos(EntityUid uid, VehicleComponent comp)
    {
        if (!TryComp<SpriteComponent>(uid, out var spriteComp))
            return;

        if (!_appearance.TryGetData<bool>(uid, VehicleState.DrawOver, out var depth))
            return;

        spriteComp.DrawDepth = (int)Content.Shared.DrawDepth.DrawDepth.Objects;

        if (comp.RenderOver == VehicleRenderOver.None)
            return;

        var eye = _eye.CurrentEye;
        var vehicleDir = (Transform(uid).LocalRotation + eye.Rotation).GetCardinalDir();
        var renderOver = (VehicleRenderOver)(1 << (int)vehicleDir);

        if ((comp.RenderOver & renderOver) == renderOver)
        {
            spriteComp.DrawDepth = (int)Content.Shared.DrawDepth.DrawDepth.OverMobs;
        }
        else if (depth)
        {
            spriteComp.DrawDepth = (int)Content.Shared.DrawDepth.DrawDepth.OverMobs;
        }
        else
        {
            spriteComp.DrawDepth = (int)Content.Shared.DrawDepth.DrawDepth.Objects;
        }
    }
}
