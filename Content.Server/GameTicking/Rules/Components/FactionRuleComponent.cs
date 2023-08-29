using Robust.Shared.Audio;
using Robust.Server.Player;
namespace Content.Server.GameTicking.Rules.Components;
using Content.Shared.Preferences;

[RegisterComponent, Access(typeof(FactionRuleSystem))]
public sealed class FactionRuleComponent : Component
{
    public List<String> FactionPrototypes = new();

    public int TotalFactions => FactionPrototypes.Count;

    public enum SelectionState
    {
        WaitingForSpawn = 0,
        ReadyToSelect = 1,
        SelectionMade = 2,
    }

    public SelectionState SelectionStatus = SelectionState.WaitingForSpawn;
    public TimeSpan AnnounceAt = TimeSpan.Zero;
    public Dictionary<IPlayerSession, HumanoidCharacterProfile> PlayerList = new();
}
