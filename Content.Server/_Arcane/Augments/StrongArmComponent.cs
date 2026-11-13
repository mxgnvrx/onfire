// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Hands.Components;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._Arcane.Augments;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class StrongArmComponent : Component
{
    [DataField]
    public HandLocation Hand;

    [DataField]
    public float DamageMultiplier = 2f;

    [DataField]
    public float ThrowDistance = 1f;

    [DataField]
    public float ThrowSpeed = 15f;

    [DataField]
    public TimeSpan SlamCooldown = TimeSpan.FromSeconds(5);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextSlam;

    [DataField]
    public float PairPrySpeedModifier = 0.83f;
}
