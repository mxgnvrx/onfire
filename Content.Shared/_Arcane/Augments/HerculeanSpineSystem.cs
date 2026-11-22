// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.Grab;
using Content.Shared.Gravity;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Network;

namespace Content.Shared._Arcane.Augments;

public sealed class HerculeanSpineSystem : EntitySystem
{
    [Dependency] private readonly SharedGravitySystem _gravity = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<HerculeanSpineComponent, GetStandUpTimeEvent>(OnGetStandUpTime);
        SubscribeLocalEvent<HerculeanSpineComponent, BeforeThrowEvent>(OnBeforeThrow);
        SubscribeLocalEvent<HerculeanSpineComponent, GetUserMeleeDamageEvent>(OnGetMeleeDamage);
        SubscribeLocalEvent<HerculeanSpineComponent, IsWeightlessEvent>(OnIsWeightless);
        SubscribeLocalEvent<HerculeanSpineComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<HerculeanSpineComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<RaiseGrabModifierEventEvent>(OnGrabModifier);
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

    private void OnGetMeleeDamage(Entity<HerculeanSpineComponent> ent, ref GetUserMeleeDamageEvent args)
    {
        if (args.Weapon == ent.Owner && ent.Comp.UnarmedDamageBonus > 0)
            args.Damage.DamageDict["Blunt"] = args.Damage.DamageDict.GetValueOrDefault("Blunt") + ent.Comp.UnarmedDamageBonus;
    }

    private void OnGrabModifier(ref RaiseGrabModifierEventEvent args)
    {
        if (TryComp<HerculeanSpineComponent>(args.User, out var spine))
            args.Multiplier *= spine.GrabEscapeMultiplier;
    }

    private void OnIsWeightless(Entity<HerculeanSpineComponent> ent, ref IsWeightlessEvent args)
    {
        if (args.Handled || !ent.Comp.PersonalGravity || ent.Comp.LifeStage >= ComponentLifeStage.Stopping
            || Transform(ent).GridUid == null)
            return;

        args.IsWeightless = false;
        args.Handled = true;
    }

    private void OnStartup(Entity<HerculeanSpineComponent> ent, ref ComponentStartup args)
    {
        if (_net.IsServer && ent.Comp.PersonalGravity && HasComp<GravityAffectedComponent>(ent))
            _gravity.RefreshWeightless(ent.Owner);
    }

    private void OnShutdown(Entity<HerculeanSpineComponent> ent, ref ComponentShutdown args)
    {
        if (_net.IsServer && ent.Comp.PersonalGravity && !TerminatingOrDeleted(ent) && HasComp<GravityAffectedComponent>(ent))
            _gravity.RefreshWeightless(ent.Owner);
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
