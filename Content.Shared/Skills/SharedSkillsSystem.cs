using System.Diagnostics.CodeAnalysis;
using Content.Shared.Stats;
using Robust.Shared.Prototypes;

namespace Content.Shared.Skills;

public abstract class SharedSkillsSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    private readonly Dictionary<string, SkillPrototype> _skillData = new();
    private readonly List<string> _publicSkills = new();
    private ISawmill _sawmill = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SharedSkillsComponent, ComponentInit>(OnComponentInit);

        _sawmill = Logger.GetSawmill("skills");
        _sawmill.Level = LogLevel.Info;

        LoadPrototypes();
        _prototypeManager.PrototypesReloaded += HandlePrototypesReloaded;
    }

    private void OnComponentInit(EntityUid uid, SharedSkillsComponent component, ComponentInit args)
    {
        base.Initialize();
        component.SkillsData.Clear();
        foreach (var skillId in _publicSkills)
        {
            RetrieveSkillDataPrototype(skillId, out var prototype);
            if (prototype == null)
                continue;

            // We need to get the Stat associated with this Skill to calculate the starting Level
            _entityManager.TryGetComponent(uid, out SharedStatsComponent? statsComp);

            var stat = statsComp?.StatsData.Find(c => c.Name == prototype.Stat);
            if (stat == null)
                continue;

            var baseLevel = 2 + 2 * stat.Value.Score + 3;

            var skillData = new Skill
            {
                Name = prototype.Name,
                Level = baseLevel
            };

            component.SkillsData.Add(skillData);
        }
    }

    public bool RetrieveSkillDataPrototype(string identifier, [NotNullWhen(true)] out SkillPrototype? skill)
    {
        if (_skillData.TryGetValue(identifier, out var skillData))
        {
            skill = skillData;
            return true;
        }

        skill = null;
        return false;
    }

    public int GetSkillScore(SharedSkillsComponent comp, string id)
    {
        return comp.SkillsData.Find(i => i.ID == id).Level;
    }

    public void SetSkillScore(SharedSkillsComponent comp, string id, int level)
    {
        var skill = comp.SkillsData.Find(i => i.ID == id);
        skill.Level = level;

        Dirty(comp);
    }

    private void LoadPrototypes()
    {
        _skillData.Clear();
        _publicSkills.Clear();
        foreach(var skill in _prototypeManager.EnumeratePrototypes<SkillPrototype>())
        {
            if(_skillData.ContainsKey(skill.ID))
            {
                Logger.ErrorS("skills",
                    "Found Skill with duplicate SkillPrototype ID {0} - all skill must have" +
                    " a unique prototype, this one will be skipped", skill.ID);
            }
            else
            {
                _sawmill.Log(LogLevel.Info, "Added skill prototype with {0} name", skill.ID);
                _skillData.Add(skill.ID, skill);
                if (skill.Visible)
                    _publicSkills.Add(skill.ID);
            }
        }
    }

    private void HandlePrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        LoadPrototypes();
    }
}
