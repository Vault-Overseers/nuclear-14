﻿using System.Threading;
using Content.Server.Body.Components;
using Content.Server.Coordinates.Helpers;
using Content.Server.Decals;
using Content.Server.DoAfter;
using Content.Server.Doors.Components;
using Content.Server.Magic.Events;
using Content.Server.Popups;
using Content.Server.Spawners.Components;
using Content.Server.Weapon.Ranged.Systems;
using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Body.Components;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Interaction.Events;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Shared.Spawners.Components;
using Content.Shared.Storage;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Magic;

/// <summary>
/// Handles learning and using spells (actions)
/// </summary>
public sealed class MagicSystem : EntitySystem
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedDoorSystem _doorSystem = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly GunSystem _gunSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpellbookComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<SpellbookComponent, UseInHandEvent>(OnUse);
        SubscribeLocalEvent<SpellbookComponent, LearnDoAfterComplete>(OnLearnComplete);
        SubscribeLocalEvent<SpellbookComponent, LearnDoAfterCancel>(OnLearnCancel);

        SubscribeLocalEvent<InstantSpawnSpellEvent>(OnInstantSpawn);
        SubscribeLocalEvent<TeleportSpellEvent>(OnTeleportSpell);
        SubscribeLocalEvent<KnockSpellEvent>(OnKnockSpell);
        SubscribeLocalEvent<SmiteSpellEvent>(OnSmiteSpell);
        SubscribeLocalEvent<WorldSpawnSpellEvent>(OnWorldSpawn);
        SubscribeLocalEvent<ProjectileSpellEvent>(OnProjectileSpell);
    }

    private void OnInit(EntityUid uid, SpellbookComponent component, ComponentInit args)
    {
        //Negative charges means the spell can be used without it running out.
        foreach (var (id, charges) in component.WorldSpells)
        {
            var spell = new WorldTargetAction(_prototypeManager.Index<WorldTargetActionPrototype>(id));
            _actionsSystem.SetCharges(spell, charges < 0 ? null : charges);
            component.Spells.Add(spell);
        }

        foreach (var (id, charges) in component.InstantSpells)
        {
            var spell = new InstantAction(_prototypeManager.Index<InstantActionPrototype>(id));
            _actionsSystem.SetCharges(spell, charges < 0 ? null : charges);
            component.Spells.Add(spell);
        }

        foreach (var (id, charges) in component.EntitySpells)
        {
            var spell = new EntityTargetAction(_prototypeManager.Index<EntityTargetActionPrototype>(id));
            _actionsSystem.SetCharges(spell, charges < 0 ? null : charges);
            component.Spells.Add(spell);
        }
    }

    private void OnUse(EntityUid uid, SpellbookComponent component, UseInHandEvent args)
    {
        if (args.Handled)
            return;

        AttemptLearn(uid, component, args);

        args.Handled = true;
    }

    private void AttemptLearn(EntityUid uid, SpellbookComponent component, UseInHandEvent args)
    {
        if (component.CancelToken != null) return;

        component.CancelToken = new CancellationTokenSource();

        var doAfterEventArgs = new DoAfterEventArgs(args.User, component.LearnTime, component.CancelToken.Token, uid)
        {
            BreakOnTargetMove = true,
            BreakOnUserMove = true,
            BreakOnDamage = true,
            BreakOnStun = true,
            NeedHand = true, //What, are you going to read with your eyes only??
            TargetFinishedEvent = new LearnDoAfterComplete(args.User),
            TargetCancelledEvent = new LearnDoAfterCancel(),
        };

        _doAfter.DoAfter(doAfterEventArgs);
    }

    private void OnLearnComplete(EntityUid uid, SpellbookComponent component, LearnDoAfterComplete ev)
    {
        component.CancelToken = null;
        _actionsSystem.AddActions(ev.User, component.Spells, uid);
    }

    private void OnLearnCancel(EntityUid uid, SpellbookComponent component, LearnDoAfterCancel args)
    {
        component.CancelToken = null;
    }

    #region Spells

    /// <summary>
    /// Handles the instant action (i.e. on the caster) attempting to spawn an entity.
    /// </summary>
    private void OnInstantSpawn(InstantSpawnSpellEvent args)
    {
        if (args.Handled)
            return;

        var transform = Transform(args.Performer);

        foreach (var position in GetSpawnPositions(transform, args.Pos))
        {
            var ent = Spawn(args.Prototype, position.SnapToGrid(EntityManager, _mapManager));

            if (args.PreventCollideWithCaster)
            {
                var comp = EnsureComp<PreventCollideComponent>(ent);
                comp.Uid = args.Performer;
            }
        }

        args.Handled = true;
    }

    private void OnProjectileSpell(ProjectileSpellEvent ev)
    {
        if (ev.Handled)
            return;

        var xform = Transform(ev.Performer);

        foreach (var pos in GetSpawnPositions(xform, ev.Pos))
        {
            var ent = Spawn(ev.Prototype, pos.SnapToGrid(EntityManager, _mapManager));
            _gunSystem.ShootProjectile(ent,ev.Target.Position - Transform(ent).MapPosition.Position, ev.Performer);
        }
    }

    private List<EntityCoordinates> GetSpawnPositions(TransformComponent casterXform, MagicSpawnData data)
    {
        switch (data)
        {
            case TargetCasterPos:
                return new List<EntityCoordinates>(1) {casterXform.Coordinates};
            case TargetInFront:
            {
                // This is shit but you get the idea.
                var directionPos = casterXform.Coordinates.Offset(casterXform.LocalRotation.ToWorldVec().Normalized);

                if (!_mapManager.TryGetGrid(casterXform.GridUid, out var mapGrid))
                    return new List<EntityCoordinates>();

                if (!directionPos.TryGetTileRef(out var tileReference, EntityManager, _mapManager))
                    return new List<EntityCoordinates>();

                var tileIndex = tileReference.Value.GridIndices;
                var coords = mapGrid.GridTileToLocal(tileIndex);
                EntityCoordinates coordsPlus;
                EntityCoordinates coordsMinus;

                var dir = casterXform.LocalRotation.GetCardinalDir();
                switch (dir)
                {
                    case Direction.North:
                    case Direction.South:
                    {
                        coordsPlus = mapGrid.GridTileToLocal(tileIndex + (1, 0));
                        coordsMinus = mapGrid.GridTileToLocal(tileIndex + (-1, 0));
                        return new List<EntityCoordinates>(3)
                        {
                            coords,
                            coordsPlus,
                            coordsMinus,
                        };
                    }
                    case Direction.East:
                    case Direction.West:
                    {
                        coordsPlus = mapGrid.GridTileToLocal(tileIndex + (0, 1));
                        coordsMinus = mapGrid.GridTileToLocal(tileIndex + (0, -1));
                        return new List<EntityCoordinates>(3)
                        {
                            coords,
                            coordsPlus,
                            coordsMinus,
                        };
                    }
                }

                return new List<EntityCoordinates>();
            }
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    /// <summary>
    /// Teleports the user to the clicked location
    /// </summary>
    /// <param name="args"></param>
    private void OnTeleportSpell(TeleportSpellEvent args)
    {
        if (args.Handled)
            return;

        var transform = Transform(args.Performer);

        if (transform.MapID != args.Target.MapId) return;

        transform.WorldPosition = args.Target.Position;
        transform.AttachToGridOrMap();
        SoundSystem.Play(args.BlinkSound.GetSound(), Filter.Pvs(args.Target), args.Performer, AudioParams.Default.WithVolume(args.BlinkVolume));
        args.Handled = true;
    }

    /// <summary>
    /// Opens all doors within range
    /// </summary>
    /// <param name="args"></param>
    private void OnKnockSpell(KnockSpellEvent args)
    {
        if (args.Handled)
            return;

        //Get the position of the player
        var transform = Transform(args.Performer);
        var coords = transform.Coordinates;

        SoundSystem.Play(args.KnockSound.GetSound(), Filter.Pvs(coords), args.Performer, AudioParams.Default.WithVolume(args.KnockVolume));

        //Look for doors and don't open them if they're already open.
        foreach (var entity in _lookup.GetEntitiesInRange(coords, args.Range))
        {
            if (TryComp<AirlockComponent>(entity, out var airlock))
                airlock.BoltsDown = false;

            if (TryComp<DoorComponent>(entity, out var doorComp) && doorComp.State is not DoorState.Open)
                _doorSystem.StartOpening(doorComp.Owner);
        }

        args.Handled = true;
    }

    private void OnSmiteSpell(SmiteSpellEvent ev)
    {
        if (ev.Handled)
            return;

        var direction = Transform(ev.Target).MapPosition.Position - Transform(ev.Performer).MapPosition.Position;
        var impulseVector = direction * 10000;
        Comp<PhysicsComponent>(ev.Target).ApplyLinearImpulse(impulseVector);

        if (!TryComp<BodyComponent>(ev.Target, out var body))
            return;

        var ents = body.Gib(true);

        if (!ev.DeleteNonBrainParts)
            return;

        foreach (var part in ents)
        {
            // just leaves a brain and clothes
            if ((HasComp<BodyPartComponent>(part) || HasComp<MechanismComponent>(part))
                && !HasComp<BrainComponent>(part))
            {
                QueueDel(part);
            }
        }
    }

    /// <summary>
    /// Spawns entity prototypes from a list within range of click.
    /// </summary>
    /// <remarks>
    /// It will offset mobs after the first mob based on the OffsetVector2 property supplied.
    /// </remarks>
    /// <param name="args"> The Spawn Spell Event args.</param>
    private void OnWorldSpawn(WorldSpawnSpellEvent args)
    {
        if (args.Handled)
            return;

        var targetMapCoords = args.Target;

        SpawnSpellHelper(args.Contents, targetMapCoords, args.Lifetime, args.Offset);

        args.Handled = true;
    }

    /// <summary>
    /// Loops through a supplied list of entity prototypes and spawns them
    /// </summary>
    /// <remarks>
    /// If an offset of 0, 0 is supplied then the entities will all spawn on the same tile.
    /// Any other offset will spawn entities starting from the source Map Coordinates and will increment the supplied
    /// offset
    /// </remarks>
    /// <param name="entityEntries"> The list of Entities to spawn in</param>
    /// <param name="mapCoords"> Map Coordinates where the entities will spawn</param>
    /// <param name="lifetime"> Check to see if the entities should self delete</param>
    /// <param name="offsetVector2"> A Vector2 offset that the entities will spawn in</param>
    private void SpawnSpellHelper(List<EntitySpawnEntry> entityEntries, MapCoordinates mapCoords, float? lifetime, Vector2 offsetVector2)
    {
        var getProtos = EntitySpawnCollection.GetSpawns(entityEntries, _random);

        var offsetCoords = mapCoords;
        foreach (var proto in getProtos)
        {
            // TODO: Share this code with instant because they're both doing similar things for positioning.
            var entity = Spawn(proto, offsetCoords);
            offsetCoords = offsetCoords.Offset(offsetVector2);

            if (lifetime != null)
            {
                var comp = EnsureComp<TimedDespawnComponent>(entity);
                comp.Lifetime = lifetime.Value;
            }
        }
    }

    #endregion

    #region DoAfterClasses

    private sealed class LearnDoAfterComplete : EntityEventArgs
    {
        public readonly EntityUid User;

        public LearnDoAfterComplete(EntityUid uid)
        {
            User = uid;
        }
    }

    private sealed class LearnDoAfterCancel : EntityEventArgs { }

    #endregion
}
