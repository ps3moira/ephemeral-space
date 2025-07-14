using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._ES.Evac.Ui;

[UsedImplicitly]
public sealed class ESEvacBeaconBui(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private ESEvacBeaconWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<ESEvacBeaconWindow>();
        _window.OpenCentered();
    }
}
