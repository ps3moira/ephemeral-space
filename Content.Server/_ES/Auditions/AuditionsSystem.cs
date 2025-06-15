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

        var captains = GenerateEmptySocialGroup();
        captains.Comp.RelativeContext = producer.CaptainContext;

        for (var i = 0; i < captainCount; i++)
        {
            var newCrew = GenerateRandomCrew(crewCount);
            captains.Comp.Members.Add(newCrew.Comp.Members[0]);
        }

        var psgEvt = new PostShipGenerateEvent(producer);
        RaiseLocalEvent(ref psgEvt);

        foreach (var group in producer.SocialGroups)
        {
            var comp = EnsureComp<SocialGroupComponent>(group);
            var ent = (group, comp);

            var pre = new SocialGroupPreIntegrationEvent(ent);
            RaiseLocalEvent(ref pre);

            IntegrateRelationshipGroup(comp.RelativeContext, comp.Members);

            var post = new SocialGroupPostIntegrationEvent(ent);
            RaiseLocalEvent(ref post);
        }

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
