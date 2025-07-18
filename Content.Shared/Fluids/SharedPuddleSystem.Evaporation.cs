using System;
using System.Linq;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;

namespace Content.Shared.Fluids;

public abstract partial class SharedPuddleSystem
{
    public static string[] EvaporationReagents { get; private set; } = Array.Empty<string>();

    private void InitializeEvaporation()
    {
        EvaporationReagents = _prototypeManager
            .EnumeratePrototypes<ReagentPrototype>()
            .Where(p => p.Evaporates)
            .Select(p => p.ID)
            .ToArray();
    }

    public bool CanFullyEvaporate(Solution solution)
    {
        return solution.GetTotalPrototypeQuantity(EvaporationReagents) == solution.Volume;
    }
}
