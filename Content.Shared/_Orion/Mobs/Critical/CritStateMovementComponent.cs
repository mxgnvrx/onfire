using Robust.Shared.GameStates;

namespace Content.Shared._Orion.Mobs.Critical;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState] // Arcane-Edit
public sealed partial class CritStateMovementComponent : Component
{
    [DataField, AutoNetworkedField] // Arcane-Edit
    public float SoftCritWalkModifier = 0.25f;

    [DataField, AutoNetworkedField] // Arcane-Edit
    public float SoftCritSprintModifier = 0.25f;

    [DataField]
    public float SoftCritBreathChance = 0.5f;

    [DataField]
    public float SoftCritSuffocationRecoveryMultiplier;

    [DataField]
    public float HardCritSuffocationRecoveryMultiplier = -1f;
}
