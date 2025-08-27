using Robust.Shared.Audio;

namespace Content.Server._ES.Extras.Components;

/// <summary>
/// This is used for an in-game "Extra" that can be spoken to.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
[Access(typeof(ESExtraSpeechSystem))]
public sealed partial class ESExtraSpeakerComponent : Component
{
    /// <summary>
    /// The dialogue that this character speaks when interacted with, in order
    /// </summary>
    [DataField]
    public List<LocId> Dialogue = new();

    /// <summary>
    /// A counter indicating which element of <see cref="Dialogue"/> is to be spoken
    /// </summary>
    [DataField]
    public int DialogueIndex;

    /// <summary>
    /// The time at which regular speech can happen again
    /// </summary>
    [DataField, AutoPausedField]
    public TimeSpan NextCanSpeakTime;

    /// <summary>
    /// The time at which <see cref="DialogueIndex"/> is automatically reset to 0.
    /// </summary>
    [DataField, AutoPausedField]
    public TimeSpan DialogueResetTime;

    /// <summary>
    /// The delay between the last time this extra spoke and when <see cref="DialogueIndex"/> is reset.
    /// </summary>
    [DataField]
    public TimeSpan DialogueResetDelay = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Sound that plays when dialogue occurs.
    /// </summary>
    [DataField]
    public SoundSpecifier? SpeakSound;
}
