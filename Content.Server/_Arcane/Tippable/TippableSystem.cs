using Content.Server.Advertise.EntitySystems;
using Content.Server.Chat.Systems;
using Content.Server.Pinpointer;
using Content.Server.Vocalization.Systems;
using Content.Shared._Arcane.Tippable;
using Content.Shared.Chat;
using Content.Shared.Dataset;
using Content.Shared.IdentityManagement;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Arcane.Tippable;

public sealed class TippableSystem : SharedTippableSystem
{
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private NavMapSystem _navMap = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TippableComponent, AttemptAdvertiseEvent>(OnAttemptAdvertise);
        SubscribeLocalEvent<TippableComponent, TryVocalizeEvent>(OnTryVocalize);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = Timing.CurTime;
        var query = EntityQueryEnumerator<TippableComponent>();
        while (query.MoveNext(out var uid, out var tippable))
        {
            if (!tippable.Tipped)
                continue;

            if (now >= tippable.SelfRightAt)
            {
                Popup.PopupEntity(Loc.GetString("tippable-self-right", ("target", Identity.Entity(uid, EntityManager))), uid);
                Right((uid, tippable), null);
                continue;
            }

            // tg only lets the bot AI plead, so a player-controlled bot speaks for itself.
            if (tippable.WorriedSpeech == null || now < tippable.NextWorriedSpeech || HasComp<ActorComponent>(uid))
                continue;

            if (!Random.Prob(1f - MathF.Pow(1f - tippable.WorriedSpeechChancePerSecond, frameTime)))
                continue;

            tippable.NextWorriedSpeech = now + tippable.WorriedSpeechCooldown;
            SayRandom(uid, tippable.WorriedSpeech);
        }
    }

    protected override void TipStarted(Entity<TippableComponent> ent, EntityUid user)
    {
        SayRandom(ent, ent.Comp.WorriedSpeech);
    }

    protected override void TippedOver(Entity<TippableComponent> ent, EntityUid user)
    {
        if (ent.Comp.TipperReportSpeech is not { } report || !Random.Prob(ent.Comp.TipperReportChance))
            return;

        Say(ent, Loc.GetString(report,
            ("user", Identity.Name(user, EntityManager)),
            ("location", _navMap.GetNearestBeaconString(ent.Owner, onlyName: true))));
    }

    protected override void Righted(Entity<TippableComponent> ent, EntityUid? user, EntityUid? tipper)
    {
        if (user == null || tipper == null)
            return;

        if (user == tipper)
        {
            if (ent.Comp.ForgiveSpeech is { } forgive)
                Say(ent, Loc.GetString(forgive));

            return;
        }

        SayRandom(ent, ent.Comp.RightedSpeech);
    }

    private void OnAttemptAdvertise(Entity<TippableComponent> ent, ref AttemptAdvertiseEvent args)
    {
        args.Cancelled |= ent.Comp.Tipped;
    }

    private void OnTryVocalize(Entity<TippableComponent> ent, ref TryVocalizeEvent args)
    {
        args.Cancelled |= ent.Comp.Tipped;
    }

    private void SayRandom(EntityUid uid, ProtoId<LocalizedDatasetPrototype>? dataset)
    {
        if (dataset is not { } id)
            return;

        Say(uid, Loc.GetString(Random.Pick(Proto.Index(id).Values)));
    }

    private void Say(EntityUid uid, string message)
    {
        _chat.TrySendInGameICMessage(uid, message, InGameICChatType.Speak, hideChat: false);
    }
}
