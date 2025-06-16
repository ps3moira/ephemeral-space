using Content.Shared.Humanoid;
using Content.Shared.Preferences;
using Robust.Shared.Enums;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._ES.Auditions;

/// <summary>
/// This is used for marking the character of components.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CharacterComponent : Component
{
    [DataField, AutoNetworkedField]
    public string Name;

    [DataField, AutoNetworkedField]
    public int Age;

    [DataField, AutoNetworkedField]
    public Gender Gender = Gender.Neuter;

    [DataField, AutoNetworkedField]
    public HumanoidCharacterAppearance Appearance = default!;

    [DataField, AutoNetworkedField]
    public DateTime DateOfBirth;

    [DataField, AutoNetworkedField]
    public Dictionary<string, ProtoId<RelationshipPrototype>> Relationships = new ();

    [DataField, AutoNetworkedField]
    public ProtoId<BackgroundPrototype> Background;

    [DataField, AutoNetworkedField]
    public List<EntityUid> Memories = new ();

    [DataField, AutoNetworkedField]
    public HumanoidCharacterProfile Profile;
}
