using Content.Shared.Tools;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Content.Shared.Storage;

namespace Content.Server.Construction.Components
{
    /// <summary>
    /// Used for something that can be refined by welder.
    /// For example, glass shard can be refined to glass sheet.
    /// </summary>
    [RegisterComponent]
    public sealed partial class WelderRefinableComponent : Component
    {
        [DataField("refineResult")]
        public List<EntitySpawnEntry>? RefineResult = new();

        [DataField("refineTime")]
        public float RefineTime = 2f;

        [DataField("qualityNeeded", customTypeSerializer: typeof(PrototypeIdSerializer<ToolQualityPrototype>))]
        public string QualityNeeded = "Welding";
    }
}
