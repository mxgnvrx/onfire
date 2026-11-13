// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server._Shitmed.Cybernetics;
using Content.Server.EUI;
using Content.Server.Ghost;
using Content.Shared._Shitmed.Damage;
using Content.Shared._Shitmed.Medical.Surgery.Consciousness.Components;
using Content.Shared._Shitmed.Medical.Surgery.Consciousness.Systems;
using Content.Shared._Shitmed.Targeting;
using Content.Shared.Atmos.Rotting;
using Content.Shared.Body.Organ;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Emp;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Traits.Assorted;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._Arcane.Augments;

public sealed class ReviverImplantSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly EuiManager _eui = default!;
    [Dependency] private readonly ConsciousnessSystem _consciousness = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedRottingSystem _rotting = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ReviverImplantComponent, EmpPulseEvent>(OnEmp, after: [typeof(CyberneticsSystem)]);
    }

    private void OnEmp(Entity<ReviverImplantComponent> ent, ref EmpPulseEvent args)
    {
        if (ent.Comp.ReviveAt != null)
            ent.Comp.Cost += ent.Comp.EmpPenalty;
        else
            ent.Comp.CooldownUntil += ent.Comp.EmpPenalty;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<ReviverImplantComponent, OrganComponent>();
        while (query.MoveNext(out var uid, out var reviver, out var organ))
        {
            if (now < reviver.NextHeal || !organ.Enabled
                || organ.Body is not {} body || !TryComp<MobStateComponent>(body, out var mobState))
                continue;

            reviver.NextHeal = now + reviver.HealInterval;
            var down = _mobState.IsIncapacitated(body, mobState);

            if (reviver.ReviveAt is not {} reviveAt)
            {
                if (!down || now < reviver.CooldownUntil)
                    continue;

                reviver.Cost = TimeSpan.Zero;
                reviver.ReviveAt = now + reviver.ReviveDelay;
                _popup.PopupEntity(Loc.GetString("reviver-implant-started"), body, body);
                continue;
            }

            if (!down)
            {
                reviver.ReviveAt = null;
                reviver.CooldownUntil = now + reviver.Cost;
                _popup.PopupEntity(Loc.GetString("reviver-implant-recharging",
                        ("seconds", (int) reviver.Cost.TotalSeconds)),
                    body,
                    body);
                continue;
            }

            if (mobState.CurrentState == MobState.Dead && now >= reviveAt)
                TryRevive((uid, reviver), (body, mobState));

            Heal(reviver, body);
        }
    }

    private void Heal(ReviverImplantComponent reviver, EntityUid body)
    {
        if (!TryComp<DamageableComponent>(body, out var damageable))
            return;

        foreach (var (group, amount) in reviver.Heal)
        {
            if (!damageable.DamagePerGroup.TryGetValue(group, out var damage) || damage <= 0)
                continue;

            // Only head, chest and groin count towards crit and death here, so limbs get nothing.
            _damageable.HealEvenly((body, damageable), -amount, group,
                targetPart: TargetBodyPart.Vital,
                damageSplit: SplitDamageBehavior.SplitEnsureAllDamaged);
            if (reviver.HealCost.TryGetValue(group, out var cost))
                reviver.Cost += cost;
        }
    }

    // Mirrors the defibrillator checks and steps for bringing a dead body back.
    private void TryRevive(Entity<ReviverImplantComponent> ent, Entity<MobStateComponent> body)
    {
        if (_rotting.IsRotten(body) || HasComp<UnrevivableComponent>(body)
            || !TryComp<DamageableComponent>(body, out var damageable)
            || !_mobThreshold.TryGetThresholdForState(body, MobState.Dead, out var threshold)
            || _mobThreshold.CheckVitalDamage(body, damageable) >= threshold)
            return;

        if (HasComp<ConsciousnessComponent>(body) && _consciousness.TryGetNerveSystem(body, out _))
        {
            _consciousness.RemoveConsciousnessModifier(body, body, "Suffocation");
            _consciousness.RemoveConsciousnessModifier(body, body, "DeathThreshold");
        }

        _mobState.ChangeMobState(body, MobState.Critical, body.Comp, ent);
        ent.Comp.Cost += ent.Comp.ReviveCost;
        ent.Comp.ReviveAt = _timing.CurTime + ent.Comp.ReviveRetryDelay;

        _audio.PlayPvs(ent.Comp.ReviveSound, body);
        _popup.PopupEntity(Loc.GetString("reviver-implant-revived", ("target", body.Owner)),
            body,
            PopupType.MediumCaution);

        if (_mind.TryGetMind(body, out _, out var mind)
            && mind.CurrentEntity != body.Owner
            && _player.TryGetSessionById(mind.UserId, out var session))
            _eui.OpenEui(new ReturnToBodyEui(mind, _mind, _player), session);
    }
}
