using Content.Shared.Nutrition.Components;
using Robust.Shared.GameStates;

namespace Content.Shared._Arcane.Augments;

[RegisterComponent, NetworkedComponent]
public sealed partial class NutrimentPumpComponent : Component
{
    [DataField]
    public HungerThreshold Threshold = HungerThreshold.Starving;

    [DataField]
    public float Nutrition = 50;

    [DataField]
    public float FedThreshold = 350;

    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(5);

    public TimeSpan NextUpdate;
}
