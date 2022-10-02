using System.Collections.Generic;
using System.Text;
using Content.Client.Lathe.Components;
using Content.Shared.Lathe;
using Content.Shared.Materials;
using Content.Shared.Research.Prototypes;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.Utility;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.Lathe.UI
{
    public sealed class LatheMenu : DefaultWindow
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        private readonly ItemList _items;
        private readonly ItemList _materials;
        private readonly LineEdit _amountLineEdit;
        private readonly LineEdit _searchBar;
        public Button QueueButton;
        public Button ServerConnectButton;
        public Button ServerSyncButton;

        public const float RecipeTooltipDelay = 0.5f;

        public LatheBoundUserInterface Owner { get; }

        private readonly List<LatheRecipePrototype> _shownRecipes = new();

        public LatheMenu(LatheBoundUserInterface owner)
        {
            SetSize = MinSize = (300, 450);
            IoCManager.InjectDependencies(this);

            Owner = owner;

            Title = Loc.GetString("lathe-menu-title"); // TODO Replace this with the name of the lathe itself

            var vBox = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical,
                VerticalExpand = true,
                SeparationOverride = 5,
            };

            var hBoxButtons = new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal,
                HorizontalExpand = true,
                VerticalExpand = true,
                SizeFlagsStretchRatio = 1,
            };

            QueueButton = new Button()
            {
                Text = Loc.GetString("lathe-menu-queue"),
                TextAlign = Label.AlignMode.Center,
                SizeFlagsStretchRatio = 1,
            };

            ServerConnectButton = new Button()
            {
                Text = Loc.GetString("lathe-menu-server-list"),
                TextAlign = Label.AlignMode.Center,
                SizeFlagsStretchRatio = 1,
            };

            ServerSyncButton  = new Button()
            {
                Text = Loc.GetString("lathe-menu-sync"),
                TextAlign = Label.AlignMode.Center,
                SizeFlagsStretchRatio = 1,
            };

            var spacer = new Control()
            {
                HorizontalExpand = true,
                SizeFlagsStretchRatio = 3,
            };

            var hBoxFilter = new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal,
                HorizontalExpand = true,
                VerticalExpand = true,
                SizeFlagsStretchRatio = 1
            };

            _searchBar = new LineEdit()
            {
                PlaceHolder = Loc.GetString("lathe-menu-search-designs"),
                HorizontalExpand = true,
                SizeFlagsStretchRatio = 3
            };

            _searchBar.OnTextChanged += Populate;

            var filterButton = new Button()
            {
                Text = Loc.GetString("lathe-menu-search-filter"),
                TextAlign = Label.AlignMode.Center,
                SizeFlagsStretchRatio = 1,
                Disabled = true,
            };

            _items = new ItemList()
            {
                SizeFlagsStretchRatio = 8,
                VerticalExpand = true,
                SelectMode = ItemList.ItemListSelectMode.Button,
            };

            // This is a shitty hack, because item lists apparently don't actually support tooltips. Yay..
            _items.OnItemHover += (ev) =>
            {
                ev.ItemList.HideTooltip();
                ev.ItemList.ToolTip = ev.ItemList[ev.ItemIndex].TooltipText;
            };
            _items.TooltipDelay = RecipeTooltipDelay;

            _items.OnItemSelected += ItemSelected;

            _amountLineEdit = new LineEdit()
            {
                PlaceHolder = Loc.GetString("lathe-menu-search-amount"),
                Text = "1",
                HorizontalExpand = true,
            };

            _amountLineEdit.OnTextChanged += PopulateDisabled;

            _materials = new ItemList()
            {
                VerticalExpand = true,
                SizeFlagsStretchRatio = 3
            };

            hBoxButtons.AddChild(spacer);
            if (Owner.Database is ProtolatheDatabaseComponent database)
            {
                hBoxButtons.AddChild(ServerConnectButton);
                hBoxButtons.AddChild(ServerSyncButton);
                database.OnDatabaseUpdated += Populate;
            }
            hBoxButtons.AddChild(QueueButton);

            hBoxFilter.AddChild(_searchBar);
            hBoxFilter.AddChild(filterButton);

            vBox.AddChild(hBoxButtons);
            vBox.AddChild(hBoxFilter);
            vBox.AddChild(_items);
            vBox.AddChild(_amountLineEdit);
            vBox.AddChild(_materials);

            Contents.AddChild(vBox);
        }

        public void ItemSelected(ItemList.ItemListSelectedEventArgs args)
        {
            args.ItemList.HideTooltip();
            args.ItemList.ToolTip = args.ItemList[args.ItemIndex].TooltipText;

            int.TryParse(_amountLineEdit.Text, out var quantity);
            if (quantity <= 0) quantity = 1;
            Owner.Queue(_shownRecipes[args.ItemIndex], quantity);
        }

        public void PopulateMaterials()
        {
            _materials.Clear();

            if (Owner.Storage == null) return;

            foreach (var (id, amount) in Owner.Storage)
            {
                if (!_prototypeManager.TryIndex(id, out MaterialPrototype? materialPrototype)) continue;
                var material = materialPrototype;
                _materials.AddItem($"{material.Name} {amount} cm³", material.Icon.Frame0(), false);
            }
        }

        /// <summary>
        ///     Disables or enables shown recipes depending on whether there are enough materials for it or not.
        /// </summary>
        public void PopulateDisabled()
        {
            int.TryParse(_amountLineEdit.Text, out var quantity);
            if (quantity <= 0) quantity = 1;
            for (var i = 0; i < _shownRecipes.Count; i++)
            {
                var prototype = _shownRecipes[i];
                _items[i].Disabled = !Owner.Lathe?.CanProduce(prototype, quantity) ?? true;
            }
        }

        /// <inheritdoc cref="PopulateDisabled()"/>
        public void PopulateDisabled(LineEdit.LineEditEventArgs args)
        {
            PopulateDisabled();
        }

        /// <summary>
        ///     Adds shown recipes to the ItemList control.
        /// </summary>
        public void PopulateList()
        {
            _items.Clear();
            foreach (var prototype in _shownRecipes)
            {
                var item = _items.AddItem(prototype.Name, prototype.Icon.Frame0());

                StringBuilder sb = new();
                bool first = true;
                foreach (var (id, quantity) in prototype.RequiredMaterials)
                {
                    if (!_prototypeManager.TryIndex<MaterialPrototype>(id, out var proto))
                        continue;

                    if (first)
                        first = false;
                    else
                        sb.Append("\n");

                    sb.Append(quantity.ToString());
                    sb.Append(" ");
                    sb.Append(proto.Name);
                }

                item.TooltipText = sb.ToString();
            }

            PopulateDisabled();
        }

        /// <summary>
        ///     Populates the list of recipes that will actually be shown, using the current filters.
        /// </summary>
        public void Populate()
        {
            _shownRecipes.Clear();

            if (Owner.Database == null) return;

            foreach (var prototype in Owner.Database)
            {
                if (_searchBar.Text.Trim().Length != 0)
                {
                    if (prototype.Name.ToLowerInvariant().Contains(_searchBar.Text.Trim().ToLowerInvariant()))
                        _shownRecipes.Add(prototype);
                    continue;
                }

                _shownRecipes.Add(prototype);
            }

            PopulateList();
        }

        /// <inheritdoc cref="Populate"/>
        public void Populate(LineEdit.LineEditEventArgs args)
        {
            Populate();
        }
    }
}
