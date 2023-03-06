using System;
using System.Collections.Generic;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using UnicontaClient.Models;
using Uniconta.API.System;
using Uniconta.ClientTools.Controls;
using Uniconta.DataModel;
using Uniconta.ClientTools.DataModel;
using System.IO;
using Uniconta.API.GeneralLedger;
using Uniconta.ClientTools.Util;
using System.Windows;
using System.Threading.Tasks;
using Borgun.CreditCard.Settlement;
using static UnicontaClient.Pages.Iceland_PaymentProviderConnectionInfo;
using Uniconta.Common.Utility;
using System.Text;
using Uniconta.ClientTools;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class Iceland_ImportPSPSettlements : FormBasePage
    {
        public override UnicontaBaseEntity ModifiedRow { get; set; }
        public override Type TableType
        {
            get { return null; }
        }

        public override void OnClosePage(object[] refreshParams)
        {
            globalEvents.OnRefresh(NameOfControl, refreshParams);
        }

        public string selectedJournal { get { return (importMaster as GLDailyJournalClient).Journal; } }
        public string CreditcardClaims { get; set; }
        public string CreditcardFees { get; set; }
        public string DeviceRent { get; set; }
        public string BankAccount { get; set; }
        public DateTime LastImport { get { return dtLastImport.DateTime; } set { dtLastImport.EditValue = value; } }
        public string LoginId { get; set; }
        public string Password { get; set; }
        public string MerchantID { get; set; }
        public string SettlementNumber { get; set; }
        public DateTime FromDate { get { return txtfromDate.DateTime; } set { txtfromDate.EditValue = value; } }
        public bool AllTrans { get { return (bool)chkallTrans.IsChecked; } set { chkallTrans.IsChecked = value; } }
        public string CodeRent { get; set; }

        public CrudAPI Api = null;
        public Company Comp = null;
        public Iceland_PSPHelpers helpers;
        BankImportFormatClient formattouse;
        readonly UnicontaBaseEntity importMaster;
        SQLCache AccountsCache, CreditorsCache;

        public Iceland_ImportPSPSettlements(UnicontaBaseEntity sourceData) :
            base(sourceData)
        {
            InitializeComponent();
            importMaster = sourceData;

            this.DataContext = this;
            Api = this.api;
            Comp = Api.CompanyEntity;
            helpers = new Iceland_PSPHelpers(Api);
            layoutControl = layoutItems;
            frmRibbon.OnItemClicked += frmRibbon_OnItemClicked;
            BusyIndicator = busyIndicator;
            LoginId = "";
            Password = "";
            LastImport = DateTime.Now.AddDays(-1);
            MerchantID = "";
            SettlementNumber = "";
            AllTrans = false;
            CodeRent = "";

            PrepareUI();
        }

        public override string NameOfControl
        {
            get { return TabControls.Iceland_ImportPSPSettlements; }
        }

        private async Task<bool> SettlementAlreadyBookedAsync(string settlementNumber)
        {
            var listPropval = new List<PropValuePair>() { PropValuePair.GenereteWhereElements("ReferenceNumber", settlementNumber, CompareOperator.Equal) };
            var FindDebInvLocal = await Api.Query<GLTransClient>(listPropval);
            if (FindDebInvLocal.Length != 0)
                return true;
            else
                return false;
        }

        private async Task<bool> getUIData()
        {
            var listPropval = new List<PropValuePair>() { PropValuePair.GenereteWhereElements(nameof(BankImportFormatClient.CountryId), typeof(int), NumberConvert.ToString((int)CountryCode.Iceland)) };
            var res = await Api.Query<BankImportFormatClient>(listPropval);
            cmdProviders.ItemsSource = res;

            foreach (var bankformat in res)
            {
                if (bankformat.ImportInto == "Dagbók" && bankformat.FormatName.Contains("Færsluhirðir") && bankformat.FormatName.Contains(selectedJournal))
                {
                    formattouse = bankformat;
                    return true;
                }
            }
            return false;
        }

        private async Task<bool> saveUIData()
        {
            formattouse.PaymentIdentifier = CreditcardClaims + ";" + CreditcardFees + ";" + DeviceRent + ";" + BankAccount + ";";
            formattouse.LoginId = LoginId;
            formattouse.Password = Password;

            var merc = helpers.ToByteArray(MerchantID);
            formattouse.SkipLines = merc[0];
            formattouse.DatePosition = merc[1];
            formattouse.TextPosition = merc[2];
            formattouse.AmountPosition = merc[3];
            formattouse.VoucherPos = merc[4];
            formattouse.InvoicePos = merc[5];
            formattouse.ReferencePosition = merc[6];
            formattouse.BankAccount = BankAccount;


            formattouse.SetMaster(Api.CompanyEntity);
            var res = await Api.Update(formattouse);
            if (res != ErrorCodes.Succes)
            {
                UtilDisplay.ShowErrorCode(res);
                return true;
            }
            else
            {
                return false;
            }
        }

        private async Task LoadUI()
        {
            if (!await getUIData())
            {
                var newprovider = new BankImportFormatClient();
                newprovider.SetMaster(Api.CompanyEntity);
                newprovider.CountryId = CountryCode.Iceland;
                newprovider.FormatName = "Færsluhirðir: " + selectedJournal;
                newprovider.PaymentIdentifier = CreditcardClaims + ";" + CreditcardFees + ";" + DeviceRent + ";";
                newprovider.LoginId = LoginId;
                newprovider.Password = Password;
                newprovider.Journal = selectedJournal;
                newprovider.BankAccount = BankAccount;
                var res = await Api.Insert(newprovider);
                if (res != ErrorCodes.Succes)
                    UtilDisplay.ShowErrorCode(res);
                else
                    await getUIData();
            }
            else
            {
                var merc = new byte[7];
                string[] sUIprops = formattouse.PaymentIdentifier.Split(';');
                CreditcardClaims = sUIprops[0];
                CreditcardFees = sUIprops[1];
                DeviceRent = sUIprops[2];
                BankAccount = formattouse.BankAccount;
                LoginId = formattouse.LoginId;
                Password = formattouse.Password;
                merc[0] = formattouse.SkipLines;
                merc[1] = formattouse.DatePosition;
                merc[2] = formattouse.TextPosition;
                merc[3] = formattouse.AmountPosition;
                merc[4] = formattouse.VoucherPos;
                merc[5] = formattouse.InvoicePos;
                merc[6] = formattouse.ReferencePosition;
                MerchantID = Encoding.Default.GetString(merc);
            }
            cmdProviders.SelectedItem = formattouse;
        }

        async void PrepareUI()
        {
            LoadCacheInBackGround();
            if (SelectedProvider() != Providers.Unknown)
                await LoadUI();

            CreditcardClaimsLookupEditor.cache = AccountsCache;
            CreditcardFeesLookupEditor2.cache = AccountsCache;
            DeviceRentLookupEditor.cache = CreditorsCache;
            BankAccountLookupEditor.cache = AccountsCache;

            // Set code for device rent
            switch (SelectedProvider())
            {
                case Providers.Saltpay:
                    CodeRent = "F_MONTHLY";
                    FromDate = DateTime.Now.AddDays(-1);
                    break;

                case Providers.Valitor:
                    CodeRent = "";
                    break;

                case Providers.Rapyd:
                    CodeRent = "";
                    break;

                default:
                    CodeRent = "";
                    break;
            }
        }

        private Providers SelectedProvider()
        {
            if ((bool)(selectedJournal.ToLower().Contains("saltpay")) || (bool)(selectedJournal.ToLower().Contains("borgun")))
                return Providers.Saltpay;
            else if ((bool)(selectedJournal.ToLower().Contains("rapyd")) || (bool)(selectedJournal.ToLower().Contains("korta")))
                return Providers.Rapyd;
            else if ((bool)(selectedJournal.ToLower().Contains("valitor")) || (bool)(selectedJournal.ToLower().Contains("visa")) || (bool)(selectedJournal.ToLower().Contains("vísa")))
                return Providers.Valitor;
            else
                return Providers.Unknown;
        }

        async void Import()
        {
            if (formattouse == null || SelectedProvider() == Providers.Unknown)
            {
                UnicontaMessageBox.Show("Óþekktur færsluhirðir", "Villa");
                return;
            }

            var postingApi = new PostingAPI(Api);

            var maxline = await postingApi.MaxLineNumber(selectedJournal);
            if (maxline == 0)
                doImport(formattouse, selectedJournal);
            else
            {
                var text = string.Format("Dagbókin '{0}' inniheldur línur. Viltu halda áfram?", selectedJournal);
                CWConfirmationBox dialog = new CWConfirmationBox(text, Uniconta.ClientTools.Localization.lookup("Warning"), true);
                dialog.Closing += delegate
                {
                    var res = dialog.ConfirmationResult;
                    if (res == CWConfirmationBox.ConfirmationResultEnum.Yes)
                        doImport(formattouse, selectedJournal);
                };
                dialog.Show();
            }
        }

        async void doImport(BankImportFormatClient selectedbankFormat, string journal = null)
        {
            SetBusy();
            double settlementAmount = 0.0;

            using (var journalsettlement = new Iceland_JournalSettlements(Api, journal, MerchantID, CreditcardClaims, CreditcardFees, DeviceRent, BankAccount))
            using (var prov = new Iceland_PaymentProviderSettlements(new Iceland_PaymentProviderConnectionInfo(SelectedProvider(), MerchantID, FromDate, LoginId, Password)))
            {
                var settlements = prov.getSettlements();

                if (prov.Ok && settlements != null)
                {
                    foreach (var settlement in (CreditCardSettlement[])settlements)
                    {
                        if (!(await SettlementAlreadyBookedAsync(settlement.SettlementRunNumber)) &&
                            (string.IsNullOrEmpty(SettlementNumber) || SettlementNumber == settlement.SettlementRunNumber))
                        {
                            if (!await handleTransactions(settlementAmount, !AllTrans, journalsettlement, prov, settlement))
                                break;

                            if (!await handleDeductions(journalsettlement, prov, settlement))
                                break;

                            await journalsettlement.queuePaymentAsync("Millifært (" + prov.connectInfo.NameOfProvider() + ")", settlement.SettlementRunNumber, settlement.Payment, settlement.CurrencyCode, settlement.SettlementDate);
                            await journalsettlement.saveToJournal();
                        }
                    }

                }

                ClearBusy();
                if (!prov.Ok)
                    UnicontaMessageBox.Show(prov.Message, prov.MessageHeader);
                else if (!await saveUIData())
                    ShowGlDailyJournalLines(journal);
            }
        }

        private async Task<bool> handleTransactions(double settlementAmount, bool aggregateTransactions, Iceland_JournalSettlements journalsettlement, Iceland_PaymentProviderSettlements prov, CreditCardSettlement settlement)
        {
            var transactions = prov.getTransactions(settlement.SettlementRunNumber);
            if (prov.Ok && transactions != null)
            {
                settlementAmount = 0.0;
                foreach (var transaction in (CreditCardTransaction[])transactions)
                {
                    if (transaction.SettlementRunNumber == settlement.SettlementRunNumber)
                    {
                        if (!aggregateTransactions)
                        {
                            await journalsettlement.queueTransactionAsync("auth. no." + transaction.AuthorizationCode, transaction.CurrencyCode, (-1) * transaction.Amount,
                                    settlement.SettlementRunNumber, transaction.BatchDate);
                        }
                        else
                        {
                            settlementAmount = settlementAmount + (double)((-1) * transaction.Amount);
                        }
                    }
                }
                if (aggregateTransactions)
                {
                    await journalsettlement.queueTransactionAsync("settlem. no." + settlement.SettlementRunNumber, settlement.CurrencyCode,
                        (decimal)settlementAmount, settlement.SettlementRunNumber, settlement.SettlementDate);
                }
                await journalsettlement.saveToJournal();
            }
            return prov.Ok;
        }

        private async Task<bool> handleDeductions(Iceland_JournalSettlements journalsettlement, Iceland_PaymentProviderSettlements prov, CreditCardSettlement settlement)
        {
            var deductions = prov.getDeduction(settlement.SettlementRunNumber);
            if (prov.Ok && deductions != null)
            {
                foreach (var deduction in (CreditCardSettlementDeduction[])deductions)
                {
                    if (deduction.SettlementRunNumber == settlement.SettlementRunNumber)
                    {
                        await journalsettlement.queueDeductionAsync((-1) * deduction.Amount, deduction.Code == CodeRent, deduction.CurrencyCode, deduction.Text, settlement.SettlementRunNumber, settlement.SettlementDate);
                    }
                }
                await journalsettlement.saveToJournal();
            }
            return prov.Ok;
        }

        private void ShowGlDailyJournalLines(string journal)
        { // display the journal on the screen
            var parms = new[] { new BasePage.ValuePair("Journal", journal) };
            var header = string.Concat(Uniconta.ClientTools.Localization.lookup("Journal"), ": ", journal);
            AddDockItem(TabControls.GL_DailyJournalLine, null, header, null, true, null, parms);
        }

        void frmRibbon_OnItemClicked(string ActionType)
        {
            MoveFocus(true);
            switch (ActionType)
            {
                case "Import":
                    Import();
                    break;
            }
        }

        protected override async void LoadCacheInBackGround()
        {
            AccountsCache = Comp.GetCache(typeof(Uniconta.DataModel.GLAccount));
            if (AccountsCache == null)
                AccountsCache = await Comp.LoadCache(typeof(Uniconta.DataModel.GLAccount), Api).ConfigureAwait(false);
            CreditorsCache = Comp.GetCache(typeof(Uniconta.DataModel.Creditor));
            if (CreditorsCache == null)
                CreditorsCache = await Comp.LoadCache(typeof(Uniconta.DataModel.Creditor), Api).ConfigureAwait(false);
        }

        private void btnAttach_Click(object sender, RoutedEventArgs e)
        {
        }

        private void cmdProviders_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            cmdProviders.IsEnabled = false;
        }
    }

    public class Iceland_PaymentProviderConnectionInfo : IDisposable
    {
        private Providers _provider;
        public Providers Provider { get => _provider; set { _provider = value; } }
        private string _merchantId;
        public string MerchantId { get => _merchantId; set { _merchantId = value; } }
        private string _username;
        public string Username { get => _username; set { _username = value; } }
        private string _password;
        public string Password { get => _password; set { _password = value; } }
        private DateTime _datefrom;
        public DateTime DateFrom { get => _datefrom; set { _datefrom = value; } }

        public Iceland_PaymentProviderConnectionInfo(Providers _prov, string _merchId, DateTime _dateFr, string _user, string _passw)
        {
            Provider = _prov;
            MerchantId = _merchId;
            Username = _user;
            Password = _passw;
            DateFrom = _dateFr;
        }

        public void Dispose() { }

        public enum Providers
        {
            Saltpay, Rapyd, Valitor, Unknown
        }

        public string NameOfProvider()
        {
            switch (Provider)
            {
                case Providers.Saltpay:
                    return "SaltPay";
                case Providers.Rapyd:
                    return "Rapyd";
                case Providers.Valitor:
                    return "Valitor";
                case Providers.Unknown:
                    return "Unknown";
            }
            return "";
        }
    }

    public class Iceland_PaymentProviderSettlements : IDisposable
    {
        private string _message;
        public string Message { get => _message; set { _message = value; } }
        private string _messageheader;
        public string MessageHeader { get => _messageheader; set { _messageheader = value; } }

        public Iceland_PaymentProviderConnectionInfo connectInfo = null;
        private object p = null;

        public Iceland_PaymentProviderSettlements(Iceland_PaymentProviderConnectionInfo _iceland_PaymentProviderConnectionInfo)
        {
            _okstatus = true;
            connectInfo = _iceland_PaymentProviderConnectionInfo;
            switch (connectInfo.Provider)
            {
                case Providers.Saltpay:
                    p = new SaltPay_CreditCardClient(connectInfo.Username, connectInfo.Password);
                    break;
            }
        }

        private bool _okstatus;
        public bool Ok { get => _okstatus; set { _okstatus = value; } }

        public void Dispose()
        {
            ((IDisposable)p)?.Dispose();
        }

        internal object getSettlements()
        {
            try
            {
                switch (connectInfo.Provider)
                {
                    case Providers.Saltpay:
                        var res = (p as SaltPay_CreditCardClient).CreditCardClient.GetSettlementsByMerchant(new GetSettlementsByMerchantRequest(connectInfo.MerchantId, connectInfo.DateFrom, DateTime.Now, true, true));
                        return res.GetSettlementsByMerchantResult;
                }
            }
            catch (Exception ex)
            {
                _okstatus = false;
                _message = ex.InnerException.Message;
                _messageheader = "Error";
            }
            return null;
        }

        internal object getTransactions(string settlementRunNumber)
        {
            try
            {
                switch (connectInfo.Provider)
                {
                    case Providers.Saltpay:
                        var res = (p as SaltPay_CreditCardClient).CreditCardClient.GetTransactionsByRunNumber(new GetTransactionsByRunNumberRequest(connectInfo.MerchantId, settlementRunNumber));
                        return res.GetTransactionsByRunNumberResult;
                }
            }
            catch (Exception ex)
            {
                _okstatus = false;
                _message = ex.InnerException.Message;
                _messageheader = "Error";
            }
            return null;
        }

        internal object getDeduction(string settlementRunNumber)
        {
            try
            {
                switch (connectInfo.Provider)
                {
                    case Providers.Saltpay:
                        var res = (p as SaltPay_CreditCardClient).CreditCardClient.GetSettlementDeductionItems(new GetSettlementDeductionItemsRequest(connectInfo.MerchantId, settlementRunNumber));
                        return res.GetSettlementDeductionItemsResult;
                }
            }
            catch (Exception ex)
            {
                _okstatus = false;
                _message = ex.InnerException.Message;
                _messageheader = "Error";
            }
            return null;
        }
    }

    public class Iceland_JournalSettlements : IDisposable
    {
        private CrudAPI api;
        private Company Comp;
        private SQLCache Cache, accountsCache, VatCache;
        private Uniconta.DataModel.GLDailyJournal masterJournal = null;
        List<GLDailyJournalLineClient> journalLinesToPost = new List<GLDailyJournalLineClient>();
        private string merchantId = "";
        private string CreditcardClaims;
        private string CreditcardFees;
        private string DeviceRent;
        private string BankAccount;
        private Iceland_PSPHelpers helpers;

        public Iceland_JournalSettlements(CrudAPI _api, string _journal, string _mId, string _CreditcardClaims, string _CreditcardFees, string _DeviceRent, string _BankAccount)
        {
            api = _api;
            Comp = api.CompanyEntity;
            merchantId = _mId;
            getAvailResources(_journal);
            CreditcardClaims = _CreditcardClaims;
            CreditcardFees = _CreditcardFees;
            DeviceRent = _DeviceRent;
            BankAccount = _BankAccount;
            helpers = new Iceland_PSPHelpers(api);
        }

        private async void getAvailResources(string journalName)
        {
            Cache = Comp.GetCache(typeof(Uniconta.DataModel.GLDailyJournal));
            if (Cache == null)
                Cache = await Comp.LoadCache(typeof(Uniconta.DataModel.GLDailyJournal), api);
            masterJournal = (Uniconta.DataModel.GLDailyJournal)Cache.Get(journalName);
            accountsCache = api.CompanyEntity.GetCache(typeof(Uniconta.DataModel.GLAccount)) ?? await api.CompanyEntity.LoadCache(typeof(Uniconta.DataModel.GLAccount), api);
            if (VatCache == null)
                VatCache = await api.LoadCache(typeof(Uniconta.DataModel.GLVat)).ConfigureAwait(false);
        }

        public void Dispose()
        {
            journalLinesToPost = null;
        }

        internal async Task saveToJournal()
        {
            foreach (var journalline in journalLinesToPost)
                journalline.SetMaster(masterJournal);
            var resInsert = await api.Insert(journalLinesToPost);
            if (resInsert == ErrorCodes.Succes)
                journalLinesToPost.Clear();
            else
                UtilDisplay.ShowErrorCode(resInsert);
        }

        private async Task<GLDailyJournalLineClient> createNewJournalLineAsync(string CurrencyCode, decimal Amount, DateTime transDate, string SettlementRunNumber)
        {
            var journalLine = new GLDailyJournalLineClient();
            journalLine._AccountType = (byte)masterJournal._DefaultAccountType;
            journalLine._Account = masterJournal._Account;
            journalLine._OffsetAccount = masterJournal._OffsetAccount;
            if (journalLine._TransType == null)
                journalLine._TransType = masterJournal._TransType;
            journalLine._Vat = masterJournal._Vat;
            journalLine._OffsetVat = masterJournal._OffsetVat;
            journalLine._Dim1 = masterJournal._Dim1;
            journalLine._Dim2 = masterJournal._Dim2;
            journalLine._Dim3 = masterJournal._Dim3;
            journalLine._Dim4 = masterJournal._Dim4;
            journalLine._Dim5 = masterJournal._Dim5;
            journalLine._SettleValue = masterJournal._SettleValue;
            journalLine.Currency = CurrencyCode;
            if (CurrencyCode == "ISK")
                journalLine.Amount = (double)Amount;
            else
            {
                journalLine.AmountCur = (double)Amount;
                var xchngRate = await api.session.ExchangeRate((Currencies)journalLine.CurrencyEnum, Currencies.ISK, transDate, api.CompanyEntity);
                journalLine.Amount = Math.Round(journalLine.AmountCur * xchngRate, api.CompanyEntity.RoundTo100 ? 0 : 2);
            }
            journalLine.Date = transDate;
            journalLine.ReferenceNumber = SettlementRunNumber;
            return journalLine;
        }

        internal async Task queueTransactionAsync(string AuthorizationCode, string CurrencyCode, decimal Amount, string SettlementRunNumber, DateTime transDate)
        {
            var journalLine = await createNewJournalLineAsync(CurrencyCode, Amount, transDate, SettlementRunNumber);
            journalLine.Text = "MerchantId " + merchantId + ", " + AuthorizationCode;
            journalLine.Account = CreditcardClaims;
            journalLinesToPost.Add(journalLine);
        }

        internal async Task queueDeductionAsync(decimal Amount, bool deviceRent, string CurrencyCode, string Text, string SettlementRunNumber, DateTime transDate)
        {
            var journalLine = await createNewJournalLineAsync(CurrencyCode, Amount, transDate, SettlementRunNumber);
            if (deviceRent)
            {
                var bookingaccount = (GLAccountClient)accountsCache.Get(DeviceRent);
                journalLine.AccountType = AppEnums.DebtorCreditor.ToString(2);
                journalLine.Account = DeviceRent;
                //journalLine._Vat = bookingaccount._Vat;
                //journalLine._VatOperation = bookingaccount._VatOperation;
            }
            else
                journalLine.Account = CreditcardFees;
            journalLine.Text = Text;
            journalLinesToPost.Add(journalLine);
        }

        internal async Task queuePaymentAsync(string text, string SettlementRunNumber, decimal Amount, string CurrencyCode, DateTime transDate)
        {
            var journalLine = await createNewJournalLineAsync(CurrencyCode, Amount, transDate, SettlementRunNumber);
            journalLine.Account = BankAccount;
            journalLine.Text = text;
            journalLinesToPost.Add(journalLine);
        }
    }

    public class Iceland_PSPHelpers
    {
        private CrudAPI api = null;

        public Iceland_PSPHelpers(CrudAPI _api)
        {
            api = _api;
        }

        public byte[] ToByteArray(string str)
        {
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write(Encoding.UTF8.GetBytes(str));
                return stream.ToArray();
            }
        }
    }
}
