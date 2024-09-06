// License granted by JerryImMouse to Corvax Forge.
// Non-exclusive, non-transferable, perpetual license to use, distribute, and modify.
// All other rights reserved by JerryImMouse.
using System.Numerics;
using Content.Shared._NC.CameraFollow.Components;
using Content.Shared._NC.CameraFollow.Events;
using Content.Shared._NC.FollowDistance.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Shared.Timing;

namespace Content.Client._NC.CameraFollow;

/// <summary>
/// Use to make camera follow for player's mouse, uses Lerp() func. to follow player's mouse
/// </summary>
public sealed class CameraFollowSystem : EntitySystem
{
    [Dependency] private readonly IInputManager _input = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IEyeManager _manager = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        // Check if mouse position is valid
        if (!_timing.IsFirstTimePredicted || !_input.MouseScreenPosition.IsValid)
            return;

        var player = _player.LocalSession?.AttachedEntity;

        if (player == null || !TryComp<CameraFollowComponent>(player, out var followComponent))
            return;
        if (!TryComp<EyeComponent>(player, out var eye) || !followComponent.Enabled)
            return;

        // Get player map position and eye offset
        var xform = Transform(player.Value);
        var playerPos = _transform.GetMapCoordinates(xform).Position;
        var eyeOffset = new Vector2(eye.Offset.X, eye.Offset.Y);

        // Get mouse position on map
        var coords = _input.MouseScreenPosition;
        var mapPos = _manager.ScreenToMap(coords);

        // Get currentLerpProgress and difference between player position and mouse position
        float currentLerpTime = 0;
        currentLerpTime += frameTime;
        if (currentLerpTime > followComponent.LerpTime)
        {
            currentLerpTime = followComponent.LerpTime;
        }

        // Counts diff between player and mouse position
        // Counts our progress through lerp, don't touch it please, idk how it works.
        var lerpProgress = currentLerpTime / followComponent.LerpTime;
        var diff = new Vector2(mapPos.X, mapPos.Y) - playerPos;

        // Checks if player pointed his mouse on UI
        if (mapPos is { X: 0, Y: 0 })
            return;

        var offset = followComponent.Offset;
        // If eye offset is higher than max distance lerp offset to max distance and apply "back strength"
        // TODO: Prob need to change the multiplication of lerpProgress to 4f, because this can cause the camera to jerk around


        if (Math.Abs(eyeOffset.X) >= Math.Abs(followComponent.MaxDistance.X) ||
            Math.Abs(eyeOffset.Y) >= Math.Abs(followComponent.MaxDistance.Y))
        {
            offset = Vector2.Lerp(offset, followComponent.MaxDistance, lerpProgress * followComponent.BackStrength);
        }
        // Just lerp to calculated offset position
        offset = Vector2.Lerp(offset, diff, lerpProgress);

        RaisePredictiveEvent(new ChangeCamOffsetEvent
        {
            Offset = offset
        });
    }
}