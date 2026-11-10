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

    [DataField, AutoNetworkedField]
    public bool LightOn;

    [DataField, AutoNetworkedField]
    public bool HadLight;
    [DataField, AutoNetworkedField]
    public bool PreviousEnabled;
    [DataField, AutoNetworkedField]
    public float PreviousRadius;
    [DataField, AutoNetworkedField]
    public float PreviousEnergy;
    [DataField, AutoNetworkedField]
    public Color PreviousColor;
}
