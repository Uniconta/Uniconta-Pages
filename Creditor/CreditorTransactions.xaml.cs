using UnicontaClient.Models;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Uniconta.DataModel;
using Uniconta.ClientTools.Util;
using Uniconta.API.Service;
using System.Windows;
using Uniconta.Client.Pages;
using Uniconta.Common.Utility;
using UnicontaClient.Utilities;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class GridCreditorTrans : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(CreditorTransClient); } }
    }
    public partial class CreditorTransactions : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.CreditorTransactions; } }
        DateTime filterDate;

        protected override Filter[] DefaultFilters()
        {
            if (filterDate != DateTime.MinValue)
            {
                Filter dateFilter = new Filter() { name = "Date", value = String.Format("{0:d}..", filterDate) };
                return new Filter[] { dateFilter };
            }
            return base.DefaultFilters();
        }
        protected override SortingProperties[] DefaultSort()
        {
            SortingProperties dateSort = new SortingProperties("Date") { Ascending = false };
            SortingProperties VoucherSort = new SortingProperties("Voucher");
            return new SortingProperties[] { dateSort, VoucherSort };
        }

        public CreditorTransactions(UnicontaBaseEntity master)
            : base(master)
        {
            Initialize(master);
        }
        public CreditorTransactions(SynchronizeEntity syncEntity)
            : base(syncEntity, true)
        {
            this.syncEntity = syncEntity;
            Initialize(syncEntity.Row);
            SetHeader();
        }
        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            dgCreditorTrans.UpdateMaster(args);
            SetHeader();
            FilterGrid(gridControl, false, false);
        }
        void SetHeader()
        {
            var syncMaster = dgCreditorTrans.masterRecord as Uniconta.DataModel.Creditor;
            if (syncMaster == null)
                return;
            string header = string.Format("{0}/{1}", Uniconta.ClientTools.Localization.lookup("CreditorTransactions"), syncMaster._Account);
            SetHeader(header);
        }
        public CreditorTransactions(BaseAPI API)
            : base(API, string.Empty)
        {
            InitializeComponent();
            Initialize(null);
        }

        UnicontaBaseEntity master;
        private void Initialize(UnicontaBaseEntity _master)
        {
            InitializeComponent();
            master = _master;
            dgCreditorTrans.UpdateMaster(master);
            dgCreditorTrans.api = api;
            var Comp = api.CompanyEntity;
            filterDate = BasePage.GetFilterDate(Comp, master != null);
            SetRibbonControl(localMenu, dgCreditorTrans);
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgCreditorTrans.BusyIndicator = busyIndicator;
            dgCreditorTrans.ShowTotalSummary();
            if (Comp.RoundTo100)
                Amount.HasDecimals = Debit.HasDecimals = Credit.HasDecimals = false;
        }

        public override Task InitQuery()
        {
            return FilterGrid(dgCreditorTrans, master == null, false);
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            bool showFields = (dgCreditorTrans.masterRecords == null);
            colAccount.Visible = showFields;
            colName.Visible = showFields;
            Open.Visible = !showFields;
            if (!api.CompanyEntity.Project)
                Project.Visible = false;
            Text.ReadOnly = Invoice.ReadOnly = PostType.ReadOnly = TransType.ReadOnly = showFields;
            if (showFields)
            {
                Open.Visible = false;
                RibbonBase rb = (RibbonBase)localMenu.DataContext;
                UtilDisplay.RemoveMenuCommand(rb, "SaveGrid");
            }
            var credMaster = master as Uniconta.DataModel.Creditor;
            if (credMaster != null)
            {
#if !SILVERLIGHT
                FromDebtor.Visible =
#endif
                dgCreditorTrans.Readonly = (credMaster._D2CAccount != null);
            }
            else
            {
                dgCreditorTrans.Readonly = showFields;
            }
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            CreditorTransClient selectedItem = dgCreditorTrans.SelectedItem as CreditorTransClient;
            switch (ActionType)
            {
                case "ViewDownloadRow":
                    if (selectedItem != null)
                        DebtorTransactions.ShowVoucher(dgCreditorTrans.syncEntity, api, busyIndicator);
                    break;
                case "Settlements":
                    if (selectedItem != null)
                    {
                        string header = Util.ConcatParenthesis(Uniconta.ClientTools.Localization.lookup("Settlements"), selectedItem._Voucher);
                        AddDockItem(TabControls.CreditorSettlements, dgCreditorTrans.syncEntity, true, header, null, floatingLoc: Utility.GetDefaultLocation());
                    }
                    break;
                case "VoucherTransactions":
                    if (selectedItem != null)
                    {
                        string vheader = Util.ConcatParenthesis(Uniconta.ClientTools.Localization.lookup("VoucherTransactions"), selectedItem._Voucher);
                        AddDockItem(TabControls.AccountsTransaction, dgCreditorTrans.syncEntity, vheader);
                    }
                    break;
                case "SaveGrid":
                    Save();
                    break;
                case "PostedBy":
                    if (selectedItem != null)
                        JournalPosted(selectedItem);
                    break;
                case "RefreshGrid":
                    FilterGrid(gridControl, master == null, false);
                    break;
                case "Invoices":
                    if (selectedItem != null)
                    {
                        string header = string.Format("{0}/{1}", Uniconta.ClientTools.Localization.lookup("CreditorInvoice"), selectedItem._Account);
                        AddDockItem(TabControls.CreditorInvoice, dgCreditorTrans.syncEntity, header);
                    }
                    break;
                case "InvoiceLine":
                    if (selectedItem != null)
                        ShowInvoiceLines(selectedItem);
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        async void ShowInvoiceLines(CreditorTransClient creditorTrans)
        {
            var creditorInvoice = await api.Query<CreditorInvoiceClient>(new UnicontaBaseEntity[] { creditorTrans }, null);
            if (creditorInvoice != null && creditorInvoice.Length > 0)
            {
                var credInv = creditorInvoice[0];
                AddDockItem(TabControls.CreditorInvoiceLine, credInv, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("InvoiceNumber"), credInv.InvoiceNum));
            }
        }

        async private void JournalPosted(CreditorTransClient selectedItem)
        {
            var result = await api.Query(new GLDailyJournalPostedClient(), new UnicontaBaseEntity[] { selectedItem }, null);
            if (result != null && result.Length == 1)
            {
                CWGLPostedClientFormView cwPostedClient = new CWGLPostedClientFormView(result[0]);
                cwPostedClient.Show();
            }
        }

        async private void Save()
        {
            SetBusy();
            dgCreditorTrans.BusyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("Saving");
            var err = await dgCreditorTrans.SaveData();
            ClearBusy();
        }
    }
}
