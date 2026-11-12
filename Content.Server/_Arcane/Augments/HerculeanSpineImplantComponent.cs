// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage;

namespace Content.Server._Arcane.Augments;

[RegisterComponent]
public sealed partial class HerculeanSpineImplantComponent : Component
{
    [DataField]
    public DamageSpecifier EmpDamage = new();
}
