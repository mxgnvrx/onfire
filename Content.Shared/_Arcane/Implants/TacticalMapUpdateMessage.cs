// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Robust.Shared.Serialization;

namespace Content.Shared._Arcane.Implants;

[Serializable, NetSerializable]
public sealed class TacticalMapUpdateMessage(NetEntity? grid, Vector2[] allies) : BoundUserInterfaceMessage
{
    public NetEntity? Grid { get; } = grid;
    public Vector2[] Allies { get; } = allies;
}
