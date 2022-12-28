using Robust.Shared.Serialization;

namespace Content.Shared.Stats;

[Serializable, NetSerializable]
public struct Stat
{
    [ViewVariables] public string Name { get; set; }

    [ViewVariables] public string ID { get; }

    [ViewVariables] public int Score { get; set; }

    [ViewVariables] public bool Visible { get; }

    [ViewVariables] public int MaxScore { get; }

    public Stat(string name, int score, int maxScore, bool display, string id)
    {
        Name = name;
        Score = score;
        MaxScore = maxScore;
        Visible = display;
        ID = id;
    }
}
