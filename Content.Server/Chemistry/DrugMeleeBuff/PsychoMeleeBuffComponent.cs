using Content.Shared.Damage.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.DrugMeleeBuff;

/// <summary>
///     Grants a melee damage bonus via <see cref="BonusMeleeDamageComponent"/> when active.
/// </summary>
[RegisterComponent]
public sealed partial class PsychoMeleeBuffComponent : Component
{
    [DataField("modifier")]
    public ProtoId<DamageModifierSetPrototype> ModifierSetId { get; set; } = "PsychoBonus";
}
