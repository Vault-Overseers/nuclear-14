using System.Linq;
using Robust.Shared.GameStates;

namespace Content.Shared.Stats;

[RegisterComponent, NetworkedComponent]
public sealed class SharedStatsComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public List<Stat> StatsData { get; set; } = new();

    [ViewVariables(VVAccess.ReadWrite)]
    public Dictionary<string, int> Stats => StatsData.ToDictionary(stat => stat.Name, stat => stat.Score);
}
