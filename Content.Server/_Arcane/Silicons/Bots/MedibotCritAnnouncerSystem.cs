using Content.Server.Pinpointer;
using Content.Server.Radio.EntitySystems;
using Content.Shared.Emag.Systems;
using Content.Shared.Examine;
using Content.Shared.GameTicking;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Mind;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._Arcane.Silicons.Bots;

public sealed class MedibotCritAnnouncerSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private EmagSystem _emag = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private ExamineSystemShared _examine = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private NavMapSystem _navMap = default!;
    [Dependency] private RadioSystem _radio = default!;

    private readonly Dictionary<EntityUid, TimeSpan> _nextPatientReport = new();
    private readonly HashSet<Entity<MobStateComponent>> _patients = new();
    private TimeSpan _nextReportAttempt;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        _nextPatientReport.Clear();
        _nextReportAttempt = TimeSpan.Zero;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        if (now < _nextReportAttempt)
            return;

        var query = EntityQueryEnumerator<MedibotCritAnnouncerComponent, ActiveNPCComponent>();
        while (query.MoveNext(out var uid, out var announcer, out _))
        {
            if (now < announcer.NextScan)
                continue;

            announcer.NextScan = now + announcer.ScanInterval;

            // tg only lets the bot AI report, and an emagged bot no longer looks after the crew.
            if (HasComp<ActorComponent>(uid) || _emag.CheckFlag(uid, EmagType.Interaction))
                continue;

            _patients.Clear();
            _lookup.GetEntitiesInRange(Transform(uid).Coordinates, announcer.Range, _patients, LookupFlags.Uncontained);
            foreach (var patient in _patients)
            {
                if (!_mobState.IsHardCritical(patient, patient.Comp)
                    || !HasComp<HumanoidAppearanceComponent>(patient)
                    || _nextPatientReport.TryGetValue(patient, out var next) && now < next
                    || !_mind.TryGetMind(patient, out _, out _)
                    || !_examine.InRangeUnOccluded(uid, patient, announcer.Range))
                {
                    continue;
                }

                _nextReportAttempt = now + announcer.GlobalCooldown;
                var message = Loc.GetString(announcer.Message,
                    ("patient", Identity.Name(patient, EntityManager)),
                    ("location", _navMap.GetNearestBeaconString(patient.Owner, onlyName: true)));
                if (_radio.SendRadioMessage(uid, message, announcer.Channel, uid))
                    _nextPatientReport[patient] = now + announcer.PatientCooldown;

                return;
            }
        }
    }
}
