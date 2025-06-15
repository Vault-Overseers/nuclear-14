using System.Linq;
using System.Text;
using Content.KayMisaZlevels.Server.Components;
using Content.KayMisaZlevels.Server.Systems;
using Content.KayMisaZlevels.Shared.Components;
using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Server.GameObjects;
using Robust.Shared.EntitySerialization;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Utility;

namespace Content.Server.Toolshed;

[ToolshedCommand, AdminCommand(AdminFlags.Admin)]
public sealed class ZStackCommand : ToolshedCommand
{
    private ZStackSystem? _zStack;
    private MapSystem? _map;
    private MapLoaderSystem? _mapLoader;
    private ZDefinedStackSystem? _defStackSys;

    [CommandImplementation("make_stack")]
    public EntityUid MakeStack([PipedArgument] EntityUid map)
    {
        _zStack ??= GetSys<ZStackSystem>();
        EntityUid? stack = null;
        _zStack.AddToStack(map, ref stack);
        return stack.Value;
    }

    [CommandImplementation("add_to_stack")]
    public EntityUid AddToStack([PipedArgument] EntityUid stack,
        [CommandArgument] ValueRef<EntityUid> map,
        [CommandInvocationContext] IInvocationContext ctx)
    {
        var val = map.Evaluate(ctx);
        _zStack ??= GetSys<ZStackSystem>();
        var stackLoc = (EntityUid?) stack;
        _zStack.AddToStack(val, ref stackLoc);
        return stack;
    }

    [CommandImplementation("quick_stack")]
    public EntityUid QuickStack([PipedArgument] EntityUid map)
    {
        _map ??= GetSys<MapSystem>();
        _zStack ??= GetSys<ZStackSystem>();

        var lowest = _map.CreateMap();
        var lower = _map.CreateMap();
        var stackLoc = (EntityUid?) null;
        _zStack.AddToStack(lowest, ref stackLoc);
        _zStack.AddToStack(lower, ref stackLoc);
        _zStack.AddToStack(map, ref stackLoc);

        return stackLoc.Value;
    }

    [CommandImplementation("quick_stack_atop")]
    public EntityUid QuickStackAtop([PipedArgument] EntityUid map)
    {
        _map ??= GetSys<MapSystem>();
        _zStack ??= GetSys<ZStackSystem>();

        var lowest = _map.CreateMap();
        var lower = _map.CreateMap();
        var stackLoc = (EntityUid?) null;
        _zStack.AddToStack(map, ref stackLoc);
        _zStack.AddToStack(lower, ref stackLoc);
        _zStack.AddToStack(lowest, ref stackLoc);

        return stackLoc.Value;
    }

    [CommandImplementation("load_stack")]
    public void LoadStack(IInvocationContext ctx, int intMapId, string mapPath)
    {
        var mapId = new MapId(intMapId);

        // no loading null space
        if (mapId == MapId.Nullspace)
        {
            ctx.WriteLine(Loc.GetString("cmd-loadmap-nullspace"));
            return;
        }

        _map ??= GetSys<MapSystem>();
        if (_map.MapExists(mapId))
        {
            ctx.WriteLine(Loc.GetString("cmd-loadmap-exists", ("mapId", mapId)));
            return;
        }

        _zStack ??= GetSys<ZStackSystem>();
        _defStackSys ??= GetSys<ZDefinedStackSystem>();
        _mapLoader ??= GetSys<MapLoaderSystem>();

        var opts = new DeserializationOptions
        {
            StoreYamlUids = false,
            InitializeMaps = false // dont
        };

        var path = new ResPath(mapPath);
        _mapLoader.TryLoadMapWithId(mapId, path, out var mapUid, out _, opts);

        // No map? Well, you are coocked bro
        if (!_map.MapExists(mapId) || mapUid is null)
        {
            ctx.WriteLine(Loc.GetString("cmd-loadmap-error", ("path", mapPath)));
            return;
        }

        // Try load levels
        var result = _defStackSys.LoadMap(mapUid.Value);
        if (!result)
        {
            ctx.WriteLine($"Levels for {mapId.ToString()} can't loaded! You are coocked bro!");
            return;
        }

        // Initialize parent main map
        //_map.InitializeMap(mapId);

        ctx.WriteLine(Loc.GetString("cmd-loadmap-success", ("mapId", mapId), ("path", mapPath)));
    }

    [CommandImplementation("save_stack")]
    public void SaveStack(IInvocationContext ctx, int intMapId, string mapPath)
    {
        var mapId = new MapId(intMapId);

        // no saving null space
        if (mapId == MapId.Nullspace)
            return;

        _map ??= GetSys<MapSystem>();
        if (!_map.MapExists(mapId))
        {
            return;
        }

        _zStack ??= GetSys<ZStackSystem>();
        _defStackSys ??= GetSys<ZDefinedStackSystem>();
        _mapLoader ??= GetSys<MapLoaderSystem>();

        if (_map.IsInitialized(mapId))
        {
            ctx.WriteLine(Loc.GetString("cmd-savemap-init-warning"));
            return;
        }

        if (!_map.TryGetMap(mapId, out var stack))
        {
            ctx.WriteLine($"Stack {intMapId} doesn't exist!");
            return;
        }

        if (!_zStack.TryGetZStack((EntityUid) stack, out var stackComp))
        {
            ctx.WriteLine($"Stack {stack.ToString()} is not stack!");
            return;
        }

        ctx.WriteLine(Loc.GetString("cmd-savemap-attempt", ("mapId", mapId), ("path", mapPath)));

        // Save childrent maps from stack
        foreach (var map in stackComp.Value.Comp.Maps)
        {
            if (TryComp<ZDefinedStackMemberComponent>(map, out var defStackMemberComp) &&
                defStackMemberComp.SavePath is not null)
            {
                _mapLoader.TrySaveMap(map, (ResPath) defStackMemberComp.SavePath);
            }
        }

        // And save the main map
        _mapLoader.TrySaveMap(mapId, new ResPath(mapPath));
        ctx.WriteLine(Loc.GetString("cmd-savemap-success"));
    }

    [CommandImplementation("del_stack")]
    public void DeleteStack(IInvocationContext ctx, int intMapId)
    {
        var mapId = new MapId(intMapId);

        _map ??= GetSys<MapSystem>();
        _zStack ??= GetSys<ZStackSystem>();

        if (!_map.TryGetMap(mapId, out var stack))
        {
            ctx.WriteLine($"Stack {intMapId} doesn't exist!");
            return;
        }

        if (!_zStack.TryGetZStack((EntityUid) stack, out var stackComp))
        {
            ctx.WriteLine($"Stack {intMapId} is not stack!");
            return;
        }

        foreach (var map in stackComp.Value.Comp.Maps)
        {
            QDel(map);
        }

        // And delete stack tracker
        QDel((EntityUid) stack);

        ctx.WriteLine($"Stack {intMapId} successfuly deleted!");
    }

    [CommandImplementation("init_stack")]
    public void InitializeStack(IInvocationContext ctx, int intMapId)
    {
        var mapId = new MapId(intMapId);

        _map ??= GetSys<MapSystem>();
        _zStack ??= GetSys<ZStackSystem>();

        if (_map.IsInitialized(mapId))
        {
            ctx.WriteLine($"Stack {intMapId} already initialized!");
            return;
        }

        if (!_map.TryGetMap(mapId, out var stack))
        {
            ctx.WriteLine($"Stack {intMapId} doesn't exist!");
            return;
        }

        if (!_zStack.TryGetZStack((EntityUid) stack, out var stackComp))
        {
            ctx.WriteLine($"Stack {intMapId} is not stack!");
            return;
        }

        foreach (var map in stackComp.Value.Comp.Maps)
        {
            if (!_map.IsInitialized(map))
                _map.InitializeMap(map);
        }

        // And initialize stack tracker
        _map.InitializeMap(mapId);

        ctx.WriteLine($"Stack {intMapId} successfuly initialized!");
    }

    [CommandImplementation("list_stack")]
    public void ListStack(IInvocationContext ctx)
    {
        _map ??= GetSys<MapSystem>();
        _zStack ??= GetSys<ZStackSystem>();

        var msg = new StringBuilder();
        var trackers = _zStack.GetAllZStackTrackers();

        foreach (var mapUid in trackers.OrderBy(uid => uid.Id))
        {
            if (!TryComp<MapComponent>(mapUid, out var mapComp))
                return;

            msg.AppendFormat("{0}: {1}, init: {2}, paused: {3}\n",
                mapComp.MapId,
                Comp<MetaDataComponent>(mapUid).EntityName,
                _map.IsInitialized(mapUid),
                _map.IsPaused(mapComp.MapId));
        }

        ctx.WriteLine(msg.ToString());
    }
}
