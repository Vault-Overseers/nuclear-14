using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Inventory;
using Content.Shared.Movement.Systems;
using Content.Shared.Stats.StatModifier;
using Robust.Shared.Prototypes;

namespace Content.Shared.Stats;

public abstract class SharedStatsSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    private readonly Dictionary<string, StatPrototype> _statData = new();
    private readonly List<string> _publicStats = new();
    private ISawmill _sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SharedStatsComponent, ComponentInit>(OnComponentInit);

        _sawmill = Logger.GetSawmill("stats");
        _sawmill.Level = LogLevel.Info;

        LoadPrototypes();
        _prototypeManager.PrototypesReloaded += HandlePrototypesReloaded;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
    }

    private void OnComponentInit(EntityUid uid, SharedStatsComponent component, ComponentInit args)
    {
        base.Initialize();
        component.StatsData.Clear();
        foreach (var statId in _publicStats)
        {
            RetrieveStatDataPrototype(statId, out var prototype);
            if (prototype == null)
                continue;

            var statData = new Stat
            {
                Name = prototype.Name,
                Score = prototype.DefaultScore
            };

            component.StatsData.Add(statData);
        }
    }

    private void HandlePrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        LoadPrototypes();
    }

    private void RetrieveStatDataPrototype(string identifier, [NotNullWhen(true)] out StatPrototype? stat)
    {
        if (_statData.TryGetValue(identifier, out var statData))
        {
            stat = statData;
            return;
        }

        stat = null;
    }

    public int GetStatScore(EntityUid uid, SharedStatsComponent comp, string id)
    {
        var score = comp.StatsData.Find(i => i.ID == id).Score;

        if (EntityManager.TryGetComponent(uid, out StatsModifierComponent? modifierComp))
        {
            score += modifierComp.CurrentModifiers[id];
        }

        return score;
    }

    public void SetStatScore(SharedStatsComponent comp, string id, int score)
    {
        var stat = comp.StatsData.Find(i => i.ID == id);
        stat.Score = score;

        Dirty(comp);
    }

    private void LoadPrototypes()
    {
        _statData.Clear();
        _publicStats.Clear();
        foreach(var stat in _prototypeManager.EnumeratePrototypes<StatPrototype>())
        {
            if(_statData.ContainsKey(stat.ID))
            {
                Logger.ErrorS("stats",
                    "Found Stat with duplicate StatPrototype ID {0} - all stats must have" +
                    " a unique prototype, this one will be skipped", stat.ID);
            }
            else
            {
                _sawmill.Log(LogLevel.Info, "Added stat prototype with {0} name", stat.ID);
                _statData.Add(stat.ID, stat);
                if (stat.Visible)
                    _publicStats.Add(stat.ID);
            }
        }
    }

    public void UpdateStats(EntityUid uid, SharedStatsComponent comp)
    {
        foreach (var stat in comp.StatsData)
        {
            SetStatScore(comp, stat.ID, GetStatScore(uid, comp, stat.ID));
        }
    }
}
