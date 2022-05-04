using UnicontaClient.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using System.Collections;
using Uniconta.ClientTools.Util;
using Uniconta.API.Service;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class InvVariantCombiGridClient : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(InvVariantCombiClient); } }
        public override IComparer GridSorting
        {
            get
            {
                return new InvVariantCombiSort();
            }
        }
    }

    public partial class InvVariantCombiPage : GridBasePage
    {
        public InvVariantCombiPage(BaseAPI API) : base(API, string.Empty)
        {
            InitializeComponent();
            InitControls();
        }
        public InvVariantCombiPage(BaseAPI api, string lookupKey)
            : base(api, lookupKey)
        {
            InitializeComponent();
            InitControls();
        }
        public override string NameOfControl { get { return TabControls.InvVariantCombiPage; } }

        public InvVariantCombiPage(UnicontaBaseEntity sourceMaster) : base(sourceMaster)
        {
            InitializeComponent();
            InitControls(sourceMaster);
        }

        private void InitControls(UnicontaBaseEntity master = null)
        {
            dgStandardVariantLines.UpdateMaster(master);
            Utilities.Utility.SetupVariants(api, null, colVariant1, colVariant2, colVariant3, colVariant4, colVariant5, null, null, null, null, null);
            SetRibbonControl(localMenu, dgStandardVariantLines);
            RemoveMenuItem(master);

            dgStandardVariantLines.api = api;
            dgStandardVariantLines.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            bool showFields = (dgStandardVariantLines.masterRecords == null);
            colItem.Visible = showFields;
            Name.Visible = showFields;
        }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgStandardVariantLines.SelectedItem as InvVariantCombiClient;
            switch (ActionType)
            {
                case "InvBOMPartOfContains":
                    if (selectedItem != null)
                        AddDockItem(TabControls.PartInvItemsPage, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("BOM"), selectedItem._Item));
                    break;
                case "InvBOMPartOfWhereUsed":
                    if (selectedItem != null)
                        AddDockItem(TabControls.InvBOMPartOfPage, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("BOM"), selectedItem._Item));
                    break;
                case "InvBOMProductionPosted":
                    if (selectedItem != null)
                        AddDockItem(TabControls.ProductionPostedGridPage, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("ProductionPosted"), selectedItem._Item));
                    break;
                case "InvHierarichalBOM":
                    if (selectedItem != null)
                        AddDockItem(TabControls.InventoryHierarchicalBOMStatement, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("HierarchicalBOM"), selectedItem._Item));
                    break;
                case "InvExplodeBOM":
                    if (selectedItem != null)
                        AddDockItem(TabControls.InvBOMExplodePage, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("ExplodedBOM"), selectedItem._Item));
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        void RemoveMenuItem(UnicontaBaseEntity master)
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;

            if (master != null)
            {
                UtilDisplay.RemoveMenuCommand(rb, new string[] { "Filter", "ClearFilter" });
                var invItem = master as InvItemClient;
                if (invItem != null && invItem._ItemType >= (byte)Uniconta.DataModel.ItemType.BOM)
                    return;
            }

            UtilDisplay.RemoveMenuCommand(rb, new string[] { "PartInvItems" });
        }
    }
}
