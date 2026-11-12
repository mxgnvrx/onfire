// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server._Shitmed.Cybernetics;
using Content.Shared._Shitmed.Targeting;
using Content.Shared.Body.Organ;
using Content.Shared.Damage.Systems;
using Content.Shared.Emp;
using Content.Shared.Popups;

namespace Content.Server._Arcane.Augments;

public sealed class HerculeanSpineImplantSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<HerculeanSpineImplantComponent, EmpPulseEvent>(OnEmp, after: [typeof(CyberneticsSystem)]);
    }

    private void OnEmp(Entity<HerculeanSpineImplantComponent> ent, ref EmpPulseEvent args)
    {
        if (!TryComp<OrganComponent>(ent, out var organ) || organ.Body is not { } body)
            return;

        _damageable.TryChangeDamage(body, ent.Comp.EmpDamage, targetPart: TargetBodyPart.Chest);
        _popup.PopupEntity(Loc.GetString("herculean-spine-emp"), body, body, PopupType.MediumCaution);
    }
}
