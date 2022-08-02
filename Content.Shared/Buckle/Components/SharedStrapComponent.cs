using Content.Shared.DragDrop;
using Content.Shared.Interaction;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Buckle.Components
{
    public enum StrapPosition
    {
        /// <summary>
        /// (Default) Makes no change to the buckled mob
        /// </summary>
        None = 0,

        /// <summary>
        /// Makes the mob stand up
        /// </summary>
        Stand,

        /// <summary>
        /// Makes the mob lie down
        /// </summary>
        Down
    }

    [NetworkedComponent()]
    public abstract class SharedStrapComponent : Component, IDragDropOn
    {
        /// <summary>
        /// The change in position to the strapped mob
        /// </summary>
        [DataField("position")]
        public StrapPosition Position { get; set; } = StrapPosition.None;


        /// <summary>
        /// The entity that is currently buckled here, synced from <see cref="BuckleComponent.BuckledTo"/>
        /// </summary>
        public readonly HashSet<EntityUid> BuckledEntities = new();

        /// <summary>
        /// The distance above which a buckled entity will be automatically unbuckled.
        /// Don't change it unless you really have to
        /// </summary>
        [DataField("maxBuckleDistance", required: false)]
        public float MaxBuckleDistance = 0.1f;

        /// <summary>
        /// Gets and clamps the buckle offset to MaxBuckleDistance
        /// </summary>
        public Vector2 BuckleOffset => Vector2.Clamp(
            BuckleOffsetUnclamped,
            Vector2.One * -MaxBuckleDistance,
            Vector2.One * MaxBuckleDistance);

        /// <summary>
        /// The buckled entity will be offset by this amount from the center of the strap object.
        /// If this offset it too big, it will be clamped to <see cref="MaxBuckleDistance"/>
        /// </summary>
        [DataField("buckleOffset", required: false)]
        public Vector2 BuckleOffsetUnclamped = Vector2.Zero;

        bool IDragDropOn.CanDragDropOn(DragDropEvent eventArgs)
        {
            if (!IoCManager.Resolve<IEntityManager>().TryGetComponent(eventArgs.Dragged, out SharedBuckleComponent? buckleComponent)) return false;
            bool Ignored(EntityUid entity) => entity == eventArgs.User || entity == eventArgs.Dragged || entity == eventArgs.Target;

            return EntitySystem.Get<SharedInteractionSystem>().InRangeUnobstructed(eventArgs.Target, eventArgs.Dragged, buckleComponent.Range, predicate: Ignored);
        }

        public abstract bool DragDropOn(DragDropEvent eventArgs);
    }

    [Serializable, NetSerializable]
    public sealed class StrapComponentState : ComponentState
    {
        /// <summary>
        /// The change in position that this strap makes to the strapped mob
        /// </summary>
        public StrapPosition Position;

        public float MaxBuckleDistance;
        public Vector2 BuckleOffsetClamped;
        public HashSet<EntityUid> BuckledEntities;

        public StrapComponentState(StrapPosition position, Vector2 offset, HashSet<EntityUid> buckled, float maxBuckleDistance)
        {
            Position = position;
            BuckleOffsetClamped = offset;
            BuckledEntities = buckled;
            MaxBuckleDistance = maxBuckleDistance;
        }
    }

    [Serializable, NetSerializable]
    public enum StrapVisuals : byte
    {
        RotationAngle,
        BuckledState
    }
}
