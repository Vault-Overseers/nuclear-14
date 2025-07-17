using Content.Shared._CP14.Skill.Prototypes;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Shared._CP14.Skill.Restrictions;

[ImplicitDataDefinitionForInheritors]
[MeansImplicitUse]
public abstract partial class CP14SkillRestriction
{
    public abstract bool Check(IEntityManager entManager, EntityUid target, CP14SkillPrototype skill);

    public abstract string GetDescription(IEntityManager entManager, IPrototypeManager protoManager);
}
