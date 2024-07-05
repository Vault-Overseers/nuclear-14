using Robust.Shared.Prototypes;

namespace Content.Shared.Clothing.Loadouts.Prototypes;


/// <summary>
///     A prototype defining a valid category for <see cref="LoadoutPrototype"/>s to go into.
/// </summary>
[Prototype("loadoutCategory")]
public sealed class LoadoutCategoryPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;
}
