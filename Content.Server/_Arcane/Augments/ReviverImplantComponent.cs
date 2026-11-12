// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Maths.FixedPoint;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._Arcane.Augments;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class ReviverImplantComponent : Component
{
    [DataField]
    public TimeSpan HealInterval = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Damage healed per tick for each group the body is hurt in.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<DamageGroupPrototype>, FixedPoint2> Heal = new();

    /// <summary>
    /// Recharge time added per tick for each group that was healed.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<DamageGroupPrototype>, TimeSpan> HealCost = new();

    /// <summary>
    /// Delay between starting to work on a dead body and the first revival attempt.
    /// </summary>
    [DataField]
    public TimeSpan ReviveDelay = TimeSpan.FromSeconds(8);

    [DataField]
    public TimeSpan ReviveRetryDelay = TimeSpan.FromSeconds(16);

    [DataField]
    public TimeSpan ReviveCost = TimeSpan.FromMinutes(10);

    [DataField]
    public TimeSpan EmpPenalty = TimeSpan.FromSeconds(20);

    [DataField]
    public SoundSpecifier ReviveSound = new SoundPathSpecifier("/Audio/Items/Defib/defib_zap.ogg");

    [DataField]
    public bool Reviving;

    [DataField]
    public TimeSpan Cost;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextHeal;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan ReviveAt;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan CooldownUntil;
}
