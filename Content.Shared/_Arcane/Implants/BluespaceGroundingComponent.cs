// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Arcane.Implants;

[RegisterComponent, NetworkedComponent]
public sealed partial class BluespaceGroundingComponent : Component
{
    [DataField]
    public TimeSpan ParalyzeDuration = TimeSpan.FromSeconds(5);

    [DataField]
    public EntProtoId SparksEffect = "EffectSparks";
}
