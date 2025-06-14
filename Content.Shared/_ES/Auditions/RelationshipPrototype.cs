using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Auditions;

/// <summary>
/// This is a prototype for marking relationships
/// </summary>
[Prototype("relationship")]
public sealed partial class RelationshipPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; } = default!;

    [DataField, ViewVariables]
    public LocId Name;

    [DataField, ViewVariables]
    public Color Color;
}
