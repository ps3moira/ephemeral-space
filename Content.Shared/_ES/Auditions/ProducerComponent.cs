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
    public List<EntityUid> Characters = new ();

    /// <summary>
    /// List of all active crew entities.
    /// </summary>
    public List<EntityUid> Crew = new ();
}
