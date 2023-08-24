using DevExpress.XtraRichEdit.Painters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Caching;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;
using UnicontaClient.Models;
using UnicontaClient.Utilities;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    /// <summary>
    /// Interaction logic for AddMultiInventoryItemsForProject.xaml
    /// </summary>
    public partial class AddMultiInventoryItemsForProject : GridBasePage
    {
        Type recordType;
        ItemBase ibase;
        List<InventoryItem> itemList;
        UnicontaBaseEntity _master;
        public override string LayoutName { get { return GetLayoutName(); } }

        public override string NameOfControl { get { return TabControls.AddMultiInventoryItemsForProject; } }

        public AddMultiInventoryItemsForProject(SQLCacheFilter itemcache, Type tp, UnicontaBaseEntity master) : base(null)
        {
            DataContext = this;
            InitializeComponent();
            localMenu.dataGrid = dgMultipleInventoryItems;
            SetRibbonControl(localMenu, dgMultipleInventoryItems);
            dgMultipleInventoryItems.api = api;
            dgMultipleInventoryItems.BusyIndicator = busyIndicator;
            recordType = tp;
            _master = master;
            itemList = new List<InventoryItem>();
            foreach (var rec in itemcache)
                itemList.Add(new InventoryItem((Uniconta.DataModel.InvItem)rec));
            dgMultipleInventoryItems.ItemsSource = itemList;
            dgMultipleInventoryItems.Visibility = Visibility.Visible;
            dgMultipleInventoryItems.Loaded += DgMultipleInventoryItems_Loaded;
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
            GetMenuItem();
            this.KeyDown += AddMultipleInventoryItemPage_KeyDown;
            DXSerializerHandler.AvoidSaveHeader(false);
        }

        private void AddMultipleInventoryItemPage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F8)
                Generate();
        }
        private void LocalMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "Marked":
                    SetMarkedOrAll();
                    break;
                case "Generate":
                    Generate();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        private void DgMultipleInventoryItems_Loaded(object sender, RoutedEventArgs e)
        {
            dgMultipleInventoryItems.tableView.ShowAutoFilterRow = true;
        }

        string GetLayoutName()
        {
            string layoutName;
            if (_master is Uniconta.DataModel.PrJournal prJournal)
                layoutName = string.Format("{0}-{1}", "ProjectJournal", TabControls.AddMultipleInventoryItem);
            else if (_master is Uniconta.DataModel.ProjectBudget projectBudget)
                layoutName = string.Format("{0}-{1}", "ProjectBudget", TabControls.AddMultipleInventoryItem);
            else
                layoutName = TabControls.AddMultipleInventoryItem;
            return layoutName;
        }

        void GetMenuItem()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            ibase = UtilDisplay.GetMenuCommandByName(rb, "Marked");
        }

        public override bool IsDataChaged
        {
            get
            {
                return false;
            }
        }

        public override Task InitQuery()
        {
            return null;
        }

        private void SetMarkedOrAll()
        {
            if (dgMultipleInventoryItems.ItemsSource == null) return;
            if (ibase == null) return;
            if (ibase.Caption == Uniconta.ClientTools.Localization.lookup("All"))
            {
                Marked(false);
                ibase.Caption = Uniconta.ClientTools.Localization.lookup("Marked");
                ibase.LargeGlyph = Utility.GetGlyph("Check_Journal_32x32");
            }
            else if (ibase.Caption == Uniconta.ClientTools.Localization.lookup("Marked"))
            {
                Marked(true);
                ibase.Caption = Uniconta.ClientTools.Localization.lookup("All");
                ibase.LargeGlyph = Utility.GetGlyph("Item_Number_32x32");
            }
        }

        void Marked(bool marked)
        {
            dgMultipleInventoryItems.ItemsSource = null;
            if (marked)
            {
                localMenu.SearchControl.SearchText = string.Empty;
                dgMultipleInventoryItems.ClearFilter(false);
                dgMultipleInventoryItems.ItemsSource = itemList.Where(x => x.Quantity != 0d);
            }
            else
                dgMultipleInventoryItems.ItemsSource = itemList;
        }

        void Generate()
        {
            PrJournal projectJournal = null;
            ProjectBudget projectBudget = null;
            int masterId;

            if (_master is PrJournal prJournal)
            {
                projectJournal = prJournal;
                masterId = projectJournal.RowId;
            }
            else if (_master is ProjectBudget projBudget)
            {
                projectBudget = projBudget;
                masterId = projectBudget.RowId;
            }
            else
                return;

            var InvItems = new List<UnicontaBaseEntity>();
            foreach (var rec in (IEnumerable<InventoryItem>)dgMultipleInventoryItems.ItemsSource)
                if (rec.Quantity != 0d)
                {
                    if (projectJournal != null)
                        InvItems.Add(AddItemsForProjectJournal(rec, projectJournal));
                    else if (projectBudget != null)
                        InvItems.Add(AddItemsForProjectBudget(rec, projectBudget));
                    else
                        continue;
                }

            if (InvItems.Count > 0)
                globalEvents.OnRefresh(TabControls.AddMultiInventoryItemsForProject, new object[2] { InvItems, masterId });

            CloseDockItem();
        }

        private UnicontaBaseEntity AddItemsForProjectJournal(InventoryItem item, PrJournal master)
        {
            var prJournalLine = Activator.CreateInstance(recordType) as PrJournalLine;
            prJournalLine.SetMaster(master);
            prJournalLine._Item = item.Item;
            prJournalLine._Qty = item.Quantity;
            return prJournalLine;
        }

        private UnicontaBaseEntity AddItemsForProjectBudget(InventoryItem item, ProjectBudget master)
        {
            var projectBudgetLine = Activator.CreateInstance(recordType) as ProjectBudgetLine;
            projectBudgetLine.SetMaster(master);
            projectBudgetLine._Item = item.Item;
            projectBudgetLine._Qty = item.Quantity;

            return projectBudgetLine;
        }
    }
}
