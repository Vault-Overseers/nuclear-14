using Content.Server.Inventory;
using Content.Server.Medical;
using Content.Server.Nutrition.Components;
using Content.Server.Popups;
using Content.Shared.IdentityManagement.Components;
using Content.Shared.Inventory;
using Content.Shared.Throwing;
using Robust.Shared.Random;

namespace Content.Server._N14.Nutrition;

public sealed class DungPileSystem : EntitySystem
{
    [Dependency] private readonly VomitSystem _vomit = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DungPileComponent, ThrowDoHitEvent>(OnThrowHit);
    }

    private void OnThrowHit(EntityUid uid, DungPileComponent component, ThrowDoHitEvent args)
    {
        if (!HasComp<InventoryComponent>(args.Target))
            return;

        if (IsProtected(args.Target))
            return;

        if (_random.Prob(component.VomitChance))
            _vomit.Vomit(args.Target);
    }

    private bool IsProtected(EntityUid target)
    {
        if (_inventory.TryGetSlotEntity(target, "mask", out var mask))
        {
            if (HasProtection(mask))
                return true;
        }

        if (_inventory.TryGetSlotEntity(target, "head", out var head))
        {
            if (HasProtection(head))
                return true;
        }

        return false;
    }

    private bool HasProtection(EntityUid item)
    {
        if (HasComp<IdentityBlockerComponent>(item))
            return true;

        if (TryComp<IngestionBlockerComponent>(item, out var blocker) && blocker.Enabled)
            return true;

        if (HasComp<ClothingComponent>(item))
            return true;

        return false;
    }
}
