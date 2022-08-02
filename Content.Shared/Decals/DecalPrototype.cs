using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Decals
{
    [Prototype("decal")]
    public sealed class DecalPrototype : IPrototype
    {
        [IdDataFieldAttribute] public string ID { get; } = null!;
        [DataField("sprite")] public SpriteSpecifier Sprite { get; } = SpriteSpecifier.Invalid;
        [DataField("tags")] public List<string> Tags = new();
        [DataField("showMenu")] public bool ShowMenu = true;
    }
}
