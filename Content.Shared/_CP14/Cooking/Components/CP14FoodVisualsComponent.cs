/*
 * This file is sublicensed under MIT License
 * https://github.com/space-wizards/space-station-14/blob/master/LICENSE.TXT
 */

using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared._CP14.Cooking.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true, true), Access(typeof(CP14SharedCookingSystem))]
public sealed partial class CP14FoodVisualsComponent : Component
{
    /// <summary>
    /// What food is currently stored here?
    /// </summary>
    [DataField, AutoNetworkedField]
    public CP14FoodData? FoodData;

    [DataField]
    public int MaxDisplacementFillLevels = 8;

    [DataField]
    public string? DisplacementRsiPath = null;

    [DataField]
    public string? SolutionId;

    /// <summary>
    /// target layer, where new layers will be added. This allows you to control the order of generative layers and static layers.
    /// </summary>
    [DataField]
    public string TargetLayerMap = "cp14_foodLayers";

    public HashSet<string> RevealedLayers = new();
}
