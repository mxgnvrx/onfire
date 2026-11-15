// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.CodeAnalysis;
using Content.Server._Shitmed.Cybernetics;
using Content.Server.Atmos.EntitySystems;
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
using Content.Shared.Toggleable;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server._Arcane.Augments;

public sealed class LaserArmSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly FlammableSystem _flammable = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<LaserArmComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<LaserArmComponent, OrganAddedToBodyEvent>(OnAddedToBody);
        SubscribeLocalEvent<LaserArmComponent, OrganRemovedFromBodyEvent>(OnRemovedFromBody);
        SubscribeLocalEvent<LaserArmComponent, OrganComponentsModifyEvent>(OnOrganModified);
        SubscribeLocalEvent<LaserArmComponent, ToggleActionEvent>(OnToggle);
        SubscribeLocalEvent<LaserArmComponent, EmpPulseEvent>(OnEmp, after: [typeof(CyberneticsSystem)]);
    }

    private void OnMapInit(Entity<LaserArmComponent> ent, ref MapInitEvent args)
    {
        GetOrSpawnGun(ent);
    }

    private void OnAddedToBody(Entity<LaserArmComponent> ent, ref OrganAddedToBodyEvent args)
    {
        if (Comp<OrganComponent>(ent).Enabled)
            SetBodyEnabled(ent, args.Body, true);
    }

    private void OnRemovedFromBody(Entity<LaserArmComponent> ent, ref OrganRemovedFromBodyEvent args)
    {
        SetBodyEnabled(ent, args.OldBody, false);
    }

    private void OnOrganModified(Entity<LaserArmComponent> ent, ref OrganComponentsModifyEvent args)
    {
        SetBodyEnabled(ent, args.Body, args.Add);
    }

    private void SetBodyEnabled(Entity<LaserArmComponent> ent, EntityUid body, bool enabled)
    {
        if (enabled)
        {
            _actions.AddAction(body, ref ent.Comp.Action, ent.Comp.ToggleAction, ent);
            return;
        }

        TryRetract(ent, body);
        _actions.RemoveAction(ent.Comp.Action);
    }

    private void OnToggle(Entity<LaserArmComponent> ent, ref ToggleActionEvent args)
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

    private void Extend(Entity<LaserArmComponent> ent, EntityUid user)
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

        var gun = GetOrSpawnGun(ent);
        if (!_hands.TryPickup(user, gun, hand, animate: false))
            return;

        EnsureComp<UnremoveableComponent>(gun);
        _audio.PlayPvs(ent.Comp.ToggleSound, user);
        _popup.PopupEntity(Loc.GetString("laser-arm-extend", ("user", user), ("gun", gun)), user, Filter.PvsExcept(user), true);
        _popup.PopupEntity(Loc.GetString("laser-arm-extend-self", ("gun", gun)), user, user);
    }

    private bool TryRetract(Entity<LaserArmComponent> ent, EntityUid user)
    {
        if (ent.Comp.Gun is not { } gun || !_hands.IsHolding(user, gun))
            return false;

        if (TerminatingOrDeleted(ent))
        {
            QueueDel(gun);
            return true;
        }

        RemComp<UnremoveableComponent>(gun);
        // Returning the implant's own gun must also work while AntiDrop holds it in the hand.
        if (!_container.TryRemoveFromContainer(gun, force: true)
            || !_container.Insert(gun, _container.GetContainer(ent, ent.Comp.ContainerId)))
        {
            // Never leave a detachable implant weapon behind if its return fails.
            QueueDel(gun);
            return true;
        }

        _audio.PlayPvs(ent.Comp.ToggleSound, user);
        _popup.PopupEntity(Loc.GetString("laser-arm-retract", ("user", user), ("gun", gun)), user, Filter.PvsExcept(user), true);
        _popup.PopupEntity(Loc.GetString("laser-arm-retract-self", ("gun", gun)), user, user);
        return true;
    }

    private void OnEmp(Entity<LaserArmComponent> ent, ref EmpPulseEvent args)
    {
        if (ent.Comp.Broken || !TryComp<OrganComponent>(ent, out var organ)
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

    // The gun is lost if its hand is severed while it is out; the implant then grows a new one.
    private EntityUid GetOrSpawnGun(Entity<LaserArmComponent> ent)
    {
        if (ent.Comp.Gun is { } gun && !TerminatingOrDeleted(gun))
            return gun;

        _container.EnsureContainer<ContainerSlot>(ent, ent.Comp.ContainerId);
        ent.Comp.Gun = SpawnInContainerOrDrop(ent.Comp.GunPrototype, ent, ent.Comp.ContainerId);
        return ent.Comp.Gun.Value;
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
