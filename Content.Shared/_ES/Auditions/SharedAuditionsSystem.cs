using System.Linq;
using Content.Shared._ES.CCVar;
using Content.Shared.Mind;
using Content.Shared.Preferences;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Robust.Shared.Configuration;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

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
    public ProducerComponent GetProducer()
    {
        var query = EntityQuery<ProducerComponent>().ToList();
        return !query.Any() ? CreateProducerEntity() : query.First();
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
        characterA.Comp.Relationships[characterB] = relationshipId;
        if (mutual)
            characterB.Comp.Relationships[characterA] = relationshipId;
        Dirty(characterA);
    }

    /// <summary>
    /// Removes the relationship between two characters. If the removal is not mutual, then it removes A's relationship with B, and does not affect B.
    /// </summary>
    public void RemoveRelationship(Entity<CharacterComponent> characterA, Entity<CharacterComponent> characterB, bool mutual = true)
    {
        characterA.Comp.Relationships.Remove(characterB);
        if (mutual)
            characterB.Comp.Relationships.Remove(characterA);
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
    /// Generates a character with randomized name, age, gender and appearance.
    /// </summary>
    public Entity<MindComponent, CharacterComponent> GenerateCharacter([ForbidLiteral] string randomPrototype = "DefaultBackground", ProducerComponent? producer = null)
    {
        producer ??= GetProducer();

        var profile = HumanoidCharacterProfile.RandomWithSpecies();

        var (ent, mind) = _mind.CreateMind(null, profile.Name);
        var character = EnsureComp<CharacterComponent>(ent);

        var year = _config.GetCVar(ECCVars.InGameYear) - profile.Age;
        var month = _random.Next(1, 12);
        var day = _random.Next(1, DateTime.DaysInMonth(year, month));
        character.DateOfBirth = new DateTime(year, month, day);
        character.Background = _prototypeManager.Index<WeightedRandomPrototype>(randomPrototype).Pick(_random);
        character.Profile = profile;

        Dirty(ent, character);

        producer.Characters.Add(ent);
        producer.UnusedCharacterPool.Add(ent);

        return (ent, mind, character);
    }

    /// <summary>
    /// Generates a completely empty crew entity.
    /// </summary>
    public Entity<SocialGroupComponent> GenerateEmptySocialGroup(ProducerComponent? producer = null)
    {
        producer ??= GetProducer();

        var newCrew = EntityManager.Spawn();
        var component = EnsureComp<SocialGroupComponent>(newCrew);
        producer.SocialGroups.Add(newCrew);

        return (newCrew, component);
    }
}
