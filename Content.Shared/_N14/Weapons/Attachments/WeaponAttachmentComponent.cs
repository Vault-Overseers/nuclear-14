using Robust.Shared.GameStates;

namespace Content.Shared._N14.Weapons.Attachments;

[RegisterComponent, NetworkedComponent]
public sealed partial class WeaponAttachmentComponent : Component
{
    [DataField(required: true)]
    public string Slot = string.Empty;
}
