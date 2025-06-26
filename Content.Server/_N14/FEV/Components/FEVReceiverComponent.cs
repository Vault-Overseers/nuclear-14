using Content.Shared.Humanoid;
using Content.Shared.Random;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server._N14.FEV.Components;

/// <summary>
/// Tracks metabolized FEV on a mob and handles transformation once thresholds are met.
/// </summary>
[RegisterComponent]
public sealed partial class FEVReceiverComponent : Component
{
    /// <summary>
    /// Weighted species list used when selecting a random mutation result.
    /// </summary>
    [DataField("speciesWeights", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<WeightedRandomSpeciesPrototype>))]
    public string SpeciesWeights = "SpeciesWeights";

    /// <summary>
    /// Minimum FEV units required before a slow transformation begins.
    /// </summary>
    [DataField("slowThreshold")] public FixedPoint2 SlowThreshold = FixedPoint2.New(5);

    /// <summary>
    /// Minimum FEV units required for an instant transformation.
    /// </summary>
    [DataField("instantThreshold")] public FixedPoint2 InstantThreshold = FixedPoint2.New(15);

    /// <summary>
    /// Messages shown to the player during a slow transformation.
    /// </summary>
    [DataField("stageMessages")]
    public List<string> StageMessages = new() { "fev-stage-1", "fev-stage-2", "fev-stage-3" };

    /// <summary>
    /// Time between transformation stage messages.
    /// </summary>
    [DataField("stageInterval")] public TimeSpan StageInterval = TimeSpan.FromSeconds(5);

    public FixedPoint2 Accumulated;
    public bool Transforming;
    public string? TargetSpecies;
    public int CurrentStage;
    public TimeSpan NextStage;
}
