using Robust.Shared.GameStates;

namespace Content.Server.Chemistry.Addiction;

/// <summary>
/// Indicates that an entity is addicted to various drugs.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class AddictedComponent : Component
{
    [DataField("addictions"), AutoNetworkedField]
    public HashSet<string> Addictions = new();

    /// <summary>
    /// Last time each drug was consumed. Used to determine withdrawal.
    /// Stored as seconds since round start to avoid TimeSpan serialization.
    /// </summary>
    [DataField("lastUse"), AutoNetworkedField]
    public Dictionary<string, float> LastUse = new();
}
