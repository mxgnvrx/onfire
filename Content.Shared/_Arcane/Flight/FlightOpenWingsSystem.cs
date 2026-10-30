using Content.Shared._EinsteinEngines.Flight.Events;

namespace Content.Shared._Arcane.Flight;

public sealed class FlightOpenWingsSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FlightOpenWingsComponent, FlightEvent>(OnFlight);
    }

    private void OnFlight(Entity<FlightOpenWingsComponent> ent, ref FlightEvent args)
    {
        _appearance.SetData(ent, FlightOpenWingsVisuals.Open, args.IsFlying);
    }
}
