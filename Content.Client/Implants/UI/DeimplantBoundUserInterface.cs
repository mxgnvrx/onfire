// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Robust.Client.UserInterface;

namespace Content.Client.Implants.UI;

public sealed class DeimplantBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private DeimplantChoiceWindow? _window;

    public DeimplantBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<DeimplantChoiceWindow>();

        _window.OnImplantChange += implant => SendMessage(new DeimplantChangeVerbMessage(implant));

        // Arcane-Start
        // The component state can arrive before the interface opens.
        if (EntMan.TryGetComponent<ImplanterComponent>(Owner, out var component))
            EntMan.System<ImplanterSystem>().UpdateDeimplantUi(Owner, component, this);
        // Arcane-End
    }
    
    public void UpdateState(Dictionary<string, string> implantList, string? implant)
    {
        if (_window != null)
        {
            _window.UpdateImplantList(implantList);
            _window.UpdateState(implant);
        }
    }
}
