using Robust.Shared.GameStates;

namespace Content.Shared._N14.Weapons.Attachments;

[RegisterComponent, NetworkedComponent]
public sealed partial class WeaponAttachmentHolderComponent : Component
{
    [DataField]
    public Dictionary<string, WeaponAttachmentSlot> Slots = new();
}
