using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Weapons.Melee
{
    [Prototype("MeleeWeaponAnimation")]
    public sealed class MeleeWeaponAnimationPrototype : IPrototype
    {
        [ViewVariables]
        [IdDataFieldAttribute]
        public string ID { get; } = default!;

        [ViewVariables]
        [DataField("state")]
        public string State { get; } = string.Empty;

        [ViewVariables]
        [DataField("prototype", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string Prototype { get; } = "WeaponArc";

        [ViewVariables]
        [DataField("length")]
        public TimeSpan Length { get; } = TimeSpan.FromSeconds(0.5f);

        [ViewVariables]
        [DataField("speed")]
        public float Speed { get; } = 1;

        [ViewVariables]
        [DataField("color")]
        public Vector4 Color { get; } = new(1,1,1,1);

        [ViewVariables]
        [DataField("colorDelta")]
        public Vector4 ColorDelta { get; } = Vector4.Zero;

        [ViewVariables]
        [DataField("arcType")]
        public WeaponArcType ArcType { get; } = WeaponArcType.Slash;

        [ViewVariables]
        [DataField("width")]
        public float Width { get; } = 90;
    }

    public enum WeaponArcType
    {
        Slash,
        Poke,
    }
}
