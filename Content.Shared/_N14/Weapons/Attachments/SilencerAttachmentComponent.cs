using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._N14.Weapons.Attachments;

[RegisterComponent, NetworkedComponent]
public sealed partial class SilencerAttachmentComponent : Component
{
    [DataField(required: true)]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/Admin/ahelp_receive.ogg");

    [DataField]
    public SoundSpecifier? OriginalSound;
}
