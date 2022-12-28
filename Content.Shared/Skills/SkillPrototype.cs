using Robust.Shared.Prototypes;

namespace Content.Shared.Skills;

/// <summary>
/// Determines a Players proficiency at a given Skill
/// </summary>
[Prototype("skill")]
public sealed class SkillPrototype : IPrototype
{
    private string _name = string.Empty;

    [ViewVariables] [IdDataField] public string ID { get; } = default!;

    /// <summary>
    /// Name of the Skill
    /// </summary>
    [ViewVariables]
    [DataField("name")]
    public string Name
    {
        get => _name;
        private set => _name = Loc.GetString(value);
    }

    /// <summary>
    /// ID of the Stat that is relevant to this Skill
    /// </summary>
    [ViewVariables]
    [DataField("stat", required: true)]
    public string? Stat { get; }

    /// <summary>
    /// Maximum level that can be reached in this Skill
    /// </summary>
    [ViewVariables]
    [DataField("maxLevel")]
    public int MaxLevel { get; } = 100;

    /// <summary>
    /// Used in the EXP formulae to determine how much EXP is required for the next level
    /// A higher number means steeper leveling curve.
    /// RequiredExp = Math.Ceil(ExpConstant * Math.Sqrt(CurrentLevel))
    /// </summary>
    [ViewVariables]
    [DataField("expConstant")]
    public int ExpConstant { get; } = 100;

    /// <summary>
    /// Whether this Skill is visible in the Player menu
    /// </summary>
    [ViewVariables]
    [DataField("visible")]
    public bool Visible { get; } = true;
}
