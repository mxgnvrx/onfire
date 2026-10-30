using Content.Client.Humanoid;
using Content.Shared._Arcane.Flight;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;

namespace Content.Client._Arcane.Flight;

public sealed class FlightOpenWingsVisualizerSystem : VisualizerSystem<FlightOpenWingsComponent>
{
    [Dependency] private readonly MarkingManager _markings = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    private const string OpenStateSuffix = "_open";

    public override void Initialize()
    {
        base.Initialize();

        // Humanoid sprite updates rebuild marking layers with their default states.
        SubscribeLocalEvent<FlightOpenWingsComponent, HumanoidAppearanceUpdatedEvent>(OnHumanoidUpdated);
    }

    protected override void OnAppearanceChange(EntityUid uid, FlightOpenWingsComponent component, ref AppearanceChangeEvent args)
    {
        UpdateWings((uid, component, args.Sprite));
    }

    private void OnHumanoidUpdated(Entity<FlightOpenWingsComponent> ent, ref HumanoidAppearanceUpdatedEvent args)
    {
        UpdateWings((ent.Owner, ent.Comp, null));
    }

    private void UpdateWings(Entity<FlightOpenWingsComponent, SpriteComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp2, false)
            || !TryComp<HumanoidAppearanceComponent>(ent, out var humanoid)
            || !humanoid.MarkingSet.TryGetCategory(MarkingCategories.Wings, out var wings))
            return;

        AppearanceSystem.TryGetData<bool>(ent, FlightOpenWingsVisuals.Open, out var open);
        var spriteEnt = (ent.Owner, ent.Comp2);

        foreach (var marking in wings)
        {
            if (!_markings.TryGetMarking(marking, out var proto))
                continue;

            foreach (var spriteSpecifier in proto.Sprites)
            {
                if (spriteSpecifier is not SpriteSpecifier.Rsi rsi
                    || !_sprite.LayerMapTryGet(spriteEnt, $"{proto.ID}-{rsi.RsiState}", out var index, false))
                    continue;

                var state = open ? rsi.RsiState + OpenStateSuffix : rsi.RsiState;
                if (_sprite.LayerGetEffectiveRsi(spriteEnt, index)?.TryGetState(state, out _) != true)
                    continue;

                _sprite.LayerSetRsiState(spriteEnt, index, state);
            }
        }
    }
}
