using Content.Shared.Interaction;
using Content.Shared.Teleportation.Systems;

namespace Content.Server._N14.PortalAutoLink
{
    public sealed partial class PortalAutoLinkSystem : EntitySystem
    {
        [Dependency] private readonly LinkedEntitySystem _linkedEntitySystem = default!;
        [Dependency] private readonly IEntityManager _entityMgr = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PortalAutoLinkComponent, MapInitEvent>(HandleMapInitialization);
        }

        private void HandleMapInitialization(Entity<PortalAutoLinkComponent> entity, ref MapInitEvent eventArgs)
        {
            PerformAutoLink(entity, out _);
        }

        public bool PerformAutoLink(Entity<PortalAutoLinkComponent> entity, out EntityUid? linkedEntityId)
        {
            linkedEntityId = null;

            var entityEnumerator = EntityQueryEnumerator<PortalAutoLinkComponent>();
            while (entityEnumerator.MoveNext(out var currentEntityUid, out var currentAutoLinkComponent))
            {
                if (entity.Comp == currentAutoLinkComponent)
                    continue;

                if (entity.Comp.LinkKey == currentAutoLinkComponent.LinkKey)
                {
                    if (_linkedEntitySystem.TryLink(entity, currentEntityUid, false))
                    {
                        RemComp<PortalAutoLinkComponent>(currentEntityUid);
                        RemComp<PortalAutoLinkComponent>(entity);
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
