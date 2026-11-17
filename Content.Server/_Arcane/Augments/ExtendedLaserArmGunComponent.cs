// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Server._Arcane.Augments;

[RegisterComponent]
public sealed partial class ExtendedLaserArmGunComponent : Component
{
    public EntityUid Implant;
    public string Hand = string.Empty;
}
