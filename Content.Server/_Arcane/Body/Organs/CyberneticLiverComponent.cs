using Content.Goobstation.Maths.FixedPoint;

namespace Content.Server._Arcane.Body.Organs;

[RegisterComponent]
public sealed partial class CyberneticLiverComponent : Component
{
    [DataField]
    public FixedPoint2 Tolerance = 10;

    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);

    public TimeSpan NextUpdate;
}
