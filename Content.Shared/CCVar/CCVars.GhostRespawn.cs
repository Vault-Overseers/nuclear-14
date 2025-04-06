using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    public static readonly CVarDef<double> GhostRespawnTime =
        CVarDef.Create("ghost.respawn_time", 5d, CVar.SERVERONLY);

    public static readonly CVarDef<int> GhostRespawnMaxPlayers =
        CVarDef.Create("ghost.respawn_max_players", 80, CVar.SERVERONLY);

    public static readonly CVarDef<bool> GhostAllowSameCharacter =
        CVarDef.Create("ghost.allow_same_character", true, CVar.SERVERONLY);
}
