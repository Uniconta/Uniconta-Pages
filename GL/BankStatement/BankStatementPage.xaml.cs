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
using DevExpress.Xpf.Docking;
using System.Web;
using System.Security.Policy;
using static System.Net.WebRequestMethods;
using Uniconta.WindowsAPI.GL.Bank.Bank;
using UnicontaClient.Controls.Dialogs;

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
        }

        void dgBankStatement_RowDoubleClick()
        {
            ribbonControl.PerformRibbonAction("MatchLines");
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
                    AddDockItem(TabControls.BankStatementPage2, api, Uniconta.ClientTools.Localization.lookup("BankStatement"), "Add_16x16");
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
                case "ConnectToBank":
                    if (selectedItem == null) return;

                    CWBankAPI cwBank = new CWBankAPI(api, selectedItem);
                    cwBank.Closing += async delegate
                    {
                        if (cwBank.DialogResult == true)
                            await BankAPI(cwBank);
                    };
                    cwBank.Show();
                    break;
                case "BankLog":
                    if (selectedItem != null)
                        AddDockItem(TabControls.BankLogGridPage, dgBankStatement.syncEntity);
                    break;

                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        async Task ConnectAiia(bool aiiaHub)
        {
            try
            {
                var uri = aiiaHub ? Aiia.GetURIAiiaHub() : await Aiia.GetURIAiiaApp(api);
                if (aiiaHub)
                {
                    Utility.OpenWebSite(uri);
                }
                else
                {
                    var cwBrowserView = new CWBrowserDialog(uri);
                    cwBrowserView.Show();
                }
            }
            catch
            {
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
            var bankStmtLines = (BankStatementLineGridClient[])await bankTransApi.GetTransactions(new BankStatementLineGridClient(), master, fromDate, toDate, false);

            var src = new List<TextAmount>();
            long BankTotalNoMatch = 0, Total = 0, Void = 0;
            if (bankStmtLines != null)
            {
                if (bankStmtLines.Length > 0)
                    bankStmtLines[0]._AmountCent += Uniconta.Common.Utility.NumberConvert.ToLong(100d * master._StartBalance);
                for (int i = 0; (i < bankStmtLines.Length); i++)
                {
                    var p = bankStmtLines[i];
                    if (!p._Void)
                    {
                        Total += p._AmountCent;
                        p._Total = Total;
                        if (p.State == 1) // red
                        {
                            BankTotalNoMatch += p._AmountCent;
                            src.Add(new TextAmount() { Date = p._Date, AmountCent = p._AmountCent, Text = p.Text }); // use Text property
                        }
                    }
                    else
                        Void += p._AmountCent;
                }
            }

            var pollsrc = new PollReportSource();
            pollsrc.FromDate = fromDate;
            pollsrc.Date = toDate;
            pollsrc.BankAcc = master;
            pollsrc.BankTotal = (Total + Void) / 100d;
            pollsrc.BankVoid = Void / -100d;
            pollsrc.Source2 = src;

            src = new List<TextAmount>();
            Total = 0;
            Void = 0;
            BankTotalNoMatch = 0;

            var tranApi = new Uniconta.API.GeneralLedger.ReportAPI(api);
            var listtran = (GLTransClientTotal[])await tranApi.GetBank(new GLTransClientTotal(), master._Account, fromDate, toDate, true, false);
            if (listtran != null)
            {
                Array.Sort(listtran, new GLTransClientSort());
                bool ShowCurrency = (!tranApi.CompanyEntity.SameCurrency(master._Currency));

                for (int i = 0; (i < listtran.Length); i++)
                {
                    var p = listtran[i];
                    var AmountCent = ShowCurrency ? p._AmountCurCent : p._AmountCent;
                    if (!p._Void)
                    {
                        Total += AmountCent;
                        p._Total = Total;

                        if (p.State == 1) // red
                        {
                            BankTotalNoMatch += p._AmountCent;
                            src.Add(new TextAmount() { Date = p._Date, Voucher = p._Voucher, Text = p.Text, AmountCent = AmountCent }); // use Text property
                        }
                    }
                    else
                        Void += p._AmountCent;
                }
            }

            pollsrc.PostedTotal = (Total + Void) / 100d;
            pollsrc.PostedVoid = Void / -100d;
            pollsrc.Source3 = src;

            busyIndicator.IsBusy = false;

            return pollsrc;
        }

        async Task BankAPI(CWBankAPI cwBank)
        {
            BankStatementAPI bankApi = new BankStatementAPI(api);

            string dialogText = null;
            string logText = null;
            var serviceId = cwBank.ServiceId;
            ErrorCodes err;

            switch (CWBankAPI.Type)
            {
                case 0: //Register
                    if (CWBankAPI.BankService == 0)
                        serviceId = string.Concat("CheckServiceId", serviceId);

                    err = await bankApi.ActivateBankConnect(serviceId, (byte)CWBankAPI.BankService, (byte)cwBank.Bank, cwBank.ActivationCode);
                    switch (err)
                    {
                        case ErrorCodes.Succes:
                            if (CWBankAPI.BankService == 0)
                                await ConnectAiia(false);
                            else
                                dialogText = string.Concat(Uniconta.ClientTools.Localization.lookup("ConnectedTo"), " ", cwBank.BankServiceName);
                            break;
                        case ErrorCodes.AutoBankingNotActivated: dialogText = Uniconta.ClientTools.Localization.lookup("AutoBankingNotActivated"); break;
                        case ErrorCodes.AiiaNotActivated: dialogText = Uniconta.ClientTools.Localization.lookup("AiiaNotActivated"); break;
                        case ErrorCodes.IgnoreUpdate:
                        case ErrorCodes.CouldNotCreate:
                        case ErrorCodes.NoSucces: dialogText = string.Concat(Uniconta.ClientTools.Localization.lookup("UnableToConnectTo"), " ", cwBank.BankServiceName); break;
                        case ErrorCodes.KeyExists:
                        case ErrorCodes.RecordExists: dialogText = string.Concat(Uniconta.ClientTools.Localization.lookup("AlreadyConnectedTo"), " ", cwBank.BankServiceName); break;
                        default: dialogText = Uniconta.ClientTools.Localization.lookup(err.ToString()); break;
                    }
                    break;
                case 1: //Connect
                    if (CWBankAPI.BankService == 0)
                        break;
                    err = await bankApi.AddBankConnect(serviceId, (byte)CWBankAPI.BankService, cwBank.Company.CompanyId, 1);
                    switch (err)
                    {
                        case ErrorCodes.Succes: dialogText = string.Format("{0} {1}: ({2}) {3}", Uniconta.ClientTools.Localization.lookup("ConnectedTo"), Uniconta.ClientTools.Localization.lookup("Company"), cwBank.Company.CompanyId, cwBank.Company.Name); break;
                        case ErrorCodes.AutoBankingNotActivated: dialogText = Uniconta.ClientTools.Localization.lookup("AutoBankingNotActivated"); break;
                        case ErrorCodes.AiiaNotActivated: dialogText = Uniconta.ClientTools.Localization.lookup("AiiaNotActivated"); break;
                        case ErrorCodes.IgnoreUpdate:
                        case ErrorCodes.CouldNotCreate:
                        case ErrorCodes.NoSucces: dialogText = string.Format("{0} {1}: ({2}) {3}", Uniconta.ClientTools.Localization.lookup("UnableToConnectTo"), Uniconta.ClientTools.Localization.lookup("Company"), cwBank.Company.CompanyId, cwBank.Company.Name); break;
                        case ErrorCodes.KeyExists:
                        case ErrorCodes.RecordExists: dialogText = string.Concat(Uniconta.ClientTools.Localization.lookup("AlreadyConnectedTo"), " ", cwBank.BankServiceName); break;
                        default: dialogText = Uniconta.ClientTools.Localization.lookup(err.ToString()); break;
                    }
                    break;
                case 2: //Unregister
                    err = await bankApi.AddBankConnect(serviceId, (byte)CWBankAPI.BankService, cwBank.Company.CompanyId, 2);
                    switch (err)
                    {
                        case ErrorCodes.Succes: dialogText = string.Concat(Uniconta.ClientTools.Localization.lookup("Unregistered"), " ", cwBank.BankServiceName); break;
                        case ErrorCodes.AutoBankingNotActivated: dialogText = Uniconta.ClientTools.Localization.lookup("AutoBankingNotActivated"); break;
                        case ErrorCodes.AiiaNotActivated: dialogText = Uniconta.ClientTools.Localization.lookup("AiiaNotActivated"); break;
                        case ErrorCodes.IgnoreUpdate: dialogText = string.Format("{0} {1}: ({2}) {3}", Uniconta.ClientTools.Localization.lookup("UnableToConnectTo"), Uniconta.ClientTools.Localization.lookup("Company"), cwBank.Company.CompanyId, cwBank.Company.Name); break;
                        case ErrorCodes.NoSubscription:
                        case ErrorCodes.CannotDeleteRecord: dialogText = Uniconta.ClientTools.Localization.lookup("ConnectionCannotUnregister"); break;
                        default: dialogText = Uniconta.ClientTools.Localization.lookup(err.ToString()); break;
                    }
                    break;
                case 3: //Sync
                    if (CWBankAPI.BankService == 0)
                    {
                        var authUrl = await bankApi.AiiaSynchronize();
                        if (authUrl != null)
                        {
                            var cwBrowserView = new CWBrowserDialog(authUrl);
                            cwBrowserView.Show();
                        }
                        else
                            dialogText = Uniconta.ClientTools.Localization.lookup("RequestSent");
                    }
                    break;
                case 4: //OnDemand
                    if (CWBankAPI.BankService == 0)
                    {
                        err = await bankApi.AiiaTransOnDemand(cwBank.BankAccount, CWBankAPI.FromDate, CWBankAPI.ToDate);
                        switch (err)
                        {
                            case ErrorCodes.Succes: dialogText = Uniconta.ClientTools.Localization.lookup("RequestSent"); break;
                            case ErrorCodes.AiiaNotActivated: dialogText = Uniconta.ClientTools.Localization.lookup("AiiaNotActivated"); break;
                            default: dialogText = Uniconta.ClientTools.Localization.lookup(err.ToString()); break;
                        }
                    }
                    else
                        dialogText = Uniconta.ClientTools.Localization.lookup("FunctionNotSupported");
                    break;
                case 5: //Service Info
                    logText = await bankApi.ShowBankConnect(serviceId, (byte)CWBankAPI.BankService);
                    if (logText == null)
                    {
                        dialogText = string.Concat(Uniconta.ClientTools.Localization.lookup("UnableToConnectTo"), " ", cwBank.BankServiceName);
                        logText = null;
                    }
                    break;
                case 6: //Settings
                    if (CWBankAPI.BankService == 0)
                        await ConnectAiia(true);
                    break;
            }

            if (logText != null)
            {
                CWShowBankAPILog cwLog = new CWShowBankAPILog(logText);
                cwLog.Show();
            }
            else if (dialogText != null)
                UnicontaMessageBox.Show(dialogText, Uniconta.ClientTools.Localization.lookup("Information"));
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
                            {
                                result = await bkapi.DeleteLines(selectedItem, Wininterval.FromDate, Wininterval.ToDate, OnlyVoided: Wininterval.OnlyVoided);
                                if (result == ErrorCodes.Succes)
                                    bkapi.CompanyEntity.LoadCache(typeof(BankStatement), api, true); // we delete lines, then we have a new balance
                            }
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

        protected override void LoadCacheInBackGround()
        {
            var lst = new List<Type>(12) { typeof(Uniconta.DataModel.GLAccount), typeof(Uniconta.DataModel.GLVat), typeof(Uniconta.DataModel.Debtor), typeof(Uniconta.DataModel.Creditor), typeof(Uniconta.DataModel.GLTransType), typeof(Uniconta.DataModel.NumberSerie), typeof(Uniconta.DataModel.PaymentTerm), typeof(Uniconta.DataModel.GLTransType), typeof(Uniconta.DataModel.GLChargeGroup) };
            var noofDimensions = api.CompanyEntity.NumberOfDimensions;
            if (noofDimensions >= 1)
                lst.Add(typeof(Uniconta.DataModel.GLDimType1));
            if (noofDimensions >= 2)
                lst.Add(typeof(Uniconta.DataModel.GLDimType2));
            if (noofDimensions >= 3)
                lst.Add(typeof(Uniconta.DataModel.GLDimType3));
            if (noofDimensions >= 4)
                lst.Add(typeof(Uniconta.DataModel.GLDimType4));
            if (noofDimensions >= 5)
                lst.Add(typeof(Uniconta.DataModel.GLDimType5));
            LoadType(lst);
        }

        private void Name_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            dgBankStatement_RowDoubleClick();
        }
    }
}
