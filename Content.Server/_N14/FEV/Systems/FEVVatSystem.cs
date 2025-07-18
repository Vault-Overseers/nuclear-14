using Content.Server._N14.FEV.Components;
using Content.Shared.Buckle.Components;
using Content.Shared.Buckle; 
using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Server.Inventory;
using Content.Shared.Inventory;
using Content.Shared.IdentityManagement.Components;
using Content.Server.Nutrition.Components;
using Content.Shared.Tag;

namespace Content.Server._N14.FEV.Systems;

public sealed partial class FEVVatSystem : EntitySystem
{
    [Dependency] private readonly SolutionContainerSystem _solutions = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly TagSystem _tags = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<FEVVatComponent, StrappedEvent>(OnStrapped);
    }

    private void OnStrapped(EntityUid uid, FEVVatComponent comp, ref StrappedEvent args)
    {
        var target = args.Buckle.Owner;

        if (!_solutions.TryGetSolution(target, "bloodstream", out var solEnt, out var solution))
            return;

        bool covered = false;
        if (_inventory.TryGetSlotEntity(target, "head", out var headUid))
        {
            if (EntityManager.HasComponent<IdentityBlockerComponent>(headUid.Value))
                covered = true;
            else if (EntityManager.TryGetComponent(headUid.Value, out IngestionBlockerComponent? ingest) && ingest.Enabled)
                covered = true;
            else if (_tags.HasTag(headUid.Value, "HidesHair"))
                covered = true;
        }

        if (covered)
            return;

        _solutions.TryAddReagent(solEnt.Value, "FEV", comp.TransferAmount, out _);
    }
}
