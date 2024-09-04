using System.Linq;
using System.Numerics;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Maps;
using Content.Shared.Weather;
using Robust.Shared.Console;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Content.Shared.Atmos;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Server.GameObjects;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Robust.Shared.Map;
using Content.Server.GameTicking;
using Content.Server.Chat.Systems;
using Robust.Shared.Random;
using Content.Server.Radiation.Systems;

namespace Content.Server.Weather;

public sealed class WeatherSystem : SharedWeatherSystem
{
    [Dependency] private readonly IConsoleHost _console = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly RadiationSystem _radiation = default!;
    [Dependency] private readonly TransformSystem _xform = default!;

    private List<Entity<MapGridComponent>> _grids = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<WeatherComponent, ComponentGetState>(OnWeatherGetState);
        _console.RegisterCommand("weather",
            Loc.GetString("cmd-weather-desc"),
            Loc.GetString("cmd-weather-help"),
            WeatherTwo,
            WeatherCompletion);
        SubscribeLocalEvent<WeatherComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid uid, WeatherComponent component, MapInitEvent args)
    {
        Logger.InfoS("weather", $"UID = {uid}!");
        EnsureComp<WeatherComponent>(uid);
        var mapId = _entManager.GetComponent<TransformComponent>(uid).MapID;
        if (!ProtoMan.TryIndex<WeatherPrototype>("Default", out var weatherProto))
            return;
        var curTime = Timing.CurTime;
        Logger.InfoS("weather", $"proto = {weatherProto}!");
        SetWeather(mapId, weatherProto, curTime + TimeSpan.FromSeconds(30));

    }

    private void OnWeatherGetState(EntityUid uid, WeatherComponent component, ref ComponentGetState args)
    {
        args.State = new WeatherComponentState(component.Weather);
    }

    [AdminCommand(AdminFlags.Fun)]
    private void WeatherTwo(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 2)
        {
            shell.WriteError(Loc.GetString("cmd-weather-error-no-arguments"));
            return;
        }

        if (!int.TryParse(args[0], out var mapInt))
            return;

        var mapId = new MapId(mapInt);

        if (!MapManager.MapExists(mapId))
            return;

        if (!_mapSystem.TryGetMap(mapId, out var mapUid))
            return;

        var weatherComp = EnsureComp<WeatherComponent>(mapUid.Value);

        //Weather Proto parsing
        WeatherPrototype? weather = null;
        if (!args[1].Equals("null"))
        {
            if (!ProtoMan.TryIndex(args[1], out weather))
            {
                shell.WriteError(Loc.GetString("cmd-weather-error-unknown-proto"));
                return;
            }
        }

        //Time parsing
        TimeSpan? endTime = null;
        if (args.Length == 3)
        {
            var curTime = Timing.CurTime;
            if (int.TryParse(args[2], out var durationInt))
            {
                endTime = curTime + TimeSpan.FromSeconds(durationInt);
            }
            else
            {
                shell.WriteError(Loc.GetString("cmd-weather-error-wrong-time"));
            }
        }

        SetWeather(mapId, weather, endTime);
    }

    private CompletionResult WeatherCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
            return CompletionResult.FromHintOptions(CompletionHelper.MapIds(EntityManager), "Map Id");

        var a = CompletionHelper.PrototypeIDs<WeatherPrototype>(true, ProtoMan);
        return CompletionResult.FromHintOptions(a, Loc.GetString("cmd-weather-hint"));
    }

    protected override void Run(EntityUid uid, WeatherData weather, WeatherPrototype weatherProto, float frameTime)
    {
        var atmosphereSystem = _entManager.System<AtmosphereSystem>();

        foreach (var session in _playerManager.Sessions)
        {
            if (session.AttachedEntity is not {Valid: true} entity)
                continue;
            var transform = Transform(entity);
            var gridUid = transform.GridUid;

            if (!TryComp<MapGridComponent>(gridUid, out var map))
                return;
            var tiles = map.GetTilesIntersecting(Box2.CenteredAround(transform.WorldPosition,
                new Vector2(5, 5))).ToArray();

            foreach(var tile in tiles)
            {
                var tileDef = (ContentTileDefinition) _tileDefManager[tile.Tile.TypeId];
                var environment = atmosphereSystem.GetTileMixture(tile.GridUid, transform.MapUid, tile.GridIndices, false);
                if(environment == null)
                    continue;
                if(tileDef.Weather)
                    environment.Temperature = weatherProto.Temperature;
                else
                    environment.Temperature = 293.15f;
            }
        }
    }

    public override void SelectNewWeather(EntityUid uid, WeatherComponent component, string proto)
    {
        var mapId = _entManager.GetComponent<TransformComponent>(uid).MapID;
        if (!TryComp<WeatherComponent>(MapManager.GetMapEntityId(mapId), out var weatherComp))
            return;
        var curTime = Timing.CurTime;

        if(proto == "Default")
        {
            if (!ProtoMan.TryGetRandom<WeatherPrototype>(_random, out var wproto))
                return;
            Logger.InfoS("weather", $"new weather = {proto}!");
            var weatherProto = (WeatherPrototype) wproto;
            SetWeather(mapId, weatherProto, curTime + TimeSpan.FromSeconds(weatherProto.Duration));
            if(weatherProto.ShowMessage && weatherProto.Message != string.Empty)
            {
                var message = Loc.GetString(weatherProto.Message);
                var sender = weatherProto.Sender != null ? Loc.GetString(weatherProto.Sender) : "Inner feeling";
                var color = weatherProto.Color != null ? weatherProto.Color : Color.LightGray;
                _chat.DispatchGlobalAnnouncement(message, sender, playSound: false, null, color);
            }
        }

        else
        {
            if(!ProtoMan.TryIndex<WeatherPrototype>("Default", out var weatherProto))
                return;
            Logger.InfoS("weather", $"proto = {weatherProto}!");
            SetWeather(mapId, weatherProto, curTime + TimeSpan.FromSeconds(weatherProto.Duration));

        }
    }

}
