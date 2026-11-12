// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Server._Arcane.Augments;

[RegisterComponent]
public sealed partial class StrongArmUserComponent : Component
{
    /// <summary>
    /// Whether the implant pair added the body's prying, so it is only removed when we own it.
    /// </summary>
    [DataField]
    public bool AddedPrying;
}
