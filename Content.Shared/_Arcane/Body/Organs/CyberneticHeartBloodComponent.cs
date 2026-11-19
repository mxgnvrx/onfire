namespace Content.Shared._Arcane.Body.Organs;

/// <summary>
/// Added to the body by an enabled cybernetic heart to support clotting.
/// </summary>
[RegisterComponent]
public sealed partial class CyberneticHeartBloodComponent : Component
{
    public EntityUid Implant;

    [DataField]
    public int ExtraBloodMultiplier = 0;

    [DataField]
    public float ExtraBleedReductionMultiplier = 0.25f;
}
