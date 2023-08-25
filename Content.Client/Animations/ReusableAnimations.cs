using System.Numerics;
using Content.Shared.Spawners.Components;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;
using Robust.Shared.Map;

namespace Content.Client.Animations
{
    public static class ReusableAnimations
    {
        public static void AnimateEntityPickup(EntityUid entity, EntityCoordinates initialPosition, Vector2 finalPosition, Angle initialAngle, IEntityManager? entMan = null)
        {
            IoCManager.Resolve(ref entMan);

            if (entMan.Deleted(entity) || !initialPosition.IsValid(entMan))
                return;

            var animatableClone = entMan.SpawnEntity("clientsideclone", initialPosition);
            string val = entMan.GetComponent<MetaDataComponent>(entity).EntityName;
            entMan.GetComponent<MetaDataComponent>(animatableClone).EntityName = val;

            if (!entMan.TryGetComponent(entity, out SpriteComponent? sprite0))
            {
                Logger.Error("Entity ({0}) couldn't be animated for pickup since it doesn't have a {1}!", entMan.GetComponent<MetaDataComponent>(entity).EntityName, nameof(SpriteComponent));
                return;
            }
            var sprite = entMan.GetComponent<SpriteComponent>(animatableClone);
            sprite.CopyFrom(sprite0);
            sprite.Visible = true;

            var animations = entMan.GetComponent<AnimationPlayerComponent>(animatableClone);
            animations.AnimationCompleted += (_) => {
                entMan.DeleteEntity(animatableClone);
            };

            var despawn = entMan.EnsureComponent<TimedDespawnComponent>(animatableClone);
            despawn.Lifetime = 0.25f;
            entMan.System<SharedTransformSystem>().SetLocalRotationNoLerp(animatableClone, initialAngle);

            animations.Play(new Animation
            {
                Length = TimeSpan.FromMilliseconds(125),
                AnimationTracks =
                {
                    new AnimationTrackComponentProperty
                    {
                        ComponentType = typeof(TransformComponent),
                        Property = nameof(TransformComponent.LocalPosition),
                        InterpolationMode = AnimationInterpolationMode.Linear,
                        KeyFrames =
                        {
                            new AnimationTrackProperty.KeyFrame(initialPosition.Position, 0),
                            new AnimationTrackProperty.KeyFrame(finalPosition, 0.125f)
                        }
                    },
                }
            }, "fancy_pickup_anim");
        }
    }
}
