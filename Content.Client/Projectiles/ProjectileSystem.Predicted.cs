using Content.Shared.Projectiles;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;

namespace Content.Client.Projectiles;

public sealed partial class ProjectileSystem
{
    /// <summary>
    /// Handles a projectile collision for prediction code on the client.
    /// </summary>
    public void ProjectileCollide((EntityUid Uid, ProjectileComponent Component, PhysicsComponent Physics) projectile,
        EntityUid target)
    {
        var (uid, component, physics) = projectile;

        if (component.DamagedEntity || component is { Weapon: null, OnlyCollideWhenShot: true })
            return;

        var ev = new ProjectileHitEvent(component.Damage, target, component.Shooter);
        RaiseLocalEvent(uid, ref ev);

        if (component.Penetrate)
            component.IgnoredEntities.Add(target);
        else
            component.DamagedEntity = true;

        if (component.DeleteOnCollide)
            QueueDel(uid);

        if (component.ImpactEffect != null && TryComp(uid, out TransformComponent? xform))
            RaiseNetworkEvent(new ImpactEffectEvent(component.ImpactEffect, GetNetCoordinates(xform.Coordinates)),
                Filter.Pvs(xform.Coordinates));
    }
}
