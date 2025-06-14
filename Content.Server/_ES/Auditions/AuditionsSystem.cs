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
    [Dependency] private readonly IRobustRandom _random = default!;

    /// <summary>
    /// Generates and integrates a crew.
    /// </summary>
    public Entity<SocialGroupComponent> GenerateAndIntegrateCrew(int crewCount = 10, ProducerComponent? producer = null)
    {
        if (!TryGetProducer(ref producer))
            throw new Exception("Could not get ProducerComponent!");

        var crew = GenerateRandomCrew(crewCount);

        var pre = new CrewGeneratePreIntegrationEvent(crew);
        RaiseLocalEvent(ref pre);

        IntegrateRelationshipGroup(crew.Comp);

        var post = new CrewGeneratePostIntegrationEvent(crew);
        RaiseLocalEvent(ref post);

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

        var captains = new List<EntityUid>();

        for (var i = 0; i < captainCount; i++)
        {
            var newCrew = GenerateAndIntegrateCrew(crewCount, producer);
            captains.Add(newCrew.Comp.Members[0]);
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
public struct CrewGeneratePreIntegrationEvent
{
    public Entity<SocialGroupComponent> Crew;

    public CrewGeneratePreIntegrationEvent(Entity<SocialGroupComponent> crew)
    {
        Crew = crew;
    }
}

[ByRefEvent]
public struct CrewGeneratePostIntegrationEvent
{
    public Entity<SocialGroupComponent> Crew;

    public CrewGeneratePostIntegrationEvent(Entity<SocialGroupComponent> crew)
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
