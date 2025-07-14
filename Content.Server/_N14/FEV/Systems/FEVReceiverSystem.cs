using Content.Server._N14.FEV.Components;
using Content.Server.Medical;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Humanoid;
using Content.Server.Humanoid;
using Content.Server.Polymorph.Systems;
using Content.Shared.Popups;
using Content.Shared.FixedPoint;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Content.Shared.Polymorph;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Localization;
using Content.Shared.Inventory;
using Content.Server.Inventory;

namespace Content.Server._N14.FEV.Systems;

public sealed partial class FEVReceiverSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutions = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoid = default!;
    [Dependency] private readonly PolymorphSystem _polymorph = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<FEVReceiverComponent, TryMetabolizeReagent>(OnMetabolize);
        SubscribeLocalEvent<FEVReceiverComponent, SolutionContainerChangedEvent>(OnSolutionChanged);
    }

    private void OnMetabolize(EntityUid uid, FEVReceiverComponent comp, ref TryMetabolizeReagent args)
    {
        if (args.Reagent.Prototype != "FEV")
            return;

        comp.Accumulated += args.Quantity;

        if (comp.Accumulated >= comp.InstantThreshold && !HasComp<PendingFEVTransformComponent>(uid))
        {
            StartInstantTransform(uid, comp);
            return;
        }

        if (!comp.Transforming && comp.Accumulated >= comp.SlowThreshold)
        {
            StartSlowTransform(uid, comp);
        }
    }

    private void OnSolutionChanged(EntityUid uid, FEVReceiverComponent comp, ref SolutionContainerChangedEvent args)
    {
        if (args.SolutionId != "bloodstream")
            return;

        var quantity = args.Solution.GetTotalPrototypeQuantity("FEV");
        comp.Accumulated = quantity;

        if (comp.Accumulated >= comp.InstantThreshold && !HasComp<PendingFEVTransformComponent>(uid))
        {
            StartInstantTransform(uid, comp);
            return;
        }

        if (!comp.Transforming && comp.Accumulated >= comp.SlowThreshold)
        {
            StartSlowTransform(uid, comp);
        }
    }

    private void StartSlowTransform(EntityUid uid, FEVReceiverComponent comp)
    {
        var weights = _proto.Index<WeightedRandomEntityPrototype>(comp.EntityWeights);
        var entity = weights.Pick(_random);

        var pending = EnsureComp<PendingFEVTransformComponent>(uid);
        pending.Species = entity;
        pending.Stage = 0;
        pending.NextTime = _timing.CurTime + comp.StageInterval;
        comp.Transforming = true;
        comp.TargetSpecies = entity;
    }

    private void StartInstantTransform(EntityUid uid, FEVReceiverComponent comp)
    {
        var weights = _proto.Index<WeightedRandomEntityPrototype>(comp.EntityWeights);
        var entity = weights.Pick(_random);

        var pending = EnsureComp<PendingFEVTransformComponent>(uid);
        pending.Species = entity;
        pending.Stage = comp.StageMessages.Count; // Skip messages
        pending.NextTime = _timing.CurTime; // Transform on next update

        comp.Transforming = true;
        comp.TargetSpecies = entity;
    }


    public override void Update(float frameTime)
    {
        var cur = _timing.CurTime;
        var query = EntityQueryEnumerator<FEVReceiverComponent, PendingFEVTransformComponent, HumanoidAppearanceComponent>();
        while (query.MoveNext(out var uid, out var comp, out var pending, out var humanoid))
        {
            if (pending.NextTime > cur)
                continue;

            if (pending.Stage < comp.StageMessages.Count)
            {
                _popup.PopupEntity(Loc.GetString(comp.StageMessages[pending.Stage]), uid, uid);
                pending.Stage++;
                pending.NextTime = cur + comp.StageInterval;
            }
            else
            {
                var config = new PolymorphConfiguration
                {
                    Entity = pending.Species,
                    Forced = true,
                    TransferDamage = true,
                    TransferName = true,
                    TransferHumanoidAppearance = false,
                    Inventory = PolymorphInventoryChange.Transfer,
                    RevertOnCrit = false,
                    RevertOnDeath = false
                };

                var newEnt = _polymorph.PolymorphEntity(uid, config);
                if (newEnt != null)
                    ReplaceOutfit(newEnt.Value, comp.SlotReplacements);

                RemCompDeferred<PendingFEVTransformComponent>(uid);
                comp.Transforming = false;
                comp.Accumulated = FixedPoint2.Zero;
                comp.TargetSpecies = null;
            }
        }
    }

    private void ReplaceOutfit(EntityUid uid, Dictionary<string, string>? replacements)
    {
        if (replacements == null || replacements.Count == 0)
            return;

        var xform = Transform(uid);

        foreach (var (slot, proto) in replacements)
        {
            _inventory.TryUnequip(uid, slot, force: true, predicted: false);
            var item = Spawn(proto, xform.Coordinates);
            if (!_inventory.TryEquip(uid, item, slot, silent: true, force: true))
                _transform.DropNextTo(item, uid);
        }
    }
}
