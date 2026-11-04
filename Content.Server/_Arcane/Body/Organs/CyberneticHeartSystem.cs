using Content.Shared._Arcane.Body.Organs;
using Content.Shared._Orion.Mobs.Critical;
using Content.Shared.Body.Components;
using Content.Shared.Body.Organ;
using Content.Shared.Body.Systems;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Robust.Shared.Timing;

namespace Content.Server._Arcane.Body.Organs;

public sealed class CyberneticHeartSystem : EntitySystem
{
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly SharedBloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<BloodstreamComponent, BloodstreamRefreshEvent>(OnBloodstreamRefresh);
        SubscribeLocalEvent<CyberneticHeartComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnBloodstreamRefresh(Entity<BloodstreamComponent> ent, ref BloodstreamRefreshEvent args)
    {
        if (_bloodstream.GetBloodLevel(ent.AsNullable()) >= 1)
            return;

        foreach (var (uid, organ) in _body.GetBodyOrgans(ent))
        {
            if (!organ.Enabled || !TryComp<CyberneticHeartComponent>(uid, out var heart))
                continue;

            _bloodstream.TryModifyBloodLevel(ent.AsNullable(), ent.Comp.BloodRefreshAmount * heart.ExtraBloodMultiplier);
            _bloodstream.TryModifyBleedAmount(ent.AsNullable(), -ent.Comp.BleedReductionAmount * heart.ExtraBleedReductionMultiplier);
        }
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

            heart.AddedMovement = !HasComp<CritStateMovementComponent>(body);
            var movement = EnsureComp<CritStateMovementComponent>(body);
            heart.PreviousWalkModifier = movement.SoftCritWalkModifier;
            heart.PreviousSprintModifier = movement.SoftCritSprintModifier;
            movement.SoftCritWalkModifier = 1;
            movement.SoftCritSprintModifier = 1;
            heart.StabilizedBody = body;
            heart.StabilizationEnd = _timing.CurTime + heart.StabilizationDuration;
            heart.NextStabilization = _timing.CurTime + heart.StabilizationCooldown;
            Dirty(body, movement);
            _movement.RefreshMovementSpeedModifiers(body);
        }
    }

    private void OnShutdown(Entity<CyberneticHeartComponent> ent, ref ComponentShutdown args)
    {
        StopStabilization(ent.Comp);
    }

    private void StopStabilization(CyberneticHeartComponent heart)
    {
        if (heart.StabilizedBody is not {} body)
            return;

        if (TryComp<CritStateMovementComponent>(body, out var movement))
        {
            movement.SoftCritWalkModifier = heart.PreviousWalkModifier;
            movement.SoftCritSprintModifier = heart.PreviousSprintModifier;
            Dirty(body, movement);
            if (heart.AddedMovement)
                RemComp<CritStateMovementComponent>(body);
            _movement.RefreshMovementSpeedModifiers(body);
        }

        heart.StabilizedBody = null;
    }
}
