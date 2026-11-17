using Content.Shared._Shitmed.Body.Organ;
using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.Body.Organ;
using Content.Shared.Body.Systems;

namespace Content.Shared._Arcane.Augments;

public sealed class BreathingTubeSystem : EntitySystem
{
    [Dependency] private readonly SharedInternalsSystem _internals = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<BreathingTubeComponent, OrganComponentsModifyEvent>(OnOrganComponentsModify);
        SubscribeLocalEvent<BreathingTubeComponent, OrganAddedToBodyEvent>(OnAddedToBody);
        SubscribeLocalEvent<BreathingTubeComponent, OrganRemovedFromBodyEvent>(OnRemovedFromBody);
    }

    private void OnAddedToBody(Entity<BreathingTubeComponent> ent, ref OrganAddedToBodyEvent args)
    {
        if (Comp<OrganComponent>(ent).Enabled)
            SetConnected(ent, args.Body, true);
    }

    private void OnRemovedFromBody(Entity<BreathingTubeComponent> ent, ref OrganRemovedFromBodyEvent args)
    {
        SetConnected(ent, args.OldBody, false);
    }

    private void OnOrganComponentsModify(Entity<BreathingTubeComponent> ent, ref OrganComponentsModifyEvent args)
    {
        SetConnected(ent, args.Body, args.Add);
    }

    private void SetConnected(Entity<BreathingTubeComponent> ent, EntityUid body, bool connected)
    {
        if (!TryComp<InternalsComponent>(body, out var internals))
            return;

        if (connected)
            _internals.ConnectBreathTool((body, internals), ent);
        else
            _internals.DisconnectBreathTool((body, internals), ent, forced: true);
    }
}
