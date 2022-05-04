using Uniconta.API.DebtorCreditor;
using UnicontaClient.Models;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.DataModel;
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
using System.Windows.Threading;
using Uniconta.ClientTools.Util;

using Uniconta.API.GeneralLedger;
using System.Text;
using UnicontaClient.Utilities;
using System.ComponentModel.DataAnnotations;
using UnicontaClient.Controls.Dialogs;
using DevExpress.Xpf.Grid;
using System.ComponentModel;
using Uniconta.API.Service;
using Uniconta.Common.Utility;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class BankStGLTransGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(GLTransClientTotal); } }
    }
    public partial class BankStGLTransPage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.StatementLineTransPage; } }
        static public DateTime fromDate { get { return BankStatementPage.fromDate; } set { BankStatementPage.fromDate = value; } }
        static public DateTime toDate { get { return BankStatementPage.toDate; } set { BankStatementPage.toDate = value; } }
        BankStatement master;
        bool ShowCurrency;

        public BankStGLTransPage(BaseAPI API)
            : base(API, string.Empty)
        {
            Init();
        }
        public BankStGLTransPage(UnicontaBaseEntity sourceData)
            : base(sourceData)
        {
            Init();
            master = sourceData as BankStatement;
            UpdateMaster();
        }

        void Init()
        {
            InitializeComponent();
            this.DataContext = this;

            dgAccountsTransGrid.api = api;
            SetRibbonControl(localMenu, dgAccountsTransGrid);
            dgAccountsTransGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;

            ibase = UtilDisplay.GetMenuCommandByName((RibbonBase)localMenu.DataContext, "ShowHideGreenLines");
        }

        public override bool CheckIfBindWithUserfield(out bool isReadOnly, out bool useBinding)
        {
            isReadOnly = true;
            useBinding = true;
            return true;
        }

        void UpdateMaster()
        {
            dgAccountsTransGrid.UpdateMaster(master);
            bool RoundTo100;
            var Comp = api.CompanyEntity;
            if (Comp.SameCurrency(master._Currency))
                RoundTo100 = Comp.RoundTo100;
            else
            {
                ShowCurrency = true;
                RoundTo100 = !CurrencyUtil.HasDecimals(master._Currency);
            }
            if (RoundTo100)
                Debit.HasDecimals = Credit.HasDecimals = Amount.HasDecimals = Total.HasDecimals = false;

            if (!Comp._UseVatOperation)
                VatOperation.Visible = false;
        }

        public override void SetParameter(IEnumerable<ValuePair> Parameters)
        {
            foreach (var rec in Parameters)
            {
                if (string.Compare(rec.Name, "Bank", StringComparison.CurrentCultureIgnoreCase) == 0)
                {
                    var cache = api.GetCache(typeof(Uniconta.DataModel.BankStatement)) ?? api.LoadCache(typeof(Uniconta.DataModel.BankStatement)).GetAwaiter().GetResult();
                    master = (Uniconta.DataModel.BankStatement)cache.Get(rec.Value);
                    if (master != null)
                        UpdateMaster();
                }
            }
            base.SetParameter(Parameters);
        }

        protected override Filter[] DefaultFilters()
        {
            return new[] { new Filter() { name = "Date", value = string.Format("{0:d}..{1:d}", fromDate, toDate) } };
        }

        protected override void LoadCacheInBackGround()
        {
            LoadType(new Type[] { typeof(Uniconta.DataModel.Debtor), typeof(Uniconta.DataModel.Creditor) });
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            setDim();
            Debit.Visible = Credit.Visible = !ShowCurrency;
            if (ShowCurrency)
                Currency.Visible = DebitCur.Visible = CreditCur.Visible = true;
        }
        protected override LookUpTable HandleLookupOnLocalPage(LookUpTable lookup, CorasauDataGrid dg)
        {
            return AccountsTransaction.HandleLookupOnLocalPage(dgAccountsTransGrid, lookup);
        }

        ItemBase ibase;
        bool hideGreen;

        private void setShowHideGreen(bool hideGreen)
        {
            if (ibase == null)
                return;
            if (hideGreen)
            {
                ibase.Caption = string.Format(Uniconta.ClientTools.Localization.lookup("ShowOBJ"), Uniconta.ClientTools.Localization.lookup("Green"));
                ibase.LargeGlyph = Utilities.Utility.GetGlyph("ShowGreen_32x32.png");
            }
            else
            {
                ibase.Caption = string.Format(Uniconta.ClientTools.Localization.lookup("HideOBJ"), Uniconta.ClientTools.Localization.lookup("Green"));
                ibase.LargeGlyph = Utilities.Utility.GetGlyph("HideGreen_32x32.png");
            }
        }
        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgAccountsTransGrid.SelectedItem as GLTrans;
            switch (ActionType)
            {
                case "VoucherTransactions":
                    if (selectedItem != null)
                    {
                        string vheader = Util.ConcatParenthesis(Uniconta.ClientTools.Localization.lookup("VoucherTransactions"), selectedItem._Voucher);
                        AddDockItem(TabControls.AccountsTransaction, dgAccountsTransGrid.syncEntity, vheader);
                    }
                    break;
                case "ViewDownloadRow":
                    if (selectedItem == null)
                        return;
                    busyIndicator.IsBusy = true;
                    ViewVoucher(TabControls.VouchersPage3, dgAccountsTransGrid.syncEntity);
                    busyIndicator.IsBusy = false;
                    break;
                case "ShowHideGreenLines":
                    hideGreen = !hideGreen;
                    setShowHideGreen(hideGreen);
                    string filterString = dgAccountsTransGrid.FilterString;
                    if (filterString.Contains("[State]"))
                        filterString = "";
                    else
                        filterString = "[State]<2";
                    dgAccountsTransGrid.FilterString = filterString;
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
        void setDim()
        {
            UnicontaClient.Utilities.Utility.SetDimensionsGrid(api, cldim1, cldim2, cldim3, cldim4, cldim5);
        }
        public async override Task InitQuery()
        {
            busyIndicator.IsBusy = true;
            var tranApi = new Uniconta.API.GeneralLedger.ReportAPI(api);
            var tsk =  tranApi.GetBank(new GLTransClientTotal(), master._Account, fromDate, toDate, true);
            StartLoadCache(tsk);

            var listtran = (GLTransClientTotal[])await tsk;
            if (listtran != null)
            {
                Array.Sort(listtran, new GLTransClientSort());

                var ShowCurrency = this.ShowCurrency;
                long Total = 0;
                for (int i = 0; (i < listtran.Length); i++)
                {
                    var p = listtran[i];
                    Total += ShowCurrency ? p._AmountCurCent : p._AmountCent;
                    p._Total = Total;
                }
            }
            else
            {
                busyIndicator.IsBusy = false;
                UtilDisplay.ShowErrorCode(tranApi.LastError);
            }
            dgAccountsTransGrid.SetSource(listtran);
        }
        private void btnSerach_Click(object sender, RoutedEventArgs e)
        {
            InitQuery();
        }
    }
}
