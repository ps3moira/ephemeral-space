using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Mind;
using Content.Shared.Preferences;
using Robust.Shared.Enums;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Shared._ES.Auditions;

/// <summary>
/// The main system for handling the creation, integration of relations
/// </summary>
public abstract class SharedAuditionsSystem : EntitySystem
{
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedPvsOverrideSystem _pvs = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ProducerComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(EntityUid uid, ProducerComponent component, ComponentStartup args)
    {
        _pvs.AddGlobalOverride(uid);
    }

    public bool TryGetProducer([NotNullWhen(true)] ref ProducerComponent? component)
    {
        if (component != null)
            return true;

        var query = EntityQuery<ProducerComponent>().ToList();
        component = !query.Any() ? CreateCastEntity() : query.First();
        return true;
    }

    private ProducerComponent CreateCastEntity()
    {
        var manager = Spawn(null, MapCoordinates.Nullspace);
        return EnsureComp<ProducerComponent>(manager);
    }

    public void ChangeRelationship(Entity<CharacterComponent> characterA, Entity<CharacterComponent> characterB, string relationshipId, bool unified = true)
    {
        characterA.Comp.Relationships[characterB.Comp.Name] = relationshipId;
        if (unified)
            characterB.Comp.Relationships[characterA.Comp.Name] = relationshipId;
    }

    public (Entity<MindComponent>, CharacterComponent) CreateBlankCharacter()
    {
        var mind = _mind.CreateMind(null);
        var component = EnsureComp<CharacterComponent>(mind.Owner);
        component.Name = "No Name";
        component.Age = 21;
        component.Gender = Gender.Neuter;

        ProducerComponent? producer = null;
        if (!TryGetProducer(ref producer))
            throw new Exception("Could not get ProducerComponent!");
        producer.Characters.Add(mind.Owner);

        return (mind, component);
    }

    public Entity<CharacterComponent> GenerateCharacter()
    {
        var newCharacter = CreateBlankCharacter();
        var characterComp = newCharacter.Item2;
        var mind = newCharacter.Item1;

        var profile = HumanoidCharacterProfile.RandomWithSpecies();

        characterComp.Name = profile.Name;
        characterComp.Age = profile.Age;
        characterComp.Gender = profile.Gender;
        characterComp.Appearance = profile.Appearance;

        mind.Comp.CharacterName = profile.Name;

        return (mind.Owner, characterComp);
    }

    public Entity<CrewComponent> GenerateEmptyCrew(ResPath mapPath)
    {
        var newCrew = EntityManager.Spawn();
        var component = EnsureComp<CrewComponent>(newCrew);

        component.Crew = new();
        component.CrewCount = 0;
        component.MapPath = mapPath;

        ProducerComponent? producer = null;
        if (!TryGetProducer(ref producer))
            throw new Exception("Could not get ProducerComponent!");
        producer.Crew.Add(newCrew);

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
