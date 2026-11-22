// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Lavaland.Shuttles.Components;
using Content.Shared.Popups;
using Content.Shared.Salvage.Expeditions;
using Content.Shared.UserInterface;

namespace Content.Shared._Arcane.Implants;

public abstract class SharedExileImplantSystem : EntitySystem
{
    [Dependency] protected readonly SharedPopupSystem Popup = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ExileImplantedComponent, UserOpenActivatableUIAttemptEvent>(OnUserOpenUIAttempt);
    }

    private void OnUserOpenUIAttempt(Entity<ExileImplantedComponent> ent, ref UserOpenActivatableUIAttemptEvent args)
    {
        if (!HasComp<DockingConsoleComponent>(args.Target) && !HasComp<SalvageExpeditionConsoleComponent>(args.Target))
            return;

        args.Cancel();

        if (!args.Silent)
            Popup.PopupClient(Loc.GetString("exile-implant-shuttle-console-locked"), args.Target, ent, PopupType.MediumCaution);
    }
}
