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

[Prototype("lightCraftingRecipe"), Serializable, NetSerializable]
public sealed class LightCraftingPrototype : IPrototype
{
    [ViewVariables, IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public StepDetails Steps = new();

    [DataField]
    public List<string> Results = new();
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class StepDetails
{
    /// <summary>
    /// One of the required items for the recipe
    /// </summary>
    [DataField]
    public EntProtoId FirstIngredient = default;

    /// <summary>
    /// Other required item for the recipe
    /// </summary>
    [DataField]
    public EntProtoId SecondIngredient = default;

    /// <summary>
    /// If false, the first item will be deleted after the craft
    /// </summary>
    [DataField]
    public bool KeepFirst = false;

    /// <summary>
    /// If false, the second item will be deleted after the craft
    /// </summary>
    [DataField]
    public bool KeepSecond = false;

    /// <summary>
    /// If false, the whole parent-prototype hierarchy will be checked
    /// </summary>
    [DataField]
    public bool ExactFirst = true;

    /// <summary>
    /// If false, the whole parent-prototype hierarchy will be checked
    /// </summary>
    [DataField]
    public bool ExactSecond = true;

    /// <summary>
    /// Time in seconds for the craft
    /// </summary>
    [DataField]
    public float Time { get; set; } = 2f;
}
