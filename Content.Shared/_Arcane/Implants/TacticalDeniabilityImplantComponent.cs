// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Shared._Arcane.Implants;

/// <summary>
/// Absorbs other explosive implants of its host into its own explosion and timer.
/// </summary>
[RegisterComponent]
public sealed partial class TacticalDeniabilityImplantComponent : Component
{
    [DataField]
    public TimeSpan MaxDelay = TimeSpan.FromSeconds(30);
}
