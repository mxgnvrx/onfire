// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Gateway.Components;
using Content.Server.Station.Systems;
using Content.Shared._Arcane.Implants;
using Content.Shared._Arcane.Teleportation;
using Content.Shared.Popups;

namespace Content.Server._Arcane.Implants;

public sealed class ExileImplantSystem : SharedExileImplantSystem
{
    [Dependency] private readonly StationSystem _station = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PortalTeleportAttemptEvent>(OnPortalTeleportAttempt);
    }

    private void OnPortalTeleportAttempt(ref PortalTeleportAttemptEvent args)
    {
        if (args.Cancelled
            || !HasComp<GatewayComponent>(args.Target)
            || _station.GetOwningStation(args.Target) == null
            || FindExile(args.Subject) is not { } exile)
            return;

        args.Cancelled = true;
        Popup.PopupEntity(Loc.GetString("exile-implant-gateway-blocked"), exile, exile, PopupType.MediumCaution);
    }

    // Covers exiles carried in containers or buckled to the entity going through.
    private EntityUid? FindExile(EntityUid uid)
    {
        if (HasComp<ExileImplantedComponent>(uid))
            return uid;

        var children = Transform(uid).ChildEnumerator;
        while (children.MoveNext(out var child))
        {
            if (FindExile(child) is { } exile)
                return exile;
        }

        return null;
    }
}
