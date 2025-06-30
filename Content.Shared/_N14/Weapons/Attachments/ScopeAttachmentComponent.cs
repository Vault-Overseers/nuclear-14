using System.Numerics;
using Content.Shared._NC.CameraFollow.Components;
using Content.Shared._NC.FollowDistance.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._N14.Weapons.Attachments;

[RegisterComponent, NetworkedComponent]
public sealed partial class ScopeAttachmentComponent : Component
{
    // Allows disabling the scoped camera toggle by leaving this null.
    [DataField]
    public EntProtoId? ActionPrototype = "ActionToggleCamera";

    // Runtime storage of the weapon's original follow distance
    [DataField] public Vector2? OriginalMaxDistance;
    [DataField] public float? OriginalBackStrength;

    [DataField] public EntityUid? ActionEntity;
}
