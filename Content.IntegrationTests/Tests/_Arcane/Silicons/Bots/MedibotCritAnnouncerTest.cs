using System.Collections.Generic;
using System.Linq;
using Content.Client.UserInterface.Systems.Chat;
using Content.Server._Arcane.Silicons.Bots;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Components;
using Content.Server.NPC.HTN;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared._Shitmed.Targeting;
using Content.Shared.Atmos;
using Content.Shared.Chat;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Inventory;
using Content.Shared.Mind;
using Robust.Client.UserInterface;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Arcane.Silicons.Bots;

[TestFixture]
[TestOf(typeof(MedibotCritAnnouncerSystem))]
public sealed class MedibotCritAnnouncerTest
{
    private static readonly ProtoId<DamageTypePrototype> Cellular = "Cellular";

    [Test]
    public async Task FailedTransmissionCanRetryWithoutDuplicateReports()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true, Dirty = true });
        var server = pair.Server;
        var map = await pair.CreateTestMap();
        var entMan = server.ResolveDependency<IEntityManager>();
        var protoMan = server.ResolveDependency<IPrototypeManager>();
        var chat = pair.Client.ResolveDependency<IUserInterfaceManager>().GetUIController<ChatUIController>();
        var bots = new HashSet<NetEntity>();
        EntityUid telecom = default;
        const string firstPatient = "First Patient";
        const string secondPatient = "Second Patient";

        string[] Reports() => chat.History
            .Where(entry => entry.Msg.Channel == ChatChannel.Radio && bots.Contains(entry.Msg.SenderEntity))
            .Select(entry => entry.Msg.Message)
            .ToArray();

        await server.WaitAssertion(() =>
        {
            var moles = new float[Atmospherics.AdjustedNumberOfGases];
            moles[(int) Gas.Oxygen] = 21.824779f;
            moles[(int) Gas.Nitrogen] = 82.10312f;
            entMan.System<AtmosphereSystem>().SetMapAtmosphere(map.MapUid, false, new GasMixture(moles, Atmospherics.T20C));

            for (var i = 0; i < 3; i++)
            {
                var bot = entMan.SpawnAtPosition("MobMedibot", map.GridCoords);
                // Keep the bots stationary while their independent scans run.
                entMan.System<HTNSystem>().SetHTNEnabled((bot, entMan.GetComponent<HTNComponent>(bot)), false);
                bots.Add(entMan.GetNetEntity(bot));
            }

            var listener = entMan.SpawnAtPosition("MobHuman", map.GridCoords);
            var headset = entMan.SpawnAtPosition("ClothingHeadsetMedical", map.GridCoords);
            Assert.That(entMan.System<InventorySystem>().TryEquip(listener, headset, "ears"), Is.True);
            server.PlayerMan.SetAttachedEntity(pair.Player!, listener);

            telecom = entMan.SpawnAtPosition("TelecomServerFilledMedical", map.GridCoords);
            SpawnHardCritPatient(entMan, protoMan, map.GridCoords, firstPatient);
        });
        await pair.RunTicksSync(5);
        await pair.Client.WaitAssertion(() => Assert.That(Reports(), Is.Empty));

        await server.WaitAssertion(() =>
        {
            Assert.That(entMan.GetComponent<ApcPowerReceiverComponent>(telecom).Powered, Is.False);
            entMan.System<PowerReceiverSystem>().SetNeedsPower(telecom, false);
        });
        await pair.RunSeconds(4);
        await server.WaitAssertion(() => Assert.That(entMan.GetComponent<ApcPowerReceiverComponent>(telecom).Powered, Is.True));
        await pair.Client.WaitAssertion(() => Assert.That(Reports(), Is.Empty));

        await pair.RunSeconds(28);
        await pair.Client.WaitAssertion(() =>
        {
            var reports = Reports();
            Assert.That(reports, Has.Length.EqualTo(1));
            Assert.That(reports[0], Does.Contain(firstPatient));
        });

        await server.WaitPost(() => SpawnHardCritPatient(entMan, protoMan, map.GridCoords, secondPatient));
        await pair.RunSeconds(4);
        await pair.Client.WaitAssertion(() => Assert.That(Reports(), Has.Length.EqualTo(1)));

        await pair.RunSeconds(28);
        await pair.Client.WaitAssertion(() =>
        {
            var reports = Reports();
            Assert.That(reports, Has.Length.EqualTo(2));
            Assert.That(reports[0], Does.Contain(firstPatient));
            Assert.That(reports[1], Does.Contain(secondPatient));
        });

        await pair.CleanReturnAsync();
    }

    private static void SpawnHardCritPatient(IEntityManager entMan, IPrototypeManager protoMan, EntityCoordinates coords, string name)
    {
        var patient = entMan.SpawnAtPosition("MobHuman", coords);
        entMan.System<MetaDataSystem>().SetEntityName(patient, name);
        // Keeps the patient in hard crit while the test waits out the shared cooldown.
        entMan.RemoveComponent<RespiratorComponent>(patient);

        var damage = new DamageSpecifier(protoMan.Index(Cellular), 160);
        entMan.System<DamageableSystem>().TryChangeDamage(patient, damage, true, targetPart: TargetBodyPart.Chest);

        var minds = entMan.System<SharedMindSystem>();
        minds.TransferTo(minds.CreateMind(null), patient);
    }
}
