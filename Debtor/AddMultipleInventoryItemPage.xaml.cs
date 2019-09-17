using UnicontaClient.Models;
using UnicontaClient.Pages;
using UnicontaClient.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;
using System.ComponentModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class MultipleInventoryItemGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(InventoryItem); } }
        public override bool Readonly { get { return false; } }
    }

    public partial class AddMultipleInventoryItemPage : GridBasePage
    {
        Type recordType;
        DCOrder master;
        ItemBase ibase;
        List<InventoryItem> itemList;
        public override string LayoutName { get { return GetLayoutName(); } }

        public override string NameOfControl { get { return TabControls.AddMultipleInventoryItem; } }
        public AddMultipleInventoryItemPage(SQLCacheFilter cache, Type tp, DCOrder master) : base(null)
        {
            DataContext = this;
            InitializeComponent();
            localMenu.dataGrid = dgMultipleInventoryItems;
            SetRibbonControl(localMenu, dgMultipleInventoryItems);
            dgMultipleInventoryItems.api = api;
            dgMultipleInventoryItems.BusyIndicator = busyIndicator;
            recordType = tp;
            this.master = master;
            itemList = new List<InventoryItem>();
            foreach (var rec in cache)
                itemList.Add(new InventoryItem((InvItem)rec));
            dgMultipleInventoryItems.ItemsSource = itemList;
            dgMultipleInventoryItems.Visibility = Visibility.Visible;
            dgMultipleInventoryItems.Loaded += DgMultipleInventoryItems_Loaded;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            GetMenuItem();
#if SILVERLIGHT
            Application.Current.RootVisual.KeyDown += AddMultipleInventoryItemPage_KeyDown;
#else
            this.KeyDown += AddMultipleInventoryItemPage_KeyDown;
#endif
            DXSerializerHandler.AvoidSaveHeader(false);
        }

        private void AddMultipleInventoryItemPage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F8)
                Generate();
        }

        string GetLayoutName()
        {
            string layoutName;
            if (master is DebtorOrder)
                layoutName = string.Format("{0}-{1}", "Sales", TabControls.AddMultipleInventoryItem);
            else if (master is CreditorOrder)
                layoutName = string.Format("{0}-{1}", "Purchase", TabControls.AddMultipleInventoryItem);
            else
                layoutName = TabControls.AddMultipleInventoryItem;
            return layoutName;
        }

        void GetMenuItem()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            ibase = UtilDisplay.GetMenuCommandByName(rb, "Marked");
        }

        private void DgMultipleInventoryItems_Loaded(object sender, RoutedEventArgs e)
        {
            dgMultipleInventoryItems.tableView.ShowAutoFilterRow = true;
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

        private void localMenu_OnItemClicked(string ActionType)
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

        private void SetMarkedOrAll()
        {
            if (dgMultipleInventoryItems.ItemsSource == null) return;
            if (ibase == null) return;
            if (ibase.Caption == Uniconta.ClientTools.Localization.lookup("All"))
            {
                Marked(false);
                ibase.Caption = Uniconta.ClientTools.Localization.lookup("Marked");
                ibase.LargeGlyph = Utility.GetGlyph(";component/Assets/img/Check_Journal_32x32.png");
            }
            else if (ibase.Caption == Uniconta.ClientTools.Localization.lookup("Marked"))
            {
                Marked(true);
                ibase.Caption = Uniconta.ClientTools.Localization.lookup("All");
                ibase.LargeGlyph = Utility.GetGlyph(";component/Assets/img/Item_Number_32x32.png");
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
            var InvItems = new List<UnicontaBaseEntity>();
            foreach (var rec in (IEnumerable<InventoryItem>)dgMultipleInventoryItems.ItemsSource)
                if (rec.Quantity != 0d)
                {
                    var lin = Activator.CreateInstance(recordType) as DCOrderLineClient;
                    lin.SetMaster((UnicontaBaseEntity)master);
                    lin._Item = rec.Item;
                    lin._Qty = rec.Quantity;
                    InvItems.Add((UnicontaBaseEntity)lin);
                }

            var param = new object[2] { InvItems , master._OrderNumber};
            globalEvents.OnRefresh(TabControls.AddMultipleInventoryItem, param);
            dockCtrl.CloseDockItem();
        }

        private void setMarked(bool markedState)
        {
            if (ibase == null) return;
            if (markedState)
            {
                ibase.Caption = Uniconta.ClientTools.Localization.lookup("Marked");
                ibase.LargeGlyph = Utility.GetGlyph(";component/Assets/img/Collapse_32x32.png");
            }
            else
            {
                ibase.Caption = Uniconta.ClientTools.Localization.lookup("All");
                ibase.LargeGlyph = Utility.GetGlyph(";component/Assets/img/Expand_32x32.png");
            }
        }
    }

    public class InventoryItem : UnicontaBaseEntity, INotifyPropertyChanged
    {
        public InventoryItem()
        {
            item = new InvItemClient();
        }
        public InventoryItem(InvItem item)
        {
            this.item = item;
        }

        public InvItem item;

        public void loadFields(CustomReader r, int SavedWithVersion)
        {
            item.loadFields(r, SavedWithVersion);
            _Quantity = r.readDouble();
        }
        public void saveFields(CustomWriter w, int SaveVersion) { item.saveFields(w, SaveVersion); w.write(_Quantity); }
        public int Version(int ClientAPIVersion) { return item.Version(ClientAPIVersion); }
        public int ClassId() { return 8765; }

        public Type BaseEntityType() { return typeof(InventoryItem); }
        public int CompanyId { get { return item.CompanyId; } }

        public double _Quantity;
        [Display(Name = "Qty", ResourceType = typeof(DCOrderText))]
        public double Quantity { get { return _Quantity; } set { _Quantity = value; NotifyPropertyChanged("Quantity"); } }

        [Display(Name = "Item", ResourceType = typeof(InventoryText)), Key]
        public string Item { get { return item._Item; } }

        [Display(Name = "Name", ResourceType = typeof(InventoryText))]
        public string Name { get { return item._Name; } }

        [Display(Name = "Decimals", ResourceType = typeof(InventoryText))]
        public int Decimals { get { return item._Decimals; } }

        [ReportingAttribute]
        public InvItemClient InvItem
        {
            get
            {
                var itm = item as InvItemClient;
                if (itm == null)
                {
                    itm = new InvItemClient();
                    StreamingManager.Copy(item, itm);
                    item = (InvItem)itm;
                }
                return itm;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
