using UnicontaClient.Models;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.Common.Utility;
using Uniconta.API.GeneralLedger;
using Uniconta.DataModel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Uniconta.API.System;
using UnicontaClient.Utilities;
using DevExpress.Xpf.Grid;
using DevExpress.Data;
using DevExpress.Mvvm;
using System.Windows;
using DevExpress.Utils.Serializing;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class GLTranGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(GLTransClientTotal); } }
    }
    public class SumColumn : GridSummaryItem
    {
        private string serializableTag;
        [XtraSerializableProperty]
        public string SerializableTag
        {
            get { return serializableTag; }
            set { serializableTag = value; }
        }
    }
    public partial class TransactionReport : GridBasePage
    {
        public ICommand RowDoubleClickCommand { get; private set; }
        public override string NameOfControl { get { return TabControls.TransactionReport; } }
        ReportAPI tranApi;
        SynchronizeEntity pageMaster;
        GLAccount masterClient;
        PrintReportHeader objPageHdr;
        List<UnicontaBaseEntity> masterList;

        static public DateTime DefaultFromDate, DefaultToDate;
        bool LateLoading, NoPrimo;
        static int setShowDimOnPrimoIndex = 0;

        public TransactionReport(SynchronizeEntity master)
            : base(master, true)
        {
            this.DataContext = this;
            pageMaster = master;
            InitializeComponent();
            var corasauMaster = master.Row;
            InitMaster(corasauMaster);
            Initialize();
        }

        public TransactionReport(UnicontaBaseEntity master) : base(master)
        {
            this.DataContext = this;
            InitializeComponent();
            InitMaster(master);
            Initialize();
        }
        public TransactionReport(UnicontaBaseEntity master, bool LateLoading) : base(master)
        {
            this.DataContext = this;
            this.LateLoading = LateLoading;
            InitializeComponent();
            InitMaster(master);
            Initialize();
        }
        private void InitMaster(UnicontaBaseEntity master)
        {
            if (master == null)
                return;
            if (masterList != null)
                masterList.Clear();
            else
                masterList = new List<UnicontaBaseEntity>();
            masterList.Add(master);
            masterClient = master as GLAccount;
            dgGLTran.masterRecords = masterList;
        }

        private void Initialize()
        {
            objPageHdr = new PrintReportHeader();
            tranApi = new ReportAPI(api);

            localMenu.dataGrid = dgGLTran;
            dgGLTran.api = api;
            if (!api.CompanyEntity.HasDecimals)
                Debit.HasDecimals = Credit.HasDecimals = Amount.HasDecimals = AmountBase.HasDecimals = AmountVat.HasDecimals = false;
            SetRibbonControl(localMenu, dgGLTran);
            gridControl.BusyIndicator = busyIndicator;
            cmbShowDimOnPrimo.ItemsSource = new string[] { Uniconta.ClientTools.Localization.lookup("OnePrimo"), Uniconta.ClientTools.Localization.lookup("PrimoPerDim"), Uniconta.ClientTools.Localization.lookup("NoPrimo") };
            cmbShowDimOnPrimo.SelectedIndex = setShowDimOnPrimoIndex;
            if (DefaultFromDate == DateTime.MinValue)
            {
                var Now = BasePage.GetSystemDefaultDate();
                DefaultToDate = Now;
                DefaultFromDate = Now.AddDays(1 - Now.Day).AddMonths(-2);
            }
            cbxAscending.IsChecked = api.session.Preference.TransactionReport_isAscending;
            txtDateTo.DateTime = DefaultToDate;
            txtDateFrm.DateTime = DefaultFromDate;

            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgGLTran.RowDoubleClick += DgGLTran_RowDoubleClick;
            dgGLTran.ShowTotalSummary();
            InitialLoad();
        }

        private void DgGLTran_RowDoubleClick()
        {
            var selectedItem = dgGLTran.SelectedItem as GLTransClientTotal;
            if (selectedItem != null)
            {
                string vheader = Util.ConcatParenthesis(Uniconta.ClientTools.Localization.lookup("VoucherTransactions"), selectedItem._Voucher);
                AddDockItem(TabControls.AccountsTransaction, selectedItem, vheader);
            }
        }

        public override bool CheckIfBindWithUserfield(out bool isReadOnly, out bool useBinding)
        {
            isReadOnly = true;
            useBinding = true;
            return true;
        }

        public override Task InitQuery()
        {
            return null;
        }

        void InitialLoad()
        {
            Task t;
            if (!LateLoading)
                t = SetHeaderAndLoad();
            else
                t = null;
            SetNoOfDimensions(api.CompanyEntity.NumberOfDimensions);
            SetDailyJournal(cmbJournal, api);
            StartLoadCache(t);
        }
        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            setDim();
        }

        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            InitMaster(args);
            SetHeaderAndLoad();
        }

        Task SetHeaderAndLoad()
        {
            if (masterClient?._Account != null)
            {
                string header = string.Format("{0}/{1}", Uniconta.ClientTools.Localization.lookup("AccountStatement"), masterClient._Account);
                SetHeader(header);
                return LoadGlTran();
            }
            return null;
        }

        void localMenu_OnItemClicked(string ActionType)
        {
            GLTransClientTotal selectedItem = dgGLTran.SelectedItem as GLTransClientTotal;

            switch (ActionType)
            {
                case "PostedTransaction":
                    if (selectedItem == null)
                        return;
                    string header = string.Format("{0} / {1}", Uniconta.ClientTools.Localization.lookup("PostedTransactions"), selectedItem._JournalPostedId);
                    AddDockItem(TabControls.PostedTransactions, selectedItem, header);
                    break;
                case "RefreshGrid":
                    if (gridControl != null)
                        LoadGlTran(true);
                    break;
                case "ViewDownloadRow":
                    if (selectedItem == null) return;
                    DebtorTransactions.ShowVoucher(dgGLTran.syncEntity, api, busyIndicator);
                    break;
                case "VoucherTransactions":
                    if (selectedItem == null)
                        return;
                    string vheader = Util.ConcatParenthesis(Uniconta.ClientTools.Localization.lookup("VoucherTransactions"), selectedItem._Voucher);
                    AddDockItem(TabControls.AccountsTransaction, dgGLTran.syncEntity, vheader);
                    break;
                case "ViewTransactions":
                    if (selectedItem == null)
                        return;
                     AddDockItem(TabControls.AccountsTransaction, selectedItem, string.Format("{0} : {1}", Uniconta.ClientTools.Localization.lookup("VoucherTransactions"), selectedItem._Account ));
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        async void ShowVoucherInfo(GLTransClientTotal selectedItem)
        {
            busyIndicator.IsBusy = true;
            var record = await api.Query<VouchersClient>(new UnicontaBaseEntity[] { selectedItem }, null);
            busyIndicator.IsBusy = false;
            if (record != null && record.Length > 0)
            {
                var voucherInfo = record[0];
#if SILVERLIGHT
                var viewer = new CWDocumentViewer(voucherInfo);
                viewer.Show();
#endif
            }
        }

        void setDim()
        {
            var c = api.CompanyEntity;
            lblDim1.Text = c._Dim1;
            cldim1.Header = c._Dim1;
            lblDim2.Text = c._Dim2;
            cldim2.Header = c._Dim2;
            lblDim3.Text = c._Dim3;
            cldim3.Header = c._Dim3;
            lblDim4.Text = c._Dim4;
            cldim4.Header = c._Dim4;
            lblDim5.Text = c._Dim5;
            cldim5.Header = c._Dim5;
        }

        private async void SetNoOfDimensions(int noofDimensions)
        {
            var Hdr = objPageHdr;

            if (noofDimensions < 5)
            {
#if SILVERLIGHT
                cldim5.Visibility = Visibility.Collapsed;
#endif
                cbdim5.Visibility = lblDim5.Visibility = Visibility.Collapsed;
                cldim5.Visible = false;
                Hdr.LBLDim5 = string.Empty;
                Hdr.TxtDim5 = string.Empty;
            }
            else
                await SetDimValues(typeof(GLDimType5), cbdim5, api);

            if (noofDimensions < 4)
            {
#if SILVERLIGHT
                cldim4.Visibility = Visibility.Collapsed;
#endif
                cbdim4.Visibility = lblDim4.Visibility = Visibility.Collapsed;
                cldim4.Visible = false;
                Hdr.LBLDim4 = string.Empty;
                Hdr.TxtDim4 = string.Empty;
            }
            else
                await SetDimValues(typeof(GLDimType4), cbdim4, api);

            if (noofDimensions < 3)
            {
#if SILVERLIGHT
                cldim3.Visibility = Visibility.Collapsed;
#endif
                cbdim3.Visibility = lblDim3.Visibility = Visibility.Collapsed;
                cldim3.Visible = false;
                Hdr.LBLDim3 = string.Empty;
                Hdr.TxtDim3 = string.Empty;
            }
            else
                await SetDimValues(typeof(GLDimType3), cbdim3, api);

            if (noofDimensions < 2)
            {
#if SILVERLIGHT
                cldim2.Visibility = Visibility.Collapsed;
#endif
                cbdim2.Visibility = lblDim2.Visibility = Visibility.Collapsed;
                cldim2.Visible = false;
                Hdr.LBLDim2 = string.Empty;
                Hdr.TxtDim2 = string.Empty;
            }
            else
                await SetDimValues(typeof(GLDimType2), cbdim2, api);

            if (noofDimensions < 1)
            {
#if SILVERLIGHT
               cldim1.Visibility = Visibility.Collapsed;
#endif
                cbdim1.Visibility = lblDim1.Visibility = Visibility.Collapsed;
                cldim1.Visible = false;
                Hdr.LBLDim1 = string.Empty;
                Hdr.TxtDim1 = string.Empty;
            }
            else
                await SetDimValues(typeof(GLDimType1), cbdim1, api);
        }

        protected override void LoadCacheInBackGround()
        {
            LoadType(new Type[] { typeof(Uniconta.DataModel.GLAccount), typeof(Uniconta.DataModel.Debtor), typeof(Uniconta.DataModel.Creditor), typeof(Uniconta.DataModel.GLDailyJournal), typeof(Uniconta.DataModel.GLVat), typeof(Uniconta.DataModel.GLTransType) });
        }

        public static async Task SetDimValues(Type tp, DimComboBoxEditor cb, CrudAPI api, bool showNoChnage = false)
        {
            var cache = api.GetCache(tp) ?? await api.LoadCache(tp);

            List<IdKey> lst = new List<IdKey>((cache != null) ? cache.Count + 2 : 2);
            if (showNoChnage)
                lst.Add(new KeyNamePair() { _RowId = -1, _KeyStr = Uniconta.ClientTools.Localization.lookup("NoChange") });
            else
                lst.Add(new KeyNamePair() { _RowId = -2, _KeyStr = Uniconta.ClientTools.Localization.lookup("Values") });
            lst.Add(new KeyNamePair() { _RowId = 0, _KeyStr = Uniconta.ClientTools.Localization.lookup("Blank") });

            if (cache != null && cache.Count > 0)
            {
                foreach (GLDimType dim in cache.GetKeyStrRecords)
                    lst.Add(new KeyNamePair() { _RowId = dim.RowId, _KeyStr = Util.ConcatParenthesis(dim.KeyStr, dim.KeyName) });
            }

            cb.ItemsSource = lst;
            cb.ValueMember = "RowId";
            cb.DisplayMember = "KeyStr";
            if (showNoChnage)
                cb.SelectedIndex = 0;
        }

        static public async void SetDailyJournal(ComboBoxEditor cmbJournal, CrudAPI api)
        {
            var journalSource = new List<string>();
            var cache = api.GetCache(typeof(Uniconta.DataModel.GLDailyJournal)) ?? await api.LoadCache(typeof(Uniconta.DataModel.GLDailyJournal));
            if (cache != null)
            {
                journalSource.Capacity = cache.Count;
                foreach (var rec in cache.GetKeyStrRecords)
                    journalSource.Add(rec.KeyStr);
            }
            cmbJournal.ItemsSource = journalSource;
        }

        private void btnSerach_Click(object sender, RoutedEventArgs e)
        {
            LoadGlTran();
        }

        private async Task LoadGlTran(bool maintainIndex = false)
        {
            List<int> dim1 = null, dim2 = null, dim3 = null, dim4 = null, dim5 = null;

            var NumberOfDimensions = api.CompanyEntity.NumberOfDimensions;
            if (NumberOfDimensions >= 1)
                dim1 = GetRowIDs(cbdim1);
            if (NumberOfDimensions >= 2)
                dim2 = GetRowIDs(cbdim2);
            if (NumberOfDimensions >= 3)
                dim3 = GetRowIDs(cbdim3);
            if (NumberOfDimensions >= 4)
                dim4 = GetRowIDs(cbdim4);
            if (NumberOfDimensions >= 5)
                dim5 = GetRowIDs(cbdim5);

            var isAscending = cbxAscending.IsChecked.Value;
            api.session.Preference.TransactionReport_isAscending = isAscending;

            var showDimOnPrimo = cmbShowDimOnPrimo.SelectedIndex;
            setShowDimOnPrimoIndex = showDimOnPrimo;

            setDateTime(txtDateFrm, txtDateTo);
            busyIndicator.IsBusy = true;

            string journal = cmbJournal.Text;
            var dimensionParams = BalanceReport.SetDimensionParameters(dim1, dim2, dim3, dim4, dim5, true, true, true, true, true);
            var listtran = (GLTransClientTotal[])await tranApi.GetTransactions(new GLTransClientTotal(), journal, masterClient._Account, masterClient._Account, DefaultFromDate, DefaultToDate, dimensionParams, showDimOnPrimo);
            if (listtran != null)
            {
                long Total = 0, TotalCur = 0;
                byte currentCur = (byte)masterClient._Currency;

                int l = listtran.Length;
                for (int i = 0; (i < l); i++)
                {
                    var t = listtran[i];
                    Total += t._AmountCent;
                    t._Total = Total;

                    if (t._AmountCurCent != 0 && t._Currency == currentCur)
                    {
                        TotalCur += t._AmountCurCent;
                        t._TotalCur = TotalCur;
                    }
                }

                if (!isAscending)
                    Array.Reverse(listtran);

                dgGLTran.SetSource(listtran, maintainIndex);
            }
            else if (tranApi.LastError != 0)
            {
                busyIndicator.IsBusy = false;
                Uniconta.ClientTools.Util.UtilDisplay.ShowErrorCode(tranApi.LastError);
            }
        }

        public override object GetPrintParameter()
        {
            var Hdr = objPageHdr;
            Hdr.CompanyName = api.CompanyEntity.Name;
            string actStatement = Uniconta.ClientTools.Localization.lookup("AccountStatement");
            Hdr.ReportName = actStatement;
            if (masterClient != null)
                Hdr.ReportName = string.Format("{0}:{1}", actStatement, masterClient.AccountNumber);
            Hdr.CurDateTime = DateTime.Now.ToString("g");
            Hdr.HeaderParameterTemplateStyle = Application.Current.Resources["AccountStatementPageHeaderStyle"] as Style;
            Hdr.FromDate = txtDateFrm.Text == string.Empty ? string.Empty : txtDateFrm.DateTime.ToShortDateString();
            Hdr.ToDate = txtDateTo.Text == string.Empty ? string.Empty : txtDateTo.DateTime.ToShortDateString();
            Hdr.AccountName = string.Format("{0} {1}", masterClient._Account, masterClient._Name);
            var NumberOfDimensions = api.CompanyEntity.NumberOfDimensions;
            if (NumberOfDimensions >= 5)
            {
                Hdr.LBLDim5 = lblDim5.Text + ":";
                Hdr.TxtDim5 = cbdim5.SelectedItem == null ? string.Empty : cbdim5.Text;
            }
            if (NumberOfDimensions >= 4)
            {
                Hdr.LBLDim4 = lblDim4.Text + ":";
                Hdr.TxtDim4 = cbdim4.SelectedItem == null ? string.Empty : cbdim4.Text;
            }
            if (NumberOfDimensions >= 3)
            {
                Hdr.LBLDim3 = lblDim3.Text + ":";
                Hdr.TxtDim3 = cbdim3.SelectedItem == null ? string.Empty : cbdim3.Text;
            }
            if (NumberOfDimensions >= 2)
            {
                Hdr.LBLDim2 = lblDim2.Text + ":";
                Hdr.TxtDim2 = cbdim2.SelectedItem == null ? string.Empty : cbdim2.Text;
            }
            if (NumberOfDimensions >= 1)
            {
                Hdr.LBLDim1 = lblDim1.Text + ":";
                Hdr.TxtDim1 = cbdim1.SelectedItem == null ? string.Empty : cbdim1.Text;
            }
            return Hdr;
        }

        public static List<int> GetRowIDs(DimComboBoxEditor cb)
        {
            return (cb.EditValue as List<object>)?.Cast<int>().ToList();
        }

        public static void setDateTime(DateEditor frmDateeditor, DateEditor todateeditor)
        {
            if (frmDateeditor.Text == string.Empty)
                DefaultFromDate = DateTime.MinValue;
            else
                DefaultFromDate = frmDateeditor.DateTime.Date;
            if (todateeditor.Text == string.Empty)
                DefaultToDate = DateTime.MinValue;
            else
                DefaultToDate = todateeditor.DateTime.Date;
        }

        private void HasVoucherImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            busyIndicator.IsBusy = true;
            ViewVoucher(TabControls.VouchersPage3, dgGLTran.syncEntity);
            busyIndicator.IsBusy = false;
        }

        protected override LookUpTable HandleLookupOnLocalPage(LookUpTable lookup, CorasauDataGrid dg)
        {
            return AccountsTransaction.HandleLookupOnLocalPage(dgGLTran, lookup);
        }

        public void SetControlsAndLoadGLTrans(DateTime fromDate, DateTime toDate, List<object> Dim1, List<object> Dim2, List<object> Dim3, List<object> Dim4, List<object> Dim5, string journals)
        {
            txtDateFrm.EditValue = fromDate;
            txtDateTo.EditValue = toDate;
            cbdim1.EditValue = Dim1;
            cbdim2.EditValue = Dim2;
            cbdim3.EditValue = Dim3;
            cbdim4.EditValue = Dim4;
            cbdim5.EditValue = Dim5;
            cmbJournal.Text = journals;
            this.NoPrimo = true;
            cmbShowDimOnPrimo.SelectedIndex = 2;
            LoadGlTran();
        }
    }

    public class PrintReportHeader : PageReportHeader, INotifyPropertyChanged
    {
        public string FromDate { get; set; }
        public string ToDate { get; set; }
        public string AccountName { get; set; }
        public string LBLDim1 { get; set; }
        public string TxtDim1 { get; set; }
        public string LBLDim2 { get; set; }
        public string TxtDim2 { get; set; }
        public string LBLDim3 { get; set; }
        public string TxtDim3 { get; set; }
        public string LBLDim4 { get; set; }
        public string TxtDim4 { get; set; }
        public string LBLDim5 { get; set; }
        public string TxtDim5 { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
