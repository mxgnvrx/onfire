// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Arcane.Augments;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AntiDropComponent : Component
{
    [DataField]
    public EntProtoId ToggleAction = "ActionToggleAntiDrop";

    [DataField, AutoNetworkedField]
    public EntityUid? Action;

    [DataField, AutoNetworkedField]
    public bool Active;
}
