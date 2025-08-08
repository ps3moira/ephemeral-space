using Content.Server._ES.Arrivals.Components;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.GameTicking;
using Content.Server.Screens.Components;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Events;
using Content.Server.Shuttles.Systems;
using Content.Server.Spawners.Components;
using Content.Server.Spawners.EntitySystems;
using Content.Server.Station.Components;
using Content.Server.Station.Events;
using Content.Server.Station.Systems;
using Content.Shared.CCVar;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Shuttles.Components;
using Content.Shared.Tag;
using Content.Shared.Tiles;
using Robust.Server.GameObjects;
using Robust.Shared.Collections;
using Robust.Shared.Configuration;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._ES.Arrivals;

public sealed class ESArrivalsSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly DeviceNetworkSystem _deviceNetwork = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;
    [Dependency] private readonly ShuttleSystem _shuttle = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly StationSpawningSystem _stationSpawning = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    private bool _arrivalsEnabled = true;

    private static readonly ProtoId<TagPrototype> DockTagProto = "DockArrivals";

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESStationArrivalsComponent, StationPostInitEvent>(OnStationPostInit);

        SubscribeLocalEvent<ESArrivalsShuttleComponent, FTLTagEvent>(OnShuttleTag);
        SubscribeLocalEvent<ESArrivalsShuttleComponent, FTLStartedEvent>(OnFTLStarted);
        SubscribeLocalEvent<ESArrivalsShuttleComponent, FTLCompletedEvent>(OnFTLCompleted);

        SubscribeLocalEvent<PlayerSpawningEvent>(HandlePlayerSpawning, before: [typeof(SpawnPointSystem)]);

        _config.OnValueChanged(CCVars.ArrivalsShuttles, OnArrivalsConfigChanged, true);
    }

    private void OnArrivalsConfigChanged(bool val)
    {
        if (_arrivalsEnabled && !val && _gameTicker.RunLevel != GameRunLevel.PreRoundLobby)
        {
            Log.Error("EmoGarbage didn't bother implementing disabling arrivals mid-round.");
            return;
        }

        _arrivalsEnabled = val;

        if (_arrivalsEnabled)
        {
            var query = EntityQueryEnumerator<ESStationArrivalsComponent>();
            while (query.MoveNext(out var uid, out var comp))
            {
                SetupShuttle((uid, comp));
            }
        }
    }

    private void OnStationPostInit(Entity<ESStationArrivalsComponent> ent, ref StationPostInitEvent args)
    {
        if (!_arrivalsEnabled)
            return;
        SetupShuttle(ent);
    }

    private void OnShuttleTag(Entity<ESArrivalsShuttleComponent> ent, ref FTLTagEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        args.Tag = DockTagProto;
    }

    private void OnFTLStarted(Entity<ESArrivalsShuttleComponent> ent, ref FTLStartedEvent args)
    {
        if (TryComp<DeviceNetworkComponent>(ent, out var netComp))
        {
            var payload = new NetworkPayload
            {
                [ShuttleTimerMasks.ShuttleMap] = Transform(ent).MapUid,
                [ShuttleTimerMasks.SourceMap] = args.FromMapUid,
                [ShuttleTimerMasks.ShuttleTime] = ent.Comp.FlightDelay,
                [ShuttleTimerMasks.SourceTime] = ent.Comp.FlightDelay,
            };

            _deviceNetwork.QueuePacket(ent, null, payload, netComp.TransmitFrequency);
        }

        if (!TryComp<StationDataComponent>(ent.Comp.Station, out var stationData) ||
            _station.GetLargestGrid(stationData) is not { } grid)
            return;

        var mobQuery = EntityQueryEnumerator<MobStateComponent, TransformComponent>();
        var toMove = new ValueList<Entity<TransformComponent>>();
        while (mobQuery.MoveNext(out var uid, out _, out var xform))
        {
            if (xform.GridUid != ent)
                continue;
            toMove.Add((uid, xform));
        }

        var spawnQuery = EntityQueryEnumerator<SpawnPointComponent, TransformComponent>();
        var spawns = new ValueList<EntityCoordinates>();
        while (spawnQuery.MoveNext(out var spawn, out var xform))
        {
            if (spawn.SpawnType != SpawnPointType.LateJoin)
                continue;

            if (xform.GridUid != grid)
                continue;
            spawns.Add(xform.Coordinates);
        }

        if (spawns.Count == 0)
            return;

        foreach (var mob in toMove)
        {
            _transform.SetCoordinates(mob, _random.Pick(spawns));
        }
    }

    private void OnFTLCompleted(Entity<ESArrivalsShuttleComponent> ent, ref FTLCompletedEvent args)
    {
        if (TryComp<DeviceNetworkComponent>(ent, out var netComp))
        {
            var payload = new NetworkPayload
            {
                [ShuttleTimerMasks.ShuttleMap] = Transform(ent).MapUid,
                [ShuttleTimerMasks.SourceMap] = args.MapUid,
                [ShuttleTimerMasks.ShuttleTime] = ent.Comp.RestDelay,
                [ShuttleTimerMasks.SourceTime] = ent.Comp.RestDelay,
            };

            _deviceNetwork.QueuePacket(ent, null, payload, netComp.TransmitFrequency);
        }
    }

    public void HandlePlayerSpawning(PlayerSpawningEvent ev)
    {
        if (ev.SpawnResult != null)
            return;

        // Only works on latejoin even if enabled.
        if (!_arrivalsEnabled || _gameTicker.RunLevel != GameRunLevel.InRound)
            return;

        if (!TryComp<ESStationArrivalsComponent>(ev.Station, out var arrivals) || arrivals.ShuttleUid is not { } grid)
            return;

        var points = EntityQueryEnumerator<SpawnPointComponent, TransformComponent>();
        var possiblePositions = new List<EntityCoordinates>();
        while (points.MoveNext(out _, out var spawnPoint, out var xform))
        {
            if (xform.GridUid != grid)
                continue;

            if (spawnPoint.SpawnType != SpawnPointType.LateJoin)
                continue;

            possiblePositions.Add(xform.Coordinates);
        }

        if (possiblePositions.Count <= 0)
            return;

        var spawnLoc = _random.Pick(possiblePositions);
        ev.SpawnResult = _stationSpawning.SpawnPlayerMob(
            spawnLoc,
            ev.Job,
            ev.HumanoidCharacterProfile,
            ev.Station);

        EnsureComp<PendingClockInComponent>(ev.SpawnResult.Value);
        EnsureComp<AutoOrientComponent>(ev.SpawnResult.Value);
    }

    public void SetupShuttle(Entity<ESStationArrivalsComponent> ent)
    {
        if (ent.Comp.ShuttleUid is not null)
            return;

        _map.CreateMap(out var mapId);

        if (!_mapLoader.TryLoadGrid(mapId, ent.Comp.ShuttlePath, out var shuttle))
            return;

        _shuttle.TryFTLProximity(shuttle.Value, _shuttle.EnsureFTLMap());

        ent.Comp.ShuttleUid = shuttle.Value;

        var arrivalsComp = EnsureComp<ESArrivalsShuttleComponent>(shuttle.Value);
        arrivalsComp.Station = ent;
        EnsureComp<ProtectedGridComponent>(shuttle.Value);
        EnsureComp<PreventPilotComponent>(shuttle.Value);

        ResetTimer((shuttle.Value, arrivalsComp));

        _map.DeleteMap(mapId);
    }

    private void ResetTimer(Entity<ESArrivalsShuttleComponent> ent)
    {
        if (!TryComp<StationDataComponent>(ent.Comp.Station, out var stationData) ||
            _station.GetLargestGrid(stationData) is not { } grid)
            return;

        _shuttle.FTLToDock(ent, Comp<ShuttleComponent>(ent), grid, hyperspaceTime: (float) ent.Comp.FlightDelay.TotalSeconds);
        ent.Comp.TakeoffTime = _timing.CurTime + ent.Comp.FlightDelay + ent.Comp.RestDelay;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ESArrivalsShuttleComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_timing.CurTime < comp.TakeoffTime)
                continue;
            ResetTimer((uid, comp));
        }
    }
}
