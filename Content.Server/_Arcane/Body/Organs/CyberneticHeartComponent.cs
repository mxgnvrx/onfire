namespace Content.Server._Arcane.Body.Organs;

[RegisterComponent]
public sealed partial class CyberneticHeartComponent : Component
{
    [DataField]
    public int ExtraBloodMultiplier = 8;

    [DataField]
    public float ExtraBleedReductionMultiplier = 1;

    [DataField]
    public TimeSpan StabilizationDuration = TimeSpan.FromSeconds(10);

    [DataField]
    public TimeSpan StabilizationCooldown = TimeSpan.FromMinutes(5);

    public TimeSpan NextStabilization;
    public TimeSpan StabilizationEnd;
    public EntityUid? StabilizedBody;
    public float PreviousWalkModifier;
    public float PreviousSprintModifier;
    public bool AddedMovement;
}
