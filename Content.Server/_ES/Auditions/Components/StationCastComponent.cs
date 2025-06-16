namespace Content.Server._ES.Auditions.Components;

/// <summary>
/// This is used for holding an associated cast component for a station.
/// </summary>
[RegisterComponent]
public sealed partial class StationCastComponent : Component
{
    [DataField]
    public EntityUid Crew;
}
