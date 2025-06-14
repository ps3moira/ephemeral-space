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
    [ViewVariables, DataField]
    public EntityUid? Captain = null;

    /// <summary>
    /// Who are the crew aboard the ship? This includes the captain.
    /// </summary>
    [ViewVariables, DataField]
    public List<EntityUid> Crew = new ();

    /// <summary>
    /// How many souls are aboard this ship? This excludes the captain.
    /// </summary>
    [ViewVariables, DataField]
    public int CrewCount = 5;

    /// <summary>
    /// Which ship grid is this crew entity assigned to (if it spawned)?
    /// </summary>
    [ViewVariables, DataField]
    public EntityUid? ShipGrid = null;

    /// <summary>
    /// What map should the system spawn when spawning the crew?
    /// </summary>
    [ViewVariables, DataField]
    public ResPath MapPath = default!;
}
