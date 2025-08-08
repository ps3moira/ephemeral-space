namespace Content.Server._ES.Arrivals.Components;

[RegisterComponent, AutoGenerateComponentPause, Access(typeof(ESArrivalsSystem))]
public sealed partial class ESArrivalsShuttleComponent : Component
{
    [DataField]
    public TimeSpan FlightDelay = TimeSpan.FromSeconds(120f);

    [DataField]
    public TimeSpan RestDelay = TimeSpan.FromSeconds(50f);

    [DataField, AutoPausedField]
    public TimeSpan TakeoffTime;

    [DataField]
    public EntityUid Station;
}
