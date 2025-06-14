using Content.Server.Maps;
using Content.Shared._ES.Auditions;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._ES.Auditions;

/// <summary>
/// This handles the server-side of auditioning!
/// </summary>
public sealed class AuditionsSystem : SharedAuditionsSystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    /// <summary>
    /// Generates and integrates a crew.
    /// </summary>
    public Entity<CrewComponent> GenerateAndIntegrateCrew(int crewCount = 10, ProducerComponent? producer = null)
    {
        if (!TryGetProducer(ref producer))
            throw new Exception("Could not get ProducerComponent!");

        var crew = GenerateRandomCrew(crewCount);
        if (!crew.Comp.Captain.HasValue)
            throw new Exception("Crew did not have a captain upon assignment");

        var ev = new CrewGenerateEvent(crew);
        RaiseLocalEvent(ref ev);

        return crew;
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
        if (!TryGetProducer(ref producer))
            throw new Exception("Could not get ProducerComponent!");

        var preEvt = new PreCastGenerateEvent(producer);
        RaiseLocalEvent(ref preEvt);

        var captains = new List<Entity<CharacterComponent>>();

        for (var i = 0; i < captainCount; i++)
        {
            var newCrew = GenerateAndIntegrateCrew(crewCount, producer);
            captains.Add((newCrew.Comp.Captain!.Value, EnsureComp<CharacterComponent>(newCrew.Comp.Captain.Value)));
        }

        IntegrateRelationshipGroup(producer.CaptainContext, captains);
        IntegrateRelationshipGroup(producer.IntercrewContext, producer.Characters);

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

[ByRefEvent]
public struct CrewGenerateEvent
{
    public Entity<CrewComponent> Crew;

    public CrewGenerateEvent(Entity<CrewComponent> crew)
    {
        Crew = crew;
    }
}

[ByRefEvent]
public struct PreCastGenerateEvent
{
    public ProducerComponent Producer;

    public PreCastGenerateEvent(ProducerComponent producer)
    {
        Producer = producer;
    }
}

[ByRefEvent]
public struct PostCastGenerateEvent
{
    public ProducerComponent Producer;

    public PostCastGenerateEvent(ProducerComponent producer)
    {
        Producer = producer;
    }
}
