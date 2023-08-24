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
using Uniconta.Common.Utility;
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
    public class GLTransDeletedGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(GLTransDeletedClient); } }
        public override string SetColumnTooltip(object row, ColumnBase col)
        {
            var tran = row as GLTransDeletedClient;
            if (tran != null && col != null)
            {
                switch (col.FieldName)
                {
                    case "DCAccount": return tran.DCName;
                }
            }
            return base.SetColumnTooltip(row, col);
        }
    }

    public partial class GLTransDeletedReport : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.GLTransDeletedReport; } }
        List<UnicontaBaseEntity> masterlist;
        DateTime filterDate;

        protected override Filter[] DefaultFilters()
        {
            if (masterlist == null && filterDate != DateTime.MinValue)
                return new Filter[] { new Filter() { name = "Time", value = String.Format("{0:d}..", filterDate) } };
            return base.DefaultFilters();
        }

        public GLTransDeletedReport(BaseAPI API) : base(API, string.Empty)
        {
            InitializePage();
        }

        public GLTransDeletedReport(UnicontaBaseEntity master)
            : base(null)
        {
            var mlist = new List<UnicontaBaseEntity>() { master };
            InitializePage(mlist);
        }

        public GLTransDeletedReport(SynchronizeEntity syncEntity)
            : base(syncEntity, true)
        {
            this.syncEntity = syncEntity;
            var mlist = new List<UnicontaBaseEntity>() { syncEntity.Row };
            InitializePage(mlist);
            SetPageHeader();
        }

        public GLTransDeletedReport(CrudAPI api, PropValuePair[] filterArray) : base(api, string.Empty)
        {
            filter = filterArray;
            InitializePage();
        }

        void SetPageHeader()
        {
            var syncMaster = dgDeletedTransGrid.masterRecord;
            if (syncMaster == null)
                return;
            string propertytName;
            if (syncMaster is GLAccountClient)
                propertytName = "Account";
            else
                propertytName = "JournalPostedId";

            var pInfo = syncMaster.GetType().GetProperty(propertytName);
            if (pInfo != null)
            {
                var val = pInfo.GetValue(syncMaster, null);
                string header = string.Empty;
                if (syncMaster is GLAccountClient)
                    header = Util.ConcatParenthesis(Uniconta.ClientTools.Localization.lookup("AccountsTransaction"), val);
                else if (syncMaster is GLDailyJournalPostedClient || syncMaster is GLTransLogClient)
                {
                    var voucher = syncMaster.GetType().GetProperty("Voucher")?.GetValue(syncMaster, null);
                    header = string.Format("{0}: {1}/{2}", Uniconta.ClientTools.Localization.lookup("DeletedTransactions"), val, voucher);
                }
                else
                    header = Util.ConcatParenthesis(Uniconta.ClientTools.Localization.lookup("DeletedTransactions"), val);
                SetHeader(header);
            }
        }

        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            dgDeletedTransGrid.UpdateMaster(args);
            SetPageHeader();
            BindGrid();
        }
        private void InitializePage(List<UnicontaBaseEntity> masters = null)
        {
            this.DataContext = this;
            InitializeComponent();
            if (masters != null)
                filterDate = BasePage.GetSystemDefaultDate().AddYears(-2);
            localMenu.dataGrid = dgDeletedTransGrid;
            SetRibbonControl(localMenu, dgDeletedTransGrid);
            masterlist = masters;
            gridControl.masterRecords = masters;
            dgDeletedTransGrid.api = api;
            var Comp = this.api.CompanyEntity;
            if (!Comp.HasDecimals)
                Debit.HasDecimals = Credit.HasDecimals = Amount.HasDecimals = AmountBase.HasDecimals = AmountVat.HasDecimals = false;
            dgDeletedTransGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgDeletedTransGrid.ShowTotalSummary();

            api.LoadCacheInBackground(typeof(GLAccount));
        }

        public override bool CheckIfBindWithUserfield(out bool isReadOnly, out bool useBinding)
        {
            isReadOnly = true;
            useBinding = true;
            return true;
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            setDim();
        }

        void setDim()
        {
            UnicontaClient.Utilities.Utility.SetDimensionsGrid(api, cldim1, cldim2, cldim3, cldim4, cldim5);
        }

        IEnumerable<PropValuePair> filter;
        public override Task InitQuery()
        {
            if (filter != null)
                return dgDeletedTransGrid.Filter(filter);
            else
                return Filter();
        }

        private void BindGrid()
        {
            var rb = ribbonControl;
            if (rb != null)
            {
                var pairs = rb.filterValues;
                var sort = rb.PropSort;
                if (pairs != null || sort != null)
                {
                    rb.FilterGrid?.Filter(pairs, sort);
                    return;
                }
            }
            InitQuery();
        }

        protected override void LoadCacheInBackGround()
        {
            LoadType(new Type[] { typeof(Uniconta.DataModel.Debtor), typeof(Uniconta.DataModel.Creditor), typeof(Uniconta.DataModel.GLDailyJournal), typeof(Uniconta.DataModel.GLVat), typeof(Uniconta.DataModel.GLTransType), typeof(Uniconta.DataModel.NumberSerie), typeof(Uniconta.DataModel.FAM) });
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            string header;
            var selectedItem = dgDeletedTransGrid.SelectedItem as GLTransDeletedClient;
            switch (ActionType)
            {
                case "PostedTransaction":
                    if (selectedItem == null)
                        return;
                    header = string.Format("{0} / {1}", Uniconta.ClientTools.Localization.lookup("PostedTransactions"), selectedItem._JournalPostedId);
                    AddDockItem(TabControls.PostedTransactions, selectedItem, header);
                    break;
                case "ViewDownloadRow":
                    if (selectedItem != null)
                        DebtorTransactions.ShowVoucher(dgDeletedTransGrid.syncEntity, api, busyIndicator);
                    break;
                case "AccountsTransaction":
                    if (selectedItem != null)
                    {
                        var glAccount = selectedItem.Master;
                        if (glAccount != null)
                        {
                            string accHeader = Util.ConcatParenthesis(Uniconta.ClientTools.Localization.lookup("AccountsTransaction"), selectedItem._Account);
                            AddDockItem(TabControls.AccountsTransaction, glAccount, accHeader);
                        }
                    }
                    break;
                case "DeletedBy":
                    if (selectedItem != null)
                        DeletedBy(selectedItem);
                    break;
                case "PostedBy":
                    if (selectedItem != null)
                        JournalPosted(selectedItem);
                    break;
                case "CopyVoucherToJournal":
                    if (selectedItem != null)
                        CopyToJOurnal();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        void CopyToJOurnal()
        {
            var glDelTranslst = dgDeletedTransGrid.GetVisibleRows() as IEnumerable<GLTransDeletedClient>;
            var cwObj = new CWCopyVoucherToJrnl(api);
            cwObj.DialogTableId = 2000000082;
            cwObj.Closed += async delegate
            {
                if (cwObj.DialogResult == true)
                {
                    var Accounts = api.GetCache(typeof(GLAccount));
                    var Vats = api.GetCache(typeof(GLVat));

                    GLDailyJournalLineClient gljournaLine = null;
                    double factor = CWCopyVoucherToJrnl.InvertSign ? -1d : 1d;
                    double vatAmount = 0;
                    var glDailyJrnlLineLst = new List<GLDailyJournalLineClient>(glDelTranslst.Count());
                    foreach (var trans in glDelTranslst)
                    {
                        var vat = (GLVat)Vats.Get(trans._Vat);
                        if (vat != null && vat._Account != null && vat._OffsetAccount != null && (trans._Account == vat._Account || trans._Account == vat._OffsetAccount))
                            continue;

                        if (gljournaLine != null && gljournaLine._AccountType == 0 && vatAmount != 0 && vatAmount == trans._Amount && gljournaLine._Vat == trans._Vat)
                        {
                            // We join vat with previous line
                            gljournaLine.Amount += (factor * vatAmount);
                            gljournaLine.AmountCur += factor * trans._AmountCur;
                            gljournaLine = null;
                            continue;
                        }

                        if (vat != null && vat._Account != null && vat._OffsetAccount == null)
                            vatAmount = trans._AmountVat;
                        else
                            vatAmount = 0;

                        gljournaLine = new GLDailyJournalLineClient();

                        gljournaLine.Date = CWCopyVoucherToJrnl.Date == DateTime.MinValue ? trans.Date : CWCopyVoucherToJrnl.Date;
                        gljournaLine.Text = string.IsNullOrEmpty(CWCopyVoucherToJrnl.Comment) ? trans.Text : CWCopyVoucherToJrnl.Comment;
                        gljournaLine.TransType = string.IsNullOrEmpty(CWCopyVoucherToJrnl.TransType) ? trans.TransType : CWCopyVoucherToJrnl.TransType;

                        gljournaLine._Account = trans._Account;
                        gljournaLine._Vat = trans._Vat;
                        gljournaLine._Voucher = trans._Voucher;
                        gljournaLine.Amount = factor * trans._Amount;
                        gljournaLine._Currency = trans._Currency;
                        gljournaLine.AmountCur = factor * trans._AmountCur;
                        gljournaLine._Project = trans._Project;
                        gljournaLine._DocumentRef = trans._DocumentRef;
                        gljournaLine._Qty = trans._Qty;
                        gljournaLine._Dim1 = trans._Dim1;
                        gljournaLine._Dim2 = trans._Dim2;
                        gljournaLine._Dim3 = trans._Dim3;
                        gljournaLine._Dim4 = trans._Dim4;
                        gljournaLine._Dim5 = trans._Dim5;

                        if (trans._DCType > 0)
                        {
                            var acc = (GLAccount)Accounts.Get(trans._Account);
                            if (acc != null)
                            {
                                if ((trans._DCType == GLTransRefType.Debtor && acc.AccountTypeEnum == GLAccountTypes.Debtor) ||
                                    (trans._DCType == GLTransRefType.Creditor && acc.AccountTypeEnum == GLAccountTypes.Creditor))
                                {
                                    gljournaLine._Account = trans._DCAccount;
                                    gljournaLine._AccountType = (byte)trans._DCType;
                                }
                            }
                        }

                        gljournaLine.SetMaster(cwObj.GlDailyJournal);
                        glDailyJrnlLineLst.Add(gljournaLine);
                    }
                    busyIndicator.IsBusy = true;
                    var result = await api.Insert(glDailyJrnlLineLst);
                    busyIndicator.IsBusy = false;
                    UtilDisplay.ShowErrorCode(result);
                }
            };
            cwObj.Show();
        }

        async private void DeletedBy(GLTransDeletedClient selectedItem)
        {
            var result = await api.Query(new GLTransLogClient(), new UnicontaBaseEntity[] { selectedItem }, null);
            if (result != null && result.Length == 1)
            {
                var cwTransLogClient = new CWGLTransLogClientFormView(result[0]);
                cwTransLogClient.Show();
            }
        }

        async private void JournalPosted(GLTransDeletedClient selectedItem)
        {
            var result = await api.Query(new GLDailyJournalPostedClient(), new UnicontaBaseEntity[] { selectedItem }, null);
            if (result != null && result.Length == 1)
            {
                CWGLPostedClientFormView cwPostedClient = new CWGLPostedClientFormView(result[0]);
                cwPostedClient.Show();
            }
        }

        protected override LookUpTable HandleLookupOnLocalPage(LookUpTable lookup, CorasauDataGrid dg)
        {
            return AccountsTransaction.HandleLookupOnLocalPage(dgDeletedTransGrid, lookup);
        }

        static public LookUpTable HandleLookupOnLocalPage(CorasauDataGrid grid, LookUpTable lookup)
        {
            var trans = grid.SelectedItem as Uniconta.DataModel.GLTransDeleted;
            if (trans == null)
                return lookup;
            if (grid.CurrentColumn?.Name == "DCAccount")
            {
                switch ((int)trans._DCType)
                {
                    case 0:
                        lookup.TableType = typeof(Uniconta.DataModel.GLAccount);
                        break;
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
            ViewVoucher(TabControls.VouchersPage3, dgDeletedTransGrid.syncEntity);
            busyIndicator.IsBusy = false;
        }
    }
}
