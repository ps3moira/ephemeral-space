using Content.Shared.Mind;
using Content.Shared.Preferences;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Shared._ES.Auditions;

/// <summary>
/// This handles casting!
/// </summary>
public sealed class AuditionsSystem : EntitySystem
{
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {

    }

    public void ChangeRelationship(Entity<CharacterComponent> characterA, Entity<CharacterComponent> characterB, string relationship, bool unified = true)
    {
        characterA.Comp.Relationships[characterB.Comp.Name] = relationship;
        if (unified)
            characterB.Comp.Relationships[characterA.Comp.Name] = relationship;
    }

    public Entity<CharacterComponent> GenerateCharacter()
    {
        var newCharacter = EntityManager.Spawn();
        var profile = HumanoidCharacterProfile.RandomWithSpecies();

        var component = EnsureComp<CharacterComponent>(newCharacter);
        component.Name = profile.Name;
        component.Age = profile.Age;
        component.Gender = profile.Gender;
        component.Appearance = profile.Appearance;

        _mind.CreateMind(null, component.Name);

        return (newCharacter, component);
    }

    public Entity<CrewComponent> GenerateEmptyCrew(ResPath mapPath)
    {
        var newCrew = EntityManager.Spawn();
        var component = EnsureComp<CrewComponent>(newCrew);

        component.Crew = new();
        component.CrewCount = 0;
        component.MapPath = mapPath;

        return (newCrew, component);
    }

    public Entity<CrewComponent> GenerateCrewWithCaptain(EntityUid captain, int crewCount, ResPath mapPath)
    {
        var crew = GenerateEmptyCrew(mapPath);
        var component = EnsureComp<CrewComponent>(crew);

        component.Captain = captain;
        component.CrewCount = crewCount;

        for (var i = 0; i < crewCount; i++)
        {
            var member = GenerateCharacter();
            component.Crew.Add(member);
        }

        return crew;
    }

    public Entity<CrewComponent> GenerateRandomCrew(int crewCount, ResPath mapPath)
    {
        return GenerateCrewWithCaptain(GenerateCharacter(), crewCount, mapPath);
    }
}
