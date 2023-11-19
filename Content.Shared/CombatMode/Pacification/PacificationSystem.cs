using Content.Shared.Actions;
using Content.Shared.Alert;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;

namespace Content.Shared.CombatMode.Pacification;

public sealed class PacificationSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alertsSystem = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedCombatModeSystem _combatSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PacifiedComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<PacifiedComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<PacifiedComponent, AttackAttemptEvent>(OnAttackAttempt);
    }

    private void OnAttackAttempt(EntityUid uid, PacifiedComponent component, AttackAttemptEvent args)
    {
        args.Cancel();
    }

    private void OnStartup(EntityUid uid, PacifiedComponent component, ComponentStartup args)
    {
        if (!TryComp<CombatModeComponent>(uid, out var combatMode))
            return;

        if (combatMode.CanDisarm != null)
            _combatSystem.SetCanDisarm(uid, false, combatMode);

        _combatSystem.SetInCombatMode(uid, false, combatMode);

        if (combatMode.CombatToggleAction != null)
            _actionsSystem.SetEnabled(combatMode.CombatToggleAction, false);

        _alertsSystem.ShowAlert(uid, AlertType.Pacified);
    }

    private void OnShutdown(EntityUid uid, PacifiedComponent component, ComponentShutdown args)
    {
        if (!TryComp<CombatModeComponent>(uid, out var combatMode))
            return;

        if (combatMode.CanDisarm != null)
            _combatSystem.SetCanDisarm(uid, true, combatMode);

        if (combatMode.CombatToggleAction != null)
            _actionsSystem.SetEnabled(combatMode.CombatToggleAction, true);

        _alertsSystem.ClearAlert(uid, AlertType.Pacified);
    }
}
