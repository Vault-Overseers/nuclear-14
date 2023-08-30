using System.Linq;
using Content.Server.NPC.Components;
using Content.Server.Objectives;
using Robust.Shared.Prototypes;

namespace Content.Server.FactionGoals;

/// <summary>
///     Prototype for goals. Remember that to be assigned, it should be added to one or more objective groups in prototype. E.g. crew, traitor, wizard
/// </summary>
[Prototype("goal")]
public sealed class GoalPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; } = default!;

    [DataField("canBeDuplicate")]
    public bool CanBeDuplicateAssignment { get; private set; }

    [ViewVariables] [DataField("faction")]
    public NpcFactionPrototype Faction = default!;

    public bool CanBeAssigned(Mind.Mind mind)
    {
        if (!CanBeDuplicateAssignment)
        {
            foreach (var objective in mind.AllObjectives)
            {
                if (objective.Prototype.ID == ID) return false;
            }
        }
        return true;
    }

    public Goal GetObjective(Mind.Mind mind)
    {
        return new(this, mind, this.Faction, false);
    }
}
