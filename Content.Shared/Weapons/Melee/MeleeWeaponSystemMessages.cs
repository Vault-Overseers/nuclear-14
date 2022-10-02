using Robust.Shared.Serialization;

namespace Content.Shared.Weapons.Melee
{
    public static class MeleeWeaponSystemMessages
    {
        [Serializable, NetSerializable]
        public sealed class PlayMeleeWeaponAnimationMessage : EntityEventArgs
        {
            public PlayMeleeWeaponAnimationMessage(string arcPrototype, Angle angle, EntityUid attacker, EntityUid source, List<EntityUid> hits, bool textureEffect = false, bool arcFollowAttacker = true)
            {
                ArcPrototype = arcPrototype;
                Angle = angle;
                Attacker = attacker;
                Source = source;
                Hits = hits;
                TextureEffect = textureEffect;
                ArcFollowAttacker = arcFollowAttacker;
            }

            public string ArcPrototype { get; }
            public Angle Angle { get; }
            public EntityUid Attacker { get; }
            public EntityUid Source { get; }
            public List<EntityUid> Hits { get; }
            public bool TextureEffect { get; }
            public bool ArcFollowAttacker { get; }
        }

        [Serializable, NetSerializable]
        public sealed class PlayLungeAnimationMessage : EntityEventArgs
        {
            public Angle Angle { get; }
            public EntityUid Source { get; }

            public PlayLungeAnimationMessage(Angle angle, EntityUid source)
            {
                Angle = angle;
                Source = source;
            }
        }
    }
}
