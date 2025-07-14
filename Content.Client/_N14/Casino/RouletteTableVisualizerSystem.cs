using Content.Shared._N14.Casino;
using Robust.Client.GameObjects;

namespace Content.Client._N14.Casino;

public sealed class RouletteTableVisualizerSystem : VisualizerSystem<RouletteTableComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, RouletteTableComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!AppearanceSystem.TryGetData(uid, RouletteTableVisuals.State, out RouletteTableState state, args.Component))
            return;

        var spriteState = state == RouletteTableState.On ? component.OnState : component.OffState;
        args.Sprite.LayerSetState(0, spriteState);
    }
}
