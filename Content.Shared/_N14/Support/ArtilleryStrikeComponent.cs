using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Map;
using System;

namespace Content.Shared._N14.Support
{

    /// <summary>
    /// Marks an entity that will trigger an artillery strike after a delay.
    /// </summary>
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState,
     Access(typeof(SharedArtilleryStrikeSystem))]
    [AutoGenerateComponentPause]
    public sealed partial class ArtilleryStrikeComponent : Component
    {
        /// <summary>
        /// Target location for the strike. Filled in once the flare lands.
        /// Not serialized so clients don't fail to spawn the prototype.
        /// </summary>
        public MapCoordinates Target = MapCoordinates.Nullspace;

        [DataField, AutoNetworkedField]
        public TimeSpan Delay = TimeSpan.FromSeconds(10);

        [DataField, AutoNetworkedField]
        public string ExplosionType = "Default";

        [DataField, AutoNetworkedField]
        public float Intensity = 50f;

        [DataField, AutoNetworkedField]
        public float Slope = 3f;

        [DataField, AutoNetworkedField]
        public float MaxIntensity = 10f;

        [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
        public TimeSpan StartTime;
    }
}
