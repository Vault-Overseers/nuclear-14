using Content.Shared.Damage.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.DrugProtection;

/// <summary>
///     Damage resistance applied by Buffout.
///     Adds a DamageProtectionBuffComponent with the specified modifier set.
/// </summary>
[RegisterComponent]
public sealed partial class BuffoutProtectionComponent : Component, IDrugProtectionComponent
{
    [DataField("modifier")]
    public ProtoId<DamageModifierSetPrototype> ModifierSetId { get; set; } = "BuffoutDrug";
}
