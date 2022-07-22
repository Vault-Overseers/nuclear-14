using Content.Shared.Actions;
using Content.Shared.Administration.Logs;
using Content.Shared.Audio;
using Content.Shared.CombatMode;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Damage;
using Content.Shared.Examine;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Throwing;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Tag;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Weapons.Ranged.Systems;

public abstract partial class SharedGunSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] protected readonly IMapManager MapManager = default!;
    [Dependency] private   readonly INetManager _netManager = default!;
    [Dependency] protected readonly IPrototypeManager ProtoManager = default!;
    [Dependency] protected readonly IRobustRandom Random = default!;
    [Dependency] protected readonly ISharedAdminLogManager Logs = default!;
    [Dependency] protected readonly DamageableSystem Damageable = default!;
    [Dependency] private   readonly ItemSlotsSystem _slots = default!;
    [Dependency] protected readonly SharedActionsSystem Actions = default!;
    [Dependency] private   readonly SharedCombatModeSystem _combatMode = default!;
    [Dependency] protected readonly SharedContainerSystem Containers = default!;
    [Dependency] protected readonly SharedPhysicsSystem Physics = default!;
    [Dependency] protected readonly SharedPopupSystem PopupSystem = default!;
    [Dependency] protected readonly ThrowingSystem ThrowingSystem = default!;
    [Dependency] protected readonly TagSystem TagSystem = default!;
    [Dependency] protected readonly SharedProjectileSystem Projectiles = default!;

    protected ISawmill Sawmill = default!;

    private const float InteractNextFire = 0.3f;
    private const double SafetyNextFire = 0.5;
    private const float EjectOffset = 0.4f;
    protected const string AmmoExamineColor = "yellow";
    protected const string FireRateExamineColor = "yellow";
    protected const string ModeExamineColor = "cyan";

    public override void Initialize()
    {
        Sawmill = Logger.GetSawmill("gun");
        Sawmill.Level = LogLevel.Info;
        SubscribeLocalEvent<GunComponent, ComponentGetState>(OnGetState);
        SubscribeAllEvent<RequestShootEvent>(OnShootRequest);
        SubscribeAllEvent<RequestStopShootEvent>(OnStopShootRequest);
        SubscribeLocalEvent<GunComponent, ComponentHandleState>(OnHandleState);
        SubscribeLocalEvent<GunComponent, MeleeAttackAttemptEvent>(OnGunMeleeAttempt);

        // Ammo providers
        InitializeBallistic();
        InitializeBattery();
        InitializeCartridge();
        InitializeChamberMagazine();
        InitializeMagazine();
        InitializeRevolver();
        InitializeBasicEntity();

        // Interactions
        SubscribeLocalEvent<GunComponent, GetVerbsEvent<AlternativeVerb>>(OnAltVerb);
        SubscribeLocalEvent<GunComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<GunComponent, CycleModeEvent>(OnCycleMode);
        SubscribeLocalEvent<GunComponent, ComponentInit>(OnGunInit);
    }

    private void OnGunInit(EntityUid uid, GunComponent component, ComponentInit args)
    {
        DebugTools.Assert((component.AvailableModes & component.SelectedMode) != 0x0);
    }

    private void OnGunMeleeAttempt(EntityUid uid, GunComponent component, ref MeleeAttackAttemptEvent args)
    {
        if (TagSystem.HasTag(args.User, "GunsDisabled"))
            return;

        args.Cancelled = true;
    }

    private void OnShootRequest(RequestShootEvent msg, EntitySessionEventArgs args)
    {
        var user = args.SenderSession.AttachedEntity;

        if (user == null) return;

        var gun = GetGun(user.Value);

        if (gun?.Owner != msg.Gun) return;

        gun.ShootCoordinates = msg.Coordinates;
        Sawmill.Debug($"Set shoot coordinates to {gun.ShootCoordinates}");
        AttemptShoot(user.Value, gun);
    }

    private void OnStopShootRequest(RequestStopShootEvent ev, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity == null ||
            !TryComp<GunComponent>(ev.Gun, out var gun)) return;

        var userGun = GetGun(args.SenderSession.AttachedEntity.Value);

        if (userGun != gun) return;

        StopShooting(gun);
    }

    private void OnGetState(EntityUid uid, GunComponent component, ref ComponentGetState args)
    {
        args.State = new GunComponentState
        {
            FireRate = component.FireRate,
            CurrentAngle = component.CurrentAngle,
            MinAngle = component.MinAngle,
            MaxAngle = component.MaxAngle,
            NextFire = component.NextFire,
            ShotCounter = component.ShotCounter,
            SelectiveFire = component.SelectedMode,
            AvailableSelectiveFire = component.AvailableModes,
        };
    }

    private void OnHandleState(EntityUid uid, GunComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not GunComponentState state) return;

        Sawmill.Debug($"Handle state: setting shot count from {component.ShotCounter} to {state.ShotCounter}");
        component.FireRate = state.FireRate;
        component.CurrentAngle = state.CurrentAngle;
        component.MinAngle = state.MinAngle;
        component.MaxAngle = state.MaxAngle;
        component.NextFire = state.NextFire;
        component.ShotCounter = state.ShotCounter;
        component.SelectedMode = state.SelectiveFire;
        component.AvailableModes = state.AvailableSelectiveFire;
    }

    public GunComponent? GetGun(EntityUid entity)
    {
        if (!EntityManager.TryGetComponent(entity, out SharedHandsComponent? hands) ||
            hands.ActiveHandEntity is not { } held)
        {
            return null;
        }

        if (!EntityManager.TryGetComponent(held, out GunComponent? gun))
            return null;

        if (!_combatMode.IsInCombatMode(entity))
            return null;

        return gun;
    }

    private void StopShooting(GunComponent gun)
    {
        if (gun.ShotCounter == 0) return;

        Sawmill.Debug($"Stopped shooting {ToPrettyString(gun.Owner)}");
        gun.ShotCounter = 0;
        gun.ShootCoordinates = null;
        Dirty(gun);
    }

    private void AttemptShoot(EntityUid user, GunComponent gun)
    {
        if (gun.FireRate <= 0f) return;

        var toCoordinates = gun.ShootCoordinates;

        if (toCoordinates == null) return;

        if (TagSystem.HasTag(user, "GunsDisabled"))
        {
            Popup(Loc.GetString("gun-disabled"), user, user);
            return;
        }

        var curTime = Timing.CurTime;

        // Need to do this to play the clicking sound for empty automatic weapons
        // but not play anything for burst fire.
        if (gun.NextFire > curTime) return;

        // First shot
        if (gun.ShotCounter == 0 && gun.NextFire < curTime)
            gun.NextFire = curTime;

        var shots = 0;
        var lastFire = gun.NextFire;
        var fireRate = TimeSpan.FromSeconds(1f / gun.FireRate);

        while (gun.NextFire <= curTime)
        {
            gun.NextFire += fireRate;
            shots++;
        }

        // Get how many shots we're actually allowed to make, due to clip size or otherwise.
        // Don't do this in the loop so we still reset NextFire.
        switch (gun.SelectedMode)
        {
            case SelectiveFire.SemiAuto:
                shots = Math.Min(shots, 1 - gun.ShotCounter);
                break;
            case SelectiveFire.Burst:
                shots = Math.Min(shots, 3 - gun.ShotCounter);
                break;
            case SelectiveFire.FullAuto:
                break;
            default:
                throw new ArgumentOutOfRangeException($"No implemented shooting behavior for {gun.SelectedMode}!");
        }

        var fromCoordinates = Transform(user).Coordinates;
        // Remove ammo
        var ev = new TakeAmmoEvent(shots, new List<IShootable>(), fromCoordinates, user);

        // Listen it just makes the other code around it easier if shots == 0 to do this.
        if (shots > 0)
            RaiseLocalEvent(gun.Owner, ev, false);

        DebugTools.Assert(ev.Ammo.Count <= shots);
        DebugTools.Assert(shots >= 0);
        UpdateAmmoCount(gun.Owner);

        // Even if we don't actually shoot update the ShotCounter. This is to avoid spamming empty sounds
        // where the gun may be SemiAuto or Burst.
        gun.ShotCounter += shots;

        if (ev.Ammo.Count <= 0)
        {
            // Play empty gun sounds if relevant
            // If they're firing an existing clip then don't play anything.
            if (shots > 0)
            {
                // Don't spam safety sounds at gun fire rate, play it at a reduced rate.
                // May cause prediction issues? Needs more tweaking
                gun.NextFire = TimeSpan.FromSeconds(Math.Max(lastFire.TotalSeconds + SafetyNextFire, gun.NextFire.TotalSeconds));
                PlaySound(gun.Owner, gun.SoundEmpty?.GetSound(Random, ProtoManager), user);
                Dirty(gun);
                return;
            }

            return;
        }

        // Shoot confirmed - sounds also played here in case it's invalid (e.g. cartridge already spent).
        Shoot(gun, ev.Ammo, fromCoordinates, toCoordinates.Value, user);
        Dirty(gun);
    }

    public void Shoot(
        GunComponent gun,
        EntityUid ammo,
        EntityCoordinates fromCoordinates,
        EntityCoordinates toCoordinates,
        EntityUid? user = null)
    {
        var shootable = EnsureComp<AmmoComponent>(ammo);
        Shoot(gun, new List<IShootable>(1) { shootable }, fromCoordinates, toCoordinates, user);
    }

    public abstract void Shoot(
        GunComponent gun,
        List<IShootable> ammo,
        EntityCoordinates fromCoordinates,
        EntityCoordinates toCoordinates,
        EntityUid? user = null);

    public void Shoot(
        GunComponent gun,
        IShootable ammo,
        EntityCoordinates fromCoordinates,
        EntityCoordinates toCoordinates,
        EntityUid? user = null)
    {
        Shoot(gun, new List<IShootable>(1) { ammo }, fromCoordinates, toCoordinates, user);
    }

    protected abstract void PlaySound(EntityUid gun, string? sound, EntityUid? user = null);

    protected abstract void Popup(string message, EntityUid? uid, EntityUid? user);

    /// <summary>
    /// Call this whenever the ammo count for a gun changes.
    /// </summary>
    protected virtual void UpdateAmmoCount(EntityUid uid) {}

    protected void SetCartridgeSpent(CartridgeAmmoComponent cartridge, bool spent)
    {
        if (cartridge.Spent != spent)
            Dirty(cartridge);

        cartridge.Spent = spent;
        if (!TryComp<AppearanceComponent>(cartridge.Owner, out var appearance)) return;
        appearance.SetData(AmmoVisuals.Spent, spent);
    }

    /// <summary>
    /// Drops a single cartridge / shell
    /// </summary>
    protected void EjectCartridge(
        EntityUid entity,
        bool playSound = true)
    {
        // TODO: Sound limit version.
        var offsetPos = (Random.NextVector2(EjectOffset));
        var xform = Transform(entity);

        var coordinates = xform.Coordinates;
        coordinates = coordinates.Offset(offsetPos);

        xform.LocalRotation = Random.NextAngle();
        xform.Coordinates = coordinates;

        string? sound = null;

        if (TryComp<CartridgeAmmoComponent>(entity, out var cartridge))
        {
            sound = cartridge.EjectSound?.GetSound(Random, ProtoManager);
        }

        if (sound != null && playSound)
            SoundSystem.Play(sound, Filter.Pvs(entity, entityManager: EntityManager), coordinates, AudioHelpers.WithVariation(0.05f).WithVolume(-1f));
    }

    protected void MuzzleFlash(EntityUid gun, AmmoComponent component, EntityUid? user = null)
    {
        var sprite = component.MuzzleFlash;

        if (sprite == null)
            return;

        var ev = new MuzzleFlashEvent(sprite);

        CreateEffect(gun, ev, user);
    }

    protected abstract void CreateEffect(EntityUid uid, MuzzleFlashEvent message, EntityUid? user = null);

    [Serializable, NetSerializable]
    protected sealed class GunComponentState : ComponentState
    {
        public Angle CurrentAngle;
        public Angle MinAngle;
        public Angle MaxAngle;
        public TimeSpan NextFire;
        public float FireRate;
        public int ShotCounter;
        public SelectiveFire SelectiveFire;
        public SelectiveFire AvailableSelectiveFire;
    }

    /// <summary>
    /// Used for animated effects on the client.
    /// </summary>
    [Serializable, NetSerializable]
    protected sealed class HitscanEvent : EntityEventArgs
    {
        public List<(EntityCoordinates coordinates, Angle angle, SpriteSpecifier Sprite, float Distance)> Sprites = new();
    }
}

public enum EffectLayers : byte
{
    Unshaded,
}

[Serializable, NetSerializable]
public enum AmmoVisuals : byte
{
    Spent,
    AmmoCount,
    AmmoMax,
    HasAmmo, // used for generic visualizers. c# stuff can just check ammocount != 0
    MagLoaded,
}
