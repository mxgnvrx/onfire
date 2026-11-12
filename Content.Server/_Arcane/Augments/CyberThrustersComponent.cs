// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Prototypes;

namespace Content.Server._Arcane.Augments;

[RegisterComponent]
public sealed partial class CyberThrustersComponent : Component
{
    [DataField]
    public EntProtoId ToggleAction = "ActionToggleCyberThrusters";

    [DataField]
    public EntityUid? Action;

    /// <summary>
    /// Ambient pressure in kPa above which the thrusters run on surrounding air instead of internals.
    /// </summary>
    [DataField]
    public float MinEnvironmentPressure = 30f;
}
