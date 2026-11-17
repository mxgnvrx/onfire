using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Arcane.Tippable;

[Serializable, NetSerializable]
public sealed partial class TippableTipDoAfterEvent : SimpleDoAfterEvent { }

[Serializable, NetSerializable]
public sealed partial class TippableRightDoAfterEvent : SimpleDoAfterEvent { }
