// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Stunnable;
using Content.Shared.Throwing;

namespace Content.Shared._Arcane.Augments;

public sealed class HerculeanSpineSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<HerculeanSpineComponent, GetStandUpTimeEvent>(OnGetStandUpTime);
        SubscribeLocalEvent<HerculeanSpineComponent, BeforeThrowEvent>(OnBeforeThrow);
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
