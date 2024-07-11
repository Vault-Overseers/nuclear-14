using System.Numerics;
using Content.Server.NPC;
using Content.Server.NPC.Components;
using Content.Server.NPC.Systems;
using Content.Server.Popups;
using Content.Shared.Interaction;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server._NC.NPCTaming;

// ReSharper disable once InconsistentNaming
public sealed class NPCTamingOnTouchSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly NPCSystem _npc = default!;
    [Dependency] private readonly PopupSystem _popup = default!;


    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NPCTamingOnTouchBehaviourComponent, ActivateInWorldEvent>(OnPetTry);
    }

    private void OnPetTry(Entity<NPCTamingOnTouchBehaviourComponent> entity, ref ActivateInWorldEvent args)
    {
        var (uid, comp) = entity;

        if (comp.Friend == args.User)
            return;

        // if persistent and we already have a friend - do nothing
        if (comp is { Persistent: true, Friend: not null })
        {
            _popup.PopupEntity(Loc.GetString(comp.DeniedPopup), uid, args.User);
            return;
        }

        if (comp.Whitelist != null && !comp.Whitelist.IsValid(uid))
        {
            _popup.PopupEntity(Loc.GetString(comp.DeniedPopup), uid, args.User);
            return;
        }

        // check if this player already tried to tame a pet
        if (comp.OneTry && comp.TriedPlayers.Contains(args.User))
        {
            _popup.PopupEntity(Loc.GetString(comp.DeniedPopup), uid, args.User);
            return;
        }

        // prob tame chance
        if (!_random.Prob(comp.TameChance))
        {
            if (!comp.OneTry)
                return;

            comp.TriedPlayers.Add(args.User);
            _popup.PopupEntity(Loc.GetString(comp.DeniedPopup), uid, args.User);
            return;
        }

        // remove prev friend, if exists
        if (comp.Friend != null)
            RemoveFriend(uid, comp.Friend.Value, comp);

        // add new friend respectively
        AddFriend(uid, args.User, comp);

        if (comp.Follow && comp.Friend is not null)
            _npc.SetBlackboard(uid, NPCBlackboard.FollowTarget, new EntityCoordinates(comp.Friend.Value, Vector2.Zero));

        _popup.PopupEntity(Loc.GetString(comp.SuccessPopup), uid, args.User);
    }

    public void AddFriend(EntityUid owner, EntityUid friend, NPCTamingOnTouchBehaviourComponent? component = null)
    {
        if (!Resolve(owner, ref component))
            return;

        component.Friend = friend;
        var exception = EnsureComp<FactionExceptionComponent>(owner);
        exception.Ignored.Add(friend);
    }

    public void RemoveFriend(EntityUid owner, EntityUid friend, NPCTamingOnTouchBehaviourComponent? component = null)
    {
        if (!Resolve(owner, ref component))
            return;

        var exception = EnsureComp<FactionExceptionComponent>(owner);
        exception.Ignored.Remove(friend);
        component.Friend = null;
    }
}
