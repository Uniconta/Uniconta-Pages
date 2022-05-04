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
using Uniconta.Common.Utility;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class BankStLineGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(BankStatementLineGridClient); } }           
    }
    public partial class BankStLines : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.StatementLine; } }
        static public DateTime fromDate { get { return BankStatementPage.fromDate; } set { BankStatementPage.fromDate = value; } }
        static public DateTime toDate { get { return BankStatementPage.toDate; } set { BankStatementPage.toDate = value; } }
        BankStatement master;
        BankStatementAPI bankTransApi;

        public BankStLines(UnicontaBaseEntity sourceData)
            : base(sourceData)
        {
            InitializeComponent();
            this.DataContext = this;

            master = sourceData as BankStatement;

            bool RoundTo100;
            var Comp = api.CompanyEntity;
            if (Comp.SameCurrency(master._Currency))
                RoundTo100 = Comp.RoundTo100;
            else
                RoundTo100 = !CurrencyUtil.HasDecimals(master._Currency);
            if (RoundTo100)
                Debit.HasDecimals = Credit.HasDecimals = Amount.HasDecimals = Total.HasDecimals = false;

            if (!Comp._UseVatOperation)
                VatOperation.Visible = false;

            dgBankStatementLine.api = api;
            bankTransApi = new BankStatementAPI(api);
            SetRibbonControl(localMenu, dgBankStatementLine);
            dgBankStatementLine.UpdateMaster(master);
            dgBankStatementLine.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
        }

        public override bool IsDataChaged { get { return false; } }

        protected override Filter[] DefaultFilters()
        {
            return new[] { new Filter() { name = "Date", value = string.Format("{0:d}..{1:d}", fromDate, toDate) } };
        }

        protected override SortingProperties[] DefaultSort()
        {
            return new SortingProperties[] { new SortingProperties("Date"), new SortingProperties("LineNo") };
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgBankStatementLine.SelectedItem as BankStatementLineGridClient;
            if (ActionType == "VoucherTransactions")
            {
                if (selectedItem != null)
                {
                    string vheader = Util.ConcatParenthesis(Uniconta.ClientTools.Localization.lookup("VoucherTransactions"), selectedItem._Voucher);
                    AddDockItem(TabControls.AccountsTransaction, dgBankStatementLine.syncEntity, vheader);
                }
            }
            if(ActionType == "ViewDownloadRow")
            {
                if (selectedItem == null)
                    return;
                busyIndicator.IsBusy = true;
                ViewVoucher(TabControls.VouchersPage3, dgBankStatementLine.syncEntity);
                busyIndicator.IsBusy = false;             
            }
            else
                gridRibbon_BaseActions(ActionType);
        }

        public async override Task InitQuery()
        {
            busyIndicator.IsBusy = true;
            var bankStmtLines = (BankStatementLineGridClient[])await bankTransApi.GetTransactions(new BankStatementLineGridClient(), master, fromDate, toDate, true);
            if (bankStmtLines != null)
            {
                if (bankStmtLines.Length > 0)
                    bankStmtLines[0]._AmountCent += Uniconta.Common.Utility.NumberConvert.ToLong(100d * master._StartBalance);
                long Total = 0;
                for (int i = 0; (i < bankStmtLines.Length); i++)
                {
                    var p = bankStmtLines[i];
                    //if (!p._Void)
                    {
                        Total += p._AmountCent;
                        p._Total = Total;
                    }
                }
            }
            dgBankStatementLine.SetSource(bankStmtLines);
            busyIndicator.IsBusy = false;
        }
        private void btnSerach_Click(object sender, RoutedEventArgs e)
        {
            InitQuery();
        }
    }
}
