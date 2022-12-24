using Content.Server.NPC.HTN.Preconditions;

namespace Content.Server.NPC.HTN;

/// <summary>
/// AKA Method. This is a branch available for a compound task.
/// </summary>
[DataDefinition]
public sealed class HTNBranch
{
    // Made this its own class if we ever need to change it.
    [ViewVariables, DataField("preconditions")]
    public List<HTNPrecondition> Preconditions = new();

    [ViewVariables] public List<HTNTask> Tasks = new();

    /// <summary>
    /// Due to how serv3 works we need to defer getting the actual tasks until after they have all been serialized.
    /// </summary>
    [ViewVariables, DataField("tasks", required: true, customTypeSerializer:typeof(HTNTaskListSerializer))]
    public List<string> TaskPrototypes = default!;
}
