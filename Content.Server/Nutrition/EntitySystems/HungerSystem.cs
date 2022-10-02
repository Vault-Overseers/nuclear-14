using Content.Server.Nutrition.Components;
using JetBrains.Annotations;

namespace Content.Server.Nutrition.EntitySystems
{
    [UsedImplicitly]
    public sealed class HungerSystem : EntitySystem
    {
        private float _accumulatedFrameTime;

        public override void Update(float frameTime)
        {
            _accumulatedFrameTime += frameTime;

            if (_accumulatedFrameTime > 1)
            {
                foreach (var comp in EntityManager.EntityQuery<HungerComponent>())
                {
                    comp.OnUpdate(_accumulatedFrameTime);
                }

                _accumulatedFrameTime -= 1;
            }
        }
    }
}
