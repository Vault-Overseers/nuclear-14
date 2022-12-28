using Content.Shared.Rejuvenate;
using Content.Shared.StatusEffect;
using Robust.Shared.GameStates;

namespace Content.Shared.Stats.StatModifier;

public abstract class SharedStatsModifierSystem : EntitySystem
{
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly SharedStatsSystem _sharedStatsSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<StatsModifierComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<StatsModifierComponent, ComponentHandleState>(OnHandleState);
        SubscribeLocalEvent<StatsModifierComponent, RejuvenateEvent>(OnRejuvenate);
    }

    private void OnGetState(EntityUid uid, StatsModifierComponent component, ref ComponentGetState args)
    {
        args.State = new StatsModifierComponentState(component.CurrentModifiers);
    }

    private void OnHandleState(EntityUid uid, StatsModifierComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not StatsModifierComponentState modifierState)
            return;

        component.CurrentModifiers = modifierState.CurrentModifiers;

        if (EntityManager.TryGetComponent(uid, out SharedStatsComponent? statsComp))
        {
            _sharedStatsSystem.UpdateStats(uid, statsComp);
        }
    }

    private void OnRejuvenate(EntityUid uid, StatsModifierComponent component, RejuvenateEvent args)
    {
        EntityManager.RemoveComponentDeferred<StatsModifierComponent>(uid);
    }

    /// <summary>
    /// Applies a modifier to a Stat to the specified entity.
    /// </summary>
    /// <remarks>
    /// When adding a value to a currently existing modifier, if the result is 0, it will remove only that modifier.
    /// </remarks>
    /// <param name="uid"></param>
    /// <param name="time"></param>
    /// <param name="refresh"></param>
    /// <param name="statName"></param>
    /// <param name="value"></param>
    /// <param name="status"></param>
    public void AddModifier(EntityUid uid, TimeSpan time, bool refresh, string statName, int value,
        StatusEffectsComponent? status = null)
    {
        if (!Resolve(uid, ref status, false))
            return;

        if (_statusEffects.TryAddStatusEffect<StatsModifierComponent>(uid, "Modified", time, refresh, status))
        {
            var component = EntityManager.GetComponent<StatsModifierComponent>(uid);

            if (component.CurrentModifiers.ContainsKey(statName))
            {
                component.CurrentModifiers[statName] += value;
            }
            else
            {
                component.CurrentModifiers.Add(statName, value);
            }
        }
    }
}
