using Content.Shared.Random;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._ES.Auditions;

/// <summary>
/// This is the cast component placed onto the producer entity.
/// </summary>
[RegisterComponent]
public sealed partial class ProducerComponent : Component
{
    /// <summary>
    /// All the characters in the cast.
    /// </summary>
    [ViewVariables]
    public List<EntityUid> Characters = new ();

    /// <summary>
    /// List of all active crew entities.
    /// </summary>
    [ViewVariables]
    public List<EntityUid> Crew = new ();

    [ViewVariables]
    public RelationshipContext CrewContext = new ("RelationshipPoolCrew", 0.75f);

    [ViewVariables]
    public RelationshipContext CaptainContext = new ("RelationshipPoolCaptains", 0.5f);

    [ViewVariables]
    public RelationshipContext IntercrewContext = new ("RelationshipPoolIntercrew", 0.1f);
}

/// <summary>
/// Configuration for integrating relationships.
/// </summary>
[Serializable, NetSerializable]
public struct RelationshipContext
{
    /// <summary>
    /// List of possible relationships.
    /// </summary>
    [ViewVariables]
    public ProtoId<WeightedRandomPrototype> PoolPrototype;

    /// <summary>
    /// How likely is a relationship to spark in this context?
    /// </summary>
    [ViewVariables]
    public float RelationshipProbability = 0.5f;

    /// <summary>
    /// How likely is a relationship to be mutual (both sides have the same relationship)?
    /// </summary>
    [ViewVariables]
    public float UnificationProbability = 1f;

    /// <summary>
    /// If the relationship isnt mutual, what are other possible relationships to give? If null, no relationship is assigned.
    /// </summary>
    [ViewVariables]
    public ProtoId<WeightedRandomPrototype>? SeperatePoolPrototype;

    public RelationshipContext()
    {
    }

    public RelationshipContext(string prototype, float probability)
    {
        RelationshipProbability = probability;
        PoolPrototype = prototype;
        UnificationProbability = 1f;
        SeperatePoolPrototype = null;
    }

    public RelationshipContext(string prototype, float probability, float unificationProbability, string? seperatePrototype)
    {
        RelationshipProbability = probability;
        PoolPrototype = prototype;
        UnificationProbability = unificationProbability;
        SeperatePoolPrototype = seperatePrototype;
    }
}
