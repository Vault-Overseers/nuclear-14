using Content.Shared._CP14.Skill.Prototypes;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Shared._CP14.Skill.Effects;

[ImplicitDataDefinitionForInheritors]
[MeansImplicitUse]
public abstract partial class CP14SkillEffect
{
    public abstract void AddSkill(IEntityManager entManager, EntityUid target);

    public abstract void RemoveSkill(IEntityManager entManager, EntityUid target);

    public abstract string? GetName(IEntityManager entMagager, IPrototypeManager protoManager);

    public abstract string? GetDescription(IEntityManager entMagager, IPrototypeManager protoManager, ProtoId<CP14SkillPrototype> skill);
}
