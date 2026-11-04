using Content.Shared._Arcane.Augments;
using Content.Shared.Body.Organ;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Robust.Shared.Timing;

namespace Content.Server._Arcane.Augments;

public sealed class NutrimentPumpSystem : EntitySystem
{
    [Dependency] private readonly HungerSystem _hunger = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<NutrimentPumpComponent, OrganComponent>();
        while (query.MoveNext(out _, out var pump, out var organ))
        {
            if (_timing.CurTime < pump.NextUpdate)
                continue;

            pump.NextUpdate = _timing.CurTime + pump.UpdateInterval;
            if (!organ.Enabled || !TryComp<HungerComponent>(organ.Body, out var hunger)
                || _hunger.GetHunger(hunger) > hunger.Thresholds[pump.Threshold])
                continue;

            var nutrition = MathF.Round(pump.Nutrition * hunger.Thresholds[HungerThreshold.Okay] / pump.FedThreshold);
            _hunger.ModifyHunger(organ.Body.Value, nutrition, hunger);
            _popup.PopupEntity(Loc.GetString("cybernetics-nutriment-pump-feed"), organ.Body.Value, organ.Body.Value);
        }
    }
}
