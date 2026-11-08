// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server._Shitmed.Cybernetics;
using Content.Shared.Body.Organ;
using Content.Shared.Emp;
using Content.Shared.Stunnable;

namespace Content.Server._Arcane.Augments;

public sealed class BrainImplantSystem : EntitySystem
{
    [Dependency] private readonly SharedStunSystem _stun = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<BrainImplantComponent, EmpPulseEvent>(OnEmp, after: [typeof(CyberneticsSystem)]);
    }

    private void OnEmp(Entity<BrainImplantComponent> ent, ref EmpPulseEvent args)
    {
        if (TryComp<OrganComponent>(ent, out var organ) && organ.Body is {} body)
            _stun.TryUpdateStunDuration(body, ent.Comp.EmpStunDuration);
    }
}
