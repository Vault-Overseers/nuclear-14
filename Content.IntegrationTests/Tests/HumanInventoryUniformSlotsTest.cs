using System.Threading.Tasks;
using Content.Shared.Inventory;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests
{
    // Tests the behavior of InventoryComponent.
    // i.e. the interaction between uniforms and the pocket/ID slots.
    // and also how big items don't fit in pockets.
    [TestFixture]
    public sealed class HumanInventoryUniformSlotsTest
    {
        private const string Prototypes = @"
- type: entity
  name: HumanDummy
  id: HumanDummy
  components:
  - type: Inventory
  - type: ContainerContainer

- type: entity
  name: UniformDummy
  id: UniformDummy
  components:
  - type: Clothing
    slots: [innerclothing]
  - type: Item
    size: 5

- type: entity
  name: IDCardDummy
  id: IDCardDummy
  components:
  - type: Clothing
    slots:
    - idcard
  - type: Item
    size: 5
  - type: IdCard

- type: entity
  name: FlashlightDummy
  id: FlashlightDummy
  components:
  - type: Item
    size: 5

- type: entity
  name: ToolboxDummy
  id: ToolboxDummy
  components:
  - type: Item
    size: 9999
";
        [Test]
        public async Task Test()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true, ExtraPrototypes = Prototypes});
            var server = pairTracker.Pair.Server;

            EntityUid human = default;
            EntityUid uniform = default;
            EntityUid idCard = default;
            EntityUid pocketItem = default;

            InventorySystem invSystem = default!;

            await server.WaitAssertion(() =>
            {
                invSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<InventorySystem>();
                var mapMan = IoCManager.Resolve<IMapManager>();

                mapMan.CreateNewMapEntity(MapId.Nullspace);

                var entityMan = IoCManager.Resolve<IEntityManager>();

                human = entityMan.SpawnEntity("HumanDummy", MapCoordinates.Nullspace);
                uniform = entityMan.SpawnEntity("UniformDummy", MapCoordinates.Nullspace);
                idCard = entityMan.SpawnEntity("IDCardDummy", MapCoordinates.Nullspace);
                pocketItem = entityMan.SpawnEntity("FlashlightDummy", MapCoordinates.Nullspace);
                var tooBigItem = entityMan.SpawnEntity("ToolboxDummy", MapCoordinates.Nullspace);


                Assert.That(invSystem.CanEquip(human, uniform, "jumpsuit", out _));

                // Can't equip any of these since no uniform!
                Assert.That(invSystem.CanEquip(human, idCard, "id", out _), Is.False);
                Assert.That(invSystem.CanEquip(human, pocketItem, "pocket1", out _), Is.False);
                Assert.That(invSystem.CanEquip(human, tooBigItem, "pocket2", out _), Is.False); // This one fails either way.

                Assert.That(invSystem.TryEquip(human, uniform, "jumpsuit"));

                Assert.That(invSystem.TryEquip(human, idCard, "id"));
                Assert.That(invSystem.CanEquip(human, tooBigItem, "pocket1", out _), Is.False); // Still failing!
                Assert.That(invSystem.TryEquip(human, pocketItem, "pocket1"));

                Assert.That(IsDescendant(idCard, human));
                Assert.That(IsDescendant(pocketItem, human));

                // Now drop the jumpsuit.
                Assert.That(invSystem.TryUnequip(human, "jumpsuit"));
            });

            await server.WaitRunTicks(2);

            await server.WaitAssertion(() =>
            {
                // Items have been dropped!
                Assert.That(IsDescendant(uniform, human), Is.False);
                Assert.That(IsDescendant(idCard, human), Is.False);
                Assert.That(IsDescendant(pocketItem, human), Is.False);

                // Ensure everything null here.
                Assert.That(!invSystem.TryGetSlotEntity(human, "jumpsuit", out _));
                Assert.That(!invSystem.TryGetSlotEntity(human, "id", out _));
                Assert.That(!invSystem.TryGetSlotEntity(human, "pocket1", out _));
            });

            await pairTracker.CleanReturnAsync();
        }

        private static bool IsDescendant(EntityUid descendant, EntityUid parent)
        {
            var tmpParent = IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(descendant).Parent;
            while (tmpParent != null)
            {
                if (tmpParent.Owner == parent)
                {
                    return true;
                }

                tmpParent = tmpParent.Parent;
            }

            return false;
        }
    }
}
