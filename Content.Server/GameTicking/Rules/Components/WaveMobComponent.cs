namespace Content.Server.GameTicking.Rules.Components;

/// <summary>
/// This is used for tagging a mob as a wave defense candidate
/// </summary>
[RegisterComponent]
public sealed class WaveMobComponent : Component
{
    [DataField("group")]
    public string Group = "";

    [DataField("difficulty")]
    public float Difficulty = 1f;

    [DataField("unique")]
    public bool Unique = false;
}
