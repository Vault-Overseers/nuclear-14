﻿#nullable enable
using System.Linq;
using Content.Server.Ghost;
using Content.Server.Ghost.Roles;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Mind;
using Content.Server.Mind.Commands;
using Content.Server.Mind.Components;
using Content.Server.Players;
using Content.Server.Roles;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Roles;
using Robust.Server.Console;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using IPlayerManager = Robust.Server.Player.IPlayerManager;

namespace Content.IntegrationTests.Tests.Minds;

[TestFixture]
public sealed partial class MindTests
{
    [TestPrototypes]
    private const string Prototypes = @"
- type: entity
  id: MindTestEntityDamageable
  components:
  - type: MindContainer
  - type: Damageable
    damageContainer: Biological
  - type: Body
    prototype: Human
    requiredLegs: 2
  - type: MobState
  - type: MobThresholds
    thresholds:
      0: Alive
      200: Dead
  - type: Destructible
    thresholds:
    - trigger:
        !type:DamageTypeTrigger
        damageType: Blunt
        damage: 400
        behaviors:
        - !type:GibBehavior { }
";

    [Test]
    public async Task TestCreateAndTransferMindToNewEntity()
    {
        await using var pairTracker = await PoolManager.GetServerClient();
        var server = pairTracker.Pair.Server;

        var entMan = server.ResolveDependency<IServerEntityManager>();

        await server.WaitAssertion(() =>
        {
            var mindSystem = entMan.EntitySysManager.GetEntitySystem<MindSystem>();

            var entity = entMan.SpawnEntity(null, new MapCoordinates());
            var mindComp = entMan.EnsureComponent<MindContainerComponent>(entity);

            var mind = mindSystem.CreateMind(null);

            Assert.That(mind.UserId, Is.EqualTo(null));

            mindSystem.TransferTo(mind, entity);
            Assert.That(mindSystem.GetMind(entity, mindComp), Is.EqualTo(mind));
        });

        await pairTracker.CleanReturnAsync();
    }

    [Test]
    public async Task TestReplaceMind()
    {
        await using var pairTracker = await PoolManager.GetServerClient();
        var server = pairTracker.Pair.Server;

        var entMan = server.ResolveDependency<IServerEntityManager>();

        await server.WaitAssertion(() =>
        {
            var mindSystem = entMan.EntitySysManager.GetEntitySystem<MindSystem>();

            var entity = entMan.SpawnEntity(null, new MapCoordinates());
            var mindComp = entMan.EnsureComponent<MindContainerComponent>(entity);

            var mind = mindSystem.CreateMind(null);
            mindSystem.TransferTo(mind, entity);
            Assert.That(mindSystem.GetMind(entity, mindComp), Is.EqualTo(mind));

            var mind2 = mindSystem.CreateMind(null);
            mindSystem.TransferTo(mind2, entity);
            Assert.Multiple(() =>
            {
                Assert.That(mindSystem.GetMind(entity, mindComp), Is.EqualTo(mind2));
                Assert.That(mind.OwnedEntity, Is.Not.EqualTo(entity));
            });
        });

        await pairTracker.CleanReturnAsync();
    }

    [Test]
    public async Task TestEntityDeadWhenGibbed()
    {
        await using var pairTracker = await PoolManager.GetServerClient();
        var server = pairTracker.Pair.Server;

        var entMan = server.ResolveDependency<IServerEntityManager>();
        var protoMan = server.ResolveDependency<IPrototypeManager>();

        EntityUid entity = default!;
        MindContainerComponent mindContainerComp = default!;
        Mind mind = default!;
        var mindSystem = entMan.EntitySysManager.GetEntitySystem<MindSystem>();
        var damageableSystem = entMan.EntitySysManager.GetEntitySystem<DamageableSystem>();

        await server.WaitAssertion(() =>
        {
            entity = entMan.SpawnEntity("MindTestEntityDamageable", new MapCoordinates());
            mindContainerComp = entMan.EnsureComponent<MindContainerComponent>(entity);

            mind = mindSystem.CreateMind(null);

            mindSystem.TransferTo(mind, entity);
            Assert.Multiple(() =>
            {
                Assert.That(mindSystem.GetMind(entity, mindContainerComp), Is.EqualTo(mind));
                Assert.That(!mindSystem.IsCharacterDeadPhysically(mind));
            });
        });

        await PoolManager.RunTicksSync(pairTracker.Pair, 5);

        await server.WaitAssertion(() =>
        {
            var damageable = entMan.GetComponent<DamageableComponent>(entity);
            if (!protoMan.TryIndex<DamageTypePrototype>("Blunt", out var prototype))
            {
                return;
            }

            damageableSystem.SetDamage(entity, damageable, new DamageSpecifier(prototype, FixedPoint2.New(401)));
            Assert.That(mindSystem.GetMind(entity, mindContainerComp), Is.EqualTo(mind));
        });

        await PoolManager.RunTicksSync(pairTracker.Pair, 5);

        await server.WaitAssertion(() =>
        {
            Assert.That(mindSystem.IsCharacterDeadPhysically(mind));
        });

        await pairTracker.CleanReturnAsync();
    }

    [Test]
    public async Task TestMindTransfersToOtherEntity()
    {
        await using var pairTracker = await PoolManager.GetServerClient();
        var server = pairTracker.Pair.Server;

        var entMan = server.ResolveDependency<IServerEntityManager>();

        await server.WaitAssertion(() =>
        {
            var mindSystem = entMan.EntitySysManager.GetEntitySystem<MindSystem>();

            var entity = entMan.SpawnEntity(null, new MapCoordinates());
            var targetEntity = entMan.SpawnEntity(null, new MapCoordinates());
            var mindComp = entMan.EnsureComponent<MindContainerComponent>(entity);
            entMan.EnsureComponent<MindContainerComponent>(targetEntity);

            var mind = mindSystem.CreateMind(null);

            mindSystem.TransferTo(mind, entity);

            Assert.That(mindSystem.GetMind(entity, mindComp), Is.EqualTo(mind));

            mindSystem.TransferTo(mind, targetEntity);
            Assert.Multiple(() =>
            {
                Assert.That(mindSystem.GetMind(entity, mindComp), Is.EqualTo(null));
                Assert.That(mindSystem.GetMind(targetEntity), Is.EqualTo(mind));
            });
        });

        await pairTracker.CleanReturnAsync();
    }

    [Test]
    public async Task TestOwningPlayerCanBeChanged()
    {
        await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings
        {
            Connected = true,
            DummyTicker = false
        });
        var server = pairTracker.Pair.Server;

        var entMan = server.ResolveDependency<IServerEntityManager>();

        await PoolManager.RunTicksSync(pairTracker.Pair, 5);
        var mindSystem = entMan.EntitySysManager.GetEntitySystem<MindSystem>();
        var originalMind = GetMind(pairTracker.Pair);
        var userId = originalMind.UserId;

        Mind mind = default!;
        await server.WaitAssertion(() =>
        {
            var entity = entMan.SpawnEntity(null, new MapCoordinates());
            var mindComp = entMan.EnsureComponent<MindContainerComponent>(entity);
            entMan.DirtyEntity(entity);

            mind = mindSystem.CreateMind(null);
            mindSystem.TransferTo(mind, entity);
            Assert.Multiple(() =>
            {
                Assert.That(mindSystem.GetMind(entity, mindComp), Is.EqualTo(mind));
                Assert.That(mindComp.HasMind);
            });
        });

        await PoolManager.RunTicksSync(pairTracker.Pair, 5);

        await server.WaitAssertion(() =>
        {
            mindSystem.SetUserId(mind, userId);
            Assert.Multiple(() =>
            {
                Assert.That(mind.UserId, Is.EqualTo(userId));
                Assert.That(originalMind.UserId, Is.EqualTo(null));
            });

            mindSystem.SetUserId(originalMind, userId);
            Assert.Multiple(() =>
            {
                Assert.That(mind.UserId, Is.EqualTo(null));
                Assert.That(originalMind.UserId, Is.EqualTo(userId));
            });
        });

        await PoolManager.RunTicksSync(pairTracker.Pair, 5);

        await pairTracker.CleanReturnAsync();
    }

    [Test]
    public async Task TestAddRemoveHasRoles()
    {
        await using var pairTracker = await PoolManager.GetServerClient();
        var server = pairTracker.Pair.Server;

        var entMan = server.ResolveDependency<IServerEntityManager>();

        await server.WaitAssertion(() =>
        {
            var mindSystem = entMan.EntitySysManager.GetEntitySystem<MindSystem>();

            var entity = entMan.SpawnEntity(null, new MapCoordinates());
            var mindComp = entMan.EnsureComponent<MindContainerComponent>(entity);

            var mind = mindSystem.CreateMind(null);

            Assert.That(mind.UserId, Is.EqualTo(null));

            mindSystem.TransferTo(mind, entity);
            Assert.That(mindSystem.GetMind(entity, mindComp), Is.EqualTo(mind));

            Assert.Multiple(() =>
            {
                Assert.That(mindSystem.HasRole<TraitorRole>(mind), Is.False);
                Assert.That(mindSystem.HasRole<Job>(mind), Is.False);
            });

            var traitorRole = new TraitorRole(mind, new AntagPrototype());

            mindSystem.AddRole(mind, traitorRole);

            Assert.Multiple(() =>
            {
                Assert.That(mindSystem.HasRole<TraitorRole>(mind));
                Assert.That(mindSystem.HasRole<Job>(mind), Is.False);
            });

            var jobRole = new Job(mind, new JobPrototype());

            mindSystem.AddRole(mind, jobRole);

            Assert.Multiple(() =>
            {
                Assert.That(mindSystem.HasRole<TraitorRole>(mind));
                Assert.That(mindSystem.HasRole<Job>(mind));
            });

            mindSystem.RemoveRole(mind, traitorRole);

            Assert.Multiple(() =>
            {
                Assert.That(mindSystem.HasRole<TraitorRole>(mind), Is.False);
                Assert.That(mindSystem.HasRole<Job>(mind));
            });

            mindSystem.RemoveRole(mind, jobRole);

            Assert.Multiple(() =>
            {
                Assert.That(mindSystem.HasRole<TraitorRole>(mind), Is.False);
                Assert.That(mindSystem.HasRole<Job>(mind), Is.False);
            });
        });

        await pairTracker.CleanReturnAsync();
    }

    [Test]
    public async Task TestPlayerCanGhost()
    {
        // Client is needed to spawn session
        await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings { Connected = true });
        var server = pairTracker.Pair.Server;

        var entMan = server.ResolveDependency<IServerEntityManager>();
        var playerMan = server.ResolveDependency<IPlayerManager>();

        var mindSystem = entMan.EntitySysManager.GetEntitySystem<MindSystem>();
        var ghostSystem = entMan.EntitySysManager.GetEntitySystem<GhostSystem>();

        EntityUid entity = default!;
        Mind mind = default!;
        var player = playerMan.ServerSessions.Single();

        await server.WaitAssertion(() =>
        {
            entity = entMan.SpawnEntity(null, new MapCoordinates());
            var mindComp = entMan.EnsureComponent<MindContainerComponent>(entity);

            mind = mindSystem.CreateMind(player.UserId, "Mindy McThinker");

            Assert.That(mind.UserId, Is.EqualTo(player.UserId));

            mindSystem.TransferTo(mind, entity);
            Assert.That(mindSystem.GetMind(entity, mindComp), Is.EqualTo(mind));
        });

        await PoolManager.RunTicksSync(pairTracker.Pair, 5);

        await server.WaitAssertion(() =>
        {
            entMan.DeleteEntity(entity);
        });

        await PoolManager.RunTicksSync(pairTracker.Pair, 5);

        EntityUid mob = default!;
        Mind mobMind = default!;

        await server.WaitAssertion(() =>
        {
            Assert.That(mind.OwnedEntity, Is.Not.Null);

            mob = entMan.SpawnEntity(null, new MapCoordinates());

            MakeSentientCommand.MakeSentient(mob, IoCManager.Resolve<IEntityManager>());
            mobMind = mindSystem.CreateMind(player.UserId, "Mindy McThinker the Second");

            mindSystem.SetUserId(mobMind, player.UserId);
            mindSystem.TransferTo(mobMind, mob);
        });

        await PoolManager.RunTicksSync(pairTracker.Pair, 5);

        await server.WaitAssertion(() =>
        {
            var m = player.ContentData()?.Mind;
            Assert.That(m, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(m!.OwnedEntity, Is.EqualTo(mob));
                Assert.That(m, Is.Not.EqualTo(mind));
            });
        });

        await pairTracker.CleanReturnAsync();
    }

    // TODO Implement
    /*[Test]
    public async Task TestPlayerCanReturnFromGhostWhenDead()
    {
    }*/

    [Test]
    public async Task TestGhostDoesNotInfiniteLoop()
    {
        await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings
        {
            DummyTicker = false,
            Connected = true,
            Dirty = true
        });
        var server = pairTracker.Pair.Server;

        var entMan = server.ResolveDependency<IServerEntityManager>();
        var playerMan = server.ResolveDependency<IPlayerManager>();
        var serverConsole = server.ResolveDependency<IServerConsoleHost>();

        //EntityUid entity = default!;
        EntityUid ghostRole = default!;
        EntityUid ghost = default!;
        Mind mind = default!;
        var player = playerMan.ServerSessions.Single();

        await server.WaitAssertion(() =>
        {
            // entity = entMan.SpawnEntity(null, new MapCoordinates());
            // var mindComp = entMan.EnsureComponent<MindContainerComponent>(entity);

            // mind = mindSystem.CreateMind(player.UserId, "Mindy McThinker");
            //
            // Assert.That(mind.UserId, Is.EqualTo(player.UserId));
            //
            // mindSystem.TransferTo(mind, entity);
            // Assert.That(mindSystem.GetMind(entity, mindComp), Is.EqualTo(mind));

            var data = player.ContentData();

            Assert.That(data?.Mind, Is.Not.EqualTo(null));
            mind = data!.Mind!;

            Assert.That(mind.OwnedEntity, Is.Not.Null);

            ghostRole = entMan.SpawnEntity("GhostRoleTestEntity", MapCoordinates.Nullspace);
        });

        await PoolManager.RunTicksSync(pairTracker.Pair, 20);

        await server.WaitAssertion(() =>
        {
            serverConsole.ExecuteCommand(player, "aghost");
        });

        await PoolManager.RunTicksSync(pairTracker.Pair, 20);

        await server.WaitAssertion(() =>
        {
            var id = entMan.GetComponent<GhostRoleComponent>(ghostRole).Identifier;
            entMan.EntitySysManager.GetEntitySystem<GhostRoleSystem>().Takeover(player, id);
        });

        await PoolManager.RunTicksSync(pairTracker.Pair, 20);

        await server.WaitAssertion(() =>
        {
            var data = player.ContentData()!;
            Assert.That(data.Mind!.OwnedEntity, Is.EqualTo(ghostRole));

            serverConsole.ExecuteCommand(player, "aghost");
            Assert.That(player.AttachedEntity, Is.Not.Null);
            ghost = player.AttachedEntity!.Value;
        });

        await PoolManager.RunTicksSync(pairTracker.Pair, 20);

        await server.WaitAssertion(() =>
        {
            Assert.That(player.AttachedEntity, Is.Not.Null);
            Assert.That(player.AttachedEntity!.Value, Is.EqualTo(ghost));
        });

        await pairTracker.CleanReturnAsync();
    }
}
