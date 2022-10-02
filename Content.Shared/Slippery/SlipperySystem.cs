using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Inventory;
using Content.Shared.StatusEffect;
using Content.Shared.StepTrigger.Systems;
using Content.Shared.Stunnable;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Physics.Components;

namespace Content.Shared.Slippery
{
    [UsedImplicitly]
    public sealed class SlipperySystem : EntitySystem
    {
        [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly SharedStunSystem _stunSystem = default!;
        [Dependency] private readonly StatusEffectsSystem _statusEffectsSystem = default!;
        [Dependency] private readonly SharedContainerSystem _container = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SlipperyComponent, StepTriggerAttemptEvent>(HandleAttemptCollide);
            SubscribeLocalEvent<SlipperyComponent, StepTriggeredEvent>(HandleStepTrigger);
            SubscribeLocalEvent<NoSlipComponent, SlipAttemptEvent>(OnNoSlipAttempt);
            SubscribeLocalEvent<SlipperyComponent, ComponentGetState>(OnSlipperyGetState);
            SubscribeLocalEvent<SlipperyComponent, ComponentHandleState>(OnSlipperyHandleState);
        }

        private void OnSlipperyHandleState(EntityUid uid, SlipperyComponent component, ref ComponentHandleState args)
        {
            if (args.Current is not SlipperyComponentState state) return;

            component.ParalyzeTime = state.ParalyzeTime;
            component.LaunchForwardsMultiplier = state.LaunchForwardsMultiplier;
            component.SlipSound = new SoundPathSpecifier(state.SlipSound);
        }

        private void OnSlipperyGetState(EntityUid uid, SlipperyComponent component, ref ComponentGetState args)
        {
            args.State = new SlipperyComponentState(component.ParalyzeTime, component.LaunchForwardsMultiplier, component.SlipSound.GetSound());
        }

        private void HandleStepTrigger(EntityUid uid, SlipperyComponent component, ref StepTriggeredEvent args)
        {
            TrySlip(component, args.Tripper);
        }

        private void HandleAttemptCollide(
            EntityUid uid,
            SlipperyComponent component,
            ref StepTriggerAttemptEvent args)
        {
            args.Continue |= CanSlip(uid, args.Tripper);
        }

        private static void OnNoSlipAttempt(EntityUid uid, NoSlipComponent component, SlipAttemptEvent args)
        {
            args.Cancel();
        }

        private bool CanSlip(EntityUid uid, EntityUid toSlip)
        {
            return !_container.IsEntityInContainer(uid)
                   && _statusEffectsSystem.CanApplyEffect(toSlip, "Stun"); //Should be KnockedDown instead?
        }

        private void TrySlip(SlipperyComponent component, EntityUid other)
        {
            if (HasComp<KnockedDownComponent>(other))
                return;

            var ev = new SlipAttemptEvent();
            RaiseLocalEvent(other, ev, false);
            if (ev.Cancelled)
                return;

            if (TryComp(other, out PhysicsComponent? physics))
                physics.LinearVelocity *= component.LaunchForwardsMultiplier;

            var playSound = !_statusEffectsSystem.HasStatusEffect(other, "KnockedDown");

            _stunSystem.TryParalyze(other, TimeSpan.FromSeconds(component.ParalyzeTime), true);

            // Preventing from playing the slip sound when you are already knocked down.
            if (playSound)
            {
                _audio.PlayPredicted(component.SlipSound, other, other);
            }

            _adminLogger.Add(LogType.Slip, LogImpact.Low,
                $"{ToPrettyString(other):mob} slipped on collision with {ToPrettyString(component.Owner):entity}");
        }
    }

    /// <summary>
    ///     Raised on an entity to determine if it can slip or not.
    /// </summary>
    public sealed class SlipAttemptEvent : CancellableEntityEventArgs, IInventoryRelayEvent
    {
        public SlotFlags TargetSlots { get; } = SlotFlags.FEET;
    }
}
