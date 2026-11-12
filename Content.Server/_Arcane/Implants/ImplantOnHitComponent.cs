// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Prototypes;

namespace Content.Server._Arcane.Implants;

[RegisterComponent]
public sealed partial class ImplantOnHitComponent : Component
{
    [DataField(required: true)]
    public EntProtoId Implant;
}
