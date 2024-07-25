using Robust.Shared.GameStates;
using Robust.Shared.Timing;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using static Content.Shared.Nuclear14.Special.ChemistryModifiers.EnduranceModifierMetabolismComponent;
using Content.Shared.Nuclear14.Special.ChemistryModifiers;

namespace Content.Shared.Nuclear14.Special.ChemistryModifiers;
public sealed class MetabolismEnduranceModifierSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SpecialModifierSystem _specialModifiers = default!;

    private readonly List<EnduranceModifierMetabolismComponent> _components = new();

    public override void Initialize()
    {
        base.Initialize();

        UpdatesOutsidePrediction = true;

        SubscribeLocalEvent<EnduranceModifierMetabolismComponent, ComponentHandleState>(OnMovespeedHandleState);
        SubscribeLocalEvent<EnduranceModifierMetabolismComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<EnduranceModifierMetabolismComponent, ComponentStartup>(AddComponent);
        SubscribeLocalEvent<EnduranceModifierMetabolismComponent, RefreshSpecialModifiersEvent>(OnRefreshMovespeed);
    }

    private void OnGetState(EntityUid uid, EnduranceModifierMetabolismComponent component, ref ComponentGetState args)
    {
        args.State = new EnduranceModifierMetabolismComponentState(
            component.EnduranceModifier,
            component.ModifierTimer);
    }

    private void OnMovespeedHandleState(EntityUid uid, EnduranceModifierMetabolismComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not EnduranceModifierMetabolismComponentState cast)
            return;

        component.EnduranceModifier = cast.EnduranceModifier;
        component.ModifierTimer = cast.ModifierTimer;
    }

    private void OnRefreshMovespeed(EntityUid uid, EnduranceModifierMetabolismComponent component, RefreshSpecialModifiersEvent args)
    {
        args.ModifyEndurance(component.EnduranceModifier);
    }

    private void AddComponent(EntityUid uid, EnduranceModifierMetabolismComponent component, ComponentStartup args)
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
            EntityManager.RemoveComponent<EnduranceModifierMetabolismComponent>(component.Owner);

            _specialModifiers.RefreshClothingSpecialModifiers(component.Owner);
        }
    }
}
