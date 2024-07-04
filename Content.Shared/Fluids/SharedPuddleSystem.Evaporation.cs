using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;

namespace Content.Shared.Fluids;

public abstract partial class SharedPuddleSystem
{
    [ValidatePrototypeId<ReagentPrototype>]
    private const string Water = "Water";
    [ValidatePrototypeId<ReagentPrototype>]
    private const string WaterDirty = "WaterDirty";
    [ValidatePrototypeId<ReagentPrototype>]
    private const string WaterIrradiated = "WaterIrradiated";
    [ValidatePrototypeId<ReagentPrototype>]
    private const string WastelandBlood = "WastelandBlood";

    public static readonly string[] EvaporationReagents = [Water, WaterDirty, WaterIrradiated, WastelandBlood];

    public bool CanFullyEvaporate(Solution solution)
    {
        return solution.GetTotalPrototypeQuantity(EvaporationReagents) == solution.Volume;
    }
}
