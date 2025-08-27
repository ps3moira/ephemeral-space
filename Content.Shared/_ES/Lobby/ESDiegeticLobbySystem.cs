using Content.Shared._ES.Lobby.Components;
using Content.Shared.Buckle.Components;
using Content.Shared.Roles;
using Robust.Shared.Physics.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._ES.Lobby;

// see client/server
public abstract class ESSharedDiegeticLobbySystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESReadyTriggerMarkerComponent, StartCollideEvent>(OnTriggerCollided);
        SubscribeLocalEvent<ESTheatergoerMarkerComponent, UnbuckledEvent>(OnTheatergoerUnbuckled);
    }

    protected abstract void OnTheatergoerUnbuckled(Entity<ESTheatergoerMarkerComponent> ent, ref UnbuckledEvent args);

    protected abstract void OnTriggerCollided(Entity<ESReadyTriggerMarkerComponent> ent, ref StartCollideEvent args);
}

[Serializable, NetSerializable]
public sealed class ESUpdatePlayerReadiedJobCounts(Dictionary<ProtoId<JobPrototype>, int> jobs) : EntityEventArgs
{
    public Dictionary<ProtoId<JobPrototype>, int> ReadiedJobCounts = jobs;
}
