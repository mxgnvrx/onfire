// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Shared._Arcane.Augments;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CnsRebooterUserComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid Implant;
}
