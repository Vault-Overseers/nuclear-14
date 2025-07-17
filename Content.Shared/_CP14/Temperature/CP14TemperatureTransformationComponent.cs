using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CP14.Temperature;

/// <summary>
/// passively returns the solution temperature to the standard
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CP14TemperatureTransformationComponent : Component
{
    [DataField(required: true)]
    public List<CP14TemperatureTransformEntry> Entries = new();

    /// <summary>
    /// solution where reagents will be added from newly added ingredients
    /// </summary>
    [DataField]
    public string Solution = "food";

    /// <summary>
    /// The transformation will occur instantly if the entity was in FoodCookerComponent at the moment cooking ended.
    /// </summary>
    [DataField]
    public bool AutoTransformOnCooked = true;
}

[DataRecord]
public record struct CP14TemperatureTransformEntry()
{
    public EntProtoId? TransformTo { get; set; } = null;
    public Vector2 TemperatureRange { get; set; } = new();
}
