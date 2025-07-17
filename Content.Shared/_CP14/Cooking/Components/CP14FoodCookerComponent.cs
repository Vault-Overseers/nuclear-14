/*
 * This file is sublicensed under MIT License
 * https://github.com/space-wizards/space-station-14/blob/master/LICENSE.TXT
 */

using Content.Shared.DoAfter;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._CP14.Cooking.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true), Access(typeof(CP14SharedCookingSystem))]
public sealed partial class CP14FoodCookerComponent : Component
{
    [DataField]
    public bool HoldFood = false;

    [DataField(required: true)]
    public CP14FoodType FoodType;

    [DataField]
    public string ContainerId;

    [DataField]
    public string? SolutionId;

    public DoAfterId? DoAfterId = null;

    /// <summary>
    /// The moment in time when this entity was last heated. Used for calculations when DoAfter cooking should be reset.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan LastHeatingTime = TimeSpan.Zero;

    /// <summary>
    /// How often do you need to heat this entity so that doAfter does not reset?
    /// </summary>
    /// <remarks>
    /// Ideally, this system should check that the Cooker temperature is within certain limits,
    /// but at the moment the entire temperature system is a terrible mess that we don't want to get deeply involved with,
    /// so we simplify the simulation by simply requiring the heating action to be performed.
    /// We don't care how much it heats up, we care how long it takes.
    /// </remarks>
    [DataField]
    public TimeSpan HeatingFrequencyRequired = TimeSpan.FromSeconds(2f);

    [DataField]
    public EntProtoId? BurntAdditionalSpawn = "CP14Fire";

    [DataField]
    public float BurntAdditionalSpawnProb = 0.2f;
}

[Serializable]
[DataDefinition]
public sealed partial class CP14FoodData
{
    public CP14FoodData(CP14FoodData data)
    {
        CurrentRecipe = data.CurrentRecipe;
        Name = data.Name;
        Desc = data.Desc;
        Visuals = new List<PrototypeLayerData>(data.Visuals);
        Trash = new List<EntProtoId>(data.Trash);
        Flavors = new HashSet<LocId>(data.Flavors);
    }

    [DataField]
    public ProtoId<CP14CookingRecipePrototype>? CurrentRecipe;

    [DataField]
    public LocId? Name;

    [DataField]
    public LocId? Desc;

    [DataField]
    public List<PrototypeLayerData> Visuals = new();

    [DataField]
    public List<EntProtoId> Trash = new();

    [DataField]
    public HashSet<LocId> Flavors = new();
}

public enum CP14FoodType
{
    Meal,
    Soup,
}

[Serializable, NetSerializable]
public enum CP14CookingVisuals : byte
{
    Cooking,
    Burning,
}
