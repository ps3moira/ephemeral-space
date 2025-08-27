using System.Diagnostics;
using Content.Server._ES.Extras.Components;
using Content.Server.Chat.Systems;
using Content.Shared.Interaction;
using Robust.Server.Audio;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._ES.Extras;

public sealed class ESExtraSpeechSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly ChatSystem _chat = default!;

    // This is only meant to be like an anti-spam system, so it doesn't have to
    // really reflect how long it would take a person to read the dialogue.
    // It only has to serve as a *minimum* value.
    public const float DelaySecondPerWords = 0.14f;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESExtraSpeakerComponent, ActivateInWorldEvent>(OnActivateInWorld);
    }

    private void OnActivateInWorld(Entity<ESExtraSpeakerComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = TrySpeakDialogue(ent);
    }

    /// <summary>
    /// Attempts to speak the current dialogue, returning false if unable to.
    /// </summary>
    public bool TrySpeakDialogue(Entity<ESExtraSpeakerComponent> ent)
    {
        if (!CanSpeakDialogue(ent))
            return false;

        SpeakDialogue(ent);
        return true;
    }

    /// <summary>
    /// Returns if the current entity is capable of speaking.
    /// </summary>
    public bool CanSpeakDialogue(Entity<ESExtraSpeakerComponent> ent)
    {
        return _timing.CurTime > ent.Comp.NextCanSpeakTime;
    }

    private void SpeakDialogue(Entity<ESExtraSpeakerComponent> ent)
    {
        // Send the current dialogue line into chat.
        Debug.Assert(ent.Comp.Dialogue.TryGetValue(ent.Comp.DialogueIndex, out var dialogueId));
        var message = Loc.GetString(dialogueId);
        _chat.TrySendInGameICMessage(ent, message, InGameICChatType.Speak, ChatTransmitRange.GhostRangeLimit, hideLog: true, ignoreActionBlocker: true);

        // Play talk sound effect
        // TODO: associate this with message length
        _audio.PlayPvs(ent.Comp.SpeakSound, ent, ent.Comp.SpeakSound?.Params.WithVariation(0.125f));

        // Prevent sending next dialogue until the player has approximately read it (anti-spam)
        // Also refresh the counter for resetting the dialogue index.
        var wordCount = message.Split(" ").Length; // Not the most efficient way of doing this, but good enough
        var speakLength = TimeSpan.FromSeconds(wordCount * DelaySecondPerWords);
        ent.Comp.NextCanSpeakTime = _timing.CurTime + speakLength;
        ent.Comp.DialogueResetTime = _timing.CurTime + speakLength + ent.Comp.DialogueResetDelay;

        // Increment index so we read the next line
        SetDialogueIndex(ent, ent.Comp.DialogueIndex + 1);
    }

    /// <summary>
    /// Sets the dialogue index to a certain value, ensuring that the new value is always valid.
    /// </summary>
    public void SetDialogueIndex(Entity<ESExtraSpeakerComponent> ent, int index)
    {
        Debug.Assert(ent.Comp.Dialogue.Count > 0); // This will break if no dialogue is present.
        ent.Comp.DialogueIndex = index % ent.Comp.Dialogue.Count;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ESExtraSpeakerComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.DialogueIndex == 0)
                continue;
            if (_timing.CurTime < comp.DialogueResetTime)
                continue;
            SetDialogueIndex((uid, comp), 0);
        }
    }
}
