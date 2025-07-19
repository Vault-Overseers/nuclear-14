using Robust.Shared.Prototypes;

namespace Content.Shared._N14.Weapons.Attachments;

[DataDefinition]
public sealed partial record WeaponAttachmentSlot
{
    [DataField(required: true)]
    public string Name = string.Empty;

    [DataField]
    public EntProtoId? StartingAttachment;
}
