// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._Arcane.Augments;

[RegisterComponent]
public sealed partial class LaserArmComponent : Component
{
    [DataField]
    public EntProtoId GunPrototype = "WeaponLaserArm";

    [DataField]
    public EntityUid? Gun;

    [DataField]
    public string ContainerId = "laser_arm";

    [DataField]
    public EntProtoId ToggleAction = "ActionToggleLaserArm";

    [DataField]
    public EntityUid? Action;

    [DataField]
    public SoundSpecifier ToggleSound = new SoundPathSpecifier("/Audio/Mecha/mechmove03.ogg");

    [DataField]
    public float EmpBreakChance = 0.3f;

    [DataField]
    public DamageSpecifier EmpDamage = new();

    [DataField]
    public float EmpFireStacks = 15f;

    [DataField]
    public SoundSpecifier EmpSound = new SoundPathSpecifier("/Audio/Effects/flash_bang.ogg");

    [DataField]
    public bool Broken;
}
