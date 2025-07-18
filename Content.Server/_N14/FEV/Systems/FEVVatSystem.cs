using System;
using System.Collections.Generic;
using Content.Server._N14.FEV.Components;
using Content.Shared.Buckle.Components;
using Content.Shared.Buckle;
using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Server.Inventory;
using Content.Shared.Inventory;
using Content.Shared.IdentityManagement.Components;
using Content.Server.Nutrition.Components;
using Content.Shared.Tag;
using Robust.Shared.Timing;
namespace Content.Server._N14.FEV.Systems;

public sealed partial class FEVVatSystem : EntitySystem
{
    [Dependency] private readonly SolutionContainerSystem _solutions = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly TagSystem _tags = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly FEVReceiverSystem _receiver = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<FEVVatComponent, StrappedEvent>(OnStrapped);
        SubscribeLocalEvent<FEVVatComponent, UnstrappedEvent>(OnUnstrapped);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var cur = _timing.CurTime;
        var query = EntityQueryEnumerator<FEVVatComponent, StrapComponent>();
        while (query.MoveNext(out var uid, out var comp, out var strap))
        {
            foreach (var (ent, start) in comp.Buckled.ToArray())
            {
                if (cur - start < TimeSpan.FromSeconds(comp.TransformTime))
                    continue;

                if (!strap.BuckledEntities.Contains(ent))
                {
                    comp.Buckled.Remove(ent);
                    continue;
                }

                comp.Buckled.Remove(ent);
                InjectFEV(ent, comp.InstantAmount);
                if (TryComp<FEVReceiverComponent>(ent, out var recv))
                    _receiver.ForceInstantTransform(ent, recv);
            }
        }
    }

    private void OnStrapped(EntityUid uid, FEVVatComponent comp, ref StrappedEvent args)
    {
        comp.Buckled[args.Buckle.Owner] = _timing.CurTime;
    }

    private void OnUnstrapped(EntityUid uid, FEVVatComponent comp, ref UnstrappedEvent args)
    {
        var target = args.Buckle.Owner;
        if (!comp.Buckled.TryGetValue(target, out var start))
            return;

        comp.Buckled.Remove(target);
        if (_timing.CurTime - start >= TimeSpan.FromSeconds(comp.TransformTime))
            return;

        InjectFEV(target, comp.SlowAmount);
    }

    private void InjectFEV(EntityUid target, FixedPoint2 amount)
    {
        if (!_solutions.TryGetSolution(target, "bloodstream", out var solEnt, out _))
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

        _solutions.TryAddReagent(solEnt.Value, "FEV", amount, out _);
    }
}
