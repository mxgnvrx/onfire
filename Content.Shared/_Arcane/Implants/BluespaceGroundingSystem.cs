// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.BlockTeleport;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Robust.Shared.Network;

namespace Content.Shared._Arcane.Implants;

public sealed class BluespaceGroundingSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<BluespaceGroundingComponent, TeleportAttemptEvent>(OnTeleportAttempt);
    }

    private void OnTeleportAttempt(Entity<BluespaceGroundingComponent> ent, ref TeleportAttemptEvent args)
    {
        args.Cancelled = true;
        _stun.TryUpdateParalyzeDuration(ent, ent.Comp.ParalyzeDuration);

        var msg = Loc.GetString("bluespace-grounding-teleport-blocked");
        if (args.Predicted)
            _popup.PopupClient(msg, ent, ent, PopupType.MediumCaution);
        else
            _popup.PopupEntity(msg, ent, ent, PopupType.MediumCaution);

        if (_net.IsServer)
            SpawnAtPosition(ent.Comp.SparksEffect, Transform(ent).Coordinates);
    }
}
