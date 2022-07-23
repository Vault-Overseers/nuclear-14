
using System.Linq;
using Content.Server.CombatMode;
using Content.Server.Hands.Components;
using Content.Server.Popups;
using Content.Server.Pulling;
using Content.Server.Stack;
using Content.Server.Storage.Components;
using Content.Server.Storage.EntitySystems;
using Content.Server.Strip;
using Content.Server.Stunnable;
using Content.Shared.ActionBlocker;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Input;
using Content.Shared.Inventory;
using Content.Shared.Physics.Pull;
using Content.Shared.Popups;
using Content.Shared.Pulling.Components;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Players;
using Robust.Shared.Utility;

namespace Content.Server.Hands.Systems
{
    [UsedImplicitly]
    internal sealed class HandsSystem : SharedHandsSystem
    {
        [Dependency] private readonly InventorySystem _inventorySystem = default!;
        [Dependency] private readonly StackSystem _stackSystem = default!;
        [Dependency] private readonly HandVirtualItemSystem _virtualItemSystem = default!;
        [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
        [Dependency] private readonly StrippableSystem _strippableSystem = default!;
        [Dependency] private readonly SharedHandVirtualItemSystem _virtualSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
        [Dependency] private readonly PullingSystem _pullingSystem = default!;
        [Dependency] private readonly ThrowingSystem _throwingSystem = default!;
        [Dependency] private readonly StorageSystem _storageSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<HandsComponent, DisarmedEvent>(OnDisarmed, before: new[] { typeof(StunSystem) });

            SubscribeLocalEvent<HandsComponent, PullStartedMessage>(HandlePullStarted);
            SubscribeLocalEvent<HandsComponent, PullStoppedMessage>(HandlePullStopped);

            SubscribeLocalEvent<HandsComponent, EntRemovedFromContainerMessage>(HandleEntityRemoved);

            SubscribeLocalEvent<HandsComponent, ComponentGetState>(GetComponentState);

            CommandBinds.Builder
                .Bind(ContentKeyFunctions.ThrowItemInHand, new PointerInputCmdHandler(HandleThrowItem))
                .Bind(ContentKeyFunctions.SmartEquipBackpack, InputCmdHandler.FromDelegate(HandleSmartEquipBackpack))
                .Bind(ContentKeyFunctions.SmartEquipBelt, InputCmdHandler.FromDelegate(HandleSmartEquipBelt))
                .Register<HandsSystem>();
        }

        public override void Shutdown()
        {
            base.Shutdown();

            CommandBinds.Unregister<HandsSystem>();
        }

        private void GetComponentState(EntityUid uid, HandsComponent hands, ref ComponentGetState args)
        {
            args.State = new HandsComponentState(hands);
        }

        private void OnDisarmed(EntityUid uid, HandsComponent component, DisarmedEvent args)
        {
            if (args.Handled)
                return;

            // Break any pulls
            if (TryComp(uid, out SharedPullerComponent? puller) && puller.Pulling is EntityUid pulled && TryComp(pulled, out SharedPullableComponent? pullable))
                _pullingSystem.TryStopPull(pullable);

            if (!_handsSystem.TryDrop(uid, component.ActiveHand!, null, checkActionBlocker: false))
                return;

            var targEnt = Identity.Entity(args.Target, EntityManager);
            var msgOther = Loc.GetString("hands-component-disarm-success-others-message",
                ("disarmer", Identity.Entity(args.Source, EntityManager)), ("disarmed", targEnt));
            var msgUser = Loc.GetString("hands-component-disarm-success-message", ("disarmed", targEnt));

            var filter = Filter.Pvs(args.Source).RemoveWhereAttachedEntity(e => e == args.Source);
            _popupSystem.PopupEntity(msgOther, args.Source, filter);
            _popupSystem.PopupEntity(msgUser, args.Source, Filter.Entities(args.Source));

            args.Handled = true; // no shove/stun.
        }

        #region EntityInsertRemove
        public override void DoDrop(EntityUid uid, Hand hand, bool doDropInteraction = true, SharedHandsComponent? hands = null)
        {
            base.DoDrop(uid, hand,doDropInteraction, hands);

            // update gui of anyone stripping this entity.
            _strippableSystem.SendUpdate(uid);

            if (TryComp(hand.HeldEntity, out SpriteComponent? sprite))
                sprite.RenderOrder = EntityManager.CurrentTick.Value;
        }

        public override void DoPickup(EntityUid uid, Hand hand, EntityUid entity, SharedHandsComponent? hands = null)
        {
            base.DoPickup(uid, hand, entity, hands);

            // update gui of anyone stripping this entity.
            _strippableSystem.SendUpdate(uid);
        }


        public override void PickupAnimation(EntityUid item, EntityCoordinates initialPosition, Vector2 finalPosition,
            EntityUid? exclude)
        {
            if (finalPosition.EqualsApprox(initialPosition.Position, tolerance: 0.1f))
                return;

            var filter = Filter.Pvs(item);

            if (exclude != null)
                filter = filter.RemoveWhereAttachedEntity(entity => entity == exclude);

            RaiseNetworkEvent(new PickupAnimationEvent(item, initialPosition, finalPosition), filter);
        }

        private void HandleEntityRemoved(EntityUid uid, SharedHandsComponent component, EntRemovedFromContainerMessage args)
        {
            if (!Deleted(args.Entity) && TryComp(args.Entity, out HandVirtualItemComponent? @virtual))
                _virtualSystem.Delete(@virtual, uid);
        }
        #endregion

        #region pulling
        private void HandlePullStarted(EntityUid uid, HandsComponent component, PullStartedMessage args)
        {
            if (args.Puller.Owner != uid)
                return;

            if (TryComp<SharedPullerComponent>(args.Puller.Owner, out var pullerComp) && !pullerComp.NeedsHands)
                return;

            if (!_virtualItemSystem.TrySpawnVirtualItemInHand(args.Pulled.Owner, uid))
            {
                DebugTools.Assert("Unable to find available hand when starting pulling??");
            }
        }

        private void HandlePullStopped(EntityUid uid, HandsComponent component, PullStoppedMessage args)
        {
            if (args.Puller.Owner != uid)
                return;

            // Try find hand that is doing this pull.
            // and clear it.
            foreach (var hand in component.Hands.Values)
            {
                if (hand.HeldEntity == null
                    || !TryComp(hand.HeldEntity, out HandVirtualItemComponent? virtualItem)
                    || virtualItem.BlockingEntity != args.Pulled.Owner)
                    continue;

                QueueDel(hand.HeldEntity.Value);
                break;
            }
        }
        #endregion

        #region interactions
        private bool HandleThrowItem(ICommonSession? session, EntityCoordinates coords, EntityUid uid)
        {
            if (session is not IPlayerSession playerSession)
                return false;

            if (playerSession.AttachedEntity is not {Valid: true} player ||
                !Exists(player) ||
                player.IsInContainer() ||
                !TryComp(player, out SharedHandsComponent? hands) ||
                hands.ActiveHandEntity is not EntityUid throwEnt ||
                !_actionBlockerSystem.CanThrow(player))
                return false;

            if (EntityManager.TryGetComponent(throwEnt, out StackComponent? stack) && stack.Count > 1 && stack.ThrowIndividually)
            {
                var splitStack = _stackSystem.Split(throwEnt, 1, EntityManager.GetComponent<TransformComponent>(player).Coordinates, stack);

                if (splitStack is not {Valid: true})
                    return false;

                throwEnt = splitStack.Value;
            }
            else if (!TryDrop(player, throwEnt, handsComp: hands))
                return false;

            var direction = coords.ToMapPos(EntityManager) - Transform(player).WorldPosition;
            if (direction == Vector2.Zero)
                return true;

            direction = direction.Normalized * Math.Min(direction.Length, hands.ThrowRange);

            var throwStrength = hands.ThrowForceMultiplier;
            _throwingSystem.TryThrow(throwEnt, direction, throwStrength, player);

            return true;
        }

        private void HandleSmartEquipBackpack(ICommonSession? session)
        {
            HandleSmartEquip(session, "back");
        }

        private void HandleSmartEquipBelt(ICommonSession? session)
        {
            HandleSmartEquip(session, "belt");
        }

        private void HandleSmartEquip(ICommonSession? session, string equipmentSlot)
        {
            if (session is not IPlayerSession playerSession)
                return;

            if (playerSession.AttachedEntity is not {Valid: true} plyEnt || !Exists(plyEnt))
                return;

            if (!TryComp<SharedHandsComponent>(plyEnt, out var hands))
                return;

            if (HasComp<StunnedComponent>(plyEnt))
                return;

            if (!_inventorySystem.TryGetSlotEntity(plyEnt, equipmentSlot, out var slotEntity) ||
                !TryComp(slotEntity, out ServerStorageComponent? storageComponent))
            {
                plyEnt.PopupMessage(Loc.GetString("hands-system-missing-equipment-slot", ("slotName", equipmentSlot)));
                return;
            }

            if (hands.ActiveHand?.HeldEntity != null)
            {
                _storageSystem.PlayerInsertHeldEntity(slotEntity.Value, plyEnt, storageComponent);
            }
            else if (storageComponent.StoredEntities != null)
            {
                if (storageComponent.StoredEntities.Count == 0)
                {
                    plyEnt.PopupMessage(Loc.GetString("hands-system-empty-equipment-slot", ("slotName", equipmentSlot)));
                }
                else
                {
                    var lastStoredEntity = Enumerable.Last(storageComponent.StoredEntities);
                    if (storageComponent.Remove(lastStoredEntity))
                    {
                        PickupOrDrop(plyEnt, lastStoredEntity, animateUser: true, handsComp: hands);
                    }
                }
            }
        }
        #endregion
    }
}
