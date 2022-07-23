using System.Linq;
using System.Threading;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Disposal.Tube.Components;
using Content.Server.Disposal.Unit.Components;
using Content.Server.DoAfter;
using Content.Server.Hands.Components;
using Content.Server.Power.Components;
using Content.Server.UserInterface;
using Content.Server.Storage.Components;
using Content.Server.Storage.EntitySystems;
using Content.Shared.ActionBlocker;
using Content.Shared.Atmos;
using Content.Shared.Construction.Components;
using Content.Shared.Destructible;
using Content.Shared.Disposal;
using Content.Shared.Disposal.Components;
using Content.Shared.DragDrop;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Movement;
using Content.Shared.Movement.Events;
using Content.Shared.Throwing;
using Content.Shared.Verbs;
using Content.Shared.Storage.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.Disposal.Unit.EntitySystems
{
    public sealed class DisposalUnitSystem : SharedDisposalUnitSystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IRobustRandom _robustRandom = default!;
        [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
        [Dependency] private readonly AtmosphereSystem _atmosSystem = default!;
        [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
        [Dependency] private readonly DumpableSystem _dumpableSystem = default!;
        [Dependency] private readonly TransformSystem _transformSystem = default!;

        private readonly List<DisposalUnitComponent> _activeDisposals = new();

        public override void Initialize()
        {
            base.Initialize();

            // Shouldn't need re-anchoring.
            SubscribeLocalEvent<DisposalUnitComponent, AnchorStateChangedEvent>(OnAnchorChanged);
            // TODO: Predict me when hands predicted
            SubscribeLocalEvent<DisposalUnitComponent, ContainerRelayMovementEntityEvent>(HandleMovement);
            SubscribeLocalEvent<DisposalUnitComponent, PowerChangedEvent>(HandlePowerChange);

            // Component lifetime
            SubscribeLocalEvent<DisposalUnitComponent, ComponentInit>(HandleDisposalInit);
            SubscribeLocalEvent<DisposalUnitComponent, ComponentRemove>(HandleDisposalRemove);

            SubscribeLocalEvent<DisposalUnitComponent, ThrowHitByEvent>(HandleThrowCollide);

            // Interactions
            SubscribeLocalEvent<DisposalUnitComponent, ActivateInWorldEvent>(HandleActivate);
            SubscribeLocalEvent<DisposalUnitComponent, AfterInteractUsingEvent>(HandleAfterInteractUsing);
            SubscribeLocalEvent<DisposalUnitComponent, DragDropEvent>(HandleDragDropOn);
            SubscribeLocalEvent<DisposalUnitComponent, DestructionEventArgs>(HandleDestruction);

            // Verbs
            SubscribeLocalEvent<DisposalUnitComponent, GetVerbsEvent<InteractionVerb>>(AddInsertVerb);
            SubscribeLocalEvent<DisposalUnitComponent, GetVerbsEvent<AlternativeVerb>>(AddDisposalAltVerbs);
            SubscribeLocalEvent<DisposalUnitComponent, GetVerbsEvent<Verb>>(AddClimbInsideVerb);

            // Units
            SubscribeLocalEvent<DoInsertDisposalUnitEvent>(DoInsertDisposalUnit);

            //UI
            SubscribeLocalEvent<DisposalUnitComponent, SharedDisposalUnitComponent.UiButtonPressedMessage>(OnUiButtonPressed);
        }

        private void AddDisposalAltVerbs(EntityUid uid, DisposalUnitComponent component, GetVerbsEvent<AlternativeVerb> args)
        {
            if (!args.CanAccess || !args.CanInteract)
                return;

            // Behavior for if the disposals bin has items in it
            if (component.Container.ContainedEntities.Count > 0)
            {
                // Verbs to flush the unit
                AlternativeVerb flushVerb = new();
                flushVerb.Act = () => Engage(component);
                flushVerb.Text = Loc.GetString("disposal-flush-verb-get-data-text");
                flushVerb.IconTexture = "/Textures/Interface/VerbIcons/delete_transparent.svg.192dpi.png";
                flushVerb.Priority = 1;
                args.Verbs.Add(flushVerb);

                // Verb to eject the contents
                AlternativeVerb ejectVerb = new()
                {
                    Act = () => TryEjectContents(component),
                    Category = VerbCategory.Eject,
                    Text = Loc.GetString("disposal-eject-verb-contents")
                };
                args.Verbs.Add(ejectVerb);
            }

            // Behavior if using a trash bag & other dumpable containers
            if (args.Using != null
                && TryComp<DumpableComponent>(args.Using.Value, out var dumpable)
                && TryComp<ServerStorageComponent>(args.Using.Value, out var storage)
                && storage.StoredEntities is { Count: > 0 })
            {
                // Verb to dump held container into disposal unit
                AlternativeVerb dumpVerb = new()
                {
                    Act = () => _dumpableSystem.StartDoAfter(args.Using.Value, args.Target, args.User, dumpable, storage),
                    Text = Loc.GetString("dump-disposal-verb-name", ("unit", args.Target)),
                    Priority = 2
                };
                args.Verbs.Add(dumpVerb);
            }

        }

        private void AddClimbInsideVerb(EntityUid uid, DisposalUnitComponent component, GetVerbsEvent<Verb> args)
        {
            // This is not an interaction, activation, or alternative verb type because unfortunately most users are
            // unwilling to accept that this is where they belong and don't want to accidentally climb inside.
            if (!args.CanAccess ||
                !args.CanInteract ||
                component.Container.ContainedEntities.Contains(args.User) ||
                !_actionBlockerSystem.CanMove(args.User))
                return;

            // Add verb to climb inside of the unit,
            Verb verb = new()
            {
                Act = () => TryInsert(component.Owner, args.User, args.User),
                Text = Loc.GetString("disposal-self-insert-verb-get-data-text")
            };
            // TODO VERN ICON
            // TODO VERB CATEGORY
            // create a verb category for "enter"?
            // See also, medical scanner. Also maybe add verbs for entering lockers/body bags?
            args.Verbs.Add(verb);
        }

        private void AddInsertVerb(EntityUid uid, DisposalUnitComponent component, GetVerbsEvent<InteractionVerb> args)
        {
            if (!args.CanAccess || !args.CanInteract || args.Hands == null || args.Using == null)
                return;

            if (!_actionBlockerSystem.CanDrop(args.User))
                return;

            if (!CanInsert(component, args.Using.Value))
                return;

            InteractionVerb insertVerb = new()
            {
                Text = Name(args.Using.Value),
                Category = VerbCategory.Insert,
                Act = () =>
                {
                    _handsSystem.TryDropIntoContainer(args.User, args.Using.Value, component.Container, checkActionBlocker: false, args.Hands);
                    AfterInsert(component, args.Using.Value);
                }
            };

            args.Verbs.Add(insertVerb);
        }

        private void DoInsertDisposalUnit(DoInsertDisposalUnitEvent ev)
        {
            var toInsert = ev.ToInsert;

            if (!EntityManager.TryGetComponent(ev.Unit, out DisposalUnitComponent? unit))
            {
                return;
            }

            if (!unit.Container.Insert(toInsert))
            {
                return;
            }

            AfterInsert(unit, toInsert);
        }

        public void DoInsertDisposalUnit(EntityUid unit, EntityUid toInsert, DisposalUnitComponent? disposal = null)
        {
            if (!Resolve(unit, ref disposal))
                return;

            if (!disposal.Container.Insert(toInsert))
                return;

            AfterInsert(disposal, toInsert);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            for (var i = _activeDisposals.Count - 1; i >= 0; i--)
            {
                var comp = _activeDisposals[i];
                if (!Update(comp, frameTime)) continue;
                _activeDisposals.RemoveAt(i);
            }
        }

        #region UI Handlers
        private void OnUiButtonPressed(EntityUid uid, DisposalUnitComponent component, SharedDisposalUnitComponent.UiButtonPressedMessage args)
        {
            if (args.Session.AttachedEntity is not {Valid: true} player)
            {
                return;
            }

            switch (args.Button)
            {
                case SharedDisposalUnitComponent.UiButton.Eject:
                    TryEjectContents(component);
                    break;
                case SharedDisposalUnitComponent.UiButton.Engage:
                    ToggleEngage(component);
                    break;
                case SharedDisposalUnitComponent.UiButton.Power:
                    TogglePower(component);
                    SoundSystem.Play("/Audio/Machines/machine_switch.ogg", Filter.Pvs(component.Owner), component.Owner, AudioParams.Default.WithVolume(-2f));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void ToggleEngage(DisposalUnitComponent component)
        {
            component.Engaged ^= true;

            if (component.Engaged)
            {
                Engage(component);
            }
            else
            {
                Disengage(component);
            }
        }

        public void TogglePower(DisposalUnitComponent component)
        {
            if (!EntityManager.TryGetComponent(component.Owner, out ApcPowerReceiverComponent? receiver))
            {
                return;
            }

            receiver.PowerDisabled = !receiver.PowerDisabled;
            UpdateInterface(component, receiver.Powered);
        }
        #endregion

        #region Eventbus Handlers
        private void HandleActivate(EntityUid uid, DisposalUnitComponent component, ActivateInWorldEvent args)
        {
            if (!EntityManager.TryGetComponent(args.User, out ActorComponent? actor))
            {
                return;
            }

            args.Handled = true;
            component.Owner.GetUIOrNull(SharedDisposalUnitComponent.DisposalUnitUiKey.Key)?.Open(actor.PlayerSession);
        }

        private void HandleAfterInteractUsing(EntityUid uid, DisposalUnitComponent component, AfterInteractUsingEvent args)
        {
            if (args.Handled || !args.CanReach)
                return;

            if (!EntityManager.TryGetComponent(args.User, out HandsComponent? hands))
            {
                return;
            }

            if (!CanInsert(component, args.Used) || !_handsSystem.TryDropIntoContainer(args.User, args.Used, component.Container))
            {
                return;
            }

            AfterInsert(component, args.Used);
            args.Handled = true;
        }

        /// <summary>
        /// Thrown items have a chance of bouncing off the unit and not going in.
        /// </summary>
        private void HandleThrowCollide(EntityUid uid, DisposalUnitComponent component, ThrowHitByEvent args)
        {
            if (!CanInsert(component, args.Thrown) ||
                _robustRandom.NextDouble() > 0.75 ||
                !component.Container.Insert(args.Thrown))
            {
                return;
            }

            AfterInsert(component, args.Thrown);
        }

        private void HandleDisposalInit(EntityUid uid, DisposalUnitComponent component, ComponentInit args)
        {
            component.Container = component.Owner.EnsureContainer<Container>(component.Name);

            UpdateInterface(component, component.Powered);

            if (!EntityManager.HasComponent<AnchorableComponent>(component.Owner))
            {
                Logger.WarningS("VitalComponentMissing", $"Disposal unit {uid} is missing an {nameof(AnchorableComponent)}");
            }
        }

        private void HandleDisposalRemove(EntityUid uid, DisposalUnitComponent component, ComponentRemove args)
        {
            foreach (var entity in component.Container.ContainedEntities.ToArray())
            {
                component.Container.ForceRemove(entity);
            }

            component.Owner.GetUIOrNull(SharedDisposalUnitComponent.DisposalUnitUiKey.Key)?.CloseAll();

            component.AutomaticEngageToken?.Cancel();
            component.AutomaticEngageToken = null;

            component.Container = null!;
            _activeDisposals.Remove(component);
        }

        private void HandlePowerChange(EntityUid uid, DisposalUnitComponent component, PowerChangedEvent args)
        {
            if (!component.Running)
                return;

            component.Powered = args.Powered;

            // TODO: Need to check the other stuff.
            if (!args.Powered)
            {
                component.AutomaticEngageToken?.Cancel();
                component.AutomaticEngageToken = null;
            }

            HandleStateChange(component, args.Powered && component.State == SharedDisposalUnitComponent.PressureState.Pressurizing);
            UpdateVisualState(component);
            UpdateInterface(component, args.Powered);

            if (component.Engaged && !TryFlush(component))
            {
                TryQueueEngage(component);
            }
        }

        /// <summary>
        /// Add or remove this disposal from the active ones for updating.
        /// </summary>
        public void HandleStateChange(DisposalUnitComponent component, bool active)
        {
            if (active)
            {
                if (!_activeDisposals.Contains(component))
                    _activeDisposals.Add(component);
            }
            else
            {
                _activeDisposals.Remove(component);
            }
        }

        private void HandleMovement(EntityUid uid, DisposalUnitComponent component, ref ContainerRelayMovementEntityEvent args)
        {
            var currentTime = GameTiming.CurTime;

            if (!EntityManager.TryGetComponent(args.Entity, out HandsComponent? hands) ||
                hands.Count == 0 ||
                currentTime < component.LastExitAttempt + ExitAttemptDelay)
            {
                return;
            }

            component.LastExitAttempt = currentTime;
            Remove(component, args.Entity);
        }

        private void OnAnchorChanged(EntityUid uid, DisposalUnitComponent component, ref AnchorStateChangedEvent args)
        {
            UpdateVisualState(component);
            if (!args.Anchored)
                TryEjectContents(component);
        }

        private void HandleDestruction(EntityUid uid, DisposalUnitComponent component, DestructionEventArgs args)
        {
            TryEjectContents(component);
        }

        private void HandleDragDropOn(EntityUid uid, DisposalUnitComponent component, DragDropEvent args)
        {
            args.Handled = TryInsert(component.Owner, args.Dragged, args.User);
        }
        #endregion

        /// <summary>
        /// Work out if we can stop updating this disposals component i.e. full pressure and nothing colliding.
        /// </summary>
        private bool Update(DisposalUnitComponent component, float frameTime)
        {
            var oldPressure = component.Pressure;

            component.Pressure = MathF.Min(1.0f, component.Pressure + PressurePerSecond * frameTime);
            component.State = component.Pressure >= 1 ? SharedDisposalUnitComponent.PressureState.Ready : SharedDisposalUnitComponent.PressureState.Pressurizing;

            var state = component.State;

            if (oldPressure < 1 && state == SharedDisposalUnitComponent.PressureState.Ready)
            {
                UpdateVisualState(component);

                if (component.Engaged)
                {
                    TryFlush(component);
                    state = component.State;
                }
            }

            Box2? disposalsBounds = null;
            var count = component.RecentlyEjected.Count;

            if (count > 0)
            {
                if (!EntityManager.TryGetComponent(component.Owner, out PhysicsComponent? disposalsBody))
                {
                    component.RecentlyEjected.Clear();
                }
                else
                {
                    disposalsBounds = disposalsBody.GetWorldAABB();
                }
            }

            for (var i = component.RecentlyEjected.Count - 1; i >= 0; i--)
            {
                var uid = component.RecentlyEjected[i];
                if (EntityManager.EntityExists(uid) &&
                    EntityManager.TryGetComponent(uid, out PhysicsComponent? body))
                {
                    // TODO: We need to use a specific collision method (which sloth hasn't coded yet) for actual bounds overlaps.
                    // Check for itemcomp as we won't just block the disposal unit "sleeping" for something it can't collide with anyway.
                    if (!EntityManager.HasComponent<SharedItemComponent>(uid) && body.GetWorldAABB().Intersects(disposalsBounds!.Value)) continue;
                    component.RecentlyEjected.RemoveAt(i);
                }
            }

            if (count != component.RecentlyEjected.Count)
                Dirty(component);

            return state == SharedDisposalUnitComponent.PressureState.Ready && component.RecentlyEjected.Count == 0;
        }

        public bool TryInsert(EntityUid unitId, EntityUid toInsertId, EntityUid userId, DisposalUnitComponent? unit = null)
        {
            if (!Resolve(unitId, ref unit))
                return false;

            if (!CanInsert(unit, toInsertId))
                return false;

            var delay = userId == toInsertId ? unit.EntryDelay : unit.DraggedEntryDelay;
            var ev = new DoInsertDisposalUnitEvent(userId, toInsertId, unitId);

            if (delay <= 0)
            {
                DoInsertDisposalUnit(ev);
                return true;
            }

            // Can't check if our target AND disposals moves currently so we'll just check target.
            // if you really want to check if disposals moves then add a predicate.
            var doAfterArgs = new DoAfterEventArgs(userId, delay, default, toInsertId)
            {
                BreakOnDamage = true,
                BreakOnStun = true,
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                NeedHand = false,
                BroadcastFinishedEvent = ev
            };

            _doAfterSystem.DoAfter(doAfterArgs);
            return true;
        }


        public bool TryFlush(DisposalUnitComponent component)
        {
            if (component.Deleted || !CanFlush(component))
            {
                return false;
            }

            var xform = Transform(component.Owner);
            if (!TryComp(xform.GridUid, out IMapGridComponent? grid))
                return false;

            var coords = xform.Coordinates;
            var entry = grid.Grid.GetLocal(coords)
                .FirstOrDefault(entity => EntityManager.HasComponent<DisposalEntryComponent>(entity));

            if (entry == default)
            {
                return false;
            }

            var air = component.Air;
            var entryComponent = EntityManager.GetComponent<DisposalEntryComponent>(entry);
            var indices = _transformSystem.GetGridOrMapTilePosition(component.Owner, xform);

            if (_atmosSystem.GetTileMixture(xform.GridUid, xform.MapUid, indices, true) is {Temperature: > 0} environment)
            {
                var transferMoles = 0.1f * (0.25f * Atmospherics.OneAtmosphere * 1.01f - air.Pressure) * air.Volume / (environment.Temperature * Atmospherics.R);

                component.Air = environment.Remove(transferMoles);
            }

            entryComponent.TryInsert(component);

            component.AutomaticEngageToken?.Cancel();
            component.AutomaticEngageToken = null;

            component.Pressure = 0;
            component.State = component.Pressure >= 1 ? SharedDisposalUnitComponent.PressureState.Ready : SharedDisposalUnitComponent.PressureState.Pressurizing;

            component.Engaged = false;

            HandleStateChange(component, true);
            UpdateVisualState(component, true);
            UpdateInterface(component, component.Powered);

            return true;
        }

        public void UpdateInterface(DisposalUnitComponent component, bool powered)
        {
            var stateString = Loc.GetString($"{component.State}");
            var state = new SharedDisposalUnitComponent.DisposalUnitBoundUserInterfaceState(EntityManager.GetComponent<MetaDataComponent>(component.Owner).EntityName, stateString, EstimatedFullPressure(component), powered, component.Engaged);
            component.Owner.GetUIOrNull(SharedDisposalUnitComponent.DisposalUnitUiKey.Key)?.SetState(state);
        }

        private TimeSpan EstimatedFullPressure(DisposalUnitComponent component)
        {
            if (component.State == SharedDisposalUnitComponent.PressureState.Ready) return TimeSpan.Zero;

            var currentTime = GameTiming.CurTime;
            var pressure = component.Pressure;

            return TimeSpan.FromSeconds(currentTime.TotalSeconds + (1.0f - pressure) / PressurePerSecond);
        }

        public void UpdateVisualState(DisposalUnitComponent component)
        {
            UpdateVisualState(component, false);
        }

        public void UpdateVisualState(DisposalUnitComponent component, bool flush)
        {
            if (!EntityManager.TryGetComponent(component.Owner, out AppearanceComponent? appearance))
            {
                return;
            }

            if (!EntityManager.GetComponent<TransformComponent>(component.Owner).Anchored)
            {
                appearance.SetData(SharedDisposalUnitComponent.Visuals.VisualState, SharedDisposalUnitComponent.VisualState.UnAnchored);
                appearance.SetData(SharedDisposalUnitComponent.Visuals.Handle, SharedDisposalUnitComponent.HandleState.Normal);
                appearance.SetData(SharedDisposalUnitComponent.Visuals.Light, SharedDisposalUnitComponent.LightState.Off);
                return;
            }

            appearance.SetData(SharedDisposalUnitComponent.Visuals.VisualState, component.Pressure < 1 ? SharedDisposalUnitComponent.VisualState.Charging : SharedDisposalUnitComponent.VisualState.Anchored);

            appearance.SetData(SharedDisposalUnitComponent.Visuals.Handle, component.Engaged
                ? SharedDisposalUnitComponent.HandleState.Engaged
                : SharedDisposalUnitComponent.HandleState.Normal);

            if (!component.Powered)
            {
                appearance.SetData(SharedDisposalUnitComponent.Visuals.Light, SharedDisposalUnitComponent.LightState.Off);
                return;
            }

            if (flush)
            {
                appearance.SetData(SharedDisposalUnitComponent.Visuals.VisualState, SharedDisposalUnitComponent.VisualState.Flushing);
                appearance.SetData(SharedDisposalUnitComponent.Visuals.Light, SharedDisposalUnitComponent.LightState.Off);
                return;
            }

            if (component.Container.ContainedEntities.Count > 0)
            {
                appearance.SetData(SharedDisposalUnitComponent.Visuals.Light, SharedDisposalUnitComponent.LightState.Full);
                return;
            }

            appearance.SetData(SharedDisposalUnitComponent.Visuals.Light, component.Pressure < 1
                ? SharedDisposalUnitComponent.LightState.Charging
                : SharedDisposalUnitComponent.LightState.Ready);
        }

        public void Remove(DisposalUnitComponent component, EntityUid entity)
        {
            component.Container.Remove(entity);

            if (component.Container.ContainedEntities.Count == 0)
            {
                component.AutomaticEngageToken?.Cancel();
                component.AutomaticEngageToken = null;
            }

            if (!component.RecentlyEjected.Contains(entity))
                component.RecentlyEjected.Add(entity);

            Dirty(component);
            HandleStateChange(component, true);
            UpdateVisualState(component);
        }

        public bool CanFlush(DisposalUnitComponent component)
        {
            return component.State == SharedDisposalUnitComponent.PressureState.Ready && component.Powered && EntityManager.GetComponent<TransformComponent>(component.Owner).Anchored;
        }

        public void Engage(DisposalUnitComponent component)
        {
            component.Engaged = true;
            UpdateVisualState(component);
            UpdateInterface(component, component.Powered);

            if (CanFlush(component))
            {
                component.Owner.SpawnTimer(component.FlushDelay, () => TryFlush(component));
            }
        }

        public void Disengage(DisposalUnitComponent component)
        {
            component.Engaged = false;
            UpdateVisualState(component);
            UpdateInterface(component, component.Powered);
        }

        /// <summary>
        /// Remove all entities currently in the disposal unit.
        /// </summary>
        public void TryEjectContents(DisposalUnitComponent component)
        {
            foreach (var entity in component.Container.ContainedEntities.ToArray())
            {
                Remove(component, entity);
            }
        }

        public override bool CanInsert(SharedDisposalUnitComponent component, EntityUid entity)
        {
            if (!base.CanInsert(component, entity) || component is not DisposalUnitComponent serverComp)
                return false;

            return serverComp.Container.CanInsert(entity);
        }

        /// <summary>
        /// If something is inserted (or the likes) then we'll queue up a flush in the future.
        /// </summary>
        public void TryQueueEngage(DisposalUnitComponent component)
        {
            if (component.Deleted || !component.Powered && component.Container.ContainedEntities.Count == 0)
            {
                return;
            }

            component.AutomaticEngageToken = new CancellationTokenSource();

            component.Owner.SpawnTimer(component.AutomaticEngageTime, () =>
            {
                if (!TryFlush(component))
                {
                    TryQueueEngage(component);
                }
            }, component.AutomaticEngageToken.Token);
        }

        public void AfterInsert(DisposalUnitComponent component, EntityUid entity)
        {
            TryQueueEngage(component);

            if (EntityManager.TryGetComponent(entity, out ActorComponent? actor))
            {
                component.Owner.GetUIOrNull(SharedDisposalUnitComponent.DisposalUnitUiKey.Key)?.Close(actor.PlayerSession);
            }

            UpdateVisualState(component);
        }
    }
}
