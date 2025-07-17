/*
 * This file is sublicensed under MIT License
 * https://github.com/space-wizards/space-station-14/blob/master/LICENSE.TXT
 */

using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared._CP14.Cooking.Components;

[RegisterComponent, NetworkedComponent, Access(typeof(CP14SharedCookingSystem))]
public sealed partial class CP14FoodHolderComponent : Component
{
    [DataField]
    public bool HoldFood = false;

    [DataField(required: true)]
    public CP14FoodType FoodType;

    [DataField]
    public string? SolutionId;
}
