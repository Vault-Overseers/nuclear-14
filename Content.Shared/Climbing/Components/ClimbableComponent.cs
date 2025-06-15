using Content.Shared.Interaction;
using Content.Shared.Climbing;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Climbing.Components
{
    /// <summary>
    /// Indicates this entity can be vaulted on top of.
    /// </summary>
    [RegisterComponent, NetworkedComponent]
    public sealed partial class ClimbableComponent : Component
    {
        /// <summary>
        ///     The range from which this entity can be climbed.
        /// </summary>
        [DataField("range")] public float Range = SharedInteractionSystem.InteractionRange / 1.4f;

        /// <summary>
        ///     The time it takes to climb onto the entity.
        /// </summary>
        [DataField("delay")]
        public float ClimbDelay = 1.5f;

        /// <summary>
        ///     Sound to be played when a climb is started.
        /// </summary>
        [DataField("startClimbSound")]
        public SoundSpecifier? StartClimbSound = null;

        /// <summary>
        ///     Sound to be played when a climb finishes.
        /// </summary>
        [DataField("finishClimbSound")]
        public SoundSpecifier? FinishClimbSound = null;

        /// <summary>
        /// Optional direction used for Z level transitions when this climbable acts as a ladder or stairs.
        /// </summary>
        [DataField]
        public ClimbDirection? DescendDirection;

        /// <summary>
        /// If true, tile checks are skipped when determining if the user can descend.
        /// </summary>
        [DataField]
        public bool IgnoreTiles;

        /// <summary>
        /// If true, skill checks are ignored when climbing via this entity.
        /// Only used by KMZ Z-level features.
        /// </summary>
        [DataField]
        public bool IgnoreSkillCheck;
    }
}
