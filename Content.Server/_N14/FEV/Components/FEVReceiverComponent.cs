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
    /// Weighted entity list used when selecting a random mutation result.
    /// The target entities should be humanoid mobs that will replace the current one.
    /// </summary>
    [DataField("entityWeights", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<WeightedRandomEntityPrototype>))]
    public string EntityWeights = "FEVHumanTransforms";

    /// <summary>
    /// Minimum FEV units required before a slow transformation begins.
    /// </summary>
    [DataField("slowThreshold")] public FixedPoint2 SlowThreshold = FixedPoint2.New(5);

    /// <summary>
    /// Minimum FEV units required for an instant transformation.
    /// </summary>
    [DataField("instantThreshold")] public FixedPoint2 InstantThreshold = FixedPoint2.New(15);

    /// <summary>
    /// Accumulated units required before the victim violently vomits to purge the virus.
    /// </summary>
    [DataField("vomitThreshold")] public FixedPoint2 VomitThreshold = FixedPoint2.New(10);


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
    public bool Vomited;

    /// <summary>
    /// Optional mapping of inventory slots to item prototypes. When a mob finishes
    /// mutating the specified slots will be replaced with these items. Anything
    /// currently equipped in the slot will be dropped.
    /// </summary>
    [DataField("slotReplacements")]
    public Dictionary<string, string>? SlotReplacements;
}
