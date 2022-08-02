using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Content.Shared.CCVar;
using Content.Shared.Coordinates;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests
{
    [TestFixture]
    [TestOf(typeof(EntityUid))]
    public sealed class EntityTest
    {
        [Test]
        public async Task SpawnTest()
        {
            //TODO: Run this test in a for loop, and figure out why garbage is ending up in the Entities list on cleanup.
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true, Dirty = true, Destructive = true});
            var server = pairTracker.Pair.Server;

            var mapManager = server.ResolveDependency<IMapManager>();
            var entityMan = server.ResolveDependency<IEntityManager>();
            var prototypeMan = server.ResolveDependency<IPrototypeManager>();
            var tileDefinitionManager = server.ResolveDependency<ITileDefinitionManager>();

            var prototypes = new List<EntityPrototype>();
            IMapGrid grid = default;
            EntityUid testEntity;

            //Build up test environment
            await server.WaitPost(() =>
            {
                // Create a one tile grid to stave off the grid 0 monsters
                var mapId = mapManager.CreateMap();

                mapManager.AddUninitializedMap(mapId);

                grid = mapManager.CreateGrid(mapId);

                var tileDefinition = tileDefinitionManager["underplating"];
                var tile = new Tile(tileDefinition.TileId);
                var coordinates = grid.ToCoordinates();

                grid.SetTile(coordinates, tile);

                mapManager.DoMapInitialize(mapId);
            });

            await server.WaitRunTicks(5);

            await server.WaitAssertion(() =>
            {
                var testLocation = grid.ToCoordinates();

                //Generate list of non-abstract prototypes to test
                foreach (var prototype in prototypeMan.EnumeratePrototypes<EntityPrototype>())
                {
                    if (prototype.Abstract)
                    {
                        continue;
                    }

                    prototypes.Add(prototype);
                }

                Assert.Multiple(() =>
                {
                    //Iterate list of prototypes to spawn
                    foreach (var prototype in prototypes)
                    {
                        Assert.DoesNotThrow(() =>
                            {
                                Logger.LogS(LogLevel.Debug, "EntityTest", $"Testing: {prototype.ID}");
                                testEntity = entityMan.SpawnEntity(prototype.ID, testLocation);
                                server.RunTicks(1);
                                if(!entityMan.Deleted(testEntity))
                                    entityMan.DeleteEntity(testEntity);
                            }, "Entity '{0}' threw an exception.",
                            prototype.ID);
                    }
                });
            });

            await pairTracker.CleanReturnAsync();
        }

        [Test]
        public async Task AllComponentsOneToOneDeleteTest()
        {
            var skipComponents = new[]
            {
                "DebugExceptionOnAdd", // Debug components that explicitly throw exceptions
                "DebugExceptionExposeData",
                "DebugExceptionInitialize",
                "DebugExceptionStartup",
                "Map", // We aren't testing a map entity in this test
                "MapGrid",
                "Actor", // We aren't testing actor components, those need their player session set.
            };

            var testEntity = @"
- type: entity
  id: AllComponentsOneToOneDeleteTestEntity";

            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true, ExtraPrototypes = testEntity});
            var server = pairTracker.Pair.Server;

            var mapManager = server.ResolveDependency<IMapManager>();
            var entityManager = server.ResolveDependency<IEntityManager>();
            var componentFactory = server.ResolveDependency<IComponentFactory>();
            var tileDefinitionManager = server.ResolveDependency<ITileDefinitionManager>();

            IMapGrid grid = default;

            await server.WaitPost(() =>
            {
                // Create a one tile grid to stave off the grid 0 monsters
                var mapId = mapManager.CreateMap();

                mapManager.AddUninitializedMap(mapId);

                grid = mapManager.CreateGrid(mapId);

                var tileDefinition = tileDefinitionManager["underplating"];
                var tile = new Tile(tileDefinition.TileId);
                var coordinates = grid.ToCoordinates();

                grid.SetTile(coordinates, tile);

                mapManager.DoMapInitialize(mapId);
            });

            await server.WaitRunTicks(5);

            await server.WaitAssertion(() =>
            {
                Assert.Multiple(() =>
                {
                    var testLocation = grid.ToCoordinates();

                    foreach (var type in componentFactory.AllRegisteredTypes)
                    {
                        var component = (Component) componentFactory.GetComponent(type);
                        var name = componentFactory.GetComponentName(type);

                        // If this component is ignored
                        if (skipComponents.Contains(name))
                        {
                            continue;
                        }

                        var entity = entityManager.SpawnEntity("AllComponentsOneToOneDeleteTestEntity", testLocation);

                        Assert.That(entityManager.GetComponent<MetaDataComponent>(entity).EntityInitialized);

                        // The component may already exist if it is a mandatory component
                        // such as MetaData or Transform
                        if (entityManager.HasComponent(entity, type))
                        {
                            continue;
                        }

                        component.Owner = entity;

                        Logger.LogS(LogLevel.Debug, "EntityTest", $"Adding component: {name}");

                        Assert.DoesNotThrow(() =>
                            {
                                entityManager.AddComponent(entity, component);
                            }, "Component '{0}' threw an exception.",
                            name);

                        entityManager.DeleteEntity(entity);
                    }
                });
            });

            await pairTracker.CleanReturnAsync();
        }

        [Test]
        public async Task AllComponentsOneEntityDeleteTest()
        {
            var skipComponents = new[]
            {
                "DebugExceptionOnAdd", // Debug components that explicitly throw exceptions
                "DebugExceptionExposeData",
                "DebugExceptionInitialize",
                "DebugExceptionStartup",
                "Map", // We aren't testing a map entity in this test
                "MapGrid",
                "Actor", // We aren't testing actor components, those need their player session set.
            };

            var testEntity = @"
- type: entity
  id: AllComponentsOneEntityDeleteTestEntity";

            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true, ExtraPrototypes = testEntity});
            var server = pairTracker.Pair.Server;

            var mapManager = server.ResolveDependency<IMapManager>();
            var entityManager = server.ResolveDependency<IEntityManager>();
            var componentFactory = server.ResolveDependency<IComponentFactory>();
            var tileDefinitionManager = server.ResolveDependency<ITileDefinitionManager>();

            IMapGrid grid = default;

            await server.WaitPost(() =>
            {
                // Create a one tile grid to stave off the grid 0 monsters
                var mapId = mapManager.CreateMap();

                mapManager.AddUninitializedMap(mapId);

                grid = mapManager.CreateGrid(mapId);

                var tileDefinition = tileDefinitionManager["underplating"];
                var tile = new Tile(tileDefinition.TileId);

                grid.SetTile(Vector2i.Zero, tile);
                mapManager.DoMapInitialize(mapId);
            });
            await server.WaitRunTicks(5);

            var distinctComponents = new List<(List<CompIdx> components, List<CompIdx> references)>
            {
                (new List<CompIdx>(), new List<CompIdx>())
            };

            // Split components into groups, ensuring that their references don't conflict
            foreach (var type in componentFactory.AllRegisteredTypes)
            {
                var registration = componentFactory.GetRegistration(type);

                for (var i = 0; i < distinctComponents.Count; i++)
                {
                    var distinct = distinctComponents[i];

                    if (distinct.references.Intersect(registration.References).Any())
                    {
                        // Ensure the next list if this one has conflicting references
                        if (i + 1 >= distinctComponents.Count)
                        {
                            distinctComponents.Add((new List<CompIdx>(), new List<CompIdx>()));
                        }

                        continue;
                    }

                    // Add the component and its references if no conflicting references were found
                    distinct.components.Add(registration.Idx);
                    distinct.references.AddRange(registration.References);
                }
            }

            // Sanity check
            Assert.That(distinctComponents, Is.Not.Empty);

            await server.WaitAssertion(() =>
            {
                Assert.Multiple(() =>
                {
                    foreach (var distinct in distinctComponents)
                    {
                        var testLocation = grid.ToCoordinates();
                        var entity = entityManager.SpawnEntity("AllComponentsOneEntityDeleteTestEntity", testLocation);

                        Assert.That(entityManager.GetComponent<MetaDataComponent>(entity).EntityInitialized);

                        foreach (var type in distinct.components)
                        {
                            var component = (Component) componentFactory.GetComponent(type);

                            // If the entity already has this component, if it was ensured or added by another
                            if (entityManager.HasComponent(entity, component.GetType()))
                            {
                                continue;
                            }

                            var name = componentFactory.GetComponentName(component.GetType());

                            // If this component is ignored
                            if (skipComponents.Contains(name))
                                continue;

                            component.Owner = entity;
                            Logger.LogS(LogLevel.Debug, "EntityTest", $"Adding component: {name}");

                            // Note for the future coder: if an exception occurs where a component reference
                            // was already occupied it might be because some component is ensuring another // initialize.
                            // If so, search for cases of EnsureComponent<FailingType>, EnsureComponentWarn<FailingType>
                            // and all others variations (out parameter)
                            Assert.DoesNotThrow(() =>
                                {
                                    entityManager.AddComponent(entity, component);
                                }, "Component '{0}' threw an exception.",
                                name);
                        }
                        entityManager.DeleteEntity(entity);
                    }
                });
            });

            await pairTracker.CleanReturnAsync();
        }
    }
}
