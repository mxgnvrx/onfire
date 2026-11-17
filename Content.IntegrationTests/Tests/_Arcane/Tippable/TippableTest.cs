using Content.IntegrationTests.Tests.Interaction;
using Content.Server.NPC.HTN;
using Content.Shared._Arcane.Tippable;
using Content.Shared.Movement.Components;
using Content.Shared.Rotation;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests._Arcane.Tippable;

[TestOf(typeof(SharedTippableSystem))]
public sealed class TippableTest : InteractionTest
{
    private const string Medibot = "MobMedibot";

    private async Task SpawnMedibot()
    {
        await SpawnTarget(Medibot);
        // These tests drive the player interaction, so the bot AI must not walk out of reach.
        await Server.WaitPost(() => SEntMan.RemoveComponent<HTNComponent>(STarget!.Value));
    }

    [Test]
    public async Task TipOverAndRightMedibot()
    {
        await SpawnMedibot();
        var appearance = SEntMan.System<SharedAppearanceSystem>();

        await Interact(altInteract: true);

        Assert.Multiple(() =>
        {
            Assert.That(Comp<TippableComponent>().Tipped, Is.True);
            Assert.That(Comp<InputMoverComponent>().CanMove, Is.False);
            Assert.That(appearance.TryGetData<RotationState>(STarget!.Value, RotationVisuals.RotationState, out var state), Is.True);
            Assert.That(state, Is.EqualTo(RotationState.Horizontal));
            Assert.That(Comp<RotationVisualsComponent>().HorizontalRotation.EqualsApprox(Angle.FromDegrees(180)), Is.True);
        });

        await Interact(altInteract: true);

        Assert.Multiple(() =>
        {
            Assert.That(Comp<TippableComponent>().Tipped, Is.False);
            Assert.That(Comp<InputMoverComponent>().CanMove, Is.True);
        });
    }

    [Test]
    public async Task MedibotRightsItselfAfterTimeout()
    {
        await SpawnMedibot();

        await Interact(altInteract: true);
        Assert.That(Comp<TippableComponent>().Tipped, Is.True);

        await RunSeconds(211);

        Assert.Multiple(() =>
        {
            Assert.That(Comp<TippableComponent>().Tipped, Is.False);
            Assert.That(Comp<InputMoverComponent>().CanMove, Is.True);
        });
    }
}
