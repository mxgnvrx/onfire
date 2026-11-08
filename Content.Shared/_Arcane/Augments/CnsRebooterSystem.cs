// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Shitmed.Body.Organ;
using Content.Shared.Body.Organ;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.StatusEffectNew;
using Content.Shared.Stunnable;
using Robust.Shared.Timing;

namespace Content.Shared._Arcane.Augments;

public sealed class CnsRebooterSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedStaminaSystem _stamina = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<CnsRebooterComponent, OrganComponentsModifyEvent>(OnOrganModified);
        SubscribeLocalEvent<CnsRebooterUserComponent, StunnedEvent>(OnStunned);
        SubscribeLocalEvent<CnsRebooterUserComponent, KnockedDownEvent>(OnKnockedDown);
        SubscribeLocalEvent<CnsRebooterUserComponent, KnockDownAttemptEvent>(OnKnockdownAttempt);
        SubscribeLocalEvent<CnsRebooterUserComponent, BeforeStatusEffectAddedEvent>(OnBeforeStatus);
        SubscribeLocalEvent<CnsRebooterUserComponent, ModifySlowOnDamageSpeedEvent>(OnModifySlowdown);
    }

    private void OnOrganModified(Entity<CnsRebooterComponent> ent, ref OrganComponentsModifyEvent args)
    {
        if (_timing.ApplyingState)
            return;

        if (args.Add)
        {
            var user = EnsureComp<CnsRebooterUserComponent>(args.Body);
            user.Implant = ent;
            Dirty(args.Body, user);
        }
        else
        {
            ent.Comp.RebootAt = null;
            ent.Comp.ProtectedUntil = null;
            Dirty(ent);
            if (TryComp<CnsRebooterUserComponent>(args.Body, out var user) && user.Implant == ent.Owner)
                RemComp<CnsRebooterUserComponent>(args.Body);
            _movement.RefreshMovementSpeedModifiers(args.Body);
        }
    }

    private void OnStunned(Entity<CnsRebooterUserComponent> ent, ref StunnedEvent args)
    {
        ScheduleReboot(ent);
    }

    private void OnKnockedDown(Entity<CnsRebooterUserComponent> ent, ref KnockedDownEvent args)
    {
        ScheduleReboot(ent);
    }

    private void ScheduleReboot(Entity<CnsRebooterUserComponent> ent)
    {
        if (_timing.ApplyingState
            || !TryComp<CnsRebooterComponent>(ent.Comp.Implant, out var implant))
            return;

        implant.RebootAt = _timing.CurTime + implant.RebootDelay;
        Dirty(ent.Comp.Implant, implant);
    }

    private bool IsProtected(Entity<CnsRebooterUserComponent> ent)
    {
        return TryComp<CnsRebooterComponent>(ent.Comp.Implant, out var implant)
            && _timing.CurTime < implant.ProtectedUntil;
    }

    private void OnKnockdownAttempt(Entity<CnsRebooterUserComponent> ent, ref KnockDownAttemptEvent args)
    {
        if (IsProtected(ent))
            args.Cancelled = true;
    }

    private void OnBeforeStatus(Entity<CnsRebooterUserComponent> ent, ref BeforeStatusEffectAddedEvent args)
    {
        if (args.Effect == SharedStunSystem.StunId && IsProtected(ent))
            args.Cancelled = true;
    }

    private void OnModifySlowdown(Entity<CnsRebooterUserComponent> ent, ref ModifySlowOnDamageSpeedEvent args)
    {
        if (IsProtected(ent))
            args.Speed = 1f;
    }

    public override void Update(float frameTime)
    {
        if (_timing.ApplyingState)
            return;

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<CnsRebooterComponent, OrganComponent>();
        while (query.MoveNext(out var uid, out var implant, out var organ))
        {
            if (!organ.Enabled || organ.Body is not {} body || IsPaused(uid))
                continue;

            if (implant.CooldownUntil is {} readyAt && now >= readyAt)
            {
                implant.CooldownUntil = null;
                Dirty(uid, implant);
                _popup.PopupClient(Loc.GetString("cns-rebooter-ready"), body, body);
            }

            if (implant.ProtectedUntil is {} protectedUntil && now >= protectedUntil)
            {
                implant.ProtectedUntil = null;
                RestoreStamina(body);
                _movement.RefreshMovementSpeedModifiers(body);
                Dirty(uid, implant);
            }

            if (implant.RebootAt is not {} rebootAt || now < rebootAt)
                continue;

            implant.RebootAt = null;
            if (implant.CooldownUntil is {} cooldownUntil && now < cooldownUntil)
            {
                Dirty(uid, implant);
                continue;
            }

            implant.ProtectedUntil = now + implant.ProtectionDuration;
            implant.CooldownUntil = now + implant.Cooldown;
            Dirty(uid, implant);
            _status.TryRemoveStatusEffect(body, SharedStunSystem.StunId);
            _stun.SetKnockdownTime(body, TimeSpan.Zero);
            _stun.SetAutoStand(body, true);
            RestoreStamina(body);
            _movement.RefreshMovementSpeedModifiers(body);
            _popup.PopupClient(Loc.GetString("cns-rebooter-activated"), body, body);
        }
    }

    private void RestoreStamina(EntityUid body)
    {
        if (!TryComp<StaminaComponent>(body, out var stamina))
            return;

        _stamina.ExitStamCrit(body, stamina);
        _stamina.TakeStaminaDamage(body, -stamina.StaminaDamage, stamina, visual: false, ignoreResist: true);
    }
}
