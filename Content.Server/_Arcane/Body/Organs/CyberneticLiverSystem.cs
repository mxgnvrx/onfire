using Content.Goobstation.Maths.FixedPoint;
using Content.Shared.Body.Components;
using Content.Shared.Body.Organ;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Arcane.Body.Organs;

public sealed class CyberneticLiverSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutions = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<CyberneticLiverComponent, OrganComponent>();
        while (query.MoveNext(out _, out var liver, out var organ))
        {
            if (_timing.CurTime < liver.NextUpdate)
                continue;

            liver.NextUpdate = _timing.CurTime + liver.UpdateInterval;
            if (!organ.Enabled || !TryComp<BloodstreamComponent>(organ.Body, out var bloodstream)
                || !_solutions.TryGetSolution(organ.Body.Value, bloodstream.BloodSolutionName, out var solution, out var chemicals))
                continue;

            var total = FixedPoint2.Zero;
            foreach (var reagent in chemicals.Contents)
            {
                if (_prototypes.Index<ReagentPrototype>(reagent.Reagent.Prototype).Metabolisms?.ContainsKey("Poison") == true)
                    total += reagent.Quantity;
            }

            if (total <= 0 || total > liver.Tolerance)
                continue;

            for (var i = chemicals.Contents.Count - 1; i >= 0; i--)
            {
                var reagent = chemicals.Contents[i];
                if (_prototypes.Index<ReagentPrototype>(reagent.Reagent.Prototype).Metabolisms?.ContainsKey("Poison") == true)
                    _solutions.RemoveReagent(solution.Value, reagent.Reagent, reagent.Quantity);
            }
        }
    }
}
