using Robust.Shared.GameStates;
using Robust.Shared.Timing;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using static Content.Shared.Nuclear14.Special.ChemistryModifiers.CharismaModifierMetabolismComponent;
using Content.Shared.Nuclear14.Special.ChemistryModifiers;

namespace Content.Shared.Nuclear14.Special.ChemistryModifiers;
public sealed partial class MetabolismCharismaModifierSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SpecialModifierSystem _specialModifiers = default!;

    private readonly List<CharismaModifierMetabolismComponent> _components = new();

    public override void Initialize()
    {
        base.Initialize();

        UpdatesOutsidePrediction = true;

        SubscribeLocalEvent<CharismaModifierMetabolismComponent, ComponentHandleState>(OnMovespeedHandleState);
        SubscribeLocalEvent<CharismaModifierMetabolismComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<CharismaModifierMetabolismComponent, ComponentStartup>(AddComponent);
        SubscribeLocalEvent<CharismaModifierMetabolismComponent, RefreshSpecialModifiersEvent>(OnRefreshMovespeed);
    }

    private void OnGetState(EntityUid uid, CharismaModifierMetabolismComponent component, ref ComponentGetState args)
    {
        args.State = new CharismaModifierMetabolismComponentState(
            component.CharismaModifier,
            component.ModifierTimer);
    }

    private void OnMovespeedHandleState(EntityUid uid, CharismaModifierMetabolismComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not CharismaModifierMetabolismComponentState cast)
            return;

        component.CharismaModifier = cast.CharismaModifier;
        component.ModifierTimer = cast.ModifierTimer;
    }

    private void OnRefreshMovespeed(EntityUid uid, CharismaModifierMetabolismComponent component, RefreshSpecialModifiersEvent args)
    {
        args.ModifyCharisma(component.CharismaModifier);
    }

    private void AddComponent(EntityUid uid, CharismaModifierMetabolismComponent component, ComponentStartup args)
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
            EntityManager.RemoveComponent<CharismaModifierMetabolismComponent>(component.Owner);

            _specialModifiers.RefreshClothingSpecialModifiers(component.Owner);
        }
    }
}
