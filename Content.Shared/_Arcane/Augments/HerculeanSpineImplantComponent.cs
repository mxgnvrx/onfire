// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage;
using Robust.Shared.Prototypes;

namespace Content.Shared._Arcane.Augments;

[RegisterComponent]
public sealed partial class HerculeanSpineImplantComponent : Component
{
    [DataField]
    public DamageSpecifier EmpDamage = new();

    [DataField]
    public EntProtoId? Upgrade;
}
