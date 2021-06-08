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
using DevExpress.Xpf.Grid;
using System;
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
using UnicontaClient.Pages.GL.ChartOfAccount.Reports;
using Uniconta.DataModel;
using System.Collections;
using UnicontaClient.Controls.Dialogs;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Uniconta.Client.Pages;
using Uniconta.API.Service;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class FAMTransGridClient : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(FAMTransClient); } }
        public override string SetColumnTooltip(object row, ColumnBase col)
        {
            var tran = row as FAMTransClient;
            if (tran != null && col != null)
            {
                switch (col.FieldName)
                {
                    case "DCAccount": return tran.DCName;
                }
            }
            return base.SetColumnTooltip(row, col);
        }

        public override bool Readonly { get { return true; } }
    }

    public partial class FAMTransGridPage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.FAMTransGridPage; } }
        List<UnicontaBaseEntity> masterlist;
        DateTime filterDate,fromDate , toDate;
        protected override Filter[] DefaultFilters()
        {
            Filter dateFilter = new Filter() { name = "Date" };
            if (fromDate != DateTime.MinValue && toDate != DateTime.MinValue)
            {
                dateFilter.value = string.Format("{0:d}..{1:d}", fromDate, toDate);
                return new Filter[] { dateFilter };
            }
            if (filterDate != DateTime.MinValue)
            {
                dateFilter.value = String.Format("{0:d}..", filterDate);
                return new Filter[] { dateFilter };
            }
            return base.DefaultFilters();
        }

        PostingAPI postingApiInv;
        protected override SortingProperties[] DefaultSort()
        {
            SortingProperties dateSort = new SortingProperties("Date");
            var syncMaster = dgFamTransGrid.masterRecord;
            dateSort.Ascending = (syncMaster is GLDailyJournalPosted || syncMaster is GLDailyJournalLine);
            return new SortingProperties[] { dateSort, new SortingProperties("Voucher"), new SortingProperties("VoucherLine") };
        }

        public FAMTransGridPage(BaseAPI API) : base(API, string.Empty)
        {
            InitializePage();
        }

        public FAMTransGridPage(UnicontaBaseEntity master, DateTime fromDate, DateTime toDate)
            : base(null)
        {
            InitializePage(master);
            this.fromDate = fromDate;
            this.toDate = toDate;
        }

        public FAMTransGridPage(SynchronizeEntity syncEntity)
            : base(syncEntity, true)
        {
            InitializePage(syncEntity.Row);
            SetHeader();
        }

        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            dgFamTransGrid.UpdateMaster(args);
            SetHeader();
            FilterGrid(gridControl, false, false);
        }

        void SetHeader()
        {
            var masterClient = dgFamTransGrid.masterRecord as FamClient;
            if (masterClient == null)
                return;
            string header = string.Format("{0}/{1}", Uniconta.ClientTools.Localization.lookup("Asset"), masterClient.Asset);
            SetHeader(header);
        }

        private void InitializePage(UnicontaBaseEntity master = null)
        {
            InitializeComponent();
            var Comp = this.api.CompanyEntity;
            filterDate = BasePage.GetFilterDate(Comp, master != null);
            localMenu.dataGrid = dgFamTransGrid;
            SetRibbonControl(localMenu, dgFamTransGrid);
            dgFamTransGrid.UpdateMaster(master);
            dgFamTransGrid.api = api;
            if (!Comp.HasDecimals)
                Debit.HasDecimals = Credit.HasDecimals = Amount.HasDecimals = AmountBase.HasDecimals = AmountVat.HasDecimals = false;
            dgFamTransGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgFamTransGrid.RowDoubleClick += DgFamTransGrid_RowDoubleClick;
            dgFamTransGrid.ShowTotalSummary();
            postingApiInv = new PostingAPI(api);
        }

        public override Task InitQuery()
        {
            return Filter();
        }

        private void DgFamTransGrid_RowDoubleClick()
        {
            var selectedItem = dgFamTransGrid.SelectedItem as GLTransClient;
            if (selectedItem != null)
            {
                string vheader = string.Format("{0} ({1})", Uniconta.ClientTools.Localization.lookup("VoucherTransactions"), selectedItem._Voucher);
                AddDockItem(TabControls.AccountsTransaction, selectedItem, vheader);
            }
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            setDim();
        }


        protected override void LoadCacheInBackGround()
        {
            LoadType(new Type[] { typeof(Uniconta.DataModel.Debtor), typeof(Uniconta.DataModel.Creditor), typeof(Uniconta.DataModel.GLDailyJournal), typeof(Uniconta.DataModel.GLVat), typeof(Uniconta.DataModel.GLTransType), typeof(Uniconta.DataModel.NumberSerie), typeof(Uniconta.DataModel.FAM) });
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgFamTransGrid.SelectedItem as GLTransClient;
            switch (ActionType)
            {
                case "PostedTransaction":
                    if (selectedItem == null)
                        return;
                    string header = string.Format("{0} / {1}", Uniconta.ClientTools.Localization.lookup("PostedTransactions"), selectedItem._JournalPostedId);
                    AddDockItem(TabControls.PostedTransactions, selectedItem, header);
                    break;
                case "ViewDownloadRow":
                    if (selectedItem == null)
                        return;
                    DebtorTransactions.ShowVoucher(dgFamTransGrid.syncEntity, api, busyIndicator);
                    break;
                case "VoucherTransactions":
                    if (selectedItem == null)
                        return;
                    string vheader = string.Format("{0} ({1})", Uniconta.ClientTools.Localization.lookup("VoucherTransactions"), selectedItem._Voucher);
                    AddDockItem(TabControls.AccountsTransaction, dgFamTransGrid.syncEntity, vheader);
                    break;
                case "AccountsTransaction":
                    if (selectedItem != null)
                    {
                        var glAccount = selectedItem.Master;
                        if (glAccount == null) return;
                        string accHeader = string.Format("{0} ({1})", Uniconta.ClientTools.Localization.lookup("AccountsTransaction"), selectedItem._Account);
                        AddDockItem(TabControls.AccountsTransaction, glAccount, accHeader);
                    }
                    break;
                case "PostedBy":
                    if (selectedItem != null)
                        JournalPosted(selectedItem);
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        async private void JournalPosted(GLTransClient selectedItem)
        {
            var result = await api.Query(new GLDailyJournalPostedClient(), new UnicontaBaseEntity[] { selectedItem }, null);
            if (result != null && result.Length == 1)
            {
                CWGLPostedClientFormView cwPostedClient = new CWGLPostedClientFormView(result[0]);
                cwPostedClient.Show();
            }
        }

        void setDim()
        {
            Utility.SetDimensionsGrid(api, cldim1, cldim2, cldim3, cldim4, cldim5);
        }

        protected override LookUpTable HandleLookupOnLocalPage(LookUpTable lookup, CorasauDataGrid dg)
        {
            return AccountsTransaction.HandleLookupOnLocalPage(dgFamTransGrid, lookup);
        }

        static public LookUpTable HandleLookupOnLocalPage(CorasauDataGrid grid, LookUpTable lookup)
        {
            var trans = grid.SelectedItem as Uniconta.DataModel.GLTrans;
            if (trans == null)
                return lookup;
            if (grid.CurrentColumn?.Name == "DCAccount")
            {
                switch ((int)trans._DCType)
                {
                    case 1:
                        lookup.TableType = typeof(Uniconta.DataModel.Debtor);
                        break;
                    case 2:
                        lookup.TableType = typeof(Uniconta.DataModel.Creditor);
                        break;
                    case 3:
                        lookup.TableType = typeof(Uniconta.DataModel.FAM);
                        break;
                }
            }
            return lookup;
        }

        private void HasVoucherImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            busyIndicator.IsBusy = true;
            ViewVoucher(TabControls.VouchersPage3, dgFamTransGrid.syncEntity);
            busyIndicator.IsBusy = false;
        }

        private void PART_Editor_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var glTrans = dgFamTransGrid.syncEntity.Row as FAMTransClient;
            if (glTrans._HasNote)
            {
                CWAddEditNote cwAddEditNote = new CWAddEditNote(api, null, glTrans, true);
                cwAddEditNote.Show();
            }
        }
    }
}
