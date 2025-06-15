using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.KayMisaZlevels.Shared.Components;

/// <summary>
/// This is used for tracking members of a stack of maps.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, UnsavedComponent]
public sealed partial class ZStackMemberComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid Tracker;
}
