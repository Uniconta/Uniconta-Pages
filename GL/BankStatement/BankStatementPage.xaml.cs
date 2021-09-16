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
using UnicontaClient.Pages.Maintenance;
using Uniconta.API.Service;
using UnicontaClient.Pages.GL.BankStatement;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class BankStatementGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(BankStatementClient); } }
    }
    public partial class BankStatementPage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.BankStatementPage; } }
        static public DateTime fromDate;
        static public DateTime toDate;

        public BankStatementPage(BaseAPI API) : base(API, string.Empty)
        {
            Init();
        }
        public BankStatementPage(BaseAPI api, string lookupKey)
            : base(api, lookupKey)
        {
            Init();
        }
        void Init()
        {
            InitializeComponent();
            dgBankStatement.api = api;
            SetRibbonControl(localMenu, dgBankStatement);
            dgBankStatement.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgBankStatement.RowDoubleClick += dgBankStatement_RowDoubleClick;

            RemoveMenuItem();

            if (fromDate == DateTime.MinValue)
            {
                DateTime date = GetSystemDefaultDate();
                var firstDayOfMonth = new DateTime(date.Year, date.Month, 1);
                var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);
                fromDate = firstDayOfMonth;
                toDate = lastDayOfMonth;
            }
        }
        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            setDim();
        }
        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.BankStatementPage2)
                dgBankStatement.UpdateItemSource(argument);
        }

        void RemoveMenuItem()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            var Comp = api.CompanyEntity;

#if !SILVERLIGHT
            if (Comp._CountryId != CountryCode.Denmark && Comp._CountryId != CountryCode.Greenland && Comp._CountryId != CountryCode.FaroeIslands)
                UtilDisplay.RemoveMenuCommand(rb, "ConnectToBank");
#else
            UtilDisplay.RemoveMenuCommand(rb, "ConnectToBank");
#endif
        }

        void dgBankStatement_RowDoubleClick()
        {
            localMenu_OnItemClicked("MatchLines");
        }
        void setDim()
        {
            UnicontaClient.Utilities.Utility.SetDimensionsGrid(api, coldim1, coldim2, coldim3, coldim4, coldim5);
        }
        private void localMenu_OnItemClicked(string ActionType)
        {
            BankStatementClient selectedItem = dgBankStatement.SelectedItem as BankStatementClient;
            switch (ActionType)
            {
                case "AddRow":
                    AddDockItem(TabControls.BankStatementPage2, api, Uniconta.ClientTools.Localization.lookup("BankStatement"), "Add_16x16.png");
                    break;
                case "EditRow":
                    if (selectedItem != null)
                        AddDockItem(TabControls.BankStatementPage2, selectedItem);
                    break;
                case "MatchLines":
                    if (selectedItem != null)
                        AddDockItem(TabControls.BankStatementLinePage, selectedItem, string.Format("{0}, {1}: {2}", Uniconta.ClientTools.Localization.lookup("MatchLines"), Uniconta.ClientTools.Localization.lookup("Account"), selectedItem._Account));
                    break;
                case "LedgerPosting":
                    if (selectedItem != null)
                        AddDockItem(TabControls.LedgerPostingPage, selectedItem, string.Format("{0}, {1}: {2}", Uniconta.ClientTools.Localization.lookup("LedgerPosting"), Uniconta.ClientTools.Localization.lookup("Account"), selectedItem._Account));
                    break;
                case "StLines":
                    if (selectedItem != null)
                        AddDockItem(TabControls.StatementLine, selectedItem, string.Format("{0}, {1}: {2}", Uniconta.ClientTools.Localization.lookup("BankStatement"), Uniconta.ClientTools.Localization.lookup("Account"), selectedItem._Account));
                    break;
                case "GLTrans":
                    if (selectedItem != null)
                        AddDockItem(TabControls.StatementLineTransPage, selectedItem, string.Format("{0}, {1}: {2}", Uniconta.ClientTools.Localization.lookup("Transactions"), Uniconta.ClientTools.Localization.lookup("Account"), selectedItem._Account));
                    break;
                case "ImportBankStatement":
                    if (selectedItem != null)
                        AddDockItem(TabControls.ImportGLDailyJournal, selectedItem, string.Concat(string.Format(Uniconta.ClientTools.Localization.lookup("ImportOBJ"),
                            Uniconta.ClientTools.Localization.lookup("BankStatement")), " : ", selectedItem.Account), null, true);
                    break;
                case "RemoveSettlements":
                case "DeleteStatement":
                    if (selectedItem != null)
                        RemoveBankStatmentOrSettelements(ActionType, selectedItem);
                    break;
                case "ReconciliationReport":
                    if (selectedItem != null)
                        GeneratePollReport(selectedItem);
                    break;

#if WPF
                case "ConnectToBank":
                    if (selectedItem == null) return;

                    CWBankAPI cwBank = new CWBankAPI(api);
                    cwBank.Closing += async delegate
                    {
                        if (cwBank.DialogResult == true)
                            await BankAPI(cwBank.Type, cwBank.CustomerNo, cwBank.Bank, cwBank.ActivationCode, cwBank.Company);
                    };
                    cwBank.Show();
                    break;
#endif
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
        void GeneratePollReport(BankStatementClient selectedItem)
        {
            var toFromDateDialog = new CWCalculateCommission(api, fromDate, toDate);
            toFromDateDialog.Title = Uniconta.ClientTools.Localization.lookup("Reconciliation");
            toFromDateDialog.OKButton.Content = Uniconta.ClientTools.Localization.lookup("Generate");
            toFromDateDialog.Closing += async delegate
            {
                if (toFromDateDialog.DialogResult == true)
                {
                    fromDate = toFromDateDialog.FromDateTime;
                    toDate = toFromDateDialog.ToDateTime;
                    var source = await CreatePollReportSource(selectedItem);
                    var report = new PollReport();
                    report.DataSource = new PollReportSource[] { source };
                    var dockName = string.Concat(Uniconta.ClientTools.Localization.lookup("BankReconciliation"), ":", selectedItem._Account, ", ", selectedItem._Name);
                    AddDockItem(UnicontaTabs.StandardPrintReportPage, new object[] { report }, dockName);
                }
            };
            toFromDateDialog.Show();
        }

        async Task<PollReportSource> CreatePollReportSource(BankStatementClient master)
        {
            busyIndicator.IsBusy = true;
            var bankTransApi = new BankStatementAPI(api);
            var bankStmtLines = (BankStatementLineGridClient[])await bankTransApi.GetTransactions(new BankStatementLineGridClient(), master, fromDate, toDate, true);

            var src = new List<TextAmount>();
            long BankTotalNoMatch = 0, Total = 0;
            if (bankStmtLines != null)
            {
                if (bankStmtLines.Length > 0)
                    bankStmtLines[0]._AmountCent += Uniconta.Common.Utility.NumberConvert.ToLong(100d * master._StartBalance);
                for (int i = 0; (i < bankStmtLines.Length); i++)
                {
                    var p = bankStmtLines[i];
                    //if (!p._Void)
                    {
                        Total += p._AmountCent;
                        p._Total = Total;
                    }
                    if (p.State == 1) // red
                    {
                        BankTotalNoMatch += p._AmountCent;
                        src.Add(new TextAmount() { Date = p._Date, AmountCent = p._AmountCent, Text = p.Text }); // use Text property
                    }
                }
            }

            var pollsrc = new PollReportSource();
            pollsrc.FromDate = fromDate;
            pollsrc.Date = toDate;
            pollsrc.BankAcc = master;
            pollsrc.BankTotal = Total / 100d;
            pollsrc.Source2 = src;

            src = new List<TextAmount>();
            Total = 0;
            BankTotalNoMatch = 0;

            var tranApi = new Uniconta.API.GeneralLedger.ReportAPI(api);
            var listtran = (GLTransClientTotal[])await tranApi.GetBank(new GLTransClientTotal(), master._Account, fromDate, toDate, true);
            if (listtran != null)
            {
                Array.Sort(listtran, new GLTransClientSort());
                bool ShowCurrency = (!tranApi.CompanyEntity.SameCurrency(master._Currency));

                for (int i = 0; (i < listtran.Length); i++)
                {
                    var p = listtran[i];
                    var AmountCent = ShowCurrency ? p._AmountCurCent : p._AmountCent;
                    Total += AmountCent;
                    p._Total = Total;

                    if (p.State == 1) // red
                    {
                        BankTotalNoMatch += p._AmountCent;
                        src.Add(new TextAmount() { Date = p._Date, Voucher = p._Voucher, Text = p.Text, AmountCent = AmountCent }); // use Text property
                    }
                }
            }

            pollsrc.PostedTotal = Total / 100d;
            pollsrc.Source3 = src;

            busyIndicator.IsBusy = false;

            return pollsrc;
        }

        async Task BankAPI(int type, string functionId, Bank bank, string activationCode, Company masterBCCompany)
        {
            busyIndicator.IsBusy = true;
            BankStatementAPI bankApi = new BankStatementAPI(api);

            string dialogText = null;
            string logText = null;
            ErrorCodes err;

            switch (type)
            {
                case 0:
                    err = await bankApi.ActivateBankConnect(functionId, (byte)bank, activationCode);
                    switch (err)
                    {
                        case ErrorCodes.Succes:
                            dialogText = string.Concat(Uniconta.ClientTools.Localization.lookup("ConnectedTo"), " Bank Connect");
                            break;
                        case ErrorCodes.IgnoreUpdate:
                        case ErrorCodes.CouldNotCreate:
                        case ErrorCodes.NoSucces:
                            dialogText = string.Concat(Uniconta.ClientTools.Localization.lookup("UnableToConnectTo"), " Bank Connect");
                            break;
                        case ErrorCodes.KeyExists:
                        case ErrorCodes.RecordExists:
                            dialogText = string.Concat(Uniconta.ClientTools.Localization.lookup("AlreadyConnectedTo"), " Bank Connect");
                            break;
                        default:
                            dialogText = Uniconta.ClientTools.Localization.lookup(err.ToString());
                            break;
                    }
                    break;
                case 1:
                    logText = await bankApi.ShowBankConnect(functionId);

                    if (logText == ErrorCodes.IgnoreUpdate.ToString())
                    {
                        dialogText = string.Concat(Uniconta.ClientTools.Localization.lookup("UnableToConnectTo"), " Bank Connect");
                        logText = null;
                    }
                    else if (logText == ErrorCodes.FileDoesNotExist.ToString())
                    {
                        dialogText = string.Concat(Uniconta.ClientTools.Localization.lookup("NoDataCollected"), ". ",
                                     Uniconta.ClientTools.Localization.lookup("CustomerNo"), ": ", functionId);
                        logText = null;
                    }
                    break;
                case 2:

                    err = await bankApi.AddBankConnect(functionId, masterBCCompany.CompanyId, 1);

                    switch (err)
                    {
                        case ErrorCodes.Succes:
                            dialogText = string.Format("{0} {1}: ({2}) {3}", Uniconta.ClientTools.Localization.lookup("ConnectedTo"), Uniconta.ClientTools.Localization.lookup("Company"), masterBCCompany.CompanyId, masterBCCompany.Name);
                            break;
                        case ErrorCodes.IgnoreUpdate:
                        case ErrorCodes.CouldNotCreate:
                        case ErrorCodes.NoSucces:
                            dialogText = string.Format("{0} {1}: ({2}) {3}", Uniconta.ClientTools.Localization.lookup("UnableToConnectTo"), Uniconta.ClientTools.Localization.lookup("Company"), masterBCCompany.CompanyId, masterBCCompany.Name);
                            break;
                        case ErrorCodes.KeyExists:
                        case ErrorCodes.RecordExists:
                            dialogText = string.Concat(Uniconta.ClientTools.Localization.lookup("AlreadyConnectedTo"), " Bank Connect");
                            break;
                        default:
                            dialogText = Uniconta.ClientTools.Localization.lookup(err.ToString());
                            break;
                    }
                    break;
                case 3:
                    err = await bankApi.AddBankConnect(functionId, masterBCCompany.CompanyId, 2);

                    switch (err)
                    {
                        case ErrorCodes.Succes:
                            dialogText = string.Concat(Uniconta.ClientTools.Localization.lookup("Unregistered"), " Bank Connect");
                            break;
                        case ErrorCodes.IgnoreUpdate:
                            dialogText = string.Format("{0} {1}: ({2}) {3}", Uniconta.ClientTools.Localization.lookup("UnableToConnectTo"), Uniconta.ClientTools.Localization.lookup("Company"), masterBCCompany.CompanyId, masterBCCompany.Name);
                            break;
                        case ErrorCodes.NoSubscription:
                        case ErrorCodes.CannotDeleteRecord:
                            dialogText = Uniconta.ClientTools.Localization.lookup("ConnectionCannotUnregister");
                            break;
                        default:
                            dialogText = Uniconta.ClientTools.Localization.lookup(err.ToString());
                            break;
                    }
                    break;
            }
            busyIndicator.IsBusy = false;

            if (logText != null)
            {
                CWShowBankAPILog cwLog = new CWShowBankAPILog(logText);
                cwLog.Show();
            }
            else if (dialogText != null)
            {
                UnicontaMessageBox.Show(dialogText, Uniconta.ClientTools.Localization.lookup("Information"));
            }
        }

        private void RemoveBankStatmentOrSettelements(string ActionType, BankStatementClient selectedItem)
        {
            var text = string.Format("{0}: {1}, {2}", Uniconta.ClientTools.Localization.lookup("BankStatement"), selectedItem._Account, selectedItem._Name);
            var defaultdate = BasePage.GetSystemDefaultDate().Date;
            CWInterval Wininterval = new CWInterval(defaultdate, defaultdate, showJrPostId: true);
            Wininterval.Closing += delegate
            {
                if (Wininterval.DialogResult == true)
                {
                    EraseYearWindow erWindow = new EraseYearWindow(text, false);
                    erWindow.Closing += async delegate
                    {
                        if (erWindow.DialogResult == true)
                        {
                            BankStatementAPI bkapi = new BankStatementAPI(api);
                            ErrorCodes result = ErrorCodes.NoSucces;

                            if (ActionType == "DeleteStatement")
                                result = await bkapi.DeleteLines(selectedItem, Wininterval.FromDate, Wininterval.ToDate, OnlyVoided: Wininterval.OnlyVoided);
                            else if (ActionType == "RemoveSettlements")
                                result = await bkapi.RemoveSettlements(selectedItem, Wininterval.FromDate, Wininterval.ToDate, Wininterval.JournalPostedId, Wininterval.Voucher);

                            if (result != ErrorCodes.Succes)
                                UtilDisplay.ShowErrorCode(result);
                        }
                    };
                    erWindow.Show();
                }
            };
            Wininterval.Show();
        }
    }
}
