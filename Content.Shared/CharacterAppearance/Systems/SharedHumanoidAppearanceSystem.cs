using Content.Shared.Body.Part;
using Content.Shared.CharacterAppearance.Components;
using Content.Shared.Preferences;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects.Components.Localization;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.CharacterAppearance.Systems
{
    public abstract class SharedHumanoidAppearanceSystem : EntitySystem
    {
        public override void Initialize()
        {
            SubscribeLocalEvent<HumanoidAppearanceComponent, ComponentGetState>(OnAppearanceGetState);
            SubscribeLocalEvent<HumanoidAppearanceComponent, ComponentHandleState>(OnAppearanceHandleState);
        }

        public void UpdateFromProfile(EntityUid uid, ICharacterProfile profile, HumanoidAppearanceComponent? appearance=null)
        {
            var humanoid = (HumanoidCharacterProfile) profile;
            UpdateAppearance(uid, humanoid.Appearance, humanoid.Sex, humanoid.Gender, humanoid.Species, humanoid.Age, appearance);
        }

        // The magic mirror otherwise wouldn't work. (it directly modifies the component server-side)
        public void ForceAppearanceUpdate(EntityUid uid, HumanoidAppearanceComponent? component = null)
        {
            if (!Resolve(uid, ref component)) return;
            component.Dirty();
        }

        private void UpdateAppearance(EntityUid uid, HumanoidCharacterAppearance appearance, Sex sex, Gender gender, string species, int age, HumanoidAppearanceComponent? component = null)
        {
            if (!Resolve(uid, ref component)) return;

            component.Appearance = appearance;
            component.Sex = sex;
            component.Gender = gender;
            component.Species = species;
            component.Age = age;

            if (EntityManager.TryGetComponent(uid, out GrammarComponent? g))
                g.Gender = gender;

            component.Dirty();

            RaiseLocalEvent(uid, new ChangedHumanoidAppearanceEvent(appearance, sex, gender, species), true);
        }

        public void UpdateAppearance(EntityUid uid, HumanoidCharacterAppearance appearance, HumanoidAppearanceComponent? component = null)
        {
            if (!Resolve(uid, ref component)) return;

            component.Appearance = appearance;

            component.Dirty();

            RaiseLocalEvent(uid, new ChangedHumanoidAppearanceEvent(appearance, component.Sex, component.Gender, component.Species), true);
        }

        private void OnAppearanceGetState(EntityUid uid, HumanoidAppearanceComponent component, ref ComponentGetState args)
        {
            args.State = new HumanoidAppearanceComponentState(component.Appearance, component.Sex, component.Gender, component.Species, component.Age);
        }

        private void OnAppearanceHandleState(EntityUid uid, HumanoidAppearanceComponent component, ref ComponentHandleState args)
        {
            if (args.Current is not HumanoidAppearanceComponentState state)
                return;

            UpdateAppearance(uid, state.Appearance, state.Sex, state.Gender, state.Species, state.Age);
        }

        // Scaffolding until Body is moved to ECS.
        public void BodyPartAdded(EntityUid uid, BodyPartAddedEventArgs args)
        {
            RaiseLocalEvent(new HumanoidAppearanceBodyPartAddedEvent(uid, args));
        }

        public void BodyPartRemoved(EntityUid uid, BodyPartRemovedEventArgs args)
        {
            RaiseLocalEvent(new HumanoidAppearanceBodyPartRemovedEvent(uid, args));
        }

        // Scaffolding until Body is moved to ECS.
        [Serializable, NetSerializable]
        public sealed class HumanoidAppearanceBodyPartAddedEvent : EntityEventArgs
        {
            public EntityUid Uid { get; }
            public BodyPartAddedEventArgs Args { get; }

            public HumanoidAppearanceBodyPartAddedEvent(EntityUid uid, BodyPartAddedEventArgs args)
            {
                Uid = uid;
                Args = args;
            }
        }

        [Serializable, NetSerializable]
        public sealed class HumanoidAppearanceBodyPartRemovedEvent : EntityEventArgs
        {
            public EntityUid Uid { get; }
            public BodyPartRemovedEventArgs Args { get; }

            public HumanoidAppearanceBodyPartRemovedEvent(EntityUid uid, BodyPartRemovedEventArgs args)
            {
                Uid = uid;
                Args = args;
            }
        }

        [Serializable, NetSerializable]
        public sealed class ChangedHumanoidAppearanceEvent : EntityEventArgs
        {
            public HumanoidCharacterAppearance Appearance { get; }
            public Sex Sex { get; }
            public Gender Gender { get; }
            public string Species { get; }

            public ChangedHumanoidAppearanceEvent(HumanoidCharacterProfile profile)
            {
                Appearance = profile.Appearance;
                Sex = profile.Sex;
                Gender = profile.Gender;
                Species = profile.Species;
            }

            public ChangedHumanoidAppearanceEvent(HumanoidCharacterAppearance appearance, Sex sex, Gender gender, string species)
            {
                Appearance = appearance;
                Sex = sex;
                Gender = gender;
                Species = species;
            }
        }
    }
}
