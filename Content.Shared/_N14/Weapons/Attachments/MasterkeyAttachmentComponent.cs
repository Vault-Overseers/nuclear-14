using Content.Shared.Actions;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._N14.Weapons.Attachments;

[RegisterComponent, NetworkedComponent]
public sealed partial class MasterkeyAttachmentComponent : Component
{
    [DataField(required: true)]
    public EntProtoId GunPrototype = default!;

    [DataField(required: true)]
    public EntProtoId ActionPrototype = default!;

    [DataField]
    public EntityUid? GunEntity;

    [DataField]
    public EntityUid? ActionEntity;
}

/// <summary>
/// Fired when the masterkey action is used.
/// </summary>
public sealed partial class MasterkeyActionEvent : WorldTargetActionEvent;

