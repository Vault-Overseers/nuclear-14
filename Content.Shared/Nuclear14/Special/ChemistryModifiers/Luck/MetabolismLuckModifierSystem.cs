using Robust.Shared.GameStates;
using Robust.Shared.Timing;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using static Content.Shared.Nuclear14.Special.ChemistryModifiers.LuckModifierMetabolismComponent;
using Content.Shared.Nuclear14.Special.ChemistryModifiers;

namespace Content.Shared.Nuclear14.Special.ChemistryModifiers;
public sealed partial class MetabolismLuckModifierSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SpecialModifierSystem _specialModifiers = default!;

    private readonly List<LuckModifierMetabolismComponent> _components = new();

    public override void Initialize()
    {
        base.Initialize();

        UpdatesOutsidePrediction = true;

        SubscribeLocalEvent<LuckModifierMetabolismComponent, ComponentHandleState>(OnMovespeedHandleState);
        SubscribeLocalEvent<LuckModifierMetabolismComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<LuckModifierMetabolismComponent, ComponentStartup>(AddComponent);
        SubscribeLocalEvent<LuckModifierMetabolismComponent, RefreshSpecialModifiersEvent>(OnRefreshMovespeed);
    }

    private void OnGetState(EntityUid uid, LuckModifierMetabolismComponent component, ref ComponentGetState args)
    {
        args.State = new LuckModifierMetabolismComponentState(
            component.LuckModifier,
            component.ModifierTimer);
    }

    private void OnMovespeedHandleState(EntityUid uid, LuckModifierMetabolismComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not LuckModifierMetabolismComponentState cast)
            return;

        component.LuckModifier = cast.LuckModifier;
        component.ModifierTimer = cast.ModifierTimer;
    }

    private void OnRefreshMovespeed(EntityUid uid, LuckModifierMetabolismComponent component, RefreshSpecialModifiersEvent args)
    {
        args.ModifyLuck(component.LuckModifier);
    }

    private void AddComponent(EntityUid uid, LuckModifierMetabolismComponent component, ComponentStartup args)
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
            EntityManager.RemoveComponent<LuckModifierMetabolismComponent>(component.Owner);

            _specialModifiers.RefreshClothingSpecialModifiers(component.Owner);
        }
    }
}
