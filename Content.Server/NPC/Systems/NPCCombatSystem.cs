using Content.Server.Interaction;
using Content.Server.Weapon.Ranged.Systems;
using Content.Shared.CombatMode;
using Content.Shared.Interaction;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Server.NPC.Systems;

/// <summary>
/// Handles combat for NPCs.
/// </summary>
public sealed partial class NPCCombatSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly GunSystem _gun = default!;
    [Dependency] private readonly InteractionSystem _interaction = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();
        InitializeMelee();
        InitializeRanged();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        UpdateMelee(frameTime);
        UpdateRanged(frameTime);
    }
}
