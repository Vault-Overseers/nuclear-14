/*
using Robust.Shared.GameStates;
using Robust.Shared.Timing;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using static Content.Shared._N14.Special.ChemistryModifiers.AgilityModifierMetabolismComponent;
using Content.Shared._N14.Special.ChemistryModifiers;

namespace Content.Shared._N14.Special.ChemistryModifiers;
public sealed partial class MetabolismAgilityModifierSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SpecialModifierSystem _specialModifiers = default!;

    private readonly List<AgilityModifierMetabolismComponent> _components = new();

    public override void Initialize()
    {
        base.Initialize();

        UpdatesOutsidePrediction = true;

        SubscribeLocalEvent<AgilityModifierMetabolismComponent, ComponentHandleState>(OnMovespeedHandleState);
        SubscribeLocalEvent<AgilityModifierMetabolismComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<AgilityModifierMetabolismComponent, ComponentStartup>(AddComponent);
        SubscribeLocalEvent<AgilityModifierMetabolismComponent, RefreshSpecialModifiersEvent>(OnRefreshMovespeed);
    }

    private void OnGetState(EntityUid uid, AgilityModifierMetabolismComponent component, ref ComponentGetState args)
    {
        args.State = new AgilityModifierMetabolismComponentState(
            component.AgilityModifier,
            component.ModifierTimer);
    }

    private void OnMovespeedHandleState(EntityUid uid, AgilityModifierMetabolismComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not AgilityModifierMetabolismComponentState cast)
            return;

        component.AgilityModifier = cast.AgilityModifier;
        component.ModifierTimer = cast.ModifierTimer;
    }

    private void OnRefreshMovespeed(EntityUid uid, AgilityModifierMetabolismComponent component, RefreshSpecialModifiersEvent args)
    {
        args.ModifyAgility(component.AgilityModifier);
    }

    private void AddComponent(EntityUid uid, AgilityModifierMetabolismComponent component, ComponentStartup args)
    {
        _components.Add(component);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var currentTime = _gameTiming.CurTime;

        for (var i = _components.Count - 1; i >= 0; i--)
        {
            var component = _components[i];

            if (component.Deleted)
            {
                _components.RemoveAt(i);
                continue;
            }

            if (component.ModifierTimer > currentTime) continue;

            _components.RemoveAt(i);
            EntityManager.RemoveComponent<AgilityModifierMetabolismComponent>(component.Owner);

            _specialModifiers.RefreshClothingSpecialModifiers(component.Owner);
        }
    }
}
*/
