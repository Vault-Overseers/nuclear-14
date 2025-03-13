using Content.Server.Administration;
using Content.Server.Maps;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Content.Shared.Weather;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Linq;

namespace Content.Server.Weather;

public sealed class WeatherSystem : SharedWeatherSystem
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly IConsoleHost _console = default!;
    [Dependency] private readonly IMapManager _map = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<WeatherComponent, ComponentGetState>(OnWeatherGetState);
        _console.RegisterCommand("weather",
            Loc.GetString("cmd-weather-desc"),
            Loc.GetString("cmd-weather-help"),
            WeatherTwo,
            WeatherCompletion);
        _console.RegisterCommand("randomweather",
            Loc.GetString("cmd-randomweather-desc"),
            Loc.GetString("cmd-randomweather-help"),
            RandomWeatherCommand);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_config.GetCVar(CCVars.AutoWeather) && !WeatherRunning())
        {
            var (weather, map) = SetRandomWeather();
            if (weather != null)
            {
                Logger.InfoS("weather", $"Randomizing weather to {weather.ID} on map {map}");
            }
        }
    }

    private bool WeatherRunning()
    {
        var query = EntityQueryEnumerator<WeatherComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Weather.Count > 0)
            {
                return true;
            }
        }
        return false;
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

    [AdminCommand(AdminFlags.Fun)]
    private void RandomWeatherCommand(IConsoleShell shell, string argStr, string[] args)
    {
        var (weather, map) = SetRandomWeather();
        if (weather != null)
        {
            shell.WriteLine($"Picked {weather.ID} to run on map {map}");
        }
    }

    private (WeatherPrototype?, MapId) SetRandomWeather()
    {
        var weather = RandomWeather();
        if (weather != null) {
            MapId map = GetMainMap();
            SetWeather(map, weather, null);
            return (weather, map);
        }
        return (weather, MapId.Nullspace);
    }

    /**
     * Try to guess the main map on which weather effects should be applied.
     */
    private MapId GetMainMap()
    {
        foreach (var mapId in _map.GetAllMapIds().OrderBy(id => id.GetHashCode()))
        {
            return mapId;
        }
        return MapId.Nullspace;
    }

    private WeatherPrototype? RandomWeather()
    {
        int totalChance = 0;
        foreach (var proto in _prototype.EnumeratePrototypes<WeatherPrototype>())
        {
            totalChance += proto.Chance;
        }

        int tgtChance = _random.Next(totalChance);
        int curr = 0;
        foreach (var proto in _prototype.EnumeratePrototypes<WeatherPrototype>())
        {
            if (curr <= tgtChance && tgtChance < curr + proto.Chance)
                return proto;
            curr += proto.Chance;
        }
        return null;
    }

    private CompletionResult WeatherCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
            return CompletionResult.FromHintOptions(CompletionHelper.MapIds(EntityManager), "Map Id");

        var a = CompletionHelper.PrototypeIDs<WeatherPrototype>(true, ProtoMan);
        var b = a.Concat(new[] { new CompletionOption("null", Loc.GetString("cmd-weather-null")) });
        return CompletionResult.FromHintOptions(b, Loc.GetString("cmd-weather-hint"));
    }
}
