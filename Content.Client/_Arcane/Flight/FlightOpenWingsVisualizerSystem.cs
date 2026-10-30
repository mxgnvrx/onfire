using Content.Client.Humanoid;
using Content.Shared._Arcane.Flight;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
using Robust.Shared.Utility;

namespace Content.Client._Arcane.Flight;

public sealed class FlightOpenWingsVisualizerSystem : VisualizerSystem<FlightOpenWingsComponent>
{
    [Dependency] private readonly AnimationPlayerSystem _animation = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly MarkingManager _markings = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    private const string OpenRsiSuffix = "_open";
    private const string UnfoldStateSuffix = "_unfold";
    private const string FoldStateSuffix = "_fold";
    private const string AnimationKey = "flight-open-wings";

    public override void Initialize()
    {
        base.Initialize();

        // Humanoid sprite updates rebuild marking layers with their default sprites.
        SubscribeLocalEvent<FlightOpenWingsComponent, HumanoidAppearanceUpdatedEvent>(OnHumanoidUpdated);
        SubscribeLocalEvent<FlightOpenWingsComponent, AnimationCompletedEvent>(OnAnimationCompleted);
    }

    protected override void OnAppearanceChange(EntityUid uid, FlightOpenWingsComponent component, ref AppearanceChangeEvent args)
    {
        AppearanceSystem.TryGetData<bool>(uid, FlightOpenWingsVisuals.Open, out var open, args.Component);

        var applied = component.AppliedOpen;
        component.AppliedOpen = open;

        if (applied == open)
            return;

        UpdateWings((uid, component, args.Sprite), applied != null);
    }

    private void OnHumanoidUpdated(Entity<FlightOpenWingsComponent> ent, ref HumanoidAppearanceUpdatedEvent args)
    {
        UpdateWings((ent.Owner, ent.Comp, null), false);
    }

    private void OnAnimationCompleted(Entity<FlightOpenWingsComponent> ent, ref AnimationCompletedEvent args)
    {
        if (args.Key != AnimationKey || !args.Finished)
            return;

        UpdateWings((ent.Owner, ent.Comp, null), false);
    }

    private void UpdateWings(Entity<FlightOpenWingsComponent, SpriteComponent?> ent, bool animate)
    {
        if (!Resolve(ent, ref ent.Comp2, false)
            || !TryComp<HumanoidAppearanceComponent>(ent, out var humanoid)
            || !humanoid.MarkingSet.TryGetCategory(MarkingCategories.Wings, out var wings))
            return;

        AppearanceSystem.TryGetData<bool>(ent, FlightOpenWingsVisuals.Open, out var open);
        var spriteEnt = (ent.Owner, ent.Comp2);

        _animation.Stop(ent.Owner, AnimationKey);
        var animation = new Animation();

        foreach (var marking in wings)
        {
            if (!_markings.TryGetMarking(marking, out var proto))
                continue;

            foreach (var spriteSpecifier in proto.Sprites)
            {
                if (spriteSpecifier is not SpriteSpecifier.Rsi rsi)
                    continue;

                var layerKey = $"{proto.ID}-{rsi.RsiState}";
                if (!_sprite.LayerMapTryGet(spriteEnt, layerKey, out var index, false))
                    continue;

                // Open wings live in a separate, larger RSI so the folded sprite keeps the regular 32px bounds.
                var openPath = rsi.RsiPath.Directory / $"{rsi.RsiPath.FilenameWithoutExtension}{OpenRsiSuffix}.rsi";
                var transitionState = rsi.RsiState + (open ? UnfoldStateSuffix : FoldStateSuffix);

                if (animate && TryGetRsiState(openPath, transitionState, out var openRsi, out var transition))
                {
                    _sprite.LayerSetRsi(spriteEnt, index, openRsi, transitionState);
                    animation.AnimationTracks.Add(new AnimationTrackSpriteFlick
                    {
                        LayerKey = layerKey,
                        KeyFrames = { new AnimationTrackSpriteFlick.KeyFrame(transitionState, 0f) },
                    });

                    var length = TimeSpan.FromSeconds(transition.AnimationLength);
                    if (length > animation.Length)
                        animation.Length = length;

                    continue;
                }

                if (!TryGetRsiState(open ? openPath : rsi.RsiPath, rsi.RsiState, out var finalRsi, out _))
                    continue;

                _sprite.LayerSetRsi(spriteEnt, index, finalRsi, rsi.RsiState);
                _sprite.LayerSetAutoAnimated(spriteEnt, index, true);
            }
        }

        if (animation.AnimationTracks.Count > 0)
            _animation.Play(ent.Owner, animation, AnimationKey);
    }

    private bool TryGetRsiState(ResPath path, string state, out RSI rsi, out RSI.State rsiState)
    {
        rsi = default!;
        rsiState = default!;

        if (!_resourceCache.TryGetResource<RSIResource>(SpriteSpecifierSerializer.TextureRoot / path, out var resource)
            || !resource.RSI.TryGetState(state, out var found))
            return false;

        rsi = resource.RSI;
        rsiState = found;
        return true;
    }
}
