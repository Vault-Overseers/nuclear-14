using Content.Shared.Damage.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.DrugProtection;

/// <summary>
///     Damage resistance against radiation for use with Rad-X.
///     Adds a DamageProtectionBuffComponent with the specified modifier set.
/// </summary>
[RegisterComponent]
public sealed partial class RadXProtectionComponent : Component, IDrugProtectionComponent
{
    [DataField("modifier")]
    public ProtoId<DamageModifierSetPrototype> ModifierSetId { get; set; } = "RadXDrug";
}
