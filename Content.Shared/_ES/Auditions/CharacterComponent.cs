using Content.Shared.Humanoid;
using Robust.Shared.Enums;

namespace Content.Shared._ES.Auditions;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class CharacterComponent : Component
{
    public string Name = "Rain Miskovitch";
    public int Age = 25;
    public Gender Gender = Gender.Neuter;
    public HumanoidCharacterAppearance Appearance = default!;

    public Dictionary<string, string> Relationships = new ();
    public List<EntityUid> Memories = new ();
}
