// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Shitmed.Body.Organ;
using Content.Shared.Body.Events;
using Content.Shared.Body.Organ;
using Content.Shared.Body.Systems;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Robust.Shared.Timing;

namespace Content.Shared._Arcane.Augments;

public sealed class HerculeanSpineSystem : EntitySystem
{
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<HerculeanSpineComponent, GetStandUpTimeEvent>(OnGetStandUpTime);
        SubscribeLocalEvent<HerculeanSpineComponent, BeforeThrowEvent>(OnBeforeThrow);
        SubscribeLocalEvent<HerculeanSpineImplantComponent, OrganAddedToBodyEvent>(OnAddedToBody);
        SubscribeLocalEvent<HerculeanSpineImplantComponent, OrganRemovedFromBodyEvent>(OnRemovedFromBody);
        SubscribeLocalEvent<HerculeanSpineImplantComponent, OrganComponentsModifyEvent>(OnOrganEffectsChanged);
        SubscribeLocalEvent<HerculeanSpineImplantComponent, ComponentShutdown>(OnImplantShutdown);
    }

    private void OnAddedToBody(Entity<HerculeanSpineImplantComponent> ent, ref OrganAddedToBodyEvent args)
    {
        if (TryComp<OrganComponent>(ent, out var organ) && organ.Enabled)
            AddSpineEffect(args.Body, ent);
    }

    private void OnRemovedFromBody(Entity<HerculeanSpineImplantComponent> ent, ref OrganRemovedFromBodyEvent args)
    {
        RemoveSpineEffect(args.OldBody, ent);
    }

    private void OnOrganEffectsChanged(Entity<HerculeanSpineImplantComponent> ent, ref OrganComponentsModifyEvent args)
    {
        if (args.Add)
            AddSpineEffect(args.Body, ent);
        else
            RemoveSpineEffect(args.Body, ent);
    }

    private void OnImplantShutdown(Entity<HerculeanSpineImplantComponent> ent, ref ComponentShutdown args)
    {
        if (CompOrNull<OrganComponent>(ent)?.Body is {} body)
            RemoveSpineEffect(body, ent);
    }

    private void AddSpineEffect(EntityUid body, EntityUid implant)
    {
        if (_timing.ApplyingState || TerminatingOrDeleted(body) || HasComp<HerculeanSpineComponent>(body))
            return;

        var effect = AddComp<HerculeanSpineComponent>(body);
        effect.Implant = implant;
        Dirty(body, effect);
    }

    private void RemoveSpineEffect(EntityUid body, EntityUid implant)
    {
        if (_timing.ApplyingState || TerminatingOrDeleted(body)
            || !TryComp<HerculeanSpineComponent>(body, out var effect)
            || effect.Implant != implant)
            return;

        foreach (var other in _body.GetBodyOrganEntityComps<HerculeanSpineImplantComponent>(body))
        {
            if (other.Owner == implant || TerminatingOrDeleted(other) || !other.Comp2.Enabled || other.Comp2.Body != body)
                continue;

            effect.Implant = other;
            Dirty(body, effect);
            return;
        }

        RemComp<HerculeanSpineComponent>(body);
    }

    private void OnGetStandUpTime(Entity<HerculeanSpineComponent> ent, ref GetStandUpTimeEvent args)
    {
        args.DoAfterTime *= ent.Comp.AthleticsMultiplier;
    }

    private void OnBeforeThrow(Entity<HerculeanSpineComponent> ent, ref BeforeThrowEvent args)
    {
        if (args.PlayerUid == ent.Owner)
            args.ThrowSpeed *= ent.Comp.ThrowSpeedMultiplier;
    }

    public float GetAthleticsMultiplier(EntityUid uid)
    {
        return CompOrNull<HerculeanSpineComponent>(uid)?.AthleticsMultiplier ?? 1f;
    }

    public float GetThrowRangeBonus(EntityUid uid)
    {
        return CompOrNull<HerculeanSpineComponent>(uid)?.ThrowRangeBonus ?? 0f;
    }
}
