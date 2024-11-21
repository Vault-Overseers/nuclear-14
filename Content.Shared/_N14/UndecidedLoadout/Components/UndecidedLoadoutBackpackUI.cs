using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.UndecidedLoadout;

[Serializable, NetSerializable]
public sealed class UndecidedLoadoutBackpackBoundUserInterfaceState : BoundUserInterfaceState
{
    public readonly Dictionary<int, UndecidedLoadoutBackpackSetInfo> Sets;
    public int MaxSelectedSets;

    public UndecidedLoadoutBackpackBoundUserInterfaceState(Dictionary<int, UndecidedLoadoutBackpackSetInfo> sets, int max)
    {
        Sets = sets;
        MaxSelectedSets = max;
    }
}

[Serializable, NetSerializable]
public sealed class UndecidedLoadoutBackpackChangeSetMessage : BoundUserInterfaceMessage
{
    public readonly int SetNumber;

    public UndecidedLoadoutBackpackChangeSetMessage(int setNumber)
    {
        SetNumber = setNumber;
    }
}

[Serializable, NetSerializable]
public sealed class UndecidedLoadoutBackpackApproveMessage : BoundUserInterfaceMessage
{
    public UndecidedLoadoutBackpackApproveMessage() { }
}

[Serializable, NetSerializable]
public enum UndecidedLoadoutBackpackUIKey : byte
{
    Key
};

[Serializable, NetSerializable, DataDefinition]
public partial struct UndecidedLoadoutBackpackSetInfo
{
    [DataField]
    public string Name;

    [DataField]
    public string Description;

    [DataField]
    public SpriteSpecifier Sprite;

    public bool Selected;

    public UndecidedLoadoutBackpackSetInfo(string name, string desc, SpriteSpecifier sprite, bool selected)
    {
        Name = name;
        Description = desc;
        Sprite = sprite;
        Selected = selected;
    }
}
