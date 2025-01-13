/*
using Robust.Shared.GameStates;
using Robust.Shared.Timing;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using static Content.Shared._N14.Special.ChemistryModifiers.IntelligenceModifierMetabolismComponent;
using Content.Shared._N14.Special.ChemistryModifiers;

namespace Content.Shared._N14.Special.ChemistryModifiers;
public sealed class MetabolismIntelligenceModifierSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SpecialModifierSystem _specialModifiers = default!;

    private readonly List<IntelligenceModifierMetabolismComponent> _components = new();

    public override void Initialize()
    {
        base.Initialize();

        UpdatesOutsidePrediction = true;

        SubscribeLocalEvent<IntelligenceModifierMetabolismComponent, ComponentHandleState>(OnMovespeedHandleState);
        SubscribeLocalEvent<IntelligenceModifierMetabolismComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<IntelligenceModifierMetabolismComponent, ComponentStartup>(AddComponent);
        SubscribeLocalEvent<IntelligenceModifierMetabolismComponent, RefreshSpecialModifiersEvent>(OnRefreshMovespeed);
    }

    private void OnGetState(EntityUid uid, IntelligenceModifierMetabolismComponent component, ref ComponentGetState args)
    {
        args.State = new IntelligenceModifierMetabolismComponentState(
            component.IntelligenceModifier,
            component.ModifierTimer);
    }

    private void OnMovespeedHandleState(EntityUid uid, IntelligenceModifierMetabolismComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not IntelligenceModifierMetabolismComponentState cast)
            return;

        component.IntelligenceModifier = cast.IntelligenceModifier;
        component.ModifierTimer = cast.ModifierTimer;
    }

    private void OnRefreshMovespeed(EntityUid uid, IntelligenceModifierMetabolismComponent component, RefreshSpecialModifiersEvent args)
    {
        args.ModifyIntelligence(component.IntelligenceModifier);
    }

    private void AddComponent(EntityUid uid, IntelligenceModifierMetabolismComponent component, ComponentStartup args)
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
            EntityManager.RemoveComponent<IntelligenceModifierMetabolismComponent>(component.Owner);

            _specialModifiers.RefreshClothingSpecialModifiers(component.Owner);
        }
    }
}
*/
