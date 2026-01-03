using Robust.Shared.GameStates;

namespace Content.Server.Chemistry.Addiction;

/// <summary>
/// Tracks tolerance levels for various drugs on an entity.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class DrugToleranceComponent : Component
{
    [DataField("tolerances"), AutoNetworkedField]
    public Dictionary<string, float> Tolerances = new();
}
