using Content.Server.Administration.Logs;
using Content.Server.Damage.Systems;
using Content.Server.Effects;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared.Damage;
using Content.Shared.Projectiles;
using Robust.Shared.Color;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;

namespace Content.Server.Projectiles;

public sealed partial class ProjectileSystem
{
    /// <summary>
    /// Manually processes a projectile collision for prediction code.
    /// </summary>
    public void ProjectileCollide((EntityUid Uid, ProjectileComponent Component, PhysicsComponent Physics) projectile,
        EntityUid target, bool predicted = false)
    {
        var (uid, component, physics) = projectile;

        if (component.DamagedEntity || component is { Weapon: null, OnlyCollideWhenShot: true })
            return;

        var attemptEv = new ProjectileReflectAttemptEvent(uid, component, false);
        RaiseLocalEvent(target, ref attemptEv);
        if (attemptEv.Cancelled)
        {
            SetShooter(uid, component, target);
            return;
        }

        var ev = new ProjectileHitEvent(component.Damage, target, component.Shooter);
        RaiseLocalEvent(uid, ref ev);

        var modified = _damageableSystem.TryChangeDamage(target, ev.Damage, component.IgnoreResistances, origin: component.Shooter);
        var deleted = Deleted(target);

        if (modified is not null && EntityManager.EntityExists(component.Shooter))
        {
            if (modified.AnyPositive() && !deleted)
                _color.RaiseEffect(Color.Red, [target], Filter.Pvs(target, entityManager: EntityManager));

            _adminLogger.Add(LogType.BulletHit,
                HasComp<ActorComponent>(target) ? LogImpact.Extreme : LogImpact.High,
                $"Projectile {ToPrettyString(uid):projectile} shot by {ToPrettyString(component.Shooter!.Value):user} hit {ToPrettyString(target):target} and dealt {modified.GetTotal():damage} damage");
        }

        if (!deleted)
        {
            _guns.PlayImpactSound(target, modified, component.SoundHit, component.ForceSound);

            if (!physics.LinearVelocity.IsLengthZero())
                _sharedCameraRecoil.KickCamera(target, physics.LinearVelocity.Normalized());
        }

        if (component.Penetrate)
            component.IgnoredEntities.Add(target);
        else
            component.DamagedEntity = true;

        if (component.DeleteOnCollide)
            QueueDel(uid);

        if (component.ImpactEffect != null && TryComp(uid, out TransformComponent? xform))
            RaiseNetworkEvent(new ImpactEffectEvent(component.ImpactEffect, GetNetCoordinates(xform.Coordinates)),
                Filter.Pvs(xform.Coordinates, entityMan: EntityManager));
    }
}
