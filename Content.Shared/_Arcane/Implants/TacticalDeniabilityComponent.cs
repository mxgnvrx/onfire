// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Shared._Arcane.Implants;

/// <summary>
/// Keeps the implanted mob out of critical condition and arms its tactical deniability implant instead.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class TacticalDeniabilityComponent : Component { }
