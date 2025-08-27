using Content.Server._ES.Multistation.Components;
using Content.Server.GameTicking;
using Content.Server.Maps;
using Content.Server.Procedural;
using Content.Server.Shuttles.Systems;
using Content.Server.Station.Components;
using Content.Shared._ES.CCVar;
using Content.Shared._ES.Light.Components;
using Content.Shared._ES.Multistation;
using Content.Shared.CCVar;
using Content.Shared.Roles;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.EntitySerialization;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._ES.Multistation;

/// <summary>
/// This handles spawning in multiple stations in a round
/// </summary>
public sealed class ESMultistationSystem : ESSharedMultistationSystem
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly DungeonSystem _dungeon = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly ShuttleSystem _shuttle = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    private static readonly ProtoId<ESMultistationConfigPrototype> DefaultConfig = "ESDefault";

    private bool _enabled;
    private string _currentConfig = DefaultConfig;

    private readonly Dictionary<ProtoId<JobPrototype>, int?> _availableRoundstartJobs = new();

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESLobbyWorldCreatedEvent>(OnLobbyWorldCreated);

        SubscribeLocalEvent<LoadingMapsEvent>(OnLoadingMaps);
        SubscribeLocalEvent<ESPostLoadingMapsEvent>(OnPostLoadingMaps);

        Subs.CVar(_config, ESCVars.ESMultistationEnabled, value => _enabled = value, true);
        Subs.CVar(_config, ESCVars.ESMultistationCurrentConfig, OnMultistationCurrentConfigChanged, true);

        _config.OnValueChanged(CCVars.GridFill, OnGridFillChanged);

        _playerManager.PlayerStatusChanged += (_, args) =>
        {
            // Only send if they're entering the lobby.
            if (args.NewStatus is SessionStatus.Disconnected)
                return;
            RaiseNetworkEvent(new ESUpdateAvailableRoundstartJobs(_availableRoundstartJobs), args.Session);
        };
    }

    private void OnLobbyWorldCreated(ref ESLobbyWorldCreatedEvent ev)
    {
        RefreshAvailableJobs();
    }

    private void OnGridFillChanged(bool obj)
    {
        if (!obj)
            return;

        var mapQuery = EntityQueryEnumerator<ESMultistationMapComponent>();
        while (mapQuery.MoveNext(out var uid, out var comp))
        {
            LoadExtraGrids((uid, comp));
        }
    }

    private void OnLoadingMaps(LoadingMapsEvent ev)
    {
        if (!_enabled)
            return;

        ev.Maps.Clear();
    }

    private void OnPostLoadingMaps(ref ESPostLoadingMapsEvent ev)
    {
        if (!_enabled)
            return;

        if (!_prototype.TryIndex<ESMultistationConfigPrototype>(_currentConfig, out var config))
            config = _prototype.Index(DefaultConfig);

        EntityManager.AddComponents(ev.DefaultMap, config.MapComponents);

        var configComp = EnsureComp<ESMultistationMapComponent>(ev.DefaultMap);
        configComp.Config = config.ID;

        var stationCount = Math.Max(_playerManager.PlayerCount / config.PlayersPerStation, config.MinStations);

        var stations = GetMapsFromConfig();

        var baseAngle = _random.NextAngle();
        for (var i = 0; i < stationCount; i++)
        {
            baseAngle += Math.Tau / stationCount;

            var station = _prototype.Index(_random.PickAndTake(stations));
            if (!_mapLoader.TryLoadGrid(ev.DefaultMapId,
                    station.MapPath,
                    out var grid,
                    DeserializationOptions.Default,
                    baseAngle.ToVec() * config.StationDistance,
                    _random.NextAngle()))
            {
                throw new Exception($"Failed to load game-map grid {station.ID}");
            }

            // TODO: more robust system, potentially?
            AddComp<ESTileBasedRoofComponent>(grid.Value);

            var g = new List<EntityUid> { grid.Value.Owner };
            RaiseLocalEvent(new PostGameMapLoad(station, ev.DefaultMapId, g, null));
        }

        LoadExtraGrids(ev.DefaultMap);
    }

    private void OnMultistationCurrentConfigChanged(string value)
    {
        if (_currentConfig == value)
            return;

        _currentConfig = value;
        RefreshAvailableJobs();
    }

    private async void LoadExtraGrids(Entity<ESMultistationMapComponent?> map)
    {
        if (!_config.GetCVar(CCVars.GridFill))
            return;

        if (!Resolve(map, ref map.Comp) || map.Comp.GridsLoaded)
            return;

        var config = _prototype.Index(map.Comp.Config);

        foreach (var dungeon in config.Dungeons)
        {
            var count = dungeon.Count.Get(_random.GetRandom());
            for (var i = 0; i < count; i++)
            {
                _map.CreateMap(out var mapId);
                var spawnedGrid = _mapManager.CreateGridEntity(mapId);

                EntityManager.AddComponents(spawnedGrid, dungeon.Components);

                var dungeonProto = _prototype.Index(_random.Pick(dungeon.Configs));
                var distance = dungeon.Distance.Get(_random.GetRandom());
                var pos = _random.NextAngle().ToVec() * distance;

                await _dungeon.GenerateDungeonAsync(dungeonProto,
                    spawnedGrid.Owner,
                    spawnedGrid.Comp,
                    Vector2i.Zero,
                    _random.Next());

                var coords = new EntityCoordinates(map, pos);
                if (dungeon.ForcePos)
                {
                    var gridXform = Transform(spawnedGrid);

                    var angle = _random.NextAngle();

                    var transform = new Transform(_transform.ToWorldPosition(gridXform.Coordinates), angle);
                    var adjustedOffset = Robust.Shared.Physics.Transform.Mul(transform, spawnedGrid.Comp.LocalAABB.Center);

                    _transform.SetCoordinates(spawnedGrid, coords.Offset(adjustedOffset));
                }
                else
                {
                    _shuttle.TryFTLProximity(spawnedGrid.Owner, coords);
                }

                if (dungeon.Name is { } name)
                {
                    _meta.SetEntityName(spawnedGrid, Loc.GetString(_random.Pick(_prototype.Index(name).Values)));
                }
            }
        }

        map.Comp.GridsLoaded = true;
    }

    public List<ProtoId<GameMapPrototype>> GetMapsFromConfig()
    {
        if (!_prototype.TryIndex<ESMultistationConfigPrototype>(_currentConfig, out var config))
            config = _prototype.Index(DefaultConfig);

        var stationCount = Math.Max(_playerManager.PlayerCount / config.PlayersPerStation, config.MinStations);

        var rand = new Random(_gameTicker.RoundId);

        var stations = new List<ProtoId<GameMapPrototype>>(stationCount);
        for (var i = 0; i < stationCount; i++)
        {
            stations.Add(rand.Pick(config.MapPool));
        }

        return stations;
    }

    public void RefreshAvailableJobs()
    {
        _availableRoundstartJobs.Clear();
        var maps = GetMapsFromConfig();

        foreach (var mapId in maps)
        {
            var map = _prototype.Index(mapId);

            foreach (var station in map.Stations.Values)
            {
                // Yes, we basically just rely on the jobs to be specified here.
                // Every map does it like this but it's still a very imperfect solution.
                // This also doesn't account for a future system that might modify the jobs
                // randomly on shift start (wildcards, etc.)
                if (!station.StationComponentOverrides.TryGetComponent<StationJobsComponent>(EntityManager.ComponentFactory, out var jobs))
                    continue;

                foreach (var (job, counts) in jobs.SetupAvailableJobs)
                {
                    var count = counts[0];

                    if (count == 0)
                        continue;

                    if (!_availableRoundstartJobs.TryGetValue(job, out var value))
                        value = 0;
                    _availableRoundstartJobs[job] = value + count;
                }
            }
        }

        RaiseNetworkEvent(new ESUpdateAvailableRoundstartJobs(_availableRoundstartJobs));
    }
}

[ByRefEvent]
public readonly record struct ESPostLoadingMapsEvent(MapId DefaultMapId, EntityUid DefaultMap)
{
    public readonly MapId DefaultMapId = DefaultMapId;
    public readonly EntityUid DefaultMap = DefaultMap;
}
