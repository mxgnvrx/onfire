// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Projectiles;

namespace Content.Server._Arcane.Implants;

public sealed class ImplantOnHitSystem : EntitySystem
{
    [Dependency] private readonly SharedSubdermalImplantSystem _implant = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ImplantOnHitComponent, ProjectileHitEvent>(OnProjectileHit);
    }

    private void OnProjectileHit(Entity<ImplantOnHitComponent> ent, ref ProjectileHitEvent args)
    {
        if (!HasComp<MobStateComponent>(args.Target))
            return;

        if (TryComp<ImplantedComponent>(args.Target, out var implanted))
        {
            foreach (var implant in implanted.ImplantContainer.ContainedEntities)
            {
                if (Prototype(implant)?.ID == ent.Comp.Implant.Id)
                    return;
            }
        }

        _implant.AddImplant(args.Target, ent.Comp.Implant);
    }
}
