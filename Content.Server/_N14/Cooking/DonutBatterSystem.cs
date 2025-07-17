using Content.Server.Nyanotrasen.Kitchen.Components;
using Content.Server.Nyanotrasen.Kitchen.EntitySystems;
using Content.Shared._N14.Cooking;
using Content.Server.Popups;
using Content.Shared.Interaction.Events;
using Content.Server.GameTicking.Rules;
using Robust.Shared.Containers;

namespace Content.Server._N14.Cooking;

public sealed class DonutBatterSystem : EntitySystem
{
    [Dependency] private readonly EntityManager _entMan = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DonutBatterComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<DonutBatterComponent, BeingDeepFriedEvent>(OnDeepFried);
    }

    private void OnUseInHand(EntityUid uid, DonutBatterComponent component, UseInHandEvent args)
    {
        if (args.Handled || component.NextShape == null)
            return;

        args.Handled = true;
        var next = EntityManager.SpawnEntity(component.NextShape, _transform.GetMapCoordinates(uid));
        _popup.PopupEntity(Loc.GetString("n14-donut-batter-reshape"), uid, args.User);
        EntityManager.DeleteEntity(uid);
    }

    private void OnDeepFried(EntityUid uid, DonutBatterComponent component, BeingDeepFriedEvent args)
    {
        args.Handled = true;
        var cooked = EntityManager.SpawnEntity(component.CookedPrototype, _transform.GetMapCoordinates(uid));
        if (TryComp(args.DeepFryer, out DeepFryerComponent fryer))
            _container.Insert(cooked, fryer.Storage);
        EntityManager.DeleteEntity(uid);
    }
}
