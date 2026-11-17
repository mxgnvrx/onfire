using Content.Shared.Dataset;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Arcane.Tippable;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(SharedTippableSystem))]
public sealed partial class TippableComponent : Component
{
    [DataField]
    public TimeSpan TipTime = TimeSpan.FromSeconds(3);

    [DataField]
    public TimeSpan RightTime = TimeSpan.FromSeconds(3);

    [DataField]
    public TimeSpan SelfRightTime = TimeSpan.FromMinutes(3.5);

    [DataField, AutoNetworkedField]
    public bool Tipped;

    [DataField]
    public EntityUid? Tipper;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan SelfRightAt;

    [DataField]
    public SoundSpecifier? TipSound;

    [DataField]
    public ProtoId<LocalizedDatasetPrototype>? ExamineMessages;

    [DataField]
    public ProtoId<LocalizedDatasetPrototype>? WorriedSpeech;

    [DataField]
    public TimeSpan WorriedSpeechCooldown = TimeSpan.FromSeconds(20);

    [DataField]
    public float WorriedSpeechChancePerSecond = 0.15f;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextWorriedSpeech;

    [DataField]
    public ProtoId<LocalizedDatasetPrototype>? RightedSpeech;

    [DataField]
    public LocId? ForgiveSpeech;

    [DataField]
    public LocId? TipperReportSpeech;

    [DataField]
    public float TipperReportChance = 0.1f;
}
