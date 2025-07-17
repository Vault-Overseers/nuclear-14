/*
 * This file is sublicensed under MIT License
 * https://github.com/space-wizards/space-station-14/blob/master/LICENSE.TXT
 */

using System.Collections.Generic;
using System.Linq;
using Content.Shared._CP14.Cooking;
using Content.Shared._CP14.Cooking.Requirements;
using Content.Shared.Chemistry.Components;
using Content.Shared.Tag;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests._CP14;

#nullable enable

[TestFixture]
public sealed class CP14Cooking
{

    [Test]
    public async Task TestAllCookingRecipeIsCookable()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var entManager = server.ResolveDependency<IEntityManager>();
        var protoMan = server.ResolveDependency<IPrototypeManager>();

        var cookSys = entManager.System<CP14SharedCookingSystem>();

        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                foreach (var recipe in protoMan.EnumeratePrototypes<CP14CookingRecipePrototype>())
                {
                    var solution = new Solution();
                    var allTags = new List<ProtoId<TagPrototype>>();
                    foreach (var req in recipe.Requirements)
                    {
                        switch (req)
                        {
                            case AlwaysMet:
                                continue;
                            case TagRequired tagReq:
                                allTags.AddRange(tagReq.Tags);
                                break;
                            case ReagentRequired reagentReq:
                                if (reagentReq.Reagents.Count == 0)
                                    continue; // No reagents required, skip this requirement.
                                solution.AddReagent(reagentReq.Reagents.First(),
                                    reagentReq.Amount);
                                break;
                        }
                    }

                    var selectedRecipe = cookSys.GetRecipe(recipe.FoodType, solution, allTags);

                    var complexity = recipe.Requirements.Sum(req => req.GetComplexity());
                    var selectedRecipeComplexity = selectedRecipe?.Requirements.Sum(req => req.GetComplexity()) ?? 0;
                    if (selectedRecipe != recipe)
                    {
                        Assert.Fail($"The {recipe.ID} recipe is impossible to cook! " +
                                    $"Instead, the following dish was prepared: ${selectedRecipe?.ID ?? "NULL"} " +
                                    $"\nRecipe complexity: {complexity}, selected recipe complexity: {selectedRecipeComplexity}.");
                    }
                }
            });
        });

        await pair.CleanReturnAsync();
    }
}
