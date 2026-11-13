namespace Content.Shared._Arcane.Body.Organs;

/// <summary>
/// Added to the body by an enabled cybernetic heart; boosts blood regeneration and bleed reduction.
/// </summary>
[RegisterComponent]
public sealed partial class CyberneticHeartBloodComponent : Component
{
    [DataField]
    public int ExtraBloodMultiplier = 8;

    [DataField]
    public float ExtraBleedReductionMultiplier = 1;
}
