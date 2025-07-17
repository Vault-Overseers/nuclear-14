namespace Content.Server._CP14.Cooking;

/// <summary>
/// Attempting to add a random dish to CP14FoodHolderComponent
/// </summary>
[RegisterComponent, Access(typeof(CP14CookingSystem))]
public sealed partial class CP14RandomFoodDataComponent : Component
{
    /// <summary>
    /// Chance of food appearing
    /// </summary>
    [DataField]
    public float Prob = 0.75f;
}
