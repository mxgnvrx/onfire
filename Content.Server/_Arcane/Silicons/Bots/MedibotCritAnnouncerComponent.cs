using Content.Shared.Radio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._Arcane.Silicons.Bots;

[RegisterComponent, AutoGenerateComponentPause]
[Access(typeof(MedibotCritAnnouncerSystem))]
public sealed partial class MedibotCritAnnouncerComponent : Component
{
    [DataField]
    public float Range = 10f;

    [DataField]
    public TimeSpan ScanInterval = TimeSpan.FromSeconds(2);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextScan;

    // Both cooldowns apply across all announcers, so the number of bots never multiplies the reports.
    [DataField]
    public TimeSpan PatientCooldown = TimeSpan.FromMinutes(3);

    [DataField]
    public TimeSpan GlobalCooldown = TimeSpan.FromSeconds(30);

    [DataField]
    public ProtoId<RadioChannelPrototype> Channel = "Medical";

    [DataField]
    public LocId Message = "medibot-crit-announce";
}
