using Content.Server.Morgue.Components;
using Content.Shared.Morgue;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Audio;
using Content.Server.Storage.Components;
using System.Threading;
using Content.Shared.Verbs;
using Content.Shared.Database;
using Content.Shared.Interaction.Events;
using Content.Server.Players;
using Content.Server.GameTicking;
using Content.Shared.Popups;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Examine;
using Content.Shared.Standing;
using Content.Shared.Storage;
using Content.Shared.IdentityManagement;

namespace Content.Server.Morgue;

public sealed class CrematoriumSystem : EntitySystem
{
    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly EntityStorageSystem _entityStorage = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CrematoriumComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<CrematoriumComponent, StorageOpenAttemptEvent>(OnAttemptOpen);
        SubscribeLocalEvent<CrematoriumComponent, GetVerbsEvent<AlternativeVerb>>(AddCremateVerb);
        SubscribeLocalEvent<CrematoriumComponent, SuicideEvent>(OnSuicide);
    }

    private void OnExamine(EntityUid uid, CrematoriumComponent component, ExaminedEvent args)
    {
        if (!TryComp<AppearanceComponent>(uid, out var appearance))
            return;

        if (appearance.TryGetData(CrematoriumVisuals.Burning, out bool isBurning) && isBurning)
            args.PushMarkup(Loc.GetString("crematorium-entity-storage-component-on-examine-details-is-burning", ("owner", uid)));
        if (appearance.TryGetData(StorageVisuals.HasContents, out bool hasContents) && hasContents)
            args.PushMarkup(Loc.GetString("crematorium-entity-storage-component-on-examine-details-has-contents"));
        else
            args.PushMarkup(Loc.GetString("crematorium-entity-storage-component-on-examine-details-empty"));
    }

    private void OnAttemptOpen(EntityUid uid, CrematoriumComponent component, StorageOpenAttemptEvent args)
    {
        if (component.Cooking)
            args.Cancel();
    }

    private void AddCremateVerb(EntityUid uid, CrematoriumComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!TryComp<EntityStorageComponent>(uid, out var storage))
            return;

        if (!args.CanAccess || !args.CanInteract || args.Hands == null || component.Cooking || storage.Open)
            return;

        AlternativeVerb verb = new()
        {
            Text = Loc.GetString("cremate-verb-get-data-text"),
            // TODO VERB ICON add flame/burn symbol?
            Act = () => TryCremate(uid, component, storage),
            Impact = LogImpact.Medium // could be a body? or evidence? I dunno.
        };
        args.Verbs.Add(verb);
    }

    public void Cremate(EntityUid uid, CrematoriumComponent? component = null, EntityStorageComponent? storage = null)
    {
        if (!Resolve(uid, ref component, ref storage))
            return;

        if (TryComp<AppearanceComponent>(uid, out var app))
            app.SetData(CrematoriumVisuals.Burning, true);
        component.Cooking = true;

        SoundSystem.Play(component.CrematingSound.GetSound(), Filter.Pvs(uid), uid);

        component.CremateCancelToken?.Cancel();
        component.CremateCancelToken = new CancellationTokenSource();
        uid.SpawnTimer(component.BurnMilis, () =>
        {
            if (Deleted(uid))
                return;
            if (TryComp<AppearanceComponent>(uid, out var app))
                app.SetData(CrematoriumVisuals.Burning, false);
            component.Cooking = false;

            if (storage.Contents.ContainedEntities.Count > 0)
            {
                for (var i = storage.Contents.ContainedEntities.Count - 1; i >= 0; i--)
                {
                    var item = storage.Contents.ContainedEntities[i];
                    storage.Contents.Remove(item);
                    EntityManager.DeleteEntity(item);
                }

                var ash = Spawn("Ash", Transform(uid).Coordinates);
                storage.Contents.Insert(ash);
            }

            _entityStorage.OpenStorage(uid, storage);

            SoundSystem.Play(component.CremateFinishSound.GetSound(), Filter.Pvs(uid), uid);

        }, component.CremateCancelToken.Token);
    }

    public void TryCremate(EntityUid uid, CrematoriumComponent component, EntityStorageComponent? storage = null)
    {
        if (!Resolve(uid, ref storage))
            return;

        if (component.Cooking || storage.Open || storage.Contents.ContainedEntities.Count < 1)
            return;

        SoundSystem.Play(component.CremateStartSound.GetSound(), Filter.Pvs(uid), uid);

        Cremate(uid, component, storage);
    }

    private void OnSuicide(EntityUid uid, CrematoriumComponent component, SuicideEvent args)
    {
        if (args.Handled)
            return;
        args.SetHandled(SuicideKind.Heat);

        var victim = args.Victim;
        if (TryComp(victim, out ActorComponent? actor) && actor.PlayerSession.ContentData()?.Mind is { } mind)
        {
            _ticker.OnGhostAttempt(mind, false);

            if (mind.OwnedEntity is { Valid: true } entity)
            {
                _popup.PopupEntity(Loc.GetString("crematorium-entity-storage-component-suicide-message"), entity, Filter.Pvs(entity));
            }
        }

        _popup.PopupEntity(Loc.GetString("crematorium-entity-storage-component-suicide-message-others",
            ("victim", Identity.Entity(victim, EntityManager))),
            victim, Filter.PvsExcept(victim), PopupType.LargeCaution);

        if (_entityStorage.CanInsert(uid))
        {
            _entityStorage.CloseStorage(uid);
            _standing.Down(victim, false);
            _entityStorage.Insert(victim, uid);
        }
        else
        {
            EntityManager.DeleteEntity(victim);
        }
        _entityStorage.CloseStorage(uid);
        Cremate(uid, component);
    }
}
