// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Server._Arcane.Augments;

[RegisterComponent]
public sealed partial class ArmElectrodeComponent : Component
{
    [DataField]
    public float EmpEnergy = 1000f;

    [DataField]
    public TimeSpan EmpDuration = TimeSpan.FromSeconds(4);
}
