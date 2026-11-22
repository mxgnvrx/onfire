// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Shared._Arcane.Teleportation;

/// <summary>
/// Broadcast before a portal moves an entity, so systems can veto it based on where it leads.
/// </summary>
[ByRefEvent]
public record struct PortalTeleportAttemptEvent(EntityUid Subject, EntityUid? Target, bool Cancelled = false);
