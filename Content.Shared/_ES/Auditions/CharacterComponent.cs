using Content.Shared.Humanoid;
using Robust.Shared.Enums;

namespace Content.Shared._ES.Auditions;

/// <summary>
/// This is used for marking the character of components.
/// </summary>
[RegisterComponent]
public sealed partial class CharacterComponent : Component
{
    [ViewVariables]
    public string Name = "Rain Miskovitch";

    [ViewVariables]
    public int Age = 25;

    [ViewVariables]
    public Gender Gender = Gender.Neuter;

    [ViewVariables]
    public HumanoidCharacterAppearance Appearance = default!;

    [ViewVariables]
    public Dictionary<string, string> Relationships = new ();

    [ViewVariables]
    public List<EntityUid> Memories = new ();
}
