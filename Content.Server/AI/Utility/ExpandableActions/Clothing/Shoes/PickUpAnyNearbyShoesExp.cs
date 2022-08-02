using Content.Server.AI.Utility.Actions;
using Content.Server.AI.Utility.Actions.Clothing.Shoes;
using Content.Server.AI.Utility.Considerations;
using Content.Server.AI.Utility.Considerations.Clothing;
using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States;
using Content.Server.AI.WorldState.States.Clothing;
using Content.Server.Clothing.Components;
using Content.Shared.Inventory;

namespace Content.Server.AI.Utility.ExpandableActions.Clothing.Shoes
{
    public sealed class PickUpAnyNearbyShoesExp : ExpandableUtilityAction
    {
        public override float Bonus => UtilityAction.NormalBonus;

        protected override IEnumerable<Func<float>> GetCommonConsiderations(Blackboard context)
        {
            var considerationsManager = IoCManager.Resolve<ConsiderationsManager>();

            return new[]
            {
                considerationsManager.Get<ClothingInSlotCon>().Slot("shoes", context)
                    .InverseBoolCurve(context),
                considerationsManager.Get<ClothingInInventoryCon>().Slot(SlotFlags.FEET, context)
                    .InverseBoolCurve(context)
            };
        }

        public override IEnumerable<UtilityAction> GetActions(Blackboard context)
        {
            var owner = context.GetState<SelfState>().GetValue();

            foreach (var entity in context.GetState<NearbyClothingState>().GetValue())
            {
                if (IoCManager.Resolve<IEntityManager>().TryGetComponent(entity, out ClothingComponent? clothing) &&
                    (clothing.Slots & SlotFlags.FEET) != 0)
                {
                    yield return new PickUpShoes {Owner = owner, Target = entity, Bonus = Bonus};
                }
            }
        }
    }
}
