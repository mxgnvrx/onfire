// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Shitmed.Body.Organ;
using Content.Shared.Body.Events;
using Content.Shared.Body.Systems;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Components;
using Content.Shared.Prying.Components;
using Content.Shared.Standing;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Timing;

namespace Content.Server._Arcane.Augments;

public sealed class StrongArmSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<StrongArmComponent, OrganAddedToBodyEvent>(OnAddedToBody);
        SubscribeLocalEvent<StrongArmComponent, OrganRemovedFromBodyEvent>(OnRemovedFromBody);
        SubscribeLocalEvent<StrongArmComponent, OrganComponentsModifyEvent>(OnOrganModified);
        SubscribeLocalEvent<StrongArmUserComponent, MeleeHitEvent>(OnMeleeHit);
    }

    private void OnAddedToBody(Entity<StrongArmComponent> ent, ref OrganAddedToBodyEvent args)
    {
        Refresh(args.Body, null);
    }

    private void OnRemovedFromBody(Entity<StrongArmComponent> ent, ref OrganRemovedFromBodyEvent args)
    {
        Refresh(args.OldBody, ent);
    }

    private void OnOrganModified(Entity<StrongArmComponent> ent, ref OrganComponentsModifyEvent args)
    {
        Refresh(args.Body, args.Add ? null : ent);
    }

    private void Refresh(EntityUid body, EntityUid? ignored)
    {
        var arms = 0;
        var prySpeed = 1f;
        foreach (var arm in _body.GetBodyOrganEntityComps<StrongArmComponent>(body))
        {
            if (arm.Owner == ignored || !arm.Comp2.Enabled)
                continue;

            arms++;
            prySpeed = arm.Comp1.PairPrySpeedModifier;
        }

        if (arms == 0)
        {
            if (TryComp<StrongArmUserComponent>(body, out var oldUser) && oldUser.AddedPrying)
                RemComp<PryingComponent>(body);

            RemComp<StrongArmUserComponent>(body);
            return;
        }

        var user = EnsureComp<StrongArmUserComponent>(body);
        if (arms >= 2 && !HasComp<PryingComponent>(body))
        {
            var prying = AddComp<PryingComponent>(body);
            prying.PryPowered = true;
            prying.SpeedModifier = prySpeed;
            Dirty(body, prying);
            user.AddedPrying = true;
        }
        else if (arms < 2 && user.AddedPrying)
        {
            RemComp<PryingComponent>(body);
            user.AddedPrying = false;
        }
    }

    private void OnMeleeHit(Entity<StrongArmUserComponent> ent, ref MeleeHitEvent args)
    {
        if (!args.IsHit || args.Weapon != ent.Owner || args.HitEntities.Count != 1
            || !_hands.TryGetHand(ent.Owner, _hands.GetActiveHand(ent.Owner), out var hand))
            return;

        var target = args.HitEntities[0];
        if (target == ent.Owner || !HasComp<MobStateComponent>(target))
            return;

        StrongArmComponent? implant = null;
        foreach (var arm in _body.GetBodyOrganEntityComps<StrongArmComponent>(ent.Owner))
        {
            if (arm.Comp2.Enabled && arm.Comp1.Hand == hand.Value.Location)
                implant = arm.Comp1;
        }

        if (implant == null)
            return;

        var humanoid = HasComp<HumanoidAppearanceComponent>(target);
        if (humanoid && _timing.CurTime < implant.NextSlam)
            return;

        if (humanoid)
            implant.NextSlam = _timing.CurTime + implant.SlamCooldown;

        args.BonusDamage += args.BaseDamage * (implant.DamageMultiplier - 1f);

        if (_standing.IsDown(ent.Owner))
            return;

        var direction = _transform.GetWorldPosition(target) - _transform.GetWorldPosition(ent.Owner);
        if (direction.LengthSquared() > 0f)
            _throwing.TryThrow(target, direction.Normalized() * implant.ThrowDistance, implant.ThrowSpeed, ent.Owner);
    }
}
