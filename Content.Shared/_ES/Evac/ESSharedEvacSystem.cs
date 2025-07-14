using Content.Shared._ES.CCVar;
using Content.Shared._ES.Evac.Components;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;

namespace Content.Shared._ES.Evac;

public abstract class ESSharedEvacSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] protected readonly IGameTiming Timing = default!;

    protected float BeaconPercentage;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESEvacBeaconComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ESEvacBeaconComponent, ComponentShutdown>(OnShutdown);

        _config.OnValueChanged(ESCVars.ESEvacBeaconPercentage, val => BeaconPercentage = val, true);
    }

    private void OnMapInit(Entity<ESEvacBeaconComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextToggleTime = Timing.CurTime + ent.Comp.ToggleDelay;
        Dirty(ent);
    }

    private void OnShutdown(Entity<ESEvacBeaconComponent> ent, ref ComponentShutdown args)
    {
        UpdateEvacStatus();
    }

    public void SetBeaconEnabled(Entity<ESEvacBeaconComponent> ent, bool value)
    {
        if (ent.Comp.Enabled == value)
            return;
        ent.Comp.Enabled = value;
        Dirty(ent);
        UpdateEvacStatus();
    }

    protected virtual void UpdateEvacStatus()
    {

    }

}
