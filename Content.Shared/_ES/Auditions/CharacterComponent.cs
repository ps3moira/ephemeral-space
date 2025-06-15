using Content.Shared.Humanoid;
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
    public string Name = "Rain Miskovitch";

    [DataField, AutoNetworkedField]
    public int Age = 25;

    [DataField, AutoNetworkedField]
    public Gender Gender = Gender.Neuter;

    [DataField, AutoNetworkedField]
    public HumanoidCharacterAppearance Appearance = default!;

    [DataField, AutoNetworkedField]
    public DateTime DateOfBirth = new(0, 0, 0);

    [DataField, AutoNetworkedField]
    public Dictionary<string, ProtoId<RelationshipPrototype>> Relationships = new ();

    [DataField, AutoNetworkedField]
    public List<EntityUid> Memories = new ();
}
