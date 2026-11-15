// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.BlockTeleport;
using Content.Shared._Arcane.Teleportation;
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
        SubscribeLocalEvent<PortalTeleportAttemptEvent>(OnPortalTeleportAttempt);
    }

    private void OnTeleportAttempt(Entity<BluespaceGroundingComponent> ent, ref TeleportAttemptEvent args)
    {
        args.Cancelled = true;
        Block(ent, args.Predicted);
    }

    private void OnPortalTeleportAttempt(ref PortalTeleportAttemptEvent args)
    {
        if (args.Cancelled || FindGrounded(args.Subject) is not { } grounded)
            return;

        args.Cancelled = true;
        Block(grounded, false);
    }

    private void Block(Entity<BluespaceGroundingComponent> ent, bool predicted)
    {
        _stun.TryUpdateParalyzeDuration(ent, ent.Comp.ParalyzeDuration);

        var msg = Loc.GetString("bluespace-grounding-teleport-blocked");
        if (predicted)
            _popup.PopupClient(msg, ent, ent, PopupType.MediumCaution);
        else
            _popup.PopupEntity(msg, ent, ent, PopupType.MediumCaution);

        if (_net.IsServer)
            SpawnAtPosition(ent.Comp.SparksEffect, Transform(ent).Coordinates);
    }

    // Covers grounded entities carried in containers or buckled to the entity going through.
    private Entity<BluespaceGroundingComponent>? FindGrounded(EntityUid uid)
    {
        if (TryComp<BluespaceGroundingComponent>(uid, out var grounding))
            return (uid, grounding);

        var children = Transform(uid).ChildEnumerator;
        while (children.MoveNext(out var child))
        {
            if (FindGrounded(child) is { } grounded)
                return grounded;
        }

        return null;
    }
}
