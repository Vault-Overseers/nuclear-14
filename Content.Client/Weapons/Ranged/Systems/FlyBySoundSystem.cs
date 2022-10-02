using Content.Client.Projectiles;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Client.Player;
using Robust.Shared.Audio;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Client.Weapons.Ranged.Systems;

public sealed class FlyBySoundSystem : SharedFlyBySoundSystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FlyBySoundComponent, StartCollideEvent>(OnCollide);
    }

    private void OnCollide(EntityUid uid, FlyBySoundComponent component, StartCollideEvent args)
    {
        var attachedEnt = _player.LocalPlayer?.ControlledEntity;

        // If it's not our ent or we shot it.
        if (attachedEnt == null ||
            args.OtherFixture.Body.Owner != attachedEnt ||
            TryComp<ProjectileComponent>(args.OurFixture.Body.Owner, out var projectile) &&
            projectile.Shooter == attachedEnt)
        {
            return;
        }

        if (args.OurFixture.ID != FlyByFixture ||
            !_random.Prob(component.Prob))
        {
            return;
        }

        // Play attached to our entity because the projectile may immediately delete or the likes.
        _audio.Play(component.Sound, Filter.Local(), attachedEnt.Value);
    }
}
