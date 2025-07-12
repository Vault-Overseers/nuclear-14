using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Strap.Components;
using Content.Shared.Physics;
using Robust.Shared.Physics.Components;

namespace Content.Server.FEV;

public sealed partial class FEVVatSystem : EntitySystem
{
    [Dependency] private readonly SolutionContainerSystem _solutions = default!;
    [Dependency] private readonly SolutionTransferSystem _transfer = default!;

    public override void Update(float frameTime)
    {
        foreach (var (vat, strap) in EntityQuery<FEVVatComponent, StrapComponent>())
        {
            foreach (var buckled in strap.BuckledEntities)
            {
                if (!_solutions.TryGetSolution(vat.Owner, vat.Solution, out var vatSolution))
                    continue;
                if (!_solutions.TryGetDrainableSolution(buckled, out var target))
                    continue;

                var amount = vat.TransferRate * frameTime;
                _transfer.TransferSolution(vat.Owner, buckled, vatSolution, target, amount);
            }
        }
    }
}
