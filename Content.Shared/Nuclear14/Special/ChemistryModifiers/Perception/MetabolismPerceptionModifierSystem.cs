using Robust.Shared.GameStates;
using Robust.Shared.Timing;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using static Content.Shared.Nuclear14.Special.ChemistryModifiers.PerceptionModifierMetabolismComponent;
using Content.Shared.Nuclear14.Special.ChemistryModifiers;

namespace Content.Shared.Nuclear14.Special.ChemistryModifiers;
public sealed partial class MetabolismPerceptionModifierSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SpecialModifierSystem _specialModifiers = default!;

    private readonly List<PerceptionModifierMetabolismComponent> _components = new();

    public override void Initialize()
    {
        base.Initialize();

        UpdatesOutsidePrediction = true;

        SubscribeLocalEvent<PerceptionModifierMetabolismComponent, ComponentHandleState>(OnMovespeedHandleState);
        SubscribeLocalEvent<PerceptionModifierMetabolismComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<PerceptionModifierMetabolismComponent, ComponentStartup>(AddComponent);
        SubscribeLocalEvent<PerceptionModifierMetabolismComponent, RefreshSpecialModifiersEvent>(OnRefreshMovespeed);
    }

    private void OnGetState(EntityUid uid, PerceptionModifierMetabolismComponent component, ref ComponentGetState args)
    {
        args.State = new PerceptionModifierMetabolismComponentState(
            component.PerceptionModifier,
            component.ModifierTimer);
    }

    private void OnMovespeedHandleState(EntityUid uid, PerceptionModifierMetabolismComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not PerceptionModifierMetabolismComponentState cast)
            return;

        component.PerceptionModifier = cast.PerceptionModifier;
        component.ModifierTimer = cast.ModifierTimer;
    }

    private void OnRefreshMovespeed(EntityUid uid, PerceptionModifierMetabolismComponent component, RefreshSpecialModifiersEvent args)
    {
        args.ModifyPerception(component.PerceptionModifier);
    }

    private void AddComponent(EntityUid uid, PerceptionModifierMetabolismComponent component, ComponentStartup args)
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
            EntityManager.RemoveComponent<PerceptionModifierMetabolismComponent>(component.Owner);

            _specialModifiers.RefreshClothingSpecialModifiers(component.Owner);
        }
    }
}
