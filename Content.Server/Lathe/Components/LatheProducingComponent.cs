namespace Content.Server.Lathe.Components;

/// <summary>
/// For EntityQuery to keep track of which lathes are producing
/// </summary>
[RegisterComponent]
public sealed class LatheProducingComponent : Component
{
    /// <summary>
    /// How much production time has passed, in seconds.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float AccumulatedTime;
}

