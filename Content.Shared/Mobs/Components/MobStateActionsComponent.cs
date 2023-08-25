﻿using Content.Shared.Mobs.Systems;

namespace Content.Shared.Mobs.Components;

/// <summary>
///     Used for specifying actions that should be automatically added/removed on mob state transitions
/// </summary>
/// <remarks>
///     Mostly for crit-specific actions.
/// </remarks>
/// <see cref="MobStateActionsSystem"/>
[RegisterComponent]
public sealed class MobStateActionsComponent : Component
{
    /// <summary>
    ///     Specifies a list of actions that should be available if a mob is in a given state.
    /// </summary>
    /// <example>
    /// actions:
    ///   Critical:
    ///   - CritSuccumb
    ///   Alive:
    ///   - AnimalLayEgg
    /// </example>
    [DataField("actions")]
    public Dictionary<MobState, List<string>> Actions = new();
}
