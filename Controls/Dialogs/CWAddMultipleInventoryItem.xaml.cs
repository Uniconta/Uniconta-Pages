using UnicontaClient.Pages;
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
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.Common;
using Uniconta.DataModel;

namespace UnicontaClient.Controls.Dialogs
{
    public class InventoryItemGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(InventoryItem); } }
        public override bool Readonly { get { return false; } }
    }
    /// <summary>
    /// Interaction logic for CWInventoryItem.xaml
    /// </summary>
    public partial class CWAddMultipleInventoryItem : ChildWindow
    {
        public List<UnicontaBaseEntity> InvItems { get; private set; }
        Type recordType;
        DCOrder master;
        public CWAddMultipleInventoryItem(CrudAPI api, SQLCacheFilter cache, Type tp, DCOrder master)
        {
            DataContext = this;
            Title = string.Format(Uniconta.ClientTools.Localization.lookup("AddOBJ"), Uniconta.ClientTools.Localization.lookup("InventoryItems"));
            InitializeComponent();
            dgInventoryItems.api = api;
            dgInventoryItems.BusyIndicator = busyIndicator;
            recordType = tp;
            this.master = master;

            var lst = new List<InventoryItem>();
            foreach (var rec in cache)
                lst.Add(new InventoryItem((InvItemClient)rec));
            dgInventoryItems.ItemsSource = lst;
            dgInventoryItems.Visibility = Visibility.Visible;
            dgInventoryItems.Loaded += CWAddMultipleInventoryItem_Loaded;
        }

        private void CWAddMultipleInventoryItem_Loaded(object sender, RoutedEventArgs e)
        {
            dgInventoryItems.tableView.ShowAutoFilterRow = true;
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            InvItems = new List<UnicontaBaseEntity>();
            foreach(var rec in (IEnumerable<InventoryItem>)dgInventoryItems.ItemsSource)
                if (rec.Quantity != 0d)
                {
                    var lin = Activator.CreateInstance(recordType) as DCOrderLineClient;
                    lin.SetMaster((UnicontaBaseEntity)master);
                    lin._Item = rec.Item;
                    lin._Qty = rec.Quantity;
                    InvItems.Add((UnicontaBaseEntity)lin);
                }
            SetDialogResult(true);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            InvItems = null;
            SetDialogResult(false);
        }

        private void ChildWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                SetDialogResult(false);
        }
    }

    public class InventoryItem : UnicontaBaseEntity
    {
        public InventoryItem()
        {
            item = new InvItemClient();
        }
        public InventoryItem(InvItemClient item)
        {
            this.item = item;
        }

        public readonly InvItemClient item;

        public void loadFields(CustomReader r, int SavedWithVersion)
        {
            item.loadFields(r, SavedWithVersion);
            Quantity = r.readDouble();
        }
        public void saveFields(CustomWriter w, int SaveVersion) { item.saveFields(w, SaveVersion); w.write(Quantity); }
        public int Version(int ClientAPIVersion) { return item.Version(ClientAPIVersion); }
        public int ClassId() { return item.ClassId(); }

        public Type BaseEntityType() { return typeof(InventoryItem); }
        public int CompanyId { get { return item.CompanyId; } set { item.CompanyId = value; } }

        [Display(Name = "Qty", ResourceType = typeof(DCOrderText))]
        public double Quantity { get; set; }

        [Display(Name = "Item", ResourceType = typeof(InventoryText)), Key]
        public string Item { get { return item._Item; }  }

        [Display(Name = "Name", ResourceType = typeof(InventoryText))]
        public string Name { get { return item._Name; }  }

        [Display(Name = "Decimals", ResourceType = typeof(InventoryText))]
        public int Decimals { get { return item._Decimals; } }

        [ReportingAttribute]
        public InvItemClient InvItem { get  { return item; } }
    }
}
