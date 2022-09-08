using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Kitchen.Components
{
    [RegisterComponent]
    public sealed class MicrowaveComponent : Component
    {
        [DataField("cookTimeMultiplier")]
        public int CookTimeMultiplier = 1; //For upgrades and stuff I guess? don't ask me.
        [DataField("failureResult", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string BadRecipeEntityId = "FoodBadRecipe";

        #region  audio
        [DataField("beginCookingSound")]
        public SoundSpecifier StartCookingSound = new SoundPathSpecifier("/Audio/Machines/microwave_start_beep.ogg");
        [DataField("foodDoneSound")]
        public SoundSpecifier FoodDoneSound = new SoundPathSpecifier("/Audio/Machines/microwave_done_beep.ogg");
        [DataField("clickSound")]
        public SoundSpecifier ClickSound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");
        [DataField("ItemBreakSound")]
        public SoundSpecifier ItemBreakSound = new SoundPathSpecifier("/Audio/Effects/clang.ogg");

        public IPlayingAudioStream? PlayingStream { get; set; }
        [DataField("loopingSound")]
        public SoundSpecifier LoopingSound = new SoundPathSpecifier("/Audio/Machines/microwave_loop.ogg");
        #endregion

        [ViewVariables]
        public bool Broken;

        /// <summary>
        /// This is a fixed offset of 5.
        /// The cook times for all recipes should be divisible by 5,with a minimum of 1 second.
        /// For right now, I don't think any recipe cook time should be greater than 60 seconds.
        /// </summary>
        [DataField("currentCookTimerTime"), ViewVariables(VVAccess.ReadWrite)]
        public uint CurrentCookTimerTime = 5;

        /// <summary>
        ///     The max temperature that this microwave can heat objects to.
        /// </summary>
        [DataField("temperatureUpperThreshold")]
        public float TemperatureUpperThreshold = 373.15f;

        public int CurrentCookTimeButtonIndex;

        public Container Storage = default!;
    }

    public sealed class BeingMicrowavedEvent : HandledEntityEventArgs
    {
        public EntityUid Microwave;

        public BeingMicrowavedEvent(EntityUid microwave)
        {
            Microwave = microwave;
        }
    }
}
