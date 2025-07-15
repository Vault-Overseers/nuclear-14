using Content.Server.Fishing.Components;
using Content.Shared.DoAfter;
using Content.Shared.Fishing;
using Content.Shared.Interaction;
using Content.Shared.EntityList;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Server.GameObjects;

namespace Content.Server.Fishing;

public sealed partial class FishingSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FishingRodComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<FishingRodComponent, FishingDoAfterEvent>(OnDoAfter);
    }

    private void OnAfterInteract(EntityUid uid, FishingRodComponent rod, AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach)
            return;

        if (args.Target is not { Valid: true } target || !HasComp<FishingPoolComponent>(target))
            return;

        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, rod.CastTime, new FishingDoAfterEvent(), uid, target: target, used: uid)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = true
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
        args.Handled = true;
    }

    private void OnDoAfter(EntityUid uid, FishingRodComponent component, FishingDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target == null)
            return;

        args.Handled = true;

        if (!TryComp(args.Target.Value, out FishingPoolComponent? pool))
            return;

        if (!_random.Prob(pool.SuccessChance))
            return;

        var table = _proto.Index<EntityLootTablePrototype>(pool.LootTable);
        var spawns = table.GetSpawns(_random);
        if (spawns.Count == 0)
            return;

        var coords = _transform.GetMapCoordinates(args.Target.Value);
        Spawn(spawns[0], coords);
    }
}
