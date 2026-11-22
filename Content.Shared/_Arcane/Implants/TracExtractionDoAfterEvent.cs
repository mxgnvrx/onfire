// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Arcane.Implants;

[Serializable, NetSerializable]
public sealed partial class TracExtractionDoAfterEvent : SimpleDoAfterEvent;
