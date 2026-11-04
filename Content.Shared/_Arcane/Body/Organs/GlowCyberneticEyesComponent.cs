using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Arcane.Body.Organs;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GlowCyberneticEyesComponent : Component
{
    [DataField]
    public float Radius = 4;

    [DataField]
    public float Energy = 2;

    [DataField]
    public EntProtoId ToggleAction = "ActionToggleCyberneticEyes";

    [DataField, AutoNetworkedField]
    public EntityUid? Action;

    public bool AddedFlashlight;
    public EntityUid? PreviousAction;
    public bool HadLight;
    public bool PreviousEnabled;
    public float PreviousRadius;
    public float PreviousEnergy;
    public Color PreviousColor;
}
