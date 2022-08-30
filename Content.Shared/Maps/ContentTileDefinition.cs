using Content.Shared.Atmos;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Maps
{
    [UsedImplicitly]
    [Prototype("tile")]
    public sealed class ContentTileDefinition : IPrototype, IInheritingPrototype, ITileDefinition
    {
        [ParentDataFieldAttribute(typeof(AbstractPrototypeIdSerializer<ContentTileDefinition>))]
        public string? Parent { get; private set; }

        [NeverPushInheritance]
        [AbstractDataFieldAttribute]
        public bool Abstract { get; private set; }

        public string Path => "/Textures/Tiles/";

        [IdDataFieldAttribute] public string ID { get; } = string.Empty;

        public ushort TileId { get; private set; }

        [DataField("name")] public string Name { get; } = string.Empty;

        [DataField("texture")] public string SpriteName { get; } = string.Empty;

        [DataField("is_subfloor")] public bool IsSubFloor { get; private set; }

        [DataField("base_turfs")] public List<string> BaseTurfs { get; } = new();

        [DataField("can_crowbar")] public bool CanCrowbar { get; private set; }

        [DataField("footstep_sounds")] public SoundSpecifier? FootstepSounds { get; }

        [DataField("friction")] public float Friction { get; set; }

        [DataField("variants")] public byte Variants { get; set; } = 1;

        /// <summary>
        /// This controls what variants the `variantize` command is allowed to use.
        /// </summary>
        [DataField("placementVariants")] public byte[] PlacementVariants { get; set; } = new byte[1] { 0 };

        [DataField("thermalConductivity")] public float ThermalConductivity { get; set; } = 0.05f;

        // Heat capacity is opt-in, not opt-out.
        [DataField("heatCapacity")] public float HeatCapacity = Atmospherics.MinimumHeatCapacity;

        [DataField("item_drop", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string ItemDropPrototypeName { get; } = "FloorTileItemSteel";

        [DataField("is_space")] public bool IsSpace { get; private set; }
        [DataField("sturdy")] public bool Sturdy { get; private set; } = true;

        public void AssignTileId(ushort id)
        {
            TileId = id;
        }
    }
}
