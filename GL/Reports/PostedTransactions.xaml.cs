using Uniconta.API.GeneralLedger;
using Uniconta.API.System;
using UnicontaClient.Models;
using UnicontaClient.Utilities;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Uniconta.DataModel;
using DevExpress.Xpf.Grid;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class CorasauDataGridPostedTran : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(GLTransClient); } }
        public override IComparer GridSorting { get { return new GLTransPostedSort(); } }
        public override DataTemplate PrintGridFooter(ref object FooterData)
        {
            FooterData = this.FooterData;
            if (FooterData != null)
            {
                var footerTemplate = Page.Resources["ReportFooterTemplate"] as DataTemplate;
                return footerTemplate;
            }
            return null;
        }
        public override string SetColumnTooltip(object row, ColumnBase col)
        {
            var tran = row as GLTransClient;
            if (tran != null && col != null)
            {
                switch (col.FieldName)
                {
                    case "DCAccount": return tran.DCName;
                }
            }
            return base.SetColumnTooltip(row, col);
        }
        public object FooterData { get; set; }
    }
    public partial class PostedTransactions : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.PostedTransactions; } }
        List<UnicontaBaseEntity> masterList;
        UnicontaBaseEntity master;
        public PostedTransactions(UnicontaBaseEntity _master)
            : base(_master)
        {
            master = _master;
            Initialize(master);
        }

        public PostedTransactions(SynchronizeEntity syncEntity)
           : base(syncEntity, false)
        {
            master = syncEntity.Row;
            Initialize(syncEntity.Row);
        }

        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            master = args;
            masterList = new List<UnicontaBaseEntity>() { master };
            SetHeader();
            InitQuery();
        }

        private void Initialize(UnicontaBaseEntity master)
        {
            InitializeComponent();
            masterList = new List<UnicontaBaseEntity>() { master };
            dgPostedTran.api = api;
            if (!api.CompanyEntity.HasDecimals)
                Debit.HasDecimals = Credit.HasDecimals = Amount.HasDecimals = AmountBase.HasDecimals = AmountVat.HasDecimals = false;
            SetRibbonControl(localMenu, dgPostedTran);
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgPostedTran.BusyIndicator = busyIndicator;

        }

        void SetHeader()
        {
            string header = null;
            var prodPosted = dgPostedTran.masterRecord as ProductionPostedClient;
            if (prodPosted != null)
                header = string.Format("{0} / {1}", Uniconta.ClientTools.Localization.lookup("PostedTransactions"), prodPosted._JournalPostedId);
            if (header != null)
                SetHeader(header);
        }

        public async override Task InitQuery()
        {
            if (master is GLDailyJournalPostedClient || master is DebtorInvoiceClient || master is ProductionPostedClient)
            {
                dgPostedTran.masterRecords = masterList;
                await Filter(null);
            }
            else
            {
                var postedMaster = await api.Query<GLDailyJournalPostedClient>(masterList, null);
                if (postedMaster != null && postedMaster.Length == 1)
                {
                    dgPostedTran.masterRecords = postedMaster;
                    await Filter(null);
                }
            }
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            setDim();
        }

        protected override void LoadCacheInBackGround()
        {
            LoadType(new Type[] { typeof(Uniconta.DataModel.GLAccount), typeof(Uniconta.DataModel.Debtor), typeof(Uniconta.DataModel.Creditor), typeof(Uniconta.DataModel.GLDailyJournal), typeof(Uniconta.DataModel.GLVat), typeof(Uniconta.DataModel.GLTransType) });
        }

        private Task Filter(IEnumerable<PropValuePair> propValuePair)
        {
            return dgPostedTran.Filter(propValuePair);
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgPostedTran.SelectedItem as GLTransClient;

            switch (ActionType)
            {
                case "ViewDownloadRow":
                    if (selectedItem == null)
                        return;

                    if (selectedItem._Invoice != 0 || selectedItem.HasVoucher)
                        ViewVoucher(TabControls.VouchersPage3, dgPostedTran.syncEntity);
                    else
                        UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("NoVoucherExist"), Uniconta.ClientTools.Localization.lookup("Information"), MessageBoxButton.OK);
                    break;

                case "VoucherTransactions":
                    if (selectedItem == null)
                        return;
                    string vheader = string.Format("{0} ({1})", Uniconta.ClientTools.Localization.lookup("VoucherTransactions"), selectedItem._Voucher);
                    AddDockItem(TabControls.AccountsTransaction, dgPostedTran.syncEntity, vheader);
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        async void ShowVoucherInfo(GLTransClient voucherRef)
        {
            busyIndicator.IsBusy = true;
            var record = await api.Query<VouchersClient>(new UnicontaBaseEntity[] { voucherRef }, null);
            busyIndicator.IsBusy = false;
            if (record != null && record.Length > 0)
            {
                var voucherInfo = record[0];
                var viewer = new CWDocumentViewer(voucherInfo);
                viewer.Show();
            }
        }

        protected override LookUpTable HandleLookupOnLocalPage(LookUpTable lookup, CorasauDataGrid dg)
        {
            return AccountsTransaction.HandleLookupOnLocalPage(dgPostedTran, lookup);
        }

        void setDim()
        {
            UnicontaClient.Utilities.Utility.SetDimensionsGrid(api, cldim1, cldim2, cldim3, cldim4, cldim5);
        }
    }
}
