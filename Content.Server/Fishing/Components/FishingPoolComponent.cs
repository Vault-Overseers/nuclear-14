using Content.Shared.EntityList;
using Robust.Shared.Prototypes;

namespace Content.Server.Fishing.Components;

[RegisterComponent]
public sealed partial class FishingPoolComponent : Component
{
    [DataField("loot", required: true)]
    public ProtoId<EntityLootTablePrototype> LootTable = default!;

    [DataField("successChance")]
    public float SuccessChance = 0.5f;
}
