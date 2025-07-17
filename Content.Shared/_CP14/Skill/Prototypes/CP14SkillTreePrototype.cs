using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._CP14.Skill.Prototypes;

/// <summary>
/// A group of skills combined into one “branch”
/// </summary>
[Prototype("cp14SkillTree")]
public sealed partial class CP14SkillTreePrototype : IPrototype
{
    [IdDataField] public string ID { get; } = default!;

    [DataField(required: true)]
    public LocId Name;

    [DataField]
    public SpriteSpecifier? FrameIcon;

    [DataField]
    public SpriteSpecifier? HoveredIcon;

    [DataField]
    public SpriteSpecifier? SelectedIcon;

    [DataField]
    public SpriteSpecifier? LearnedIcon;

    [DataField]
    public SpriteSpecifier? AvailableIcon;

    [DataField]
    public string Parallax = "AspidParallax";

    [DataField]
    public LocId? Desc;

    [DataField]
    public Color Color;

    [DataField]
    public SoundSpecifier LearnSound = new SoundCollectionSpecifier("CP14LearnSkill");
}
