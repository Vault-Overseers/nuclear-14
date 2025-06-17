using Robust.Shared.GameStates;

namespace Content.Shared._Lavaland.Weapons;

/// <summary>
///     Base component for weapon attachments.
/// </summary>
public abstract partial class AttachmentComponent : Component
{
}

/// <summary>
///     Component to indicate a valid bayonet for weapon attachment.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class AttachmentBayonetComponent : AttachmentComponent
{
}

/// <summary>
///     Component to indicate a valid flashlight for weapon attachment.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class AttachmentFlashlightComponent : AttachmentComponent
{
}
