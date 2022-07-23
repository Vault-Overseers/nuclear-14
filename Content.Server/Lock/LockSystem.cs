using Content.Server.Storage.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Storage;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.Player;

namespace Content.Server.Lock
{
    /// <summary>
    /// Handles (un)locking and examining of Lock components
    /// </summary>
    [UsedImplicitly]
    public sealed class LockSystem : EntitySystem
    {
        [Dependency] private readonly AccessReaderSystem _accessReader = default!;
        [Dependency] private readonly SharedPopupSystem _sharedPopupSystem = default!;

        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<LockComponent, ComponentStartup>(OnStartup);
            SubscribeLocalEvent<LockComponent, ActivateInWorldEvent>(OnActivated);
            SubscribeLocalEvent<LockComponent, StorageOpenAttemptEvent>(OnStorageOpenAttempt);
            SubscribeLocalEvent<LockComponent, ExaminedEvent>(OnExamined);
            SubscribeLocalEvent<LockComponent, GetVerbsEvent<AlternativeVerb>>(AddToggleLockVerb);
            SubscribeLocalEvent<LockComponent, GotEmaggedEvent>(OnEmagged);
        }

        private void OnStartup(EntityUid uid, LockComponent lockComp, ComponentStartup args)
        {
            if (EntityManager.TryGetComponent(lockComp.Owner, out AppearanceComponent? appearance))
            {
                appearance.SetData(StorageVisuals.CanLock, true);
            }
        }

        private void OnActivated(EntityUid uid, LockComponent lockComp, ActivateInWorldEvent args)
        {
            if (args.Handled)
                return;

            // Only attempt an unlock by default on Activate
            if (lockComp.Locked)
            {
                TryUnlock(uid, args.User, lockComp);
                args.Handled = true;
            }
            else if (lockComp.LockOnClick)
            {
                TryLock(uid, args.User, lockComp);
                args.Handled = true;
            }
        }

        private void OnStorageOpenAttempt(EntityUid uid, LockComponent component, StorageOpenAttemptEvent args)
        {
            if (component.Locked)
            {
                if (!args.Silent)
                    _sharedPopupSystem.PopupEntity(Loc.GetString("entity-storage-component-locked-message"), uid, Filter.Pvs(uid));

                args.Cancel();
            }
        }

        private void OnExamined(EntityUid uid, LockComponent lockComp, ExaminedEvent args)
        {
            args.PushText(Loc.GetString(lockComp.Locked
                    ? "lock-comp-on-examined-is-locked"
                    : "lock-comp-on-examined-is-unlocked",
                ("entityName", EntityManager.GetComponent<MetaDataComponent>(lockComp.Owner).EntityName)));
        }

        public bool TryLock(EntityUid uid, EntityUid user, LockComponent? lockComp = null)
        {
            if (!Resolve(uid, ref lockComp))
                return false;

            if (!CanToggleLock(uid, user, quiet: false))
                return false;

            if (!HasUserAccess(uid, user, quiet: false))
                return false;

            lockComp.Owner.PopupMessage(user, Loc.GetString("lock-comp-do-lock-success", ("entityName", EntityManager.GetComponent<MetaDataComponent>(lockComp.Owner).EntityName)));
            lockComp.Locked = true;

            if(lockComp.LockSound != null)
            {
                SoundSystem.Play(lockComp.LockSound.GetSound(), Filter.Pvs(lockComp.Owner), lockComp.Owner, AudioParams.Default.WithVolume(-5));
            }

            if (EntityManager.TryGetComponent(lockComp.Owner, out AppearanceComponent? appearanceComp))
            {
                appearanceComp.SetData(StorageVisuals.Locked, true);
            }

            RaiseLocalEvent(lockComp.Owner, new LockToggledEvent(true), true);

            return true;
        }

        public void Unlock(EntityUid uid, EntityUid user, LockComponent? lockComp = null)
        {
            if (!Resolve(uid, ref lockComp))
                return;

            lockComp.Owner.PopupMessage(user, Loc.GetString("lock-comp-do-unlock-success", ("entityName", EntityManager.GetComponent<MetaDataComponent>(lockComp.Owner).EntityName)));
            lockComp.Locked = false;

            if (lockComp.UnlockSound != null)
            {
                SoundSystem.Play(lockComp.UnlockSound.GetSound(), Filter.Pvs(lockComp.Owner), lockComp.Owner, AudioParams.Default.WithVolume(-5));
            }

            if (EntityManager.TryGetComponent(lockComp.Owner, out AppearanceComponent? appearanceComp))
            {
                appearanceComp.SetData(StorageVisuals.Locked, false);
            }

            RaiseLocalEvent(lockComp.Owner, new LockToggledEvent(false), true);
        }

        public bool TryUnlock(EntityUid uid, EntityUid user, LockComponent? lockComp = null)
        {
            if (!Resolve(uid, ref lockComp))
                return false;

            if (!CanToggleLock(uid, user, quiet: false))
                return false;

            if (!HasUserAccess(uid, user, quiet: false))
                return false;

            Unlock(uid, user, lockComp);
            return true;
        }

        /// <summary>
        ///     Before locking the entity, check whether it's a locker. If is, prevent it from being locked from the inside or while it is open.
        /// </summary>
        public bool CanToggleLock(EntityUid uid, EntityUid user, EntityStorageComponent? storage = null, bool quiet = true)
        {
            if (!Resolve(uid, ref storage, logMissing: false))
                return true;

            // Cannot lock if the entity is currently opened.
            if (storage.Open)
                return false;

            // Cannot (un)lock from the inside. Maybe a bad idea? Security jocks could trap nerds in lockers?
            if (storage.Contents.Contains(user))
                return false;

            return true;
        }

        private bool HasUserAccess(EntityUid uid, EntityUid user, AccessReaderComponent? reader = null, bool quiet = true)
        {
            // Not having an AccessComponent means you get free access. woo!
            if (!Resolve(uid, ref reader))
                return true;

            if (!_accessReader.IsAllowed(user, reader))
            {
                if (!quiet)
                    reader.Owner.PopupMessage(user, Loc.GetString("lock-comp-has-user-access-fail"));
                return false;
            }

            return true;
        }

        private void AddToggleLockVerb(EntityUid uid, LockComponent component, GetVerbsEvent<AlternativeVerb> args)
        {
            if (!args.CanAccess || !args.CanInteract || !CanToggleLock(uid, args.User))
                return;

            AlternativeVerb verb = new();
            verb.Act = component.Locked ?
                () => TryUnlock(uid, args.User, component) :
                () => TryLock(uid, args.User, component);
            verb.Text = Loc.GetString(component.Locked ? "toggle-lock-verb-unlock" : "toggle-lock-verb-lock");
            // TODO VERB ICONS need padlock open/close icons.
            args.Verbs.Add(verb);
        }

        private void OnEmagged(EntityUid uid, LockComponent component, GotEmaggedEvent args)
        {
            if (component.Locked == true)
            {
                if (component.UnlockSound != null)
                {
                    SoundSystem.Play(component.UnlockSound.GetSound(), Filter.Pvs(component.Owner), component.Owner, AudioParams.Default.WithVolume(-5));
                }

                if (EntityManager.TryGetComponent(component.Owner, out AppearanceComponent? appearanceComp))
                {
                    appearanceComp.SetData(StorageVisuals.Locked, false);
                }
                EntityManager.RemoveComponent<LockComponent>(uid); //Literally destroys the lock as a tell it was emagged
                args.Handled = true;
            }
        }
    }
}
