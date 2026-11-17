using Content.Shared.ActionBlocker;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Events;
using Content.Shared.Popups;
using Content.Shared.Rotation;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._Arcane.Tippable;

public abstract class SharedTippableSystem : EntitySystem
{
    [Dependency] protected IGameTiming Timing = default!;
    [Dependency] protected IPrototypeManager Proto = default!;
    [Dependency] protected IRobustRandom Random = default!;
    [Dependency] protected SharedPopupSystem Popup = default!;
    [Dependency] private ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private ISharedAdminLogManager _adminLog = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TippableComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
        SubscribeLocalEvent<TippableComponent, TippableTipDoAfterEvent>(OnTipDoAfter);
        SubscribeLocalEvent<TippableComponent, TippableRightDoAfterEvent>(OnRightDoAfter);
        SubscribeLocalEvent<TippableComponent, UpdateCanMoveEvent>(OnUpdateCanMove);
        SubscribeLocalEvent<TippableComponent, ExaminedEvent>(OnExamined);
    }

    private void OnGetVerbs(Entity<TippableComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null || args.User == args.Target)
            return;

        var user = args.User;
        if (ent.Comp.Tipped)
        {
            args.Verbs.Add(new AlternativeVerb
            {
                Text = Loc.GetString("tippable-verb-right"),
                Act = () => TryRight(ent.AsNullable(), user),
            });
            return;
        }

        if (!CanTip(ent.AsNullable()))
            return;

        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("tippable-verb-tip"),
            Act = () => TryTip(ent.AsNullable(), user),
        });
    }

    public bool CanTip(Entity<TippableComponent?> ent)
    {
        return Resolve(ent, ref ent.Comp, false) && !ent.Comp.Tipped && _mobState.IsAlive(ent);
    }

    public bool TryTip(Entity<TippableComponent?> ent, EntityUid user)
    {
        if (!Resolve(ent, ref ent.Comp, false) || !CanTip(ent))
            return false;

        var doAfter = new DoAfterArgs(EntityManager, user, ent.Comp.TipTime, new TippableTipDoAfterEvent(), ent.Owner, target: ent.Owner)
        {
            BreakOnMove = true,
        };

        if (!_doAfter.TryStartDoAfter(doAfter))
            return false;

        Popup.PopupPredicted(
            Loc.GetString("tippable-tip-start-user", ("target", Identity.Entity(ent, EntityManager))),
            Loc.GetString("tippable-tip-start-others", ("user", Identity.Entity(user, EntityManager)), ("target", Identity.Entity(ent, EntityManager))),
            ent,
            user);

        TipStarted((ent, ent.Comp), user);
        return true;
    }

    public bool TryRight(Entity<TippableComponent?> ent, EntityUid user)
    {
        if (!Resolve(ent, ref ent.Comp, false) || !ent.Comp.Tipped)
            return false;

        var doAfter = new DoAfterArgs(EntityManager, user, ent.Comp.RightTime, new TippableRightDoAfterEvent(), ent.Owner, target: ent.Owner)
        {
            BreakOnMove = true,
        };

        if (!_doAfter.TryStartDoAfter(doAfter))
            return false;

        Popup.PopupPredicted(
            Loc.GetString("tippable-right-start-user", ("target", Identity.Entity(ent, EntityManager))),
            Loc.GetString("tippable-right-start-others", ("user", Identity.Entity(user, EntityManager)), ("target", Identity.Entity(ent, EntityManager))),
            ent,
            user);

        return true;
    }

    private void OnTipDoAfter(Entity<TippableComponent> ent, ref TippableTipDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || !CanTip(ent.AsNullable()))
            return;

        args.Handled = true;

        ent.Comp.Tipped = true;
        ent.Comp.Tipper = args.User;
        ent.Comp.SelfRightAt = Timing.CurTime + ent.Comp.SelfRightTime;
        ent.Comp.NextWorriedSpeech = Timing.CurTime + ent.Comp.WorriedSpeechCooldown;
        Dirty(ent);
        UpdateTippedState(ent);

        Popup.PopupPredicted(
            Loc.GetString("tippable-tip-done-user", ("target", Identity.Entity(ent, EntityManager))),
            Loc.GetString("tippable-tip-done-others", ("user", Identity.Entity(args.User, EntityManager)), ("target", Identity.Entity(ent, EntityManager))),
            ent,
            args.User);
        _audio.PlayPredicted(ent.Comp.TipSound, ent, args.User);
        _adminLog.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(args.User):user} tipped over {ToPrettyString(ent):target}");

        TippedOver(ent, args.User);
    }

    private void OnRightDoAfter(Entity<TippableComponent> ent, ref TippableRightDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || !ent.Comp.Tipped)
            return;

        args.Handled = true;

        Popup.PopupPredicted(
            Loc.GetString("tippable-right-done-user", ("target", Identity.Entity(ent, EntityManager))),
            Loc.GetString("tippable-right-done-others", ("user", Identity.Entity(args.User, EntityManager)), ("target", Identity.Entity(ent, EntityManager))),
            ent,
            args.User);

        Right(ent, args.User);
    }

    protected void Right(Entity<TippableComponent> ent, EntityUid? user)
    {
        var tipper = ent.Comp.Tipper;
        ent.Comp.Tipped = false;
        ent.Comp.Tipper = null;
        Dirty(ent);
        UpdateTippedState(ent);

        Righted(ent, user, tipper);
    }

    private void UpdateTippedState(Entity<TippableComponent> ent)
    {
        _appearance.SetData(ent, RotationVisuals.RotationState, ent.Comp.Tipped ? RotationState.Horizontal : RotationState.Vertical);
        _actionBlocker.UpdateCanMove(ent);
    }

    private void OnUpdateCanMove(Entity<TippableComponent> ent, ref UpdateCanMoveEvent args)
    {
        if (ent.Comp.Tipped)
            args.Cancel();
    }

    private void OnExamined(Entity<TippableComponent> ent, ref ExaminedEvent args)
    {
        if (!ent.Comp.Tipped || ent.Comp.ExamineMessages is not { } messages)
            return;

        args.PushMarkup(Loc.GetString(Random.Pick(Proto.Index(messages).Values)));
    }

    protected virtual void TipStarted(Entity<TippableComponent> ent, EntityUid user)
    {
    }

    protected virtual void TippedOver(Entity<TippableComponent> ent, EntityUid user)
    {
    }

    protected virtual void Righted(Entity<TippableComponent> ent, EntityUid? user, EntityUid? tipper)
    {
    }
}
