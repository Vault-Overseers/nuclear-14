namespace Content.Server._KMZLevels.ZTransition;

/// <summary>
/// Marks if entity is ladders, for moving between Z levels.
/// </summary>
[RegisterComponent]
public sealed partial class ZLadderComponent : Component
{
    [DataField("delay")]
    public float DoAfterDelay = 3f;

    [DataField]
    public bool UseDoAfter = true;
}
