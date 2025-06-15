using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared._ES.CCVar;
using Content.Shared.Mind;
using Content.Shared.Preferences;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Robust.Shared.Configuration;
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
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly SharedPvsOverrideSystem _pvs = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

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
    /// Changes the relationship between two characters. If the relationship is not mutual, then it assigns A's relationship with B, and does not affect B.
    /// </summary>
    public void ChangeRelationship(Entity<CharacterComponent> characterA, Entity<CharacterComponent> characterB, string relationshipId, bool mutual = true)
    {
        characterA.Comp.Relationships[characterB.Comp.Name] = relationshipId;
        if (mutual)
            characterB.Comp.Relationships[characterA.Comp.Name] = relationshipId;
        Dirty(characterA);
    }

    /// <summary>
    /// Removes the relationship between two characters. If the removal is not mutual, then it removes A's relationship with B, and does not affect B.
    /// </summary>
    public void RemoveRelationship(Entity<CharacterComponent> characterA, Entity<CharacterComponent> characterB, bool mutual = true)
    {
        characterA.Comp.Relationships.Remove(characterB.Comp.Name);
        if (mutual)
            characterB.Comp.Relationships.Remove(characterA.Comp.Name);
        Dirty(characterA);
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

        var mutual = _random.Prob(context.UnificationProbability);
        if (mutual)
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

    public void IntegrateRelationshipGroup(SocialGroupComponent groupComponent)
    {
        IntegrateRelationshipGroup(groupComponent.RelativeContext, groupComponent.Members);
    }

    /// <summary>
    /// Returns the amount of days there are in a month.
    /// </summary>
    public int GetDaysInMonth(int month, int year)
    {
        var leapYear = year % 4 == 0 && (year % 100 != 0 || year % 400 == 0);
        var thirtyMonths = new List<int> {4, 6, 9, 11};
        if (month == 2)
            return leapYear ? 29 : 28;
        return thirtyMonths.Contains(month) ? 30 : 31;
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

        var year = _config.GetCVar(ECCVars.InGameYear) - component.Age;
        var month = _random.Next(1, 12);
        var day = _random.Next(1, GetDaysInMonth(month, year));
        component.DateOfBirth = new DateTime(year, month, day);

        producer.Characters.Add(mind.Owner);
        Dirty(mind, component);

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

        var year = _config.GetCVar(ECCVars.InGameYear) - characterComp.Age;
        var month = _random.Next(1, 12);
        var day = _random.Next(1, GetDaysInMonth(month, year));
        characterComp.DateOfBirth = new DateTime(year, month, day);

        mind.Comp.CharacterName = profile.Name;
        Dirty(mind, characterComp);
        Dirty(mind, mind.Comp);

        return (mind.Owner, characterComp);
    }

    /// <summary>
    /// Generates a completely empty crew entity.
    /// </summary>
    public Entity<SocialGroupComponent> GenerateEmptySocialGroup(ProducerComponent? producer = null)
    {
        if (!TryGetProducer(ref producer))
            throw new Exception("Could not get ProducerComponent!");

        var newCrew = EntityManager.Spawn();
        var component = EnsureComp<SocialGroupComponent>(newCrew);
        producer.SocialGroups.Add(newCrew);

        return (newCrew, component);
    }

    /// <summary>
    /// Generates a random crew entity and crewmembers, with a captain provided. Integrates relationships between all crew members.
    /// </summary>
    public Entity<SocialGroupComponent> GenerateCrewWithCaptain(EntityUid captain, int crewCount, ProducerComponent? producer = null)
    {
        if (!TryGetProducer(ref producer))
            throw new Exception("Could not get ProducerComponent!");

        var crew = GenerateEmptySocialGroup(producer);
        var component = EnsureComp<SocialGroupComponent>(crew);
        component.RelativeContext = producer.CrewContext;

        component.Members.Add(captain);
        for (var i = 0; i < crewCount; i++)
        {
            var member = GenerateCharacter();
            component.Members.Add(member);
        }

        return crew;
    }

    /// <summary>
    /// Completely generates a random crew entity, with random captains and crewmembers.
    /// </summary>
    public Entity<SocialGroupComponent> GenerateRandomCrew(int crewCount)
    {
        return GenerateCrewWithCaptain(GenerateCharacter(), crewCount);
    }
}
