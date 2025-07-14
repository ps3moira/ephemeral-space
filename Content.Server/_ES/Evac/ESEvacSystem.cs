using Content.Server.RoundEnd;
using Content.Server.Shuttles.Components;
using Content.Server.Station.Systems;
using Content.Shared._ES.Evac;
using Content.Shared._ES.Evac.Components;

namespace Content.Server._ES.Evac;

/// <inheritdoc/>
public sealed class ESEvacSystem : ESSharedEvacSystem
{
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly RoundEndSystem _roundEnd = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
    }

    protected override void UpdateEvacStatus()
    {
        base.UpdateEvacStatus();

        if (_roundEnd.IsRoundEndRequested())
            return;

        var allStations = new HashSet<EntityUid>();

        var stationQuery = EntityQueryEnumerator<StationEmergencyShuttleComponent>();
        while (stationQuery.MoveNext(out var uid, out _))
        {
            allStations.Add(uid);
        }

        var noStations = new HashSet<EntityUid>();
        var beaconQuery = EntityQueryEnumerator<ESEvacConsoleComponent>();
        while (beaconQuery.MoveNext(out var uid, out _))
        {
            if (_station.GetOwningStation(uid) is not { } station)
                continue;

            if (noStations.Contains(station))
                continue;

            if (!TryComp<ESEvacBeaconComponent>(station, out var beacon))
                continue;

            if (beacon.Enabled)
                continue;

            noStations.Add(uid);
        }

        var yesCount = allStations.Count - noStations.Count;
        if ((float) yesCount / allStations.Count > BeaconPercentage)
            _roundEnd.RequestRoundEnd();
    }
}
