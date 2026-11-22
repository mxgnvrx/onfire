// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared._Arcane.Implants;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Implants.Components;
using Content.Shared.Kitchen.Components;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server._Arcane.Implants;

public sealed class TracExtractionSystem : EntitySystem
{
    private static readonly EntProtoId TracImplant = "TracImplant";

    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ImplantedComponent, GetVerbsEvent<InteractionVerb>>(OnGetVerbs);
        SubscribeLocalEvent<SubdermalImplantComponent, TracExtractionDoAfterEvent>(OnExtract);
    }

    private void OnGetVerbs(Entity<ImplantedComponent> ent, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !args.CanComplexInteract ||
            args.Using is not { } tool || !HasComp<SharpComponent>(tool))
            return;

        var implant = ent.Comp.ImplantContainer.ContainedEntities
            .FirstOrDefault(uid => MetaData(uid).EntityPrototype?.ID == TracImplant.Id);
        if (implant == default)
            return;

        var user = args.User;
        args.Verbs.Add(new InteractionVerb
        {
            Text = Loc.GetString("trac-extract-verb"),
            Act = () => _doAfter.TryStartDoAfter(new DoAfterArgs(
                EntityManager, user, TimeSpan.FromSeconds(20), new TracExtractionDoAfterEvent(), implant, ent, tool)
            {
                BreakOnMove = true,
                BreakOnDamage = true,
                NeedHand = true
            })
        });
    }

    private void OnExtract(Entity<SubdermalImplantComponent> ent, ref TracExtractionDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target is not { } target ||
            ent.Comp.ImplantedEntity != target || MetaData(ent).EntityPrototype?.ID != TracImplant.Id ||
            args.Used is not { } tool || !HasComp<SharpComponent>(tool) || !_hands.IsHolding(args.User, tool) ||
            !TryComp<ImplantedComponent>(target, out var implanted) ||
            !_container.Remove(ent.Owner, implanted.ImplantContainer))
            return;

        args.Handled = true;
        _popup.PopupEntity(Loc.GetString("trac-extract-success"), target, args.User);
    }
}
