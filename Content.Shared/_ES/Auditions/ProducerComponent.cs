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
    [DataField]
    public List<EntityUid> Characters = new ();

    /// <summary>
    /// List of all active social groups.
    /// </summary>
    [DataField]
    public List<EntityUid> SocialGroups = new ();

    [DataField]
    public RelationshipContext CrewContext = new ("RelationshipPoolCrew", 0.75f);

    [DataField]
    public RelationshipContext CaptainContext = new ("RelationshipPoolCaptains", 0.5f);

    [DataField]
    public RelationshipContext IntercrewContext = new ("RelationshipPoolIntercrew", 0.1f);
}

/// <summary>
/// Configuration for integrating relationships.
/// </summary>
[Serializable, NetSerializable, DataDefinition]
public partial struct RelationshipContext
{
    /// <summary>
    /// List of possible relationships.
    /// </summary>
    [DataField]
    public ProtoId<WeightedRandomPrototype> PoolPrototype;

    /// <summary>
    /// How likely is a relationship to spark in this context?
    /// </summary>
    [DataField]
    public float RelationshipProbability = 0.5f;

    /// <summary>
    /// How likely is a relationship to be mutual (both sides have the same relationship)?
    /// </summary>
    [DataField]
    public float UnificationProbability = 1f;

    /// <summary>
    /// If the relationship isnt mutual, what are other possible relationships to give? If null, no relationship is assigned.
    /// </summary>
    [DataField]
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
