using Content.Shared.Damage.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.DrugProtection;

public interface IDrugProtectionComponent
{
    ProtoId<DamageModifierSetPrototype> ModifierSetId { get; }
}
