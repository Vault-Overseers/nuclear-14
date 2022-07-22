using Content.Server.Popups;
using Content.Server.Interaction.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.MobState.Components;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Random;


namespace Content.Server.Interaction;

public sealed class InteractionPopupSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<InteractionPopupComponent, InteractHandEvent>(OnInteractHand);
    }

    private void OnInteractHand(EntityUid uid, InteractionPopupComponent component, InteractHandEvent args)
    {
        if (args.Handled || args.User == args.Target)
            return;

        var curTime = _gameTiming.CurTime;

        if (curTime < component.LastInteractTime + component.InteractDelay)
            return;

        if (TryComp<MobStateComponent>(uid, out var state) // if it has a MobStateComponent,
            && !state.IsAlive())                           // AND if that state is not Alive (e.g. dead/incapacitated/critical)
            return;

        string msg = ""; // Stores the text to be shown in the popup message
        string? sfx = null; // Stores the filepath of the sound to be played

        if (_random.Prob(component.SuccessChance))
        {
            if (component.InteractSuccessString != null)
                msg = Loc.GetString(component.InteractSuccessString, ("target", Identity.Entity(uid, EntityManager))); // Success message (localized).

            if (component.InteractSuccessSound != null)
                sfx = component.InteractSuccessSound.GetSound();
        }
        else
        {
            if (component.InteractFailureString != null)
                msg = Loc.GetString(component.InteractFailureString, ("target", Identity.Entity(uid, EntityManager))); // Failure message (localized).

            if (component.InteractFailureSound != null)
                sfx = component.InteractFailureSound.GetSound();
        }

        if (component.MessagePerceivedByOthers != null)
        {
            string msgOthers = Loc.GetString(component.MessagePerceivedByOthers,
                ("user", Identity.Entity(args.User, EntityManager)), ("target", Identity.Entity(uid, EntityManager)));
            _popupSystem.PopupEntity(msg, uid, Filter.Entities(args.User));
            _popupSystem.PopupEntity(msgOthers, uid, Filter.Pvs(uid, 2F, EntityManager).RemoveWhereAttachedEntity(puid => puid == args.User));
        }
        else
            _popupSystem.PopupEntity(msg, uid, Filter.Entities(args.User)); //play only for the initiating entity.

        if (sfx is not null) //not all cases will have sound.
        {
            if (component.SoundPerceivedByOthers)
                SoundSystem.Play(sfx, Filter.Pvs(args.Target), args.Target); //play for everyone in range
            else
                SoundSystem.Play(sfx, Filter.Entities(args.User, args.Target), args.Target); //play only for the initiating entity and its target.
        }

        component.LastInteractTime = curTime;
        args.Handled = true;
    }
}
