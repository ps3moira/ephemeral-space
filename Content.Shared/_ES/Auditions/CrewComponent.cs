using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared._ES.Auditions;

/// <summary>
/// This is used for documenting crews.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CrewComponent : Component
{
    /// <summary>
    /// Who's the captain of this ship?
    /// </summary>
    [DataField]
    public EntityUid? Captain = null;

    /// <summary>
    /// Who are the crew aboard the ship? This includes the captain.
    /// </summary>
    [DataField]
    public List<EntityUid> Crew = new ();

    /// <summary>
    /// How many souls are aboard this ship? This excludes the captain.
    /// </summary>
    [DataField]
    public int CrewCount = 5;
}
