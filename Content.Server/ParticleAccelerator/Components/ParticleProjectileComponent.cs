using Content.Server.Projectiles;
using Content.Server.Projectiles.Components;
using Content.Server.Singularity.Components;
using Content.Shared.Singularity.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Timing;

namespace Content.Server.ParticleAccelerator.Components
{
    [RegisterComponent]
    public sealed class ParticleProjectileComponent : Component
    {
        [Dependency] private readonly IEntityManager _entMan = default!;

        public ParticleAcceleratorPowerState State;

        public void Fire(ParticleAcceleratorPowerState state, Angle angle, EntityUid firer)
        {
            State = state;

            if (!_entMan.TryGetComponent<PhysicsComponent?>(Owner, out var physicsComponent))
            {
                Logger.Error("ParticleProjectile tried firing, but it was spawned without a CollidableComponent");
                return;
            }
            physicsComponent.BodyStatus = BodyStatus.InAir;

            if (!_entMan.TryGetComponent<ProjectileComponent?>(Owner, out var projectileComponent))
            {
                Logger.Error("ParticleProjectile tried firing, but it was spawned without a ProjectileComponent");
                return;
            }

            _entMan.EntitySysManager.GetEntitySystem<ProjectileSystem>().SetShooter(projectileComponent, firer);

            if (!_entMan.TryGetComponent<SinguloFoodComponent?>(Owner, out var singuloFoodComponent))
            {
                Logger.Error("ParticleProjectile tried firing, but it was spawned without a SinguloFoodComponent");
                return;
            }
            var multiplier = State switch
            {
                ParticleAcceleratorPowerState.Standby => 0,
                ParticleAcceleratorPowerState.Level0 => 1,
                ParticleAcceleratorPowerState.Level1 => 3,
                ParticleAcceleratorPowerState.Level2 => 6,
                ParticleAcceleratorPowerState.Level3 => 10,
                _ => 0
            };
            singuloFoodComponent.Energy = 10 * multiplier;

            if (_entMan.TryGetComponent(Owner, out AppearanceComponent? appearance))
            {
                appearance.SetData(ParticleAcceleratorVisuals.VisualState, state);
            }

            physicsComponent.LinearVelocity = angle.ToWorldVec() * 20f;

            _entMan.GetComponent<TransformComponent>(Owner).LocalRotation = angle;
            Timer.Spawn(3000, () => _entMan.DeleteEntity(Owner));
        }
    }
}
