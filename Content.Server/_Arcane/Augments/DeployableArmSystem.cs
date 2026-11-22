// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.CodeAnalysis;
using Content.Server._Shitmed.Cybernetics;
using Content.Server.Atmos.EntitySystems;
using Content.Shared._EinsteinEngines.Silicon.Components;
using Content.Shared._Shitmed.Body.Organ;
using Content.Shared.Actions;
using Content.Shared.Body.Events;
using Content.Shared.Body.Organ;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.Damage.Systems;
using Content.Shared.Emp;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Components;
using Content.Shared.Popups;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Toggleable;
using Content.Shared.Weapons.Melee.Events;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server._Arcane.Augments;

public sealed class DeployableArmSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedEmpSystem _emp = default!;
    [Dependency] private readonly FlammableSystem _flammable = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPointLightSystem _light = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<DeployableArmComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<DeployableArmComponent, OrganAddedToBodyEvent>(OnAddedToBody);
        SubscribeLocalEvent<DeployableArmComponent, OrganRemovedFromBodyEvent>(OnRemovedFromBody);
        SubscribeLocalEvent<DeployableArmComponent, OrganComponentsModifyEvent>(OnOrganModified);
        SubscribeLocalEvent<DeployableArmComponent, ToggleActionEvent>(OnToggle);
        SubscribeLocalEvent<DeployableArmComponent, EmpPulseEvent>(OnEmp, after: [typeof(CyberneticsSystem)]);
        SubscribeLocalEvent<ArmElectrodeComponent, MeleeHitEvent>(OnElectrodeHit);
    }

    private void OnElectrodeHit(Entity<ArmElectrodeComponent> ent, ref MeleeHitEvent args)
    {
        if (!args.IsHit)
            return;

        foreach (var target in args.HitEntities)
        {
            if (HasComp<SiliconComponent>(target) || HasComp<BorgChassisComponent>(target))
                _emp.TryEmpEffects(target, ent.Comp.EmpEnergy, ent.Comp.EmpDuration, args.User);
        }
    }

    private void OnMapInit(Entity<DeployableArmComponent> ent, ref MapInitEvent args)
    {
        GetOrSpawnItem(ent);
    }

    private void OnAddedToBody(Entity<DeployableArmComponent> ent, ref OrganAddedToBodyEvent args)
    {
        if (Comp<OrganComponent>(ent).Enabled)
            SetBodyEnabled(ent, args.Body, true);
    }

    private void OnRemovedFromBody(Entity<DeployableArmComponent> ent, ref OrganRemovedFromBodyEvent args)
    {
        SetBodyEnabled(ent, args.OldBody, false);
    }

    private void OnOrganModified(Entity<DeployableArmComponent> ent, ref OrganComponentsModifyEvent args)
    {
        SetBodyEnabled(ent, args.Body, args.Add);
    }

    private void SetBodyEnabled(Entity<DeployableArmComponent> ent, EntityUid body, bool enabled)
    {
        if (enabled)
        {
            _actions.AddAction(body, ref ent.Comp.Action, ent.Comp.ToggleAction, ent);
            return;
        }

        TryRetract(ent, body);
        _actions.RemoveAction(ent.Comp.Action);
    }

    private void OnToggle(Entity<DeployableArmComponent> ent, ref ToggleActionEvent args)
    {
        if (args.Handled || !TryComp<OrganComponent>(ent, out var organ)
            || !organ.Enabled || organ.Body != args.Performer)
            return;

        args.Handled = true;
        var user = args.Performer;
        if (TryRetract(ent, user))
            return;

        if (ent.Comp.Broken)
        {
            _popup.PopupEntity(Loc.GetString("laser-arm-broken", ("implant", ent.Owner)), user, user);
            return;
        }

        Extend(ent, user);
    }

    private void Extend(Entity<DeployableArmComponent> ent, EntityUid user)
    {
        if (!TryGetArmHand(ent, user, out var hand))
            return;

        if (_hands.GetHeldItem(user, hand) is { } held)
        {
            if (!_hands.TryDrop(user, hand))
            {
                _popup.PopupEntity(Loc.GetString("laser-arm-hand-blocked", ("item", held), ("implant", ent.Owner)), user, user);
                return;
            }

            _popup.PopupEntity(Loc.GetString("laser-arm-drop", ("item", held), ("implant", ent.Owner)), user, user);
        }

        var item = GetOrSpawnItem(ent);
        if (!_hands.TryPickup(user, item, hand, animate: false))
            return;

        EnsureComp<UnremoveableComponent>(item);
        if (TryComp<PointLightComponent>(item, out var light))
            _light.SetEnabled(item, true, light);
        _audio.PlayPvs(ent.Comp.ExtendSound, user);
        _popup.PopupEntity(Loc.GetString("laser-arm-extend", ("user", user), ("gun", item)), user, Filter.PvsExcept(user), true);
        _popup.PopupEntity(Loc.GetString("laser-arm-extend-self", ("gun", item)), user, user);
    }

    private bool TryRetract(Entity<DeployableArmComponent> ent, EntityUid user)
    {
        if (ent.Comp.Item is not { } item || !_hands.IsHolding(user, item))
            return false;

        if (TerminatingOrDeleted(ent))
        {
            QueueDel(item);
            return true;
        }

        if (TryComp<PointLightComponent>(item, out var light))
            _light.SetEnabled(item, false, light);
        RemComp<UnremoveableComponent>(item);
        // Returning the implant's own item must also work while AntiDrop holds it in the hand.
        if (!_container.TryRemoveFromContainer(item, force: true)
            || !_container.Insert(item, _container.GetContainer(ent, ent.Comp.ContainerId)))
        {
            // Never leave a detachable implant weapon behind if its return fails.
            QueueDel(item);
            return true;
        }

        _audio.PlayPvs(ent.Comp.RetractSound, user);
        _popup.PopupEntity(Loc.GetString("laser-arm-retract", ("user", user), ("gun", item)), user, Filter.PvsExcept(user), true);
        _popup.PopupEntity(Loc.GetString("laser-arm-retract-self", ("gun", item)), user, user);
        return true;
    }

    private void OnEmp(Entity<DeployableArmComponent> ent, ref EmpPulseEvent args)
    {
        if (ent.Comp.EmpBreakChance <= 0 || ent.Comp.Broken || !TryComp<OrganComponent>(ent, out var organ)
            || organ.Body is not { } body || !_random.Prob(ent.Comp.EmpBreakChance))
            return;

        args.Affected = true;
        ent.Comp.Broken = true;
        _audio.PlayPvs(ent.Comp.EmpSound, body);
        _popup.PopupEntity(Loc.GetString("laser-arm-emp", ("user", body)), body, Filter.PvsExcept(body), true, PopupType.MediumCaution);
        _popup.PopupEntity(Loc.GetString("laser-arm-emp-self"), body, body, PopupType.LargeCaution);
        _flammable.AdjustFireStacks(body, ent.Comp.EmpFireStacks, ignite: true);
        _damageable.TryChangeDamage(body, ent.Comp.EmpDamage);
    }

    // The item is lost if its hand is severed while it is out; the implant then grows a new one.
    private EntityUid GetOrSpawnItem(Entity<DeployableArmComponent> ent)
    {
        if (ent.Comp.Item is { } item && !TerminatingOrDeleted(item))
            return item;

        _container.EnsureContainer<ContainerSlot>(ent, ent.Comp.ContainerId);
        ent.Comp.Item = SpawnInContainerOrDrop(ent.Comp.ItemPrototype, ent, ent.Comp.ContainerId);
        return ent.Comp.Item.Value;
    }

    private bool TryGetArmHand(EntityUid organ, EntityUid body, [NotNullWhen(true)] out string? handId)
    {
        handId = null;
        if (!TryComp<BodyPartComponent>(_body.GetParentPartOrNull(organ), out var arm))
            return false;

        var location = arm.Symmetry switch
        {
            BodyPartSymmetry.Left => HandLocation.Left,
            BodyPartSymmetry.Right => HandLocation.Right,
            _ => HandLocation.Middle,
        };

        foreach (var hand in _hands.EnumerateHands(body))
        {
            if (!_hands.TryGetHand(body, hand, out var info) || info.Value.Location != location)
                continue;

            handId = hand;
            return true;
        }

        return false;
    }
}
