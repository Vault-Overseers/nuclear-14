using Robust.Shared.Serialization;

namespace Content.Shared.Skills;

[Serializable, NetSerializable]
public struct Skill
{
    [ViewVariables] public string Name { get; set; }

    [ViewVariables] public string ID { get; }

    [ViewVariables] public int Level { get; set; }
    [ViewVariables] public int Experience { get; set; }

    [ViewVariables] public bool Visible { get; }

    [ViewVariables] public int MaxLevel { get; }

    [ViewVariables] public int ExpConstant { get; }

    [ViewVariables] public string Stat { get; }

    public Skill(string name, int level, int maxLevel, int exp, int xpConst, bool visible, string id, string stat)
    {
        Name = name;
        Level = level;
        Experience = exp;
        ExpConstant = xpConst;
        MaxLevel = maxLevel;
        Visible = visible;
        Stat = stat;
        ID = id;
    }
}
