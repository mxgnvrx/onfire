// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Atmos.EntitySystems;
using Content.Server.Movement.Systems;
using Content.Shared._Shitmed.Body.Organ;
using Content.Shared.Actions;
using Content.Shared.Atmos.Components;
using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.Body.Organ;
using Content.Shared.Gravity;
using Content.Shared.Movement.Components;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Content.Shared.Toggleable;
using Robust.Shared.Collections;
using Robust.Shared.Timing;

namespace Content.Server._Arcane.Augments;

public sealed class CyberThrustersSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly AtmosphereSystem _atmos = default!;
    [Dependency] private readonly GasTankSystem _gasTank = default!;
    [Dependency] private readonly JetpackSystem _jetpack = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<CyberThrustersComponent, OrganAddedToBodyEvent>(OnAddedToBody);
        SubscribeLocalEvent<CyberThrustersComponent, OrganRemovedFromBodyEvent>(OnRemovedFromBody);
        SubscribeLocalEvent<CyberThrustersComponent, OrganComponentsModifyEvent>(OnOrganModified);
        SubscribeLocalEvent<CyberThrustersComponent, ToggleActionEvent>(OnToggle);
    }

    private void OnAddedToBody(Entity<CyberThrustersComponent> ent, ref OrganAddedToBodyEvent args)
    {
        if (Comp<OrganComponent>(ent).Enabled)
            SetBodyEnabled(ent, args.Body, true);
    }

    private void OnRemovedFromBody(Entity<CyberThrustersComponent> ent, ref OrganRemovedFromBodyEvent args)
    {
        SetBodyEnabled(ent, args.OldBody, false);
    }

    private void OnOrganModified(Entity<CyberThrustersComponent> ent, ref OrganComponentsModifyEvent args)
    {
        SetBodyEnabled(ent, args.Body, args.Add);
    }

    private void SetBodyEnabled(Entity<CyberThrustersComponent> ent, EntityUid body, bool enabled)
    {
        if (enabled)
        {
            _actions.AddAction(body, ref ent.Comp.Action, ent.Comp.ToggleAction, ent);
            return;
        }

        if (TryComp<JetpackComponent>(ent, out var jetpack))
            _jetpack.SetEnabled(ent, jetpack, false, body);

        _actions.RemoveAction(ent.Comp.Action);
    }

    private void OnToggle(Entity<CyberThrustersComponent> ent, ref ToggleActionEvent args)
    {
        if (args.Handled || !TryComp<OrganComponent>(ent, out var organ)
            || !organ.Enabled || organ.Body != args.Performer
            || !TryComp<JetpackComponent>(ent, out var jetpack))
            return;

        args.Handled = true;
        var user = args.Performer;
        if (HasComp<ActiveJetpackComponent>(ent))
        {
            _jetpack.SetEnabled(ent, jetpack, false, user);
            _popup.PopupEntity(Loc.GetString("cyber-thrusters-off"), user, user);
            return;
        }

        // Same restrictions as a regular jetpack toggle.
        if (Transform(user).GridUid is { } grid && HasComp<GravityComponent>(grid))
        {
            _popup.PopupEntity(Loc.GetString("jetpack-no-station"), user, user);
            return;
        }

        if (_standing.IsDown(user))
        {
            _popup.PopupEntity(Loc.GetString("jetpack-is-down"), user, user);
            return;
        }

        if (!TryUsePropellant((ent, ent.Comp, jetpack), user, false))
        {
            _popup.PopupEntity(Loc.GetString("cyber-thrusters-no-propellant"), user, user);
            return;
        }

        _jetpack.SetEnabled(ent, jetpack, true, user);
        _popup.PopupEntity(Loc.GetString("cyber-thrusters-on"), user, user);
    }

    private bool TryUsePropellant(Entity<CyberThrustersComponent, JetpackComponent> ent, EntityUid body, bool consume)
    {
        var environment = _atmos.GetContainingMixture(body);
        if (environment != null && environment.Pressure > ent.Comp1.MinEnvironmentPressure)
            return true;

        if (!TryComp<InternalsComponent>(body, out var internals)
            || !TryComp<GasTankComponent>(internals.GasTankEntity, out var tank))
            return false;

        if (!consume)
            return tank.Air.TotalMoles >= ent.Comp2.MoleUsage;

        var used = _gasTank.RemoveAir((internals.GasTankEntity.Value, tank), ent.Comp2.MoleUsage);
        return used != null && MathHelper.CloseTo(used.TotalMoles, ent.Comp2.MoleUsage, ent.Comp2.MoleUsage / 100);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var toDisable = new ValueList<(EntityUid Uid, JetpackComponent Jetpack, EntityUid Body)>();
        var query = EntityQueryEnumerator<ActiveJetpackComponent, CyberThrustersComponent, JetpackComponent, OrganComponent>();
        while (query.MoveNext(out var uid, out var active, out var thrusters, out var jetpack, out var organ))
        {
            if (_timing.CurTime < active.TargetTime || organ.Body is not { } body)
                continue;

            active.TargetTime = _timing.CurTime + TimeSpan.FromSeconds(active.EffectCooldown);
            if (!TryUsePropellant((uid, thrusters, jetpack), body, true))
                toDisable.Add((uid, jetpack, body));
        }

        foreach (var (uid, jetpack, body) in toDisable)
        {
            _jetpack.SetEnabled(uid, jetpack, false, body);
        }
    }
}
