using Robust.Shared.GameStates;

namespace Content.Server._N14.Nutrition;

/// <summary>
/// Marks dung piles that splat like a pie and may cause vomiting on hit.
/// </summary>
[RegisterComponent, Access(typeof(DungPileSystem))]
public sealed partial class DungPileComponent : Component
{
    /// <summary>
    /// Chance to make the victim vomit when hit if unprotected.
    /// </summary>
    [DataField("vomitChance")]
    public float VomitChance = 0.8f;
}
