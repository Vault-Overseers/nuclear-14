using Content.Shared._N14.Cooking;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Containers;

namespace Content.Server._N14.Cooking;

public sealed class DonutToppingSystem : EntitySystem
{
    [Dependency] private readonly SolutionTransferSystem _transfer = default!;
    [Dependency] private readonly SolutionContainerSystem _solutions = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DonutToppingComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnInteractUsing(EntityUid uid, DonutToppingComponent comp, InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!_solutions.TryGetDrainableSolution(args.Used, out var donorSoln, out var donor) ||
            !_solutions.TryGetInjectableSolution(uid, out var targetSoln, out var target))
            return;

        foreach (var (reagentId, result) in comp.Toppings)
        {
            var reagent = new ReagentId(reagentId, null);
            var qty = donor.GetReagentQuantity(reagent);
            if (qty < comp.Amount)
                continue;

            _transfer.Transfer(args.User, args.Used, donorSoln.Value, uid, targetSoln.Value, comp.Amount);
            var newDonut = EntityManager.SpawnEntity(result, _transform.GetMapCoordinates(uid));
            if (_container.TryGetContainingContainer(uid, out var container))
                _container.Insert(newDonut, container);
            EntityManager.DeleteEntity(uid);
            args.Handled = true;
            break;
        }
    }
}
