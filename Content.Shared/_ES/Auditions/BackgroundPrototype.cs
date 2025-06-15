using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Auditions;

/// <summary>
/// This is a prototype for marking backgrounds
/// </summary>
[Prototype]
public sealed partial class BackgroundPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; } = default!;

    /// <summary>
    /// Name of this relationship.
    /// </summary>
    [DataField, ViewVariables]
    public LocId Name;

    /// <summary>
    /// Description of this relationship.
    /// </summary>
    [DataField, ViewVariables]
    public LocId Description;
}
