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
        static public DateTime fromDate { get; set; }
        static public DateTime toDate { get; set; }
        readonly BankStatement master;
        readonly bool ShowCurrency;

        public BankStGLTransPage(UnicontaBaseEntity sourceData)
            : base(sourceData)
        {
            master = sourceData as BankStatement;
            if (fromDate == DateTime.MinValue)
            {
                DateTime date = DateTime.Today;
                var firstDayOfMonth = new DateTime(date.Year, date.Month, 1);
                var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);
                fromDate = firstDayOfMonth;
                toDate = lastDayOfMonth;
            }
            InitializeComponent();
            this.DataContext = this;

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

            dgAccountsTransGrid.api = api;
            SetRibbonControl(localMenu, dgAccountsTransGrid);
            dgAccountsTransGrid.UpdateMaster(master);
            dgAccountsTransGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;

            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            ibase = UtilDisplay.GetMenuCommandByName(rb, "ShowHideGreenLines");
        }

        protected override Filter[] DefaultFilters()
        {
            Filter dateFilter = new Filter();
            dateFilter.name = "Date";
            dateFilter.value = String.Format("{0:d}..{1:d}", fromDate, toDate);
            return new Filter[] { dateFilter };
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
            Currency.Visible = DebitCur.Visible = CreditCur.Visible = ShowCurrency;
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
                        string vheader = string.Format("{0} ({1})", Uniconta.ClientTools.Localization.lookup("VoucherTransactions"), selectedItem._Voucher);
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
            var tsk =  tranApi.GetBank(new GLTransClientTotal(), master._Account, fromDate, toDate);
            StartLoadCache(tsk);

            var listtran = (GLTransClientTotal[])await tsk;
            if (listtran != null)
            {
                Array.Sort(listtran, new GLTransClientSort());

                var ShowCurrency = this.ShowCurrency;
                long Total = 0;
                var l = listtran.Length;
                for (int i = 0; (i < l); i++)
                {
                    var p = listtran[i];
                    Total += ShowCurrency ? p._AmountCurCent : p._AmountCent;
                    p._Total = Total;
                }
            }
            dgAccountsTransGrid.SetSource(listtran);
        }
        private void btnSerach_Click(object sender, RoutedEventArgs e)
        {
            InitQuery();
        }
    }
}
