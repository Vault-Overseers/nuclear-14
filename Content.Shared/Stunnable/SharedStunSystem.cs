using Content.Shared.ActionBlocker;
using Content.Shared.Audio;
using Content.Shared.DragDrop;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Content.Shared.Bed.Sleep;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Standing;
using Content.Shared.StatusEffect;
using Content.Shared.Throwing;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Player;

namespace Content.Shared.Stunnable
{
    [UsedImplicitly]
    public abstract class SharedStunSystem : EntitySystem
    {
        [Dependency] private readonly ActionBlockerSystem _blocker = default!;
        [Dependency] private readonly StandingStateSystem _standingStateSystem = default!;
        [Dependency] private readonly StatusEffectsSystem _statusEffectSystem = default!;
        [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifierSystem = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;

        /// <summary>
        /// Friction modifier for knocked down players.
        /// Doesn't make them faster but makes them slow down... slower.
        /// </summary>
        public const float KnockDownModifier = 0.4f;

        public override void Initialize()
        {
            SubscribeLocalEvent<KnockedDownComponent, ComponentInit>(OnKnockInit);
            SubscribeLocalEvent<KnockedDownComponent, ComponentShutdown>(OnKnockShutdown);
            SubscribeLocalEvent<KnockedDownComponent, StandAttemptEvent>(OnStandAttempt);

            SubscribeLocalEvent<SlowedDownComponent, ComponentInit>(OnSlowInit);
            SubscribeLocalEvent<SlowedDownComponent, ComponentShutdown>(OnSlowRemove);

            SubscribeLocalEvent<StunnedComponent, ComponentStartup>(UpdateCanMove);
            SubscribeLocalEvent<StunnedComponent, ComponentShutdown>(UpdateCanMove);

            SubscribeLocalEvent<SlowedDownComponent, ComponentGetState>(OnSlowGetState);
            SubscribeLocalEvent<SlowedDownComponent, ComponentHandleState>(OnSlowHandleState);

            SubscribeLocalEvent<KnockedDownComponent, ComponentGetState>(OnKnockGetState);
            SubscribeLocalEvent<KnockedDownComponent, ComponentHandleState>(OnKnockHandleState);

            // helping people up if they're knocked down
            SubscribeLocalEvent<KnockedDownComponent, InteractHandEvent>(OnInteractHand);
            SubscribeLocalEvent<SlowedDownComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovespeed);

            SubscribeLocalEvent<KnockedDownComponent, TileFrictionEvent>(OnKnockedTileFriction);

            // Attempt event subscriptions.
            SubscribeLocalEvent<StunnedComponent, UpdateCanMoveEvent>(OnMoveAttempt);
            SubscribeLocalEvent<StunnedComponent, InteractionAttemptEvent>(OnInteractAttempt);
            SubscribeLocalEvent<StunnedComponent, UseAttemptEvent>(OnUseAttempt);
            SubscribeLocalEvent<StunnedComponent, ThrowAttemptEvent>(OnThrowAttempt);
            SubscribeLocalEvent<StunnedComponent, DropAttemptEvent>(OnDropAttempt);
            SubscribeLocalEvent<StunnedComponent, PickupAttemptEvent>(OnPickupAttempt);
            SubscribeLocalEvent<StunnedComponent, IsEquippingAttemptEvent>(OnEquipAttempt);
            SubscribeLocalEvent<StunnedComponent, IsUnequippingAttemptEvent>(OnUnequipAttempt);
        }

        private void UpdateCanMove(EntityUid uid, StunnedComponent component, EntityEventArgs args)
        {
            _blocker.UpdateCanMove(uid);
        }

        private void OnSlowGetState(EntityUid uid, SlowedDownComponent component, ref ComponentGetState args)
        {
            args.State = new SlowedDownComponentState(component.SprintSpeedModifier, component.WalkSpeedModifier);
        }

        private void OnSlowHandleState(EntityUid uid, SlowedDownComponent component, ref ComponentHandleState args)
        {
            if (args.Current is SlowedDownComponentState state)
            {
                component.SprintSpeedModifier = state.SprintSpeedModifier;
                component.WalkSpeedModifier = state.WalkSpeedModifier;
            }
        }

        private void OnKnockGetState(EntityUid uid, KnockedDownComponent component, ref ComponentGetState args)
        {
            args.State = new KnockedDownComponentState(component.HelpInterval, component.HelpTimer);
        }

        private void OnKnockHandleState(EntityUid uid, KnockedDownComponent component, ref ComponentHandleState args)
        {
            if (args.Current is KnockedDownComponentState state)
            {
                component.HelpInterval = state.HelpInterval;
                component.HelpTimer = state.HelpTimer;
            }
        }

        private void OnKnockInit(EntityUid uid, KnockedDownComponent component, ComponentInit args)
        {
            _standingStateSystem.Down(uid);
        }

        private void OnKnockShutdown(EntityUid uid, KnockedDownComponent component, ComponentShutdown args)
        {
            _standingStateSystem.Stand(uid);
        }

        private void OnStandAttempt(EntityUid uid, KnockedDownComponent component, StandAttemptEvent args)
        {
            if (component.LifeStage <= ComponentLifeStage.Running)
                args.Cancel();
        }

        private void OnSlowInit(EntityUid uid, SlowedDownComponent component, ComponentInit args)
        {
            _movementSpeedModifierSystem.RefreshMovementSpeedModifiers(uid);
        }

        private void OnSlowRemove(EntityUid uid, SlowedDownComponent component, ComponentShutdown args)
        {
            component.SprintSpeedModifier = 1f;
            component.WalkSpeedModifier = 1f;
            _movementSpeedModifierSystem.RefreshMovementSpeedModifiers(uid);
        }

        private void OnRefreshMovespeed(EntityUid uid, SlowedDownComponent component, RefreshMovementSpeedModifiersEvent args)
        {
            args.ModifySpeed(component.WalkSpeedModifier, component.SprintSpeedModifier);
        }

        // TODO STUN: Make events for different things. (Getting modifiers, attempt events, informative events...)

        /// <summary>
        ///     Stuns the entity, disallowing it from doing many interactions temporarily.
        /// </summary>
        public bool TryStun(EntityUid uid, TimeSpan time, bool refresh,
            StatusEffectsComponent? status = null)
        {
            if (time <= TimeSpan.Zero)
                return false;

            if (!Resolve(uid, ref status, false))
                return false;

            return _statusEffectSystem.TryAddStatusEffect<StunnedComponent>(uid, "Stun", time, refresh);
        }

        /// <summary>
        ///     Knocks down the entity, making it fall to the ground.
        /// </summary>
        public bool TryKnockdown(EntityUid uid, TimeSpan time, bool refresh,
            StatusEffectsComponent? status = null)
        {
            if (time <= TimeSpan.Zero)
                return false;

            if (!Resolve(uid, ref status, false))
                return false;

            return _statusEffectSystem.TryAddStatusEffect<KnockedDownComponent>(uid, "KnockedDown", time, refresh);
        }

        /// <summary>
        ///     Applies knockdown and stun to the entity temporarily.
        /// </summary>
        public bool TryParalyze(EntityUid uid, TimeSpan time, bool refresh,
            StatusEffectsComponent? status = null)
        {
            if (!Resolve(uid, ref status, false))
                return false;

            return TryKnockdown(uid, time, refresh, status) && TryStun(uid, time, refresh, status);
        }

        /// <summary>
        ///     Slows down the mob's walking/running speed temporarily
        /// </summary>
        public bool TrySlowdown(EntityUid uid, TimeSpan time, bool refresh,
            float walkSpeedMultiplier = 1f, float runSpeedMultiplier = 1f,
            StatusEffectsComponent? status = null)
        {
            if (!Resolve(uid, ref status, false))
                return false;

            if (time <= TimeSpan.Zero)
                return false;

            if (_statusEffectSystem.TryAddStatusEffect<SlowedDownComponent>(uid, "SlowedDown", time, refresh, status))
            {
                var slowed = EntityManager.GetComponent<SlowedDownComponent>(uid);
                // Doesn't make much sense to have the "TrySlowdown" method speed up entities now does it?
                walkSpeedMultiplier = Math.Clamp(walkSpeedMultiplier, 0f, 1f);
                runSpeedMultiplier = Math.Clamp(runSpeedMultiplier, 0f, 1f);

                slowed.WalkSpeedModifier *= walkSpeedMultiplier;
                slowed.SprintSpeedModifier *= runSpeedMultiplier;

                _movementSpeedModifierSystem.RefreshMovementSpeedModifiers(uid);

                return true;
            }

            return false;
        }

        private void OnInteractHand(EntityUid uid, KnockedDownComponent knocked, InteractHandEvent args)
        {
            if (args.Handled || knocked.HelpTimer > 0f)
                return;

            // TODO: This should be an event.
            if (HasComp<SleepingComponent>(uid))
                return;

            // Set it to half the help interval so helping is actually useful...
            knocked.HelpTimer = knocked.HelpInterval/2f;

            _statusEffectSystem.TryRemoveTime(uid, "KnockedDown", TimeSpan.FromSeconds(knocked.HelpInterval));
            _audio.PlayPredicted(knocked.StunAttemptSound, uid, args.User);
            Dirty(knocked);

            args.Handled = true;
        }

        private void OnKnockedTileFriction(EntityUid uid, KnockedDownComponent component, ref TileFrictionEvent args)
        {
            args.Modifier *= KnockDownModifier;
        }

        #region Attempt Event Handling

        private void OnMoveAttempt(EntityUid uid, StunnedComponent stunned, UpdateCanMoveEvent args)
        {
            if (stunned.LifeStage > ComponentLifeStage.Running)
                return;

            args.Cancel();
        }

        private void OnInteractAttempt(EntityUid uid, StunnedComponent stunned, InteractionAttemptEvent args)
        {
            args.Cancel();
        }

        private void OnUseAttempt(EntityUid uid, StunnedComponent stunned, UseAttemptEvent args)
        {
            args.Cancel();
        }

        private void OnThrowAttempt(EntityUid uid, StunnedComponent stunned, ThrowAttemptEvent args)
        {
            args.Cancel();
        }

        private void OnDropAttempt(EntityUid uid, StunnedComponent stunned, DropAttemptEvent args)
        {
            args.Cancel();
        }

        private void OnPickupAttempt(EntityUid uid, StunnedComponent stunned, PickupAttemptEvent args)
        {
            args.Cancel();
        }

        private void OnEquipAttempt(EntityUid uid, StunnedComponent stunned, IsEquippingAttemptEvent args)
        {
            // is this a self-equip, or are they being stripped?
            if (args.Equipee == uid)
                args.Cancel();
        }

        private void OnUnequipAttempt(EntityUid uid, StunnedComponent stunned, IsUnequippingAttemptEvent args)
        {
            // is this a self-equip, or are they being stripped?
            if (args.Unequipee == uid)
                args.Cancel();
        }

        #endregion

    }
}
