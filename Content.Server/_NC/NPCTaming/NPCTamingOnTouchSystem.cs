using System.Numerics;
using Content.Server.NPC;
using Content.Server.NPC.Components;
using Content.Server.NPC.Systems;
using Content.Server.Popups;
using Content.Shared.Damage;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._NC.NPCTaming;

// ReSharper disable once InconsistentNaming
public sealed class NPCTamingOnTouchSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly NPCSystem _npc = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NPCTamingOnTouchBehaviourComponent, ActivateInWorldEvent>(OnPetTry);
        SubscribeLocalEvent<TamedNpcFriendComponent, DamageChangedEvent>(OnFriendDamaged);
    }

    private void OnFriendDamaged(Entity<TamedNpcFriendComponent> entity, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased)
            return;

        if (TerminatingOrDeleted(entity))
            return;

        if (args.Origin is not { } origin)
            return;

        if (!HasComp<MobStateComponent>(origin))
            return;

        if (_npcFaction.IsEntityFriendly(entity, origin))
            return;

        if (!TryComp<NPCTamingOnTouchBehaviourComponent>(entity.Comp.Pet, out var petComp))
            return;

        // pet will try to kill itself
        if (entity.Comp.Pet == args.Origin)
            return;

        _npcFaction.AggroEntity(entity.Comp.Pet, origin);
        petComp.AggroMemories[origin] = _timing.CurTime + petComp.AggroTime;
    }
    private void OnPetTry(Entity<NPCTamingOnTouchBehaviourComponent> entity, ref ActivateInWorldEvent args)
    {
        var (uid, comp) = entity;

        if (_timing.CurTime < comp.LastPetTime + comp.PetDelay)
            return;

        comp.LastPetTime = _timing.CurTime;

        if (TerminatingOrDeleted(args.User) || TerminatingOrDeleted(uid))
            return;

        if (comp.Friend == args.User)
            return;

        // if persistent and we already have a friend - do nothing
        if (comp is { Persistent: true, Friend: not null })
        {
            DenyPet(entity, args.User);
            return;
        }

        if (comp.Whitelist != null && !comp.Whitelist.IsValid(args.User))
        {
            DenyPet(entity, args.User);
            return;
        }

        // check if this player already tried to tame a pet
        if (comp.OneTry && comp.TriedPlayers.Contains(args.User))
        {
            DenyPet(entity, args.User);
            return;
        }

        // prob tame chance
        if (!_random.Prob(comp.TameChance))
        {
            DenyPet(entity, args.User);
            if (!comp.OneTry)
                return;

            comp.TriedPlayers.Add(args.User);
            return;
        }

        // remove prev friend, if exists
        if (comp.Friend != null)
            RemoveFriend(uid, comp.Friend.Value, comp);

        if (TryComp<TamedNpcFriendComponent>(args.User, out var tamedComp))
            RemovePet(args.User, tamedComp);

        // add new friend respectively
        AddFriend(uid, args.User, comp);

        if (comp.Follow && comp.Friend is not null)
            _npc.SetBlackboard(uid, NPCBlackboard.FollowTarget, new EntityCoordinates(comp.Friend.Value, Vector2.Zero));

        SuccessPet(entity, args.User);

        _popup.PopupEntity(Loc.GetString(comp.SuccessPopup), uid, args.User);

        args.Handled = true;
    }

    public void AddFriend(EntityUid owner, EntityUid friend, NPCTamingOnTouchBehaviourComponent? component = null)
    {
        if (!Resolve(owner, ref component))
            return;

        var friendComp = EnsureComp<TamedNpcFriendComponent>(friend);
        friendComp.Pet = owner;

        component.Friend = friend;
        var exception = EnsureComp<FactionExceptionComponent>(owner);
        exception.Ignored.Add(friend);
    }

    private void RemovePet(EntityUid host, TamedNpcFriendComponent? component = null)
    {
        if (!Resolve(host, ref component, logMissing: false))
            return;

        if (Deleted(component.Pet) ||
            !TryComp<NPCTamingOnTouchBehaviourComponent>(component.Pet, out var petComp))
            return;

        if (petComp.Friend != null)
            RemoveFriend(component.Pet, petComp.Friend.Value, petComp);
    }

    public void RemoveFriend(EntityUid owner, EntityUid friend, NPCTamingOnTouchBehaviourComponent? component = null)
    {
        if (!Resolve(owner, ref component))
            return;

        RemComp<TamedNpcFriendComponent>(friend);
        var exception = EnsureComp<FactionExceptionComponent>(owner);
        exception.Ignored.Remove(friend);
        component.Friend = null;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<NPCTamingOnTouchBehaviourComponent, FactionExceptionComponent>();
        while (query.MoveNext(out var uid, out var petComp, out var factionException))
        {
            foreach (var memory in petComp.AggroMemories)
            {
                if (!TerminatingOrDeleted(memory.Key) && _timing.CurTime < memory.Value)
                    continue;

                _npcFaction.DeAggroEntity(uid, memory.Key, factionException);
            }
        }
    }

    private void DenyPet(Entity<NPCTamingOnTouchBehaviourComponent> target, EntityUid performer)
    {
        var (uid, comp) = target;

        _popup.PopupEntity(Loc.GetString(comp.DeniedPopup), uid, performer);

        if (comp.DeniedSound is not null)
            _audioSystem.PlayEntity(comp.DeniedSound, Filter.Entities(performer, uid), uid, true);

        if (comp.DeniedSpawn is not null)
            Spawn(comp.DeniedSpawn.Value, _transform.GetMapCoordinates(uid));
    }

    private void SuccessPet(Entity<NPCTamingOnTouchBehaviourComponent> target, EntityUid performer)
    {
        var (uid, comp) = target;

        _popup.PopupEntity(Loc.GetString(comp.SuccessPopup), uid, performer);

        if (comp.SuccessSound is not null)
            _audioSystem.PlayEntity(comp.SuccessSound, Filter.Entities(performer, uid), uid, true);

        if (comp.SuccessSpawn is not null)
            Spawn(comp.SuccessSpawn.Value, _transform.GetMapCoordinates(uid));
    }
}
