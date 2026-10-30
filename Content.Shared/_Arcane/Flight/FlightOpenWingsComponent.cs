namespace Content.Shared._Arcane.Flight;

/// <summary>
/// Switches wing markings to their open states while the entity is flying.
/// </summary>
[RegisterComponent]
public sealed partial class FlightOpenWingsComponent : Component
{
    [DataField]
    public string OpenStateSuffix = "_open";
}
