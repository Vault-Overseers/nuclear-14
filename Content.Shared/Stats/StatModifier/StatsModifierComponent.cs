using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Stats.StatModifier;

[Access(typeof(SharedStatsModifierSystem))]
[RegisterComponent, NetworkedComponent]
public sealed class StatsModifierComponent : Component
{
    public Dictionary<string, int> CurrentModifiers { get; set; } = new();
}

[Serializable, NetSerializable]
public sealed class StatsModifierComponentState : ComponentState
{
    public Dictionary<string, int> CurrentModifiers { get; }

    public StatsModifierComponentState(Dictionary<string, int> currentModifiers)
    {
        CurrentModifiers = currentModifiers;
    }
}
