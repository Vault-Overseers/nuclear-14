/*
 * This file is sublicensed under MIT License
 * https://github.com/space-wizards/space-station-14/blob/master/LICENSE.TXT
 */

using System.Numerics;
using Content.Shared._CP14.Cooking;
using Content.Shared._CP14.Cooking.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Random;

namespace Content.Client._CP14.Cooking;

public sealed class CP14ClientCookingSystem : CP14SharedCookingSystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CP14FoodHolderComponent, AfterAutoHandleStateEvent>(OnAfterHandleState);
    }

    private void OnAfterHandleState(Entity<CP14FoodHolderComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        if (ent.Comp.Visuals is null)
            return;

        //Remove old layers
        foreach (var key in ent.Comp.RevealedLayers)
        {
            _sprite.RemoveLayer((ent.Owner, sprite), key);
        }
        ent.Comp.RevealedLayers.Clear();


        //Add new layers
        var counter = 0;
        foreach (var layer in ent.Comp.Visuals)
        {
            var keyCode = $"cp14-food-layer-{counter}";
            ent.Comp.RevealedLayers.Add(keyCode);

            _sprite.LayerMapTryGet((ent.Owner, sprite), ent.Comp.TargetLayerMap, out var index, false);

            _sprite.AddBlankLayer((ent.Owner, sprite), index);
            _sprite.LayerMapSet((ent.Owner, sprite), keyCode, index);
            _sprite.LayerSetData((ent.Owner, sprite), index, layer);
            if (_random.Prob(0.5f)) //50% invert chance
                _sprite.LayerSetScale((ent.Owner, sprite), index, new Vector2(-1, 1));

            counter++;
        }
    }
}
