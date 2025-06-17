using Content.Shared.Preferences;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Auditions;

/// <summary>
/// This is used for marking the character of components.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CharacterComponent : Component
{
    public string Name => Profile.Name;

    [DataField, AutoNetworkedField]
    public DateTime DateOfBirth;

    [DataField, AutoNetworkedField]
    public Dictionary<EntityUid, ProtoId<RelationshipPrototype>> Relationships = new ();

    [DataField, AutoNetworkedField]
    public ProtoId<BackgroundPrototype> Background;

    [DataField, AutoNetworkedField]
    public List<EntityUid> Memories = new ();

    [DataField, AutoNetworkedField]
    public HumanoidCharacterProfile Profile;
}
