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


    public void GenerateCast(
        int captainCount = 26,
        int minimumCrew = 5,
        int maximumCrew = 12,
        string lowPopPoolPrototype = "DefaultShipPoolLowPop",
        string highPopPoolPrototype = "DefaultShipPoolHighPop",
        ProducerComponent? producer = null
    )
    {
        if (!TryGetProducer(ref producer))
            throw new Exception("Could not get ProducerComponent!");

        var lowPopPool = _prototypeManager.Index<GameMapPoolPrototype>(lowPopPoolPrototype);
        var highPopPool = _prototypeManager.Index<GameMapPoolPrototype>(highPopPoolPrototype);

        var captains = new List<Entity<CharacterComponent>>();

        for (var i = 0; i < captainCount; i++)
        {
            var crewCount = _random.Next(minimumCrew, maximumCrew);
            var lowPop = crewCount < maximumCrew / 1.5;
            var map = lowPop ? _random.Pick(lowPopPool.Maps) : _random.Pick(highPopPool.Maps);
            var gameMapPrototype = _prototypeManager.Index<GameMapPrototype>(map);

            var crew = GenerateRandomCrew(crewCount, gameMapPrototype.MapPath);
            if (!crew.Comp.Captain.HasValue)
                throw new Exception("Crew did not have a captain upon assignment");
            captains.Add((crew.Comp.Captain.Value, EnsureComp<CharacterComponent>(crew.Comp.Captain.Value)));
        }

        IntegrateRelationshipGroup(producer.CaptainContext, captains);
        IntegrateRelationshipGroup(producer.IntercrewContext, producer.Characters);
    }
}
