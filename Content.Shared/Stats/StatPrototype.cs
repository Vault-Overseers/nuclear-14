using Robust.Shared.Prototypes;

namespace Content.Shared.Stats;

/// <summary>
/// Primary scores that outline a Players overall capabilities
/// </summary>
[Prototype("stat")]
public sealed class StatPrototype : IPrototype
{
    private string _name = string.Empty;

    [ViewVariables] [IdDataField] public string ID { get; } = default!;

    /// <summary>
    /// Name of the Stat
    /// </summary>
    [ViewVariables]
    [DataField("name")]
    public string Name
    {
        get => _name;
        private set => _name = Loc.GetString(value);
    }

    /// <summary>
    /// Default amount of points in this Stat at character creation
    /// </summary>
    [ViewVariables]
    [DataField("defaultScore")]
    public int DefaultScore { get; } = 5;

    /// <summary>
    /// Maximum amount of points that can be allocated to this Stat
    /// </summary>
    [ViewVariables]
    [DataField("maxScore")]
    public int MaxScore { get; } = 10;

    /// <summary>
    /// Whether this Stat is visible in the Player menu
    /// </summary>
    [ViewVariables]
    [DataField("visible")]
    public bool Visible { get; } = true;
}
