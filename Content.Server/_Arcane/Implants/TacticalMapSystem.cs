// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server._Goobstation.Wizard.Store;
using Content.Server.Antag;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Ghost.Roles.Events;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.Pinpointer;
using Content.Shared.Station;
using Content.Shared.Station.Components;
using Robust.Shared.Containers;

namespace Content.Server._Arcane.Implants;

public sealed class TacticalMapSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _containers = default!;
    [Dependency] private readonly SharedStationSystem _station = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<AfterAntagEntitySelectedEvent>(OnAntagSelected);
        SubscribeLocalEvent<NukeopsTargetStationSelectedEvent>(OnTargetStationSelected);
        SubscribeLocalEvent<GhostRoleSpawnerUsedEvent>(OnReinforcementSpawned);
        SubscribeLocalEvent<TacticalMapOperationComponent, ItemPurchasedEvent>(OnRadioPurchased);
        SubscribeLocalEvent<TacticalMapImplantComponent, ImplantImplantedEvent>(OnImplanted);
    }

    private void OnAntagSelected(ref AfterAntagEntitySelectedEvent args)
    {
        if (HasComp<NukeopsRuleComponent>(args.GameRule.Owner))
            SetOperation(args.EntityUid, args.GameRule.Owner);
    }

    private void OnRadioPurchased(Entity<TacticalMapOperationComponent> ent, ref ItemPurchasedEvent args)
    {
        if (TryComp<TacticalMapOperationComponent>(args.Buyer, out var operation))
            ent.Comp.Rule = operation.Rule;
    }

    private void OnReinforcementSpawned(GhostRoleSpawnerUsedEvent args)
    {
        if (TryComp<TacticalMapOperationComponent>(args.Spawner, out var operation) && operation.Rule is { } rule)
            SetOperation(args.Spawned, rule);
    }

    private void OnImplanted(Entity<TacticalMapImplantComponent> ent, ref ImplantImplantedEvent args)
    {
        var operation = CompOrNull<TacticalMapOperationComponent>(args.Implanted);
        SetTargetGrid(ent, GetTargetGrid(operation?.Rule));
    }

    private void OnTargetStationSelected(ref NukeopsTargetStationSelectedEvent args)
    {
        var grid = GetTargetGrid(args.RuleEntity);
        var query = EntityQueryEnumerator<TacticalMapOperationComponent, ImplantedComponent>();
        while (query.MoveNext(out var uid, out var operation, out _))
        {
            if (operation.Rule == args.RuleEntity)
                UpdateImplants(uid, grid);
        }
    }

    private void SetOperation(EntityUid body, EntityUid rule)
    {
        EnsureComp<TacticalMapOperationComponent>(body).Rule = rule;
        UpdateImplants(body, GetTargetGrid(rule));
    }

    private EntityUid? GetTargetGrid(EntityUid? rule)
    {
        return TryComp<NukeopsRuleComponent>(rule, out var nukeops)
            && nukeops.TargetStation is { } station
            && TryComp<StationDataComponent>(station, out var data)
            ? _station.GetLargestGrid((station, data))
            : null;
    }

    private void UpdateImplants(EntityUid body, EntityUid? grid)
    {
        if (!_containers.TryGetContainer(body, ImplanterComponent.ImplantSlotId, out var implants))
            return;

        foreach (var implant in implants.ContainedEntities)
        {
            if (HasComp<TacticalMapImplantComponent>(implant))
                SetTargetGrid(implant, grid);
        }
    }

    private void SetTargetGrid(EntityUid implant, EntityUid? grid)
    {
        if (!TryComp<StationMapComponent>(implant, out var map) || map.TargetGrid == grid)
            return;

        map.TargetGrid = grid;
        Dirty(implant, map);
    }
}
