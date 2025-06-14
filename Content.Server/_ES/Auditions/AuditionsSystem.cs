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

    public void GenerateCast(int captains = 26, int minimumCrew = 5, int maximumCrew = 12, string lowPopPoolPrototype = "DefaultShipPoolLowPop", string highPopPoolPrototype = "DefaultShipPoolHighPop")
    {
        var lowPopPool = _prototypeManager.Index<GameMapPoolPrototype>(lowPopPoolPrototype);
        var highPopPool = _prototypeManager.Index<GameMapPoolPrototype>(highPopPoolPrototype);

        for (var i = 0; i < captains; i++)
        {
            var crew = _random.Next(minimumCrew, maximumCrew);
            var lowPop = crew < maximumCrew / 2;
            var map = lowPop ? _random.Pick(lowPopPool.Maps) : _random.Pick(highPopPool.Maps);
            var gameMapPrototype = _prototypeManager.Index<GameMapPrototype>(map);

            GenerateRandomCrew(crew, gameMapPrototype.MapPath);
        }
    }
}
