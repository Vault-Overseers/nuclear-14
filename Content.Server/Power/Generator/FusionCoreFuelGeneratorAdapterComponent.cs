using Content.Shared.Containers.ItemSlots;
using Content.Shared.Power.Generator;

namespace Content.Server.Power.Generator;

/// <summary>
/// Allows a fuel generator to run when a fusion core is inserted.
/// </summary>
[RegisterComponent, Access(typeof(GeneratorSystem))]
public sealed partial class FusionCoreFuelGeneratorAdapterComponent : Component
{
    /// <summary>
    /// The item slot id used for the fusion core.
    /// </summary>
    [DataField("slotId", required: true)]
    public string SlotId = "fusion_core";
}

