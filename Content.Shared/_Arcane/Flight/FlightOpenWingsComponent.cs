namespace Content.Shared._Arcane.Flight;

/// <summary>
/// Switches wing markings to their open states while the entity is flying.
/// </summary>
[RegisterComponent]
public sealed partial class FlightOpenWingsComponent : Component
{
    // Client-only. Null until the first appearance update, so entering PVS mid-flight skips the unfold animation.
    [ViewVariables]
    public bool? AppliedOpen;
}
