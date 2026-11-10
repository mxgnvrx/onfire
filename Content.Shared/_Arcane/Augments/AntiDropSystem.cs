// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Shitmed.Body.Organ;
using Content.Shared.Actions;
using Content.Shared.Body.Events;
using Content.Shared.Body.Organ;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Toggleable;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Shared._Arcane.Augments;

public sealed class AntiDropSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<AntiDropComponent, OrganComponentsModifyEvent>(OnOrganModified);
        SubscribeLocalEvent<AntiDropComponent, OrganAddedToBodyEvent>(OnAddedToBody);
        SubscribeLocalEvent<AntiDropComponent, OrganRemovedFromBodyEvent>(OnRemovedFromBody);
        SubscribeLocalEvent<AntiDropComponent, ToggleActionEvent>(OnToggle);
        SubscribeLocalEvent<AntiDropHeldItemComponent, ContainerGettingRemovedAttemptEvent>(OnRemoveAttempt);
        SubscribeLocalEvent<AntiDropHeldItemComponent, EntGotRemovedFromContainerMessage>(OnRemovedFromContainer);
    }

    private void OnAddedToBody(Entity<AntiDropComponent> ent, ref OrganAddedToBodyEvent args)
    {
        if (Comp<OrganComponent>(ent).Enabled)
            SetBodyEnabled(ent, args.Body, true);
    }

    private void OnRemovedFromBody(Entity<AntiDropComponent> ent, ref OrganRemovedFromBodyEvent args)
    {
        SetBodyEnabled(ent, args.OldBody, false);
    }

    private void OnOrganModified(Entity<AntiDropComponent> ent, ref OrganComponentsModifyEvent args)
    {
        SetBodyEnabled(ent, args.Body, args.Add);
    }

    private void SetBodyEnabled(Entity<AntiDropComponent> ent, EntityUid body, bool enabled)
    {
        if (_timing.ApplyingState)
            return;

        if (enabled)
        {
            _actions.AddAction(body, ref ent.Comp.Action, ent.Comp.ToggleAction, ent);
            Dirty(ent);
        }
        else
        {
            Release(ent, body);
            _actions.RemoveAction(ent.Comp.Action);
        }
    }

    private void OnToggle(Entity<AntiDropComponent> ent, ref ToggleActionEvent args)
    {
        if (args.Handled || !TryComp<OrganComponent>(ent, out var organ)
            || !organ.Enabled || organ.Body != args.Performer)
            return;

        args.Handled = true;
        if (ent.Comp.Active)
        {
            Release(ent, args.Performer);
            _popup.PopupClient(Loc.GetString("anti-drop-relaxed"), args.Performer, args.Performer);
            return;
        }

        foreach (var item in _hands.EnumerateHeld(args.Performer))
        {
            var held = EnsureComp<AntiDropHeldItemComponent>(item);
            held.Implant = ent;
            Dirty(item, held);
            ent.Comp.Active = true;
        }

        _actions.SetToggled(ent.Comp.Action, ent.Comp.Active);
        Dirty(ent);
        _popup.PopupClient(Loc.GetString(ent.Comp.Active ? "anti-drop-tightened" : "anti-drop-empty"),
            args.Performer,
            args.Performer);
    }

    private void Release(Entity<AntiDropComponent> ent, EntityUid body)
    {
        foreach (var item in _hands.EnumerateHeld(body))
        {
            if (TryComp<AntiDropHeldItemComponent>(item, out var held) && held.Implant == ent.Owner)
                RemComp<AntiDropHeldItemComponent>(item);
        }

        ent.Comp.Active = false;
        _actions.SetToggled(ent.Comp.Action, false);
        Dirty(ent);
    }

    private void OnRemoveAttempt(Entity<AntiDropHeldItemComponent> ent, ref ContainerGettingRemovedAttemptEvent args)
    {
        if (!_timing.ApplyingState
            && TryComp<AntiDropComponent>(ent.Comp.Implant, out var implant) && implant.Active
            && TryComp<OrganComponent>(ent.Comp.Implant, out var organ) && organ.Enabled
            && organ.Body == args.Container.Owner && _hands.IsHolding(args.Container.Owner, ent.Owner))
            args.Cancel();
    }

    private void OnRemovedFromContainer(Entity<AntiDropHeldItemComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        if (_timing.ApplyingState)
            return;

        RemComp<AntiDropHeldItemComponent>(ent);
        if (!TryComp<AntiDropComponent>(ent.Comp.Implant, out var implant))
            return;

        foreach (var item in _hands.EnumerateHeld(args.Container.Owner))
        {
            if (item != ent.Owner && TryComp<AntiDropHeldItemComponent>(item, out var held)
                && held.Implant == ent.Comp.Implant)
                return;
        }

        implant.Active = false;
        _actions.SetToggled(implant.Action, false);
        Dirty(ent.Comp.Implant, implant);
    }
}
