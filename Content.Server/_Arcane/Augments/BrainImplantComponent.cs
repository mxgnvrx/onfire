// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Server._Arcane.Augments;

[RegisterComponent]
public sealed partial class BrainImplantComponent : Component
{
    [DataField]
    public TimeSpan EmpStunDuration = TimeSpan.FromSeconds(20);
}
