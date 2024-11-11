using UnicontaClient.Models;
using UnicontaClient.Pages;
using DevExpress.Xpf.Grid;
using System;
using System.Collections.Generic;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Uniconta.API.Service;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.ClientTools;
using UnicontaClient.Controls.Dialogs;
using EnumsNET;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class InvPackagingProductPageGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(InvPackagingProductClient); } }
        public override bool Readonly { get { return false; } }
    }

    public partial class InvPackagingProductPage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.InvPackagingProductPage; } }
        public InvPackagingProductPage(UnicontaBaseEntity master) : base(master)
        {
            Init(master);
        }

        void Init(UnicontaBaseEntity master)
        {
            InitializeComponent();
            localMenu.dataGrid = dgInvPackagingProductGrid;
            SetRibbonControl(localMenu, dgInvPackagingProductGrid);
            dgInvPackagingProductGrid.api = api;
            dgInvPackagingProductGrid.UpdateMaster(master);
            dgInvPackagingProductGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
        }
        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            var item = dgInvPackagingProductGrid.masterRecord as InvItemClient;
            if (item != null && item._ItemType != (byte)ItemType.ProductionBOM)
                localMenu.DisableButtons("CopyFromBOM");
        }
        private void LocalMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgInvPackagingProductGrid.SelectedItem as InvPackagingProductClient;
            switch (ActionType)
            {
                case "AddRow":
                    dgInvPackagingProductGrid.AddRow();
                    break;
                case "DeleteRow":
                    dgInvPackagingProductGrid.DeleteRow();
                    break;
                case "SaveGrid":
                    saveGrid();
                    break;
                case "CopyRow":
                    if (selectedItem != null)
                        dgInvPackagingProductGrid.CopyRow();
                    break;
                case "CopyFromItem":
                    CopyFromItem();
                    break;
                case "CopyFromBOM":
                    CopyFromBOM();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
        void CopyFromItem()
        {
            var itemDialog = new CWInventoryItems(api);
            itemDialog.Closing += async delegate
            {
                var item = itemDialog.InvItem;
                if (item != null)
                {
                    var lines = await api.Query<InvPackagingProductClient>(item);
                    if (lines?.Length > 0)
                    {
                        dgInvPackagingProductGrid.PasteRows(lines);
                    }
                }
            };
            itemDialog.Show();
        }
        async void CopyFromBOM()
        {
            var item = dgInvPackagingProductGrid.masterRecord as InvItemClient;
            if (item != null)
            {
                var boms = await api.Query<InvBOMClient>(item);
                var lines = new List<InvPackagingProductClient>();
                foreach (var bom in boms)
                {
                    var newLines = await api.Query<InvPackagingProductClient>(bom);
                    foreach (var line in newLines)
                    {
                        line._Weight = line._Weight * bom._Qty;
                    }
                    if (newLines?.Length > 0)
                        lines.AddRange(newLines);
                }
                dgInvPackagingProductGrid.PasteRows(lines);
            }
        }
    }
}
