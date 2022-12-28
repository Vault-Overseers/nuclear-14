using System.Linq;
using Robust.Shared.GameStates;

namespace Content.Shared.Skills;

[RegisterComponent, NetworkedComponent]
public sealed class SharedSkillsComponent : Component
{
    public List<Skill> SkillsData { get; set; } = new();

    [ViewVariables(VVAccess.ReadWrite)]
    public Dictionary<string, int> Skills => SkillsData.ToDictionary(skill => skill.Name, skill => skill.Level);
}
