using Content.Shared._Arcane.Body.Organs;
using Content.Shared._Shitmed.Body.Organ;
using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.Body.Organ;
using Content.Shared.Body.Systems;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Robust.Shared.Timing;

namespace Content.Server._Arcane.Body.Organs;

public sealed class CyberneticHeartSystem : EntitySystem
{
    [Dependency] private readonly SharedBloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<CyberneticHeartBloodComponent, BloodstreamRefreshEvent>(OnBloodstreamRefresh);
        SubscribeLocalEvent<CyberneticHeartComponent, OrganAddedToBodyEvent>(OnAddedToBody);
        SubscribeLocalEvent<CyberneticHeartComponent, OrganRemovedFromBodyEvent>(OnRemovedFromBody);
        SubscribeLocalEvent<CyberneticHeartComponent, OrganComponentsModifyEvent>(OnOrganEffectsChanged);
        SubscribeLocalEvent<CyberneticHeartComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnAddedToBody(Entity<CyberneticHeartComponent> ent, ref OrganAddedToBodyEvent args)
    {
        if (TryComp<OrganComponent>(ent, out var organ) && organ.Enabled)
            AddBloodEffect(args.Body, ent);
    }

    private void OnRemovedFromBody(Entity<CyberneticHeartComponent> ent, ref OrganRemovedFromBodyEvent args)
    {
        RemoveBloodEffect(args.OldBody, ent);
        StopStabilization(ent.Comp);
    }

    private void OnOrganEffectsChanged(Entity<CyberneticHeartComponent> ent, ref OrganComponentsModifyEvent args)
    {
        if (args.Add)
        {
            AddBloodEffect(args.Body, ent);
            return;
        }

        RemoveBloodEffect(args.Body, ent);
        StopStabilization(ent.Comp);
    }

    private void AddBloodEffect(EntityUid body, EntityUid implant)
    {
        if (!TerminatingOrDeleted(body) && !HasComp<CyberneticHeartBloodComponent>(body))
            AddComp<CyberneticHeartBloodComponent>(body).Implant = implant;
    }

    private void RemoveBloodEffect(EntityUid body, EntityUid implant)
    {
        if (TerminatingOrDeleted(body)
            || !TryComp<CyberneticHeartBloodComponent>(body, out var effect)
            || effect.Implant != implant)
            return;

        foreach (var other in _body.GetBodyOrganEntityComps<CyberneticHeartComponent>(body))
        {
            if (other.Owner == implant || TerminatingOrDeleted(other) || !other.Comp2.Enabled || other.Comp2.Body != body)
                continue;

            effect.Implant = other;
            return;
        }

        RemComp<CyberneticHeartBloodComponent>(body);
    }

    private void OnBloodstreamRefresh(Entity<CyberneticHeartBloodComponent> ent, ref BloodstreamRefreshEvent args)
    {
        if (!TryComp<BloodstreamComponent>(ent, out var bloodstream)
            || _bloodstream.GetBloodLevel((ent.Owner, bloodstream)) >= 1)
            return;

        _bloodstream.TryModifyBloodLevel((ent.Owner, bloodstream), bloodstream.BloodRefreshAmount * ent.Comp.ExtraBloodMultiplier);
        _bloodstream.TryModifyBleedAmount((ent.Owner, bloodstream), -bloodstream.BleedReductionAmount * ent.Comp.ExtraBleedReductionMultiplier);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<CyberneticHeartComponent, OrganComponent>();
        while (query.MoveNext(out _, out var heart, out var organ))
        {
            if (heart.StabilizedBody != null &&
                (_timing.CurTime >= heart.StabilizationEnd || !organ.Enabled || organ.Body != heart.StabilizedBody))
                StopStabilization(heart);

            if (!organ.Enabled || organ.Body is not {} body || heart.StabilizedBody != null
                || _timing.CurTime < heart.NextStabilization || !_mobState.IsSoftCritical(body))
                continue;

            EnsureComp<CyberneticHeartStabilizedComponent>(body);
            heart.StabilizedBody = body;
            heart.StabilizationEnd = _timing.CurTime + heart.StabilizationDuration;
            heart.NextStabilization = _timing.CurTime + heart.StabilizationCooldown;
            _movement.RefreshMovementSpeedModifiers(body);
        }
    }

    private void OnShutdown(Entity<CyberneticHeartComponent> ent, ref ComponentShutdown args)
    {
        if (CompOrNull<OrganComponent>(ent)?.Body is {} body)
            RemoveBloodEffect(body, ent);

        StopStabilization(ent.Comp);
    }

    private void StopStabilization(CyberneticHeartComponent heart)
    {
        if (heart.StabilizedBody is not {} body)
            return;

        if (RemComp<CyberneticHeartStabilizedComponent>(body))
            _movement.RefreshMovementSpeedModifiers(body);

        heart.StabilizedBody = null;
    }
}
