// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Explosion.Components;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Trigger.Components;
using Content.Shared.Trigger.Components.Effects;
using Content.Shared.Trigger.Systems;
using Robust.Shared.Containers;

namespace Content.Shared._Arcane.Implants;

public sealed class TacticalDeniabilitySystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly TriggerSystem _trigger = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<TacticalDeniabilityImplantComponent, ImplantImplantedEvent>(OnImplanted);
        SubscribeLocalEvent<TacticalDeniabilityComponent, EntInsertedIntoContainerMessage>(OnImplantInserted);
        SubscribeLocalEvent<TacticalDeniabilityComponent, UpdateMobStateEvent>(OnUpdateMobState, after: [typeof(MobThresholdSystem)]);
    }

    private void OnImplanted(Entity<TacticalDeniabilityImplantComponent> ent, ref ImplantImplantedEvent args)
    {
        if (!_container.TryGetContainer(args.Implanted, ImplanterComponent.ImplantSlotId, out var implants))
            return;

        foreach (var implant in implants.ContainedEntities)
        {
            if (implant != ent.Owner)
                Absorb(ent, implant);
        }
    }

    private void OnImplantInserted(Entity<TacticalDeniabilityComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != ImplanterComponent.ImplantSlotId
            || GetImplant(ent) is not { } implant
            || args.Entity == implant.Owner)
            return;

        Absorb(implant, args.Entity);
    }

    private void OnUpdateMobState(Entity<TacticalDeniabilityComponent> ent, ref UpdateMobStateEvent args)
    {
        if (args.State is not (MobState.SoftCritical or MobState.HardCritical))
            return;

        args.State = MobState.Alive;
        if (GetImplant(ent) is { } implant)
            _trigger.Trigger(implant, ent, TriggerSystem.DefaultTriggerKey);
    }

    private void Absorb(Entity<TacticalDeniabilityImplantComponent> ent, EntityUid other)
    {
        if (!TryComp<ExplosiveComponent>(other, out var explosive)
            || !TryComp<ExplosionOnTriggerComponent>(ent, out var explosion))
            return;

        explosion.TotalIntensity += explosive.TotalIntensity;
        explosion.MaxTileIntensity = MathF.Max(explosion.MaxTileIntensity, explosive.MaxIntensity);
        explosion.CanCreateVacuum |= explosive.CanCreateVacuum;
        Dirty(ent, explosion);

        if (TryComp<TimerTriggerComponent>(ent, out var timer))
        {
            var delay = timer.Delay + (CompOrNull<TimerTriggerComponent>(other)?.Delay ?? TimeSpan.Zero);
            timer.Delay = delay < ent.Comp.MaxDelay ? delay : ent.Comp.MaxDelay;
            Dirty(ent, timer);
        }

        PredictedQueueDel(other);
    }

    private Entity<TacticalDeniabilityImplantComponent>? GetImplant(EntityUid mob)
    {
        if (!_container.TryGetContainer(mob, ImplanterComponent.ImplantSlotId, out var implants))
            return null;

        foreach (var implant in implants.ContainedEntities)
        {
            if (TryComp<TacticalDeniabilityImplantComponent>(implant, out var comp))
                return (implant, comp);
        }

        return null;
    }
}
