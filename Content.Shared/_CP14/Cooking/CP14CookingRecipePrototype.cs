/*
 * This file is sublicensed under MIT License
 * https://github.com/space-wizards/space-station-14/blob/master/LICENSE.TXT
 */

using Content.Shared._CP14.Cooking.Components;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._CP14.Cooking;

[Prototype("CP14CookingRecipe")]
public sealed class CP14CookingRecipePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// List of conditions that must be met in the set of ingredients for a dish
    /// </summary>
    [DataField]
    public List<CP14CookingCraftRequirement> Requirements = new();

    /// <summary>
    /// Reagents cannot store all the necessary information about food, so along with the reagents for all the ingredients,
    /// in this block we add the appearance of the dish, descriptions, and so on.
    /// </summary>
    [DataField]
    public CP14FoodData FoodData = new();

    [DataField]
    public CP14FoodType FoodType = CP14FoodType.Meal;

    [DataField]
    public TimeSpan CookingTime = TimeSpan.FromSeconds(20f);

    [DataField]
    public SoundSpecifier CookingAmbient = new SoundPathSpecifier("/Audio/_CP14/Ambience/pan_frying.ogg");
}
