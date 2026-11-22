// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server._Shitmed.Cybernetics;
using Content.Shared._Arcane.Augments;
using Content.Shared._Shitmed.Targeting;
using Content.Shared.Body.Organ;
using Content.Shared.Damage.Systems;
using Content.Shared.Emp;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;

namespace Content.Server._Arcane.Augments;

public sealed class HerculeanSpineImplantSystem : EntitySystem
{
    private static readonly EntProtoId GravityCore = "AnomalyCoreGravity";
    private static readonly EntProtoId InertGravityCore = "AnomalyCoreGravityInert";

    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<HerculeanSpineImplantComponent, EmpPulseEvent>(OnEmp, after: [typeof(CyberneticsSystem)]);
        SubscribeLocalEvent<HerculeanSpineImplantComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnInteractUsing(Entity<HerculeanSpineImplantComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || !TryComp<OrganComponent>(ent, out var organ) || organ.Body != null
            || MetaData(args.Used).EntityPrototype?.ID is not { } core || (core != GravityCore.Id && core != InertGravityCore.Id))
            return;

        args.Handled = true;
        if (ent.Comp.Upgrade is not { } upgrade)
        {
            _popup.PopupEntity(Loc.GetString("herculean-spine-core-already-installed"), ent, args.User);
            return;
        }

        var wasHeld = _hands.IsHolding(args.User, ent);
        var atlas = Spawn(upgrade, Transform(ent).Coordinates);
        Del(args.Used);
        Del(ent);
        if (wasHeld)
            _hands.TryPickupAnyHand(args.User, atlas);

        _popup.PopupEntity(Loc.GetString("herculean-spine-core-installed"), atlas, args.User);
    }

    private void OnEmp(Entity<HerculeanSpineImplantComponent> ent, ref EmpPulseEvent args)
    {
        if (!TryComp<OrganComponent>(ent, out var organ) || organ.Body is not { } body)
            return;

        _damageable.TryChangeDamage(body, ent.Comp.EmpDamage, targetPart: TargetBodyPart.Chest);
        _popup.PopupEntity(Loc.GetString("herculean-spine-emp"), body, body, PopupType.MediumCaution);
    }
}
