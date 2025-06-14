using Content.Shared.Humanoid;
using Robust.Shared.Enums;
using Robust.Shared.GameStates;

namespace Content.Shared._ES.Auditions;

/// <summary>
/// This is used for marking the character of components.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CharacterComponent : Component
{
    [ViewVariables, DataField]
    public string Name = "Rain Miskovitch";

    [ViewVariables, DataField]
    public int Age = 25;

    [ViewVariables, DataField]
    public Gender Gender = Gender.Neuter;

    [ViewVariables, DataField]
    public HumanoidCharacterAppearance Appearance = default!;

    [ViewVariables, DataField]
    public Dictionary<string, string> Relationships = new ();

    [ViewVariables, DataField]
    public List<EntityUid> Memories = new ();
}
