/*
Copyright (C) 2025 Stalker14
license:
  This source code is the exclusive of TornadoTech (Maltsev Daniil), JerryImMouse, LordVladimer (Valdis Fedorov)
  and is protected by copyright law.
  Any unauthorized use or reproduction of this source code
  is strictly prohibited and may result in legal action.
  For inquiries or licensing requests,
  please contact TornadoTech (Maltsev Daniil), JerryImMouse, LordVladimer (Valdis Fedorov)
  at Discord (https://discord.com/invite/pu6DEPGjsN).
*/
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Crafting.Prototypes;
/// <summary>
/// Prototype to handle by <see cref="SharedCraftingSystem"/>
/// </summary>
[Prototype("craftRecipe"), Serializable, NetSerializable]
public sealed class CraftingPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;
    /// <summary>
    /// Crafting result prototype ID
    /// </summary>
    [DataField("resultProtos")]
    public List<string> ResultProtos = new();

    /// <summary>
    /// DoAfter time for crafting
    /// </summary>
    [DataField("craftTime")]
    public float CraftTime = 2f;

    /// <summary>
    /// Chance for disassemble
    /// </summary>
    [DataField]
    public float DisassembleChance = 1f;
    /// <summary>
    /// Items required for crafting.
    /// This supports multiple items with details at once.
    /// </summary>
    [DataField("items")]
    public Dictionary<string, CraftingRecipeDetails> Items = new();

    /// <summary>
    /// Optional field to specify the workbench EntProtoId required for this recipe.
    /// If not provided, the recipe can be crafted on any workbench.
    /// </summary>
    [DataField("requiredWorkbench")]
    public string? RequiredWorkbench;

    // /// <summary>
    // /// Minimum intelligence required to craft this recipe.
    // /// </summary>
    // [DataField("requiredIntelligence")] // Corvax-Change
    // public int RequiredIntelligence = 0;

    /// <summary>
    /// A list of job IDs that are permitted to use this crafting recipe.
    /// If this list is not empty, only characters with one of the specified jobs can craft the item.
    /// </summary>
    [DataField("availableJobs")] // Corvax-Change
    public List<string> AvailableJobs = new();

    /// <summary>
    /// Optional field for specifying the department id.
    /// Makes the recipe available only for a specific faction.
    /// </summary>
    [DataField("availableFaction")] // Corvax-Change
    public List<string> AvailableFaction = new();
}
/// <summary>
/// Details for crafting recipe item
/// </summary>
[DataDefinition, Serializable, NetSerializable]
public sealed partial class CraftingRecipeDetails
{
    /// <summary>
    /// Amount of items of the type to craft
    /// </summary>
    [DataField("amount")]
    public int Amount;
    /// <summary>
    /// If this item is a catalyzer, it wont be consumed by crafting.
    /// </summary>
    [DataField("catalyzer")]
    public bool Catalyzer;

    [DataField("tag")]
    public bool Tag;
}
