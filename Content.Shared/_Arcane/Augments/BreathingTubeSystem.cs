using Content.Shared._Shitmed.Body.Organ;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;

namespace Content.Shared._Arcane.Augments;

public sealed class BreathingTubeSystem : EntitySystem
{
    [Dependency] private readonly SharedInternalsSystem _internals = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<BreathingTubeComponent, OrganComponentsModifyEvent>(OnOrganComponentsModify);
    }

    private void OnOrganComponentsModify(Entity<BreathingTubeComponent> ent, ref OrganComponentsModifyEvent args)
    {
        if (!TryComp<InternalsComponent>(args.Body, out var internals))
            return;

        if (args.Add)
            _internals.ConnectBreathTool((args.Body, internals), ent);
        else
            _internals.DisconnectBreathTool((args.Body, internals), ent, forced: true);
    }
}
