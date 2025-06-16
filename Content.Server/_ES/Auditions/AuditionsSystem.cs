using System.Diagnostics;
using Content.Server._ES.Auditions.Components;
using Content.Server.Administration;
using Content.Server.Mind;
using Content.Server.Station.Events;
using Content.Server.Station.Systems;
using Content.Shared._ES.Auditions;
using Content.Shared.Administration;
using Content.Shared.GameTicking;
using Content.Shared.Mind;
using Robust.Shared.Random;
using Robust.Shared.Toolshed;

namespace Content.Server._ES.Auditions;

/// <summary>
/// This handles the server-side of auditioning!
/// </summary>
public sealed class AuditionsSystem : SharedAuditionsSystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly StationJobsSystem _stationJobs = default!;
    [Dependency] private readonly StationSpawningSystem _stationSpawning = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StationPostInitEvent>(OnStationPostInit);

        SubscribeLocalEvent<PlayerBeforeSpawnEvent>(OnBeforeSpawn);
    }

    private void OnStationPostInit(ref StationPostInitEvent ev)
    {
        var cast = EnsureComp<StationCastComponent>(ev.Station);
        cast.Crew = GenerateRandomCrew(10); // TODO: temporary until proper role pooling.
    }

    private void OnBeforeSpawn(PlayerBeforeSpawnEvent ev)
    {
        if (!TryComp<StationCastComponent>(ev.Station, out var cast) || cast.Crew == EntityUid.Invalid)
            return;

        if (!TryComp<SocialGroupComponent>(cast.Crew, out var socialGroup))
            return;

        var availableMembers = new List<EntityUid>();
        foreach (var member in socialGroup.Members)
        {
            if (!TryComp<MindComponent>(member, out var mindComponent))
                continue;

            if (mindComponent.OriginalOwnedEntity is not null)
                continue;

            availableMembers.Add(member);
        }

        var mind = availableMembers.Count != 0 ? _random.Pick(availableMembers) : GenerateCharacter().Owner;
        _mind.SetUserId(mind, ev.Player.UserId);
        var character = Comp<CharacterComponent>(mind);

        var job = ev.JobId ?? _stationJobs.PickBestAvailableJobWithPriority(ev.Station, character.Profile.JobPriorities, true);
        var mob = _stationSpawning.SpawnPlayerCharacterOnStation(ev.Station, job, character.Profile);
        _mind.TransferTo(mind, mob);

        ev.Handled = true;
    }

    /// <summary>
    /// Hires a cast, and integrates relationships between all of the characters.
    /// </summary>
    public void GenerateCast(
        int captainCount = 26,
        int crewCount = 10,
        ProducerComponent? producer = null
    )
    {
        producer ??= GetProducer();

        var preEvt = new PreCastGenerateEvent(producer);
        RaiseLocalEvent(ref preEvt);

        var captains = GenerateEmptySocialGroup();
        captains.Comp.RelativeContext = producer.CaptainContext;

        var newCharacters = new List<EntityUid>();

        for (var i = 0; i < captainCount; i++)
        {
            var newCrew = GenerateRandomCrew(crewCount);
            captains.Comp.Members.Add(newCrew.Comp.Members[0]);
            newCharacters.AddRange(newCrew.Comp.Members);
            producer.AvailableCrews.Add(newCrew);
        }

        var psgEvt = new PostShipGenerateEvent(producer);
        RaiseLocalEvent(ref psgEvt);

        foreach (var group in producer.SocialGroups)
        {
            var comp = EnsureComp<SocialGroupComponent>(group);
            if (comp.Integrated)
                continue;

            var ent = (group, comp);

            var pre = new SocialGroupPreIntegrationEvent(ent);
            RaiseLocalEvent(ref pre);

            IntegrateRelationshipGroup(comp.RelativeContext, comp.Members);
            comp.Integrated = true;

            var post = new SocialGroupPostIntegrationEvent(ent);
            RaiseLocalEvent(ref post);
        }

        IntegrateRelationshipGroup(producer.IntercrewContext, newCharacters);
        producer.Characters.AddRange(newCharacters);

        var postEvt = new PostCastGenerateEvent(producer);
        RaiseLocalEvent(ref postEvt);
    }

    public void GenerateCast(
        int captainCount = 26,
        int minimumCrew = 5,
        int maximumCrew = 12,
        ProducerComponent? producer = null
    )
    {
        GenerateCast(captainCount, _random.Next(minimumCrew, maximumCrew), producer);
    }
}

[ToolshedCommand, AdminCommand(AdminFlags.Round)]
public sealed class CastCommand : ToolshedCommand
{
    private AuditionsSystem? _auditions;

    [CommandImplementation("generate")]
    public IEnumerable<string> Generate(int captainCount = 26, int crewSize = 10)
    {
        _auditions ??= GetSys<AuditionsSystem>();

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        _auditions.GenerateCast(captainCount, crewSize, null);

        yield return $"Generated cast in {stopwatch.Elapsed.TotalMilliseconds} ms.";
    }

    [CommandImplementation("view")]
    public IEnumerable<string> View([PipedArgument] EntityUid castMember)
    {
        _auditions ??= GetSys<AuditionsSystem>();
        if (!EntityManager.TryGetComponent<CharacterComponent>(castMember, out var character))
        {
            yield return "Invalid cast member object (did not have CharacterComponent)!";
        }
        else
        {
            yield return $"{character.Name}, {character.Age} years old ({character.DateOfBirth.ToShortDateString()})\nBackground: {character.Background}\nRelationships\n";
            Dictionary<string, List<string>> relationships = new();
            foreach (var relationship in character.Relationships)
            {
                if (relationships.ContainsKey(relationship.Value))
                    relationships[relationship.Value].Add(relationship.Key);
                else
                    relationships[relationship.Value] = [relationship.Key];
            }

            foreach (var relationship in relationships)
            {
                yield return $"{relationship.Key} ({relationship.Value.Count}): {string.Join(", ", relationship.Value.ToArray())}";
            }
        }
    }
}

/// <summary>
/// Fires prior to this social group's relationships being integrated.
/// </summary>
[ByRefEvent]
public readonly record struct SocialGroupPreIntegrationEvent(Entity<SocialGroupComponent> Group);

/// <summary>
/// Fires after this social group's relationships have been integrated.
/// </summary>
[ByRefEvent]
public readonly record struct SocialGroupPostIntegrationEvent(Entity<SocialGroupComponent> Group);

/// <summary>
/// Fires prior to any generation events (captain group, crew groups, etc).
/// </summary>
[ByRefEvent]
public readonly record struct PreCastGenerateEvent(ProducerComponent Producer);

/// <summary>
/// Fires after the primary generation events (captain group, crew group, etc) but before integration of relationships.
/// </summary>
[ByRefEvent]
public readonly record struct PostShipGenerateEvent(ProducerComponent Producer);

/// <summary>
/// Fires after all relationships have been integrated.
/// </summary>
[ByRefEvent]
public readonly record struct PostCastGenerateEvent(ProducerComponent Producer);
