using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Mind;
using Content.Shared.Preferences;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Robust.Shared.Enums;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared._ES.Auditions;

/// <summary>
/// The main system for handling the creation, integration of relations
/// </summary>
public abstract class SharedAuditionsSystem : EntitySystem
{
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
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

    /// <summary>
    /// Returns the producer entity singleton, or creates one if it doesn't exist yet
    /// </summary>
    public bool TryGetProducer([NotNullWhen(true)] ref ProducerComponent? component)
    {
        if (component != null)
            return true;

        var query = EntityQuery<ProducerComponent>().ToList();
        component = !query.Any() ? CreateProducerEntity() : query.First();
        return true;
    }

    /// <summary>
    /// Creates the producer entity, intended to be a singleton
    /// </summary>
    private ProducerComponent CreateProducerEntity()
    {
        var manager = Spawn(null, MapCoordinates.Nullspace);
        return EnsureComp<ProducerComponent>(manager);
    }

    /// <summary>
    /// Changes the relationship between two characters. If the relationship is not unified, then it assigns A's relationship with B, and does not affect B.
    /// </summary>
    public void ChangeRelationship(Entity<CharacterComponent> characterA, Entity<CharacterComponent> characterB, string relationshipId, bool unified = true)
    {
        characterA.Comp.Relationships[characterB.Comp.Name] = relationshipId;
        if (unified)
            characterB.Comp.Relationships[characterA.Comp.Name] = relationshipId;
    }

    /// <summary>
    /// Removes the relationship between two characters. If the removal is not unified, then it removes A's relationship with B, and does not affect B.
    /// </summary>
    public void RemoveRelationship(Entity<CharacterComponent> characterA, Entity<CharacterComponent> characterB, bool unified = true)
    {
        characterA.Comp.Relationships.Remove(characterB.Comp.Name);
        if (unified)
            characterB.Comp.Relationships.Remove(characterA.Comp.Name);
    }

    /// <summary>
    /// Attempts to integrate a relationship between two characcters with the following relationship context.
    /// </summary>
    public void IntegrateRelationship(RelationshipContext context, Entity<CharacterComponent> characterA, Entity<CharacterComponent> characterB)
    {
        if (!_random.Prob(context.RelationshipProbability))
            return;

        var weightList = _prototypeManager.Index(context.PoolPrototype);
        var relationship =  weightList.Pick(_random);
        ChangeRelationship(characterA, characterB, relationship);

        var unified = _random.Prob(context.UnificationProbability);
        if (unified)
            return;

        if (context.SeperatePoolPrototype is null)
        {
            RemoveRelationship(characterA, characterB, false);
            return;
        }
        var seperateWeightList = _prototypeManager.Index(context.SeperatePoolPrototype.Value);
        var newRelationship =  seperateWeightList.Pick(_random);

        ChangeRelationship(characterA, characterB, newRelationship, false);
    }

    /// <summary>
    /// Attempts to integrate relationships with a group of characters. Uses a list of Entity&lt;CharacterComponent&gt;s.
    /// </summary>
    public void IntegrateRelationshipGroup(RelationshipContext context, List<Entity<CharacterComponent>> characters)
    {
        // rain here. im no profesional, but i pulled out the paper & pencil for this "algorithm".
        // that goes to show how really great i am with programming things. yay.
        var stopwatch = new Stopwatch();

        var debugCalls = 0;
        stopwatch.Start();

        for (var i = 0; i < characters.Count; i++)
        {
            for (var j = 0; j < i; j++)
            {
                IntegrateRelationship(context, characters[i], characters[j]);
                debugCalls++;
            }
        }

        Log.Info($"Called {debugCalls} times in {stopwatch.Elapsed}.");
    }


    /// <summary>
    /// Attempts to integrate relationships with a group of characters. Uses a list of EntityUids.
    /// </summary>
    public void IntegrateRelationshipGroup(RelationshipContext context, List<EntityUid> characters)
    {
        var stopwatch = new Stopwatch();

        var debugCalls = 0;
        stopwatch.Start();

        for (var i = 0; i < characters.Count; i++)
        {
            for (var j = 0; j < i; j++)
            {
                IntegrateRelationship(context,
                    (characters[i], EnsureComp<CharacterComponent>(characters[i])),
                    (characters[j], EnsureComp<CharacterComponent>(characters[j])));
                debugCalls++;
            }
        }

        Log.Info($"Called {debugCalls} times in {stopwatch.Elapsed}.");
    }

    /// <summary>
    /// Creates a blank character.
    /// </summary>
    public (Entity<MindComponent>, CharacterComponent) CreateBlankCharacter(ProducerComponent? producer = null)
    {
        if (!TryGetProducer(ref producer))
            throw new Exception("Could not get ProducerComponent!");

        var mind = _mind.CreateMind(null);
        var component = EnsureComp<CharacterComponent>(mind.Owner);
        component.Name = "No Name";
        component.Age = 21;
        component.Gender = Gender.Neuter;
        producer.Characters.Add(mind.Owner);

        return (mind, component);
    }

    /// <summary>
    /// Generates a character with randomized name, age, gender and appearance.
    /// </summary>
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

    /// <summary>
    /// Generates a completely empty crew entity.
    /// </summary>
    public Entity<CrewComponent> GenerateEmptyCrew(ResPath mapPath, ProducerComponent? producer = null)
    {
        if (!TryGetProducer(ref producer))
            throw new Exception("Could not get ProducerComponent!");

        var newCrew = EntityManager.Spawn();
        var component = EnsureComp<CrewComponent>(newCrew);

        component.Crew = new();
        component.CrewCount = 0;
        component.MapPath = mapPath;

        producer.Crew.Add(newCrew);

        return (newCrew, component);
    }

    /// <summary>
    /// Generates a random crew entity and crewmembers, with a captain provided. Integrates relationships between all crew members.
    /// </summary>
    public Entity<CrewComponent> GenerateCrewWithCaptain(EntityUid captain, int crewCount, ResPath mapPath, ProducerComponent? producer = null)
    {
        if (!TryGetProducer(ref producer))
            throw new Exception("Could not get ProducerComponent!");

        var crew = GenerateEmptyCrew(mapPath, producer);
        var component = EnsureComp<CrewComponent>(crew);

        var relationshipList = new List<Entity<CharacterComponent>>();

        component.Captain = captain;
        component.Crew.Add(captain);
        component.CrewCount = crewCount;

        relationshipList.Add((captain, EnsureComp<CharacterComponent>(captain)));

        for (var i = 0; i < crewCount; i++)
        {
            var member = GenerateCharacter();
            component.Crew.Add(member);
            relationshipList.Add(member);
        }
        IntegrateRelationshipGroup(producer.CrewContext, relationshipList);

        return crew;
    }

    /// <summary>
    /// Completely generates a random crew entity, with random captains and crewmembers.
    /// </summary>
    public Entity<CrewComponent> GenerateRandomCrew(int crewCount, ResPath mapPath)
    {
        return GenerateCrewWithCaptain(GenerateCharacter(), crewCount, mapPath);
    }
}
