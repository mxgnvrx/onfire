// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Arcane.Augments;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class CnsRebooterComponent : Component
{
    [DataField]
    public TimeSpan RebootDelay = TimeSpan.FromSeconds(1);

    [DataField]
    public TimeSpan ProtectionDuration = TimeSpan.FromSeconds(6);

    [DataField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(60);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan? RebootAt;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan? ProtectedUntil;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan? CooldownUntil;
}
