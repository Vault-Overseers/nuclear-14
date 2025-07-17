/*
 * This file is sublicensed under MIT License
 * https://github.com/space-wizards/space-station-14/blob/master/LICENSE.TXT
 */

using Content.Shared._CP14.Workbench.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._CP14.Workbench;

[Serializable, NetSerializable]
public enum CP14WorkbenchUiKey
{
    Key,
}

[Serializable, NetSerializable]
public sealed class CP14WorkbenchUiCraftMessage(ProtoId<CP14WorkbenchRecipePrototype> recipe)
    : BoundUserInterfaceMessage
{
    public readonly ProtoId<CP14WorkbenchRecipePrototype> Recipe = recipe;
}


[Serializable, NetSerializable]
public sealed class CP14WorkbenchUiRecipesState(List<CP14WorkbenchUiRecipesEntry> recipes) : BoundUserInterfaceState
{
    public readonly List<CP14WorkbenchUiRecipesEntry> Recipes = recipes;
}

[Serializable, NetSerializable]
public readonly struct CP14WorkbenchUiRecipesEntry(ProtoId<CP14WorkbenchRecipePrototype> protoId, bool craftable)
    : IEquatable<CP14WorkbenchUiRecipesEntry>
{
    public readonly ProtoId<CP14WorkbenchRecipePrototype> ProtoId = protoId;
    public readonly bool Craftable = craftable;

    public int CompareTo(CP14WorkbenchUiRecipesEntry other)
    {
        return Craftable.CompareTo(other.Craftable);
    }

    public override bool Equals(object? obj)
    {
        return obj is CP14WorkbenchUiRecipesEntry other && Equals(other);
    }

    public bool Equals(CP14WorkbenchUiRecipesEntry other)
    {
        return ProtoId.Id == other.ProtoId.Id;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ProtoId, Craftable);
    }

    public override string ToString()
    {
        return $"{ProtoId} ({Craftable})";
    }

    public static int CompareTo(CP14WorkbenchUiRecipesEntry left, CP14WorkbenchUiRecipesEntry right)
    {
        return right.CompareTo(left);
    }
}
