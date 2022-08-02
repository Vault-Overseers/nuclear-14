using Content.Server.Buckle.Components;
using Content.Server.Interaction;
using Content.Server.Storage.Components;
using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.Interaction;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;

namespace Content.Server.Buckle.Systems
{
    [UsedImplicitly]
    internal sealed class BuckleSystem : SharedBuckleSystem
    {
        public override void Initialize()
        {
            base.Initialize();

            UpdatesAfter.Add(typeof(InteractionSystem));
            UpdatesAfter.Add(typeof(InputSystem));

            SubscribeLocalEvent<StrapComponent, ComponentGetState>(OnStrapGetState);
            SubscribeLocalEvent<StrapComponent, EntInsertedIntoContainerMessage>(ContainerModifiedStrap);
            SubscribeLocalEvent<StrapComponent, EntRemovedFromContainerMessage>(ContainerModifiedStrap);

            SubscribeLocalEvent<BuckleComponent, MoveEvent>(MoveEvent);
            SubscribeLocalEvent<BuckleComponent, InteractHandEvent>(HandleInteractHand);
            SubscribeLocalEvent<BuckleComponent, GetVerbsEvent<InteractionVerb>>(AddUnbuckleVerb);
            SubscribeLocalEvent<BuckleComponent, InsertIntoEntityStorageAttemptEvent>(OnEntityStorageInsertAttempt);
        }

        private void OnStrapGetState(EntityUid uid, StrapComponent component, ref ComponentGetState args)
        {
            args.State = new StrapComponentState(component.Position, component.BuckleOffset, component.BuckledEntities, component.MaxBuckleDistance);
        }

        private void AddUnbuckleVerb(EntityUid uid, BuckleComponent component, GetVerbsEvent<InteractionVerb> args)
        {
            if (!args.CanAccess || !args.CanInteract || !component.Buckled)
                return;

            InteractionVerb verb = new()
            {
                Act = () => component.TryUnbuckle(args.User),
                Text = Loc.GetString("verb-categories-unbuckle"),
                IconTexture = "/Textures/Interface/VerbIcons/unbuckle.svg.192dpi.png"
            };

            if (args.Target == args.User && args.Using == null)
            {
                // A user is left clicking themselves with an empty hand, while buckled.
                // It is very likely they are trying to unbuckle themselves.
                verb.Priority = 1;
            }

            args.Verbs.Add(verb);
        }

        private void HandleInteractHand(EntityUid uid, BuckleComponent component, InteractHandEvent args)
        {
            args.Handled = component.TryUnbuckle(args.User);
        }

        private void MoveEvent(EntityUid uid, BuckleComponent buckle, ref MoveEvent ev)
        {
            var strap = buckle.BuckledTo;

            if (strap == null)
            {
                return;
            }

            var strapPosition = EntityManager.GetComponent<TransformComponent>(strap.Owner).Coordinates.Offset(buckle.BuckleOffset);

            if (ev.NewPosition.InRange(EntityManager, strapPosition, strap.MaxBuckleDistance))
            {
                return;
            }

            buckle.TryUnbuckle(buckle.Owner, true);
        }

        private void ContainerModifiedStrap(EntityUid uid, StrapComponent strap, ContainerModifiedMessage message)
        {
            foreach (var buckledEntity in strap.BuckledEntities)
            {
                if (!EntityManager.TryGetComponent(buckledEntity, out BuckleComponent? buckled))
                {
                    continue;
                }

                ContainerModifiedReAttach(buckled, strap);
            }
        }

        private void ContainerModifiedReAttach(BuckleComponent buckle, StrapComponent? strap)
        {
            if (strap == null)
            {
                return;
            }

            var contained = buckle.Owner.TryGetContainer(out var ownContainer);
            var strapContained = strap.Owner.TryGetContainer(out var strapContainer);

            if (contained != strapContained || ownContainer != strapContainer)
            {
                buckle.TryUnbuckle(buckle.Owner, true);
                return;
            }

            if (!contained)
            {
                buckle.ReAttach(strap);
            }
        }

        public void OnEntityStorageInsertAttempt(EntityUid uid, BuckleComponent comp, InsertIntoEntityStorageAttemptEvent args)
        {
            if (comp.Buckled)
                args.Cancel();
        }
    }
}
