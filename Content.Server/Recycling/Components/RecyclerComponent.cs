using Content.Shared.Recycling;
using Robust.Shared.Audio;

namespace Content.Server.Recycling.Components
{
    // TODO: Add sound and safe beep
    [RegisterComponent]
    [Access(typeof(RecyclerSystem))]
    public sealed class RecyclerComponent : Component
    {
        [Dependency] private readonly IEntityManager _entMan = default!;

        [ViewVariables]
        [DataField("enabled")]
        public bool Enabled = true;

        /// <summary>
        ///     Whether or not sentient beings will be recycled
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("safe")]
        internal bool Safe = true;

        /// <summary>
        ///     The percentage of material that will be recovered
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("efficiency")]
        internal float Efficiency = 0.25f;

        private void Clean()
        {
            if (_entMan.TryGetComponent(Owner, out AppearanceComponent? appearance))
            {
                appearance.SetData(RecyclerVisuals.Bloody, false);
            }
        }

        /// <summary>
        /// Default sound to play when recycling
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)] [DataField("sound")]
        public SoundSpecifier? Sound = new SoundPathSpecifier("/Audio/Effects/saw.ogg");

        // Ratelimit sounds to avoid spam
        public TimeSpan LastSound;
    }
}
