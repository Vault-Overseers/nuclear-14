using Content.Shared.Actions;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._N14.Weapons.Attachments;

[RegisterComponent, NetworkedComponent]
public sealed partial class BipodAttachmentComponent : Component
{
    [DataField(required: true)]
    public EntProtoId ActionPrototype = default!;

    [DataField]
    public EntityUid? ActionEntity;

    [DataField]
    public bool Deployed;
}

/// <summary>
/// Fired when the bipod toggle action is used.
/// </summary>
public sealed partial class BipodToggleActionEvent : InstantActionEvent;

