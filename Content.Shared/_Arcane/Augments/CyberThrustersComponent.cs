// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Prototypes;

namespace Content.Shared._Arcane.Augments;

[RegisterComponent]
public sealed partial class CyberThrustersComponent : Component
{
    [DataField]
    public EntProtoId ToggleAction = "ActionToggleCyberThrusters";

    [DataField]
    public EntityUid? Action;

    [DataField]
    public float MinEnvironmentPressure = 30f;
}
