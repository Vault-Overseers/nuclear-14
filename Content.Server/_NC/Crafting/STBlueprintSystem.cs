/*
Copyright (C) 2025 Stalker14
license:
  This source code is the exclusive of TornadoTech (Maltsev Daniil), JerryImMouse, LordVladimer (Valdis Fedorov)
  and is protected by copyright law.
  Any unauthorized use or reproduction of this source code
  is strictly prohibited and may result in legal action.
  For inquiries or licensing requests,
  please contact TornadoTech (Maltsev Daniil), JerryImMouse, LordVladimer (Valdis Fedorov)
  at Discord (https://discord.com/invite/pu6DEPGjsN).
*/
using Content.Shared._NC.Crafting.Components;
using Content.Shared.Examine;
using Robust.Shared.Prototypes;
using System.Linq;
using System.Text;
using Content.Shared.Tag;
using Content.Shared.Crafting.Prototypes;
using System.Diagnostics;
using Content.Shared.Roles;

namespace Content.Server.Crafting;
/// <summary>
/// Система рецептов. Её фишка в том, что предметы-рецепты созданные с её помощью всегда будут иметь корретные
/// ингридиенты, потому что она будет брать данные напрямую из прототипа-рецепта (craftRecipe).
/// </summary>
public sealed class STBlueprintSystem : EntitySystem
{
    [Dependency] private readonly MetaDataSystem _metaSystem = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;

    private ISawmill _sawmill = default!;
    private Dictionary<string, string> _descriptionsByBlueprint = new();
    private Dictionary<string, string> _namesByBlueprint = new();
    private const string WORKBENCH_TAG = "N14Workbench";
    private Dictionary<string, string> _workbenchNamesById = default!;

    public override void Initialize()
    {
        base.Initialize();
        _sawmill = _logManager.GetSawmill("Blueprint");
        _workbenchNamesById = _proto.EnumeratePrototypes<EntityPrototype>().Where(entity =>
            entity.TryGetComponent<TagComponent>(out var tag, _componentFactory) && _tagSystem.HasTag(tag, WORKBENCH_TAG)
        ).ToDictionary(entity => entity.ID, entity => entity.Name);

        if (_workbenchNamesById.Count == 0)
        {
            _sawmill.Error($"There is no valid workbenches. Check that {WORKBENCH_TAG} exist");
        }
        AddDescriptions();
        SubscribeLocalEvent<STBlueprintComponent, ExaminedEvent>(OnBlueprintExamine);
        // SubscribeLocalEvent<STBlueprintComponent, ComponentStartup>(OnComponentStartup); N14-changes
    }

    /// <summary>
    /// При Shift-Right click показывает подробный рецепт крафта в описании
    /// </summary>
    public void OnBlueprintExamine(EntityUid uid, STBlueprintComponent component, ExaminedEvent args)
    {
        if (component.BlueprintId == null)
            return;
        if (!args.IsInDetailsRange)
            return;
        if (!_descriptionsByBlueprint.TryGetValue(component.BlueprintId, out var description))
            return;

        args.PushMarkup(description);
    }
    /* N14-changes
    /// <summary>
    /// Для того чтобы поменять имя у компонента автоматически, чтобы рецепты не устаревали. К сожалению,
    /// если рецепт расположен в интерфейсе торговца он будет показывать имя из yml. Но хотя бы если на полу
    /// и т.д., мы получим актуальное имя
    /// </summary>
    public void OnComponentStartup(EntityUid uid, STBlueprintComponent component, ComponentStartup args)
    {
        if (!component.BlueprintId.HasValue)
            return;

        if (!_namesByBlueprint.TryGetValue(component.BlueprintId.Value.Id, out var name))
            return;

        _metaSystem.SetEntityName(uid, name);
    }
    */

    private void AddDescriptions()
    {
        var blueprints = _proto.EnumeratePrototypes<CraftingPrototype>().ToList();

        foreach (var blueprint in blueprints)
        {
            var stringBuilder = new StringBuilder();
            string workbench = Loc.GetString("st-blueprint-anyworkbench");
            if (blueprint.RequiredWorkbench != null && _workbenchNamesById.TryGetValue(blueprint.RequiredWorkbench, out var workbenchName))
            {
                workbench = workbenchName;
            }

            string workbenchDetails = $"{Loc.GetString("st-blueprint-workbench")}: {workbench}";
            stringBuilder.AppendLine(workbenchDetails);

            // Corvax-Change-Start
            if (blueprint.AvailableFaction != null && blueprint.AvailableFaction.Any())
            {
                stringBuilder.AppendLine($"{Loc.GetString("st-blueprint-availableFaction")}:");
                foreach (var id in blueprint.AvailableFaction)
                {
                    if (!_proto.TryIndex(id, out DepartmentPrototype? department))
                        continue;
                    string localizedDep = Loc.GetString($"department-{department.ID}");
                    stringBuilder.AppendLine($"\t{localizedDep}");
                }
            }
            // string special = $"{Loc.GetString("st-blueprint-Intelligence")}: {blueprint.RequiredIntelligence}";
            // if (blueprint.RequiredIntelligence != 0)
            //     stringBuilder.AppendLine(special);

            if (blueprint.AvailableJobs != null && blueprint.AvailableJobs.Any())
            {
                stringBuilder.AppendLine($"{Loc.GetString("st-blueprint-availableJobs")}:");
                foreach (var jobId in blueprint.AvailableJobs)
                {
                    if (!_proto.TryIndex(jobId, out JobPrototype? job))
                        continue;
                    stringBuilder.AppendLine($"\t{job.LocalizedName}");
                }
            }
            // Corvax-Change-End
            stringBuilder.AppendLine(Loc.GetString("st-blueprint-ingridients"));
            foreach (var (id, details) in blueprint.Items)
            {
                if (!_proto.TryIndex(id, out var prototype))
                {
                    _sawmill.Error($"There is a recipe {blueprint.ID} with an ingridient {id}. But the ingridient prototype is missing");
                    stringBuilder.AppendLine(Loc.GetString("st-blueprint-not-found"));
                    continue;
                }
                stringBuilder.AppendLine($"\t{prototype.Name} {details.Amount} {GetCatalistIcon(details.Catalyzer)}");
            }
            stringBuilder.AppendLine(Loc.GetString("st-blueprint-result"));
            string? resultName = null;

            foreach (var id in blueprint.ResultProtos)
            {
                if (!_proto.TryIndex(id, out var prototype))
                {
                    _sawmill.Error($"There is a recipe {blueprint.ID} with a result {id}. But the result's prototype is missing");
                    stringBuilder.AppendLine(Loc.GetString("st-blueprint-not-found"));
                    continue;
                }
                if (resultName == null)
                    resultName = prototype.Name;
                stringBuilder.AppendLine($"\t{prototype.Name}");
            }
            var description = stringBuilder.ToString();
            var multipleResults = blueprint.ResultProtos.Count > 1 ? Loc.GetString("st-blueprint-multiple-results") : string.Empty;
            resultName = $"{Loc.GetString("st-blueprint-prefix")} {resultName} {multipleResults}";

            _namesByBlueprint.Add(blueprint.ID, resultName);
            _descriptionsByBlueprint.Add(blueprint.ID, description);
        }
    }

    private string GetCatalistIcon(bool isCatalyzer)
    {
        return isCatalyzer ? Loc.GetString("st-blueprint-ingridient-saved") : "";
    }
}
