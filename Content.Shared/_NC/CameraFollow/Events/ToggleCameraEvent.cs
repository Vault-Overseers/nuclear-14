// License granted by JerryImMouse to Corvax Forge.
// Non-exclusive, non-transferable, perpetual license to use, distribute, and modify.
// All other rights reserved by JerryImMouse.
using System.Numerics;
using Content.Shared.Actions;
using Robust.Shared.Serialization;

namespace Content.Shared._NC.CameraFollow.Events;

/// <summary>
/// Turns on/off the camera following the player's mouse position.
/// </summary>
public sealed partial class ToggleCameraEvent : InstantActionEvent
{
}

[Serializable, NetSerializable]
public sealed partial class ChangeCamOffsetEvent : EntityEventArgs
{
    public Vector2 Offset = Vector2.Zero;
}