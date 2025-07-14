using Robust.Shared.GameStates;

namespace Content.Shared._ES.Evac.Components;

/// <summary>
/// Tracks a beacon used for coordinating an evac shuttle call.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ESSharedEvacSystem))]
public sealed partial class ESEvacBeaconComponent : Component
{
    /// <summary>
    /// Whether the beacon is currently active
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Enabled;

    /// <summary>
    /// The time at which the beacon can be toggled again.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan NextToggleTime;

    /// <summary>
    /// The min delay for when the beacon can be toggled.
    /// </summary>
    [DataField]
    public TimeSpan ToggleDelay = TimeSpan.FromMinutes(2.5f);
}
