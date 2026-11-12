// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Shared._Arcane.Augments;

/// <summary>
/// Added to the body by an enabled Herculean spinal implant.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HerculeanSpineComponent : Component
{
    /// <summary>
    /// Multiplies standing up, climbing and carrying times.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float AthleticsMultiplier = 0.8f;

    [DataField, AutoNetworkedField]
    public float ThrowRangeBonus = 4f;

    [DataField, AutoNetworkedField]
    public float ThrowSpeedMultiplier = 1.5f;
}
