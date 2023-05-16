using Content.Server.Objectives.Interfaces;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Server.Objectives.Conditions;

/// <summary>
/// An untracked, free-form objective specified by a title and description.
/// </summary>
[UsedImplicitly]
[DataDefinition]
public sealed class FreeCondition : IObjectiveCondition, ISerializationHooks
{
    [DataField("title")] private string title = string.Empty;
    [DataField("description")] private string description = string.Empty;
    [DataField("icon")] private string iconPrototype = string.Empty;

    private Mind.Mind? mind;

    public IObjectiveCondition GetAssigned(Mind.Mind mind)
    {
        return new FreeCondition
        {
            title = title,
            description = description,
            iconPrototype = iconPrototype,
            mind = mind
        };
    }

    public string Title => title;
    public string Description => description;
    public SpriteSpecifier Icon => new SpriteSpecifier.EntityPrototype(iconPrototype);

    public float Progress => 0;
    public float Difficulty => 1f;

    public bool Equals(IObjectiveCondition? other)
    {
        return other is FreeCondition cond &&
               Equals(mind, cond.mind) &&
               title == cond.title;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((StealCondition) obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(mind, title);
    }
}
