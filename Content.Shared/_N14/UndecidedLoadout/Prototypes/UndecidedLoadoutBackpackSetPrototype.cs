using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.UndecidedLoadout;

/// <summary>
/// A prototype that defines a set of items and visuals for starter kits.
/// </summary>
[Prototype("UndecidedLoadoutBackpackSet")]
public sealed partial class UndecidedLoadoutBackpackSetPrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;
    [DataField] public string Name { get; private set; } = string.Empty;
    [DataField] public string Description { get; private set; } = string.Empty;
    [DataField] public SpriteSpecifier Sprite { get; private set; } = SpriteSpecifier.Invalid;

    [DataField] public List<EntProtoId> Content = new();
}
