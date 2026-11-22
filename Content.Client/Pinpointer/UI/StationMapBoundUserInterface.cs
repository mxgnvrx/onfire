// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Arcane.Implants;
using Content.Shared.Pinpointer;
using Robust.Client.UserInterface;

namespace Content.Client.Pinpointer.UI;

public sealed class StationMapBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private StationMapWindow? _window;

    public StationMapBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        // Arcane-Start
        if (IsOpened)
            return;

        // Arcane-End
        base.Open();
        EntityUid? gridUid = null;

        if (EntMan.TryGetComponent<StationMapComponent>(Owner, out var comp) && comp.TargetGrid != null)
        {
            gridUid = comp.TargetGrid;
        }
        else if (EntMan.TryGetComponent<TransformComponent>(Owner, out var xform))
        {
            gridUid = xform.GridUid;
        }

        _window = this.CreateWindow<StationMapWindow>();
        _window.Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName;

        string stationName = string.Empty;
        if(EntMan.TryGetComponent<MetaDataComponent>(gridUid, out var gridMetaData))
        {
            stationName = gridMetaData.EntityName;
        }

        if (comp != null && comp.ShowLocation)
            _window.Set(stationName, gridUid, Owner);
        else
            _window.Set(stationName, gridUid, null);
    }

    // Arcane-Start
    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        base.ReceiveMessage(message);
        if (_window == null || message is not TacticalMapUpdateMessage update)
            return;

        EntMan.TryGetEntity(update.Grid, out var grid);
        var stationName = EntMan.TryGetComponent<MetaDataComponent>(grid, out var metadata) ? metadata.EntityName : string.Empty;
        _window.SetTacticalMarkers(stationName, grid, Owner, update.Allies);
    }
    // Arcane-End
}
