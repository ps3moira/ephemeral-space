namespace Content.Shared._ES.Auditions;

/// <summary>
/// This is the cast component placed onto the cast entity.
/// </summary>
[RegisterComponent]
public sealed partial class CastComponent : Component
{
    /// <summary>
    /// All the characters in the cast.
    /// </summary>
    public List<EntityUid>? Characters = null;

    /// <summary>
    /// List of all active crew entities.
    /// </summary>
    public List<EntityUid>? Crew = null;
}
