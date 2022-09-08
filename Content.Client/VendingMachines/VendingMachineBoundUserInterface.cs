using Content.Client.VendingMachines.UI;
using Content.Shared.VendingMachines;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.ViewVariables;
using static Content.Shared.VendingMachines.SharedVendingMachineComponent;

namespace Content.Client.VendingMachines
{
    public sealed class VendingMachineBoundUserInterface : BoundUserInterface
    {
        [ViewVariables] private VendingMachineMenu? _menu;

        public SharedVendingMachineComponent? VendingMachine { get; private set; }

        public VendingMachineBoundUserInterface(ClientUserInterfaceComponent owner, Enum uiKey) : base(owner, uiKey)
        {
            SendMessage(new InventorySyncRequestMessage());
        }

        protected override void Open()
        {
            base.Open();

            var entMan = IoCManager.Resolve<IEntityManager>();
            if (!entMan.TryGetComponent(Owner.Owner, out SharedVendingMachineComponent? vendingMachine))
            {
                return;
            }

            VendingMachine = vendingMachine;

            _menu = new VendingMachineMenu(this) {Title = entMan.GetComponent<MetaDataComponent>(Owner.Owner).EntityName};
            _menu.Populate(VendingMachine.AllInventory);

            _menu.OnClose += Close;
            _menu.OpenCentered();
        }

        public void Eject(InventoryType type, string id)
        {
            SendMessage(new VendingMachineEjectMessage(type, id));
        }

        protected override void ReceiveMessage(BoundUserInterfaceMessage message)
        {
            switch (message)
            {
                case VendingMachineInventoryMessage msg:
                    _menu?.Populate(msg.Inventory);
                    break;
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
                return;

            _menu?.Dispose();
        }
    }
}
