/*
using Robust.Shared.GameStates;
using Robust.Shared.Timing;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using static Content.Shared._N14.Special.ChemistryModifiers.StrengthModifierMetabolismComponent;
using Content.Shared._N14.Special.ChemistryModifiers;

namespace Content.Shared._N14.Special.ChemistryModifiers;
public sealed partial class MetabolismStrengthModifierSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SpecialModifierSystem _specialModifiers = default!;

    private readonly List<StrengthModifierMetabolismComponent> _components = new();

    public override void Initialize()
    {
        base.Initialize();

        UpdatesOutsidePrediction = true;

        SubscribeLocalEvent<StrengthModifierMetabolismComponent, ComponentHandleState>(OnMovespeedHandleState);
        SubscribeLocalEvent<StrengthModifierMetabolismComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<StrengthModifierMetabolismComponent, ComponentStartup>(AddComponent);
        SubscribeLocalEvent<StrengthModifierMetabolismComponent, RefreshSpecialModifiersEvent>(OnRefreshMovespeed);
    }

    private void OnGetState(EntityUid uid, StrengthModifierMetabolismComponent component, ref ComponentGetState args)
    {
        args.State = new StrengthModifierMetabolismComponentState(
            component.StrengthModifier,
            component.ModifierTimer);
    }

    private void OnMovespeedHandleState(EntityUid uid, StrengthModifierMetabolismComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not StrengthModifierMetabolismComponentState cast)
            return;

        component.StrengthModifier = cast.StrengthModifier;
        component.ModifierTimer = cast.ModifierTimer;
    }

    private void OnRefreshMovespeed(EntityUid uid, StrengthModifierMetabolismComponent component, RefreshSpecialModifiersEvent args)
    {
        args.ModifyStrength(component.StrengthModifier);
    }

    private void AddComponent(EntityUid uid, StrengthModifierMetabolismComponent component, ComponentStartup args)
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
            EntityManager.RemoveComponent<StrengthModifierMetabolismComponent>(component.Owner);

            _specialModifiers.RefreshClothingSpecialModifiers(component.Owner);
        }
    }
}
*/
