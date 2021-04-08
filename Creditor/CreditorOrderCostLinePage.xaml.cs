using UnicontaClient.Models;
using UnicontaClient.Pages;
using UnicontaClient.Utilities;
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
using System.Windows.Shapes;
using Uniconta.API.DebtorCreditor;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class CreditorOrderCostLineGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(CreditorOrderCostLineClient); } }
        public override bool Readonly { get { return false; } }

        protected override List<string> GridSkipFields { get { return new List<string>() { "Name" }; } }
    }

    public partial class CreditorOrderCostLinePage : GridBasePage
    {
        public override string NameOfControl
        {
            get { return TabControls.CreditorOrderCostLinePage.ToString(); }
        }

        public CreditorOrderCostLinePage(SynchronizeEntity syncEntity)
           : base(syncEntity, false)
        {
            this.syncEntity = syncEntity;
            Init(syncEntity.Row);
        }

        UnicontaBaseEntity master;
        void Init(UnicontaBaseEntity _master)
        {
            InitializeComponent();
            localMenu.dataGrid = dgCreditorOrderCostLine;
            dgCreditorOrderCostLine.api = api;
            dgCreditorOrderCostLine.UpdateMaster(_master);
            dgCreditorOrderCostLine.BusyIndicator = busyIndicator;
            SetRibbonControl(localMenu, dgCreditorOrderCostLine);
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
            dgCreditorOrderCostLine.View.DataControl.CurrentItemChanged += DataControl_CurrentItemChanged;
            master = _master;
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            if (master is CreditorOrder)
                UtilDisplay.RemoveMenuCommand(rb, "PostCost");
            else
                UtilDisplay.RemoveMenuCommand(rb, new string[] { "SaveGrid", "RefreshGrid", "SyncPage" });
        }

        SQLCache CostGroups;
        protected async override void LoadCacheInBackGround()
        {
            CostGroups = api.GetCache(typeof(Uniconta.DataModel.CreditorOrderCost)) ?? await api.LoadCache(typeof(Uniconta.DataModel.CreditorOrderCost)).ConfigureAwait(false);
        }

        public override Task InitQuery()
        {
            if (master is CreditorOrder)
                return Filter(null);
            else
            {
                dgCreditorOrderCostLine.SetSource(new CreditorOrderCostLineClient[0]);
                return null;
            }
        }

        void DataControl_CurrentItemChanged(object sender, DevExpress.Xpf.Grid.CurrentItemChangedEventArgs e)
        {
            var oldselectedItem = e.OldItem as CreditorOrderCostLineClient;
            if (oldselectedItem != null)
                oldselectedItem.PropertyChanged -= CreditorOrderCostLine_PropertyChanged;

            var selectedItem = e.NewItem as CreditorOrderCostLineClient;
            if (selectedItem != null)
                selectedItem.PropertyChanged += CreditorOrderCostLine_PropertyChanged;
        }

        void SetDefaults(CreditorOrderCostLineClient rec)
        {
            var grp = (Uniconta.DataModel.CreditorOrderCost)CostGroups?.Get(rec._CostGroup);
            if (grp != null)
                rec.SetDefaults(grp);
        }

        private void CreditorOrderCostLine_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var rec = (CreditorOrderCostLineClient)sender;
            switch (e.PropertyName)
            {
                case "CostGroup":
                    SetDefaults(rec);
                    break;
            }
        }

        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            dgCreditorOrderCostLine.UpdateMaster(args);
            SetHeader();
            BindGrid();
        }

        public override bool IsDataChaged
        {
            get
            {
                if (master is CreditorOrder)
                    return base.IsDataChaged;
                else
                    return false;
            }
        }

        void SetHeader()
        {
            var syncMaster = master as CreditorOrder;
            if (syncMaster == null)
                return;
            var header = string.Format("{0}: {1}, {2}", Uniconta.ClientTools.Localization.lookup("PurchaseCharges"), syncMaster._OrderNumber, syncMaster._DCAccount);
            SetHeader(header);
        }

        private void BindGrid()
        {
            Filter(null);
        }

        public override void PageClosing()
        {
            globalEvents.OnRefresh(NameOfControl, master);
            base.PageClosing();
        }

        private Task Filter(IEnumerable<PropValuePair> propValuePair)
        {
            return dgCreditorOrderCostLine.Filter(propValuePair);
        }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "AddRow":
                    var rec = dgCreditorOrderCostLine.AddRow() as CreditorOrderCostLineClient;
                    SetDefaults(rec);
                    break;
                case "DeleteRow":
                    dgCreditorOrderCostLine.DeleteRow();
                    break;
                case "SaveGrid":
                    dgCreditorOrderCostLine.SaveData();
                    break;
                case "RefreshGrid":
                    BindGrid();
                    break;
                case "PostCost":
                    var lst = dgCreditorOrderCostLine.ItemsSource as IEnumerable<Uniconta.DataModel.CreditorOrderCostLine>;
                    if (lst == null || lst.Count() == 0)
                    {
                        UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("zeroRecords"), Uniconta.ClientTools.Localization.lookup("Warning"), MessageBoxButton.OK);
                        return;
                    }
                    PostCost(lst);
                    break;
                case "SyncPage":
                    if (master is CreditorOrder)
                        gridRibbon_BaseActions(ActionType);
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        private void PostCost(IEnumerable<Uniconta.DataModel.CreditorOrderCostLine> lst)
        {
            CWConfirmationBox dialog = new CWConfirmationBox(Uniconta.ClientTools.Localization.lookup("AreYouSureToContinue"), Uniconta.ClientTools.Localization.lookup("Confirmation"), false);
            dialog.Closing += async delegate
            {
                if (dialog.ConfirmationResult == CWConfirmationBox.ConfirmationResultEnum.Yes)
                {
                    busyIndicator.IsBusy = true;
                    InvoiceAPI invoiceApi = new InvoiceAPI(this.api);
                    var errorCodes = await invoiceApi.InvoiceAddCost(master as Uniconta.DataModel.DCInvoice, lst);
                    busyIndicator.IsBusy = false;
                    UtilDisplay.ShowErrorCode(errorCodes);

                    if (errorCodes == ErrorCodes.Succes)
                    {
                        dgCreditorOrderCostLine.ItemsSource = null;
                        CloseDockItem();
                    }
                }
            };
            dialog.Show();
        }
    }
}
