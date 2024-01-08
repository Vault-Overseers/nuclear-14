using Robust.Shared.Audio;

namespace Content.Server.Explosion.Components
{
    /// <summary>
    /// Triggers on click.
    /// </summary>
    [RegisterComponent]
    public sealed class TriggerOnActivateComponent : Component
    {
        [DataField("delay")]
        public float Delay = 1f;

        /// <summary>
        ///     If not null, a user can use verbs to configure the delay to one of these options.
        /// </summary>
        [DataField("delayOptions")]
        public List<float>? DelayOptions = null;

        /// <summary>
        ///     If not null, this timer will periodically play this sound while active.
        /// </summary>
        [DataField("beepSound")]
        public SoundSpecifier? BeepSound;

        /// <summary>
        ///     Time before beeping starts. Defaults to a single beep interval. If set to zero, will emit a beep immediately after use.
        /// </summary>
        [DataField("initialBeepDelay")]
        public float? InitialBeepDelay;

        [DataField("beepInterval")]
        public float BeepInterval = 1;

        /// <summary>
        ///     Whether you can examine the item to see its timer or not.
        /// </summary>
        [DataField("examinable")]
        public bool Examinable = true;
    }
}
