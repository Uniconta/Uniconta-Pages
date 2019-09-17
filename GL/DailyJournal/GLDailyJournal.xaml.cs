using UnicontaClient.Models;
using UnicontaClient.Utilities;
using Uniconta.ClientTools;
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

using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using UnicontaClient.Pages.Maintenance;
using Uniconta.API.GeneralLedger;
using Uniconta.ClientTools.Util;
using Uniconta.ClientTools.Controls;
using Uniconta.API.Service;
using System.Windows;
using System.Text;
using System.Text.RegularExpressions;
using Uniconta.Common.Utility;
#if !SILVERLIGHT
using Bilagscan;
using System.Net.Http;
using Newtonsoft.Json;
using System.Configuration;
using System.Net.Http.Headers;
#endif

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class GLDailyJournalGrid : CorasauDataGridClient
    {
        public override Type TableType
        {
            get { return typeof(GLDailyJournalClient); }
        }
    }
    public partial class GLDailyJournal : GridBasePage
    {
        public override string NameOfControl
        {
            get { return TabControls.GL_DailyJournal.ToString(); }
        }
        public GLDailyJournal(BaseAPI API) : base(API, string.Empty)
        {
            Init();
        }
        public GLDailyJournal(BaseAPI api, string lookupKey)
            : base(api, lookupKey)
        {
            Init();
        }

        void Init()
        {
            InitializeComponent();
            SetRibbonControl(localMenu, dgGldailyJournal);
            dgGldailyJournal.api = api;
            dgGldailyJournal.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgGldailyJournal.RowDoubleClick += dgGldailyJournal_RowDoubleClick;

            RemoveMenuItem();
        }

        void RemoveMenuItem()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            var Comp = api.CompanyEntity;

            if (Comp._CountryId != CountryCode.Denmark && Comp._CountryId != CountryCode.Iceland && Comp._CountryId != CountryCode.Netherlands && Comp._CountryId != CountryCode.Austria)
                UtilDisplay.RemoveMenuCommand(rb, "BilagscanReadVouchers");
        }

        void dgGldailyJournal_RowDoubleClick()
        {
            var selectedItem = dgGldailyJournal.SelectedItem as GLDailyJournalClient;
            if (selectedItem != null)
                AddDockItem(TabControls.GL_DailyJournalLine, selectedItem);
        }

        protected override void LoadCacheInBackGround()
        {
            var lst = new List<Type>(12) { typeof(Uniconta.DataModel.GLAccount), typeof(Uniconta.DataModel.GLVat), typeof(Uniconta.DataModel.Debtor), typeof(Uniconta.DataModel.Creditor), typeof(Uniconta.DataModel.PaymentTerm), typeof(Uniconta.DataModel.GLTransType), typeof(Uniconta.DataModel.NumberSerie) };
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
        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.GLDailyJournalPage2)
                dgGldailyJournal.UpdateItemSource(argument);
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgGldailyJournal.SelectedItem as GLDailyJournalClient;
            switch (ActionType)
            {
                case "AddRow":
                    AddDockItem(TabControls.GLDailyJournalPage2, api, Uniconta.ClientTools.Localization.lookup("Posting"), ";component/Assets/img/Add_16x16.png");
                    break;
                case "EditRow":
                    if (selectedItem == null)
                        return;
                    AddDockItem(TabControls.GLDailyJournalPage2, selectedItem);
                    break;
                case "GLDailyJournalLine":
                    if (selectedItem == null)
                        return;
                    AddDockItem(TabControls.GL_DailyJournalLine, selectedItem);
                    break;
                case "GLDailyJournalPosted":
                    if (selectedItem == null)
                        return;
                    AddDockItem(TabControls.PostedJournals, selectedItem);
                    break;
                case "MatchVoucherToJournalLines":
                    if (selectedItem == null)
                        return;
                    AddDockItem(TabControls.MatchPhysicalVoucherToGLDailyJournalLines, selectedItem);
                    break;
                case "DeleteAllJournalLines":
                    if (selectedItem == null)
                        return;
                    var text = string.Format(Uniconta.ClientTools.Localization.lookup("JournalContainsLines"), selectedItem.Name);
                    EraseYearWindow EraseYearWindowDialog = new EraseYearWindow(text, false);
                    EraseYearWindowDialog.Closed += async delegate
                    {
                        if (EraseYearWindowDialog.DialogResult == true)
                        {
                            CloseDockItem(TabControls.GL_DailyJournalLine, selectedItem);
                            PostingAPI postApi = new PostingAPI(api);
                            var res = await postApi.DeleteJournalLines(selectedItem);
                            if (res != ErrorCodes.Succes)
                                UtilDisplay.ShowErrorCode(res);
                        }
                    };
                    EraseYearWindowDialog.Show();
                    break;
                case "ImportBankStatement":
                    if (selectedItem == null)
                        return;
                    AddDockItem(TabControls.ImportGLDailyJournal, selectedItem, string.Concat(string.Format(Uniconta.ClientTools.Localization.lookup("ImportOBJ"),
                            Uniconta.ClientTools.Localization.lookup("BankStatement")), " : ", selectedItem.Journal), null, true);
                    break;
                case "TransType":
                    AddDockItem(TabControls.GLTransTypePage, null, Uniconta.ClientTools.Localization.lookup("TransTypes"));
                    break;
                case "AppendTransType":
                    CWAppendEnumeratedLabel dialog = new CWAppendEnumeratedLabel(api);
                    dialog.Show();
                    break;
                case "ImportData":
                    if (selectedItem == null)
                        return;
                    OpenImportDataPage(selectedItem);
                    break;
                case "GLOffSetAccountTemplate":
                    AddDockItem(TabControls.GLOffsetAccountTemplate, null, Uniconta.ClientTools.Localization.lookup("OffsetAccountTemplates"));
                    break;
                case "AccountActivity":
                    if (selectedItem == null) return;

                    AddDockItem(TabControls.GLTransLogPage, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("GLTransLog"), selectedItem.Journal));
                    break;

                case "BilagscanReadVouchers":
                    ReadFromBilagscan(selectedItem);
                    break;

                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        void OpenImportDataPage(GLDailyJournalClient selectedItem)
        {
            var dailyJournals = new GLDailyJournalLineClient();
            string header = selectedItem.Journal;
            UnicontaBaseEntity[] baseEntityArray = new UnicontaBaseEntity[2] { dailyJournals, selectedItem };
            object[] param = new object[2];
            param[0] = baseEntityArray;
            param[1] = header;
            AddDockItem(TabControls.ImportPage, param, string.Format("{0} : {1}", Uniconta.ClientTools.Localization.lookup("Import"), header));
        }

        private bool readingFromBilagscan = false;
        private async void ReadFromBilagscan(UnicontaBaseEntity selectedItem)
        {
#if !SILVERLIGHT
            if (!readingFromBilagscan)
            {
                readingFromBilagscan = true;
                bool processLines = false;

                var accessToken = await Bilagscan.Account.GetBilagscanAccessToken(api);

                // Check if table exists. Will be replaced when real table is created
                //if (!api.CompanyEntity.UserTables.Exists(p => p._Name == "PlugInParameter"))
                //{
                //    Tools.CreateUserTable(api);
                //    UnicontaMessageBox.Show("Opsætning af integration er i gang. Afslut Uniconta og log ind igen.", "Opstart af Bilagscan");
                //    return;
                //}

                // Databehandleraftale?

                // Verify organization
                switch (await Bilagscan.Account.VerifyOrganization(api, accessToken))
                {
                    case "NOCOMPANY":
                        UnicontaMessageBox.Show("Plug-in'en kan ikke finde et firma.", "Overfør til Bilagscan");
                        break;
                    case "NOTABLE":
                        //Tools.CreateUserTable(api);
                        await Bilagscan.Account.CreateOrganization(api, accessToken);
                        break;
                    case "NODATA":
                        await Bilagscan.Account.CreateOrganization(api, accessToken);
                        break;
                    case "NOORGANIZATION":
                        UnicontaMessageBox.Show("Plug-in'en kan ikke finde din organisation hos Bilagscan.", "Overfør til Bilagscan");
                        break;
                    default:
                        Console.WriteLine("Default case");
                        break;
                }

                var n = 0;
                var journal = dgGldailyJournal.SelectedItem as GLDailyJournalClient;

                UnicontaBaseEntity[] baseEntityArray = new UnicontaBaseEntity[1] { selectedItem };
                var companySettings = await api.Query<CompanySettingsClient>();

                var orgNo = companySettings.FirstOrDefault()._OrgNumber;

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                    var response = await client.GetAsync($"https://api.bilagscan.dk/v1/organizations/" + orgNo.ToString() + "/vouchers?seen=false&count=100&offset=0&sorts=-upload_date&status=successful");
                    var content = await response.Content.ReadAsStringAsync();
                    var vouchers = JsonConvert.DeserializeObject<BilagscanVoucher.Processed>(content);

                    var credCache = api.CompanyEntity.GetCache(typeof(Uniconta.DataModel.Creditor)) ?? await api.CompanyEntity.LoadCache(typeof(Uniconta.DataModel.Creditor), api);
                    var offsetCache = api.CompanyEntity.GetCache(typeof(Uniconta.DataModel.GLAccount)) ?? await api.CompanyEntity.LoadCache(typeof(Uniconta.DataModel.GLAccount), api);
                    var vouchersSeen = new CommaDelimitedStringCollection();

                    var filter = new List<PropValuePair>();

                    var master = new List<UnicontaBaseEntity>
                    {
                        journal
                    };

                    var newLines = new List<UnicontaBaseEntity>();

                    foreach (var voucher in vouchers.data)
                    {
                        vouchersSeen.Add(voucher.id.ToString());

                        var journalLine = new GLDailyJournalLineClient()
                        {
                            Approved = false,
                            ForceSettlement = false
                        };

                        double lineTotalExclVat = 0;
                        double lineTotalInclVat = 0;
                        string creditorNo = "";
                        string creditorName = "";
                        CountryCode countryCode = CountryCode.Denmark;
                        var postingType = BilagscanVoucherType.Invoice;
                        var paymentCodeId = string.Empty;
                        var paymentMethod = PaymentMethods.FIK;
                        var ibanNo = "";

                        var hint = JsonConvert.DeserializeObject<BilagscanWrite.Hint>(voucher.note);

                        foreach (var header in voucher.header_fields)
                        {
                            if (header != null && header.value != null)
                            {
                                switch (header.code.ToLower())
                                {
                                    case "voucher_type":
                                        switch (header.value)
                                        {
                                            case "invoice":
                                                postingType = BilagscanVoucherType.Invoice;
                                                break;
                                            case "creditnote":
                                                postingType = BilagscanVoucherType.Creditnote;
                                                break;
                                            case "receipt":
                                                postingType = BilagscanVoucherType.Receipt;
                                                break;
                                        }
                                        break;
                                    case "country":
                                        switch (header.value)
                                        {
                                            case "DK":
                                            default:
                                                countryCode = CountryCode.Denmark;
                                                break;
                                        }
                                        break;
                                    case "payment_code_id":
                                        if (header.value == "71" || header.value == "73" || header.value == "75" || header.value == "04")
                                        {
                                            paymentCodeId = header.value;
                                        }
                                        break;
                                }
                            }
                        }

                        if (countryCode == CountryCode.Denmark && !string.IsNullOrEmpty(paymentCodeId))
                            paymentMethod = PaymentMethods.FIK;
                        else
                            paymentMethod = PaymentMethods.IBAN;

                        var paymentId = string.Empty;

                        foreach (var header in voucher.header_fields)
                        {
                            if (header != null && header.value != null)
                            {
                                switch (header.code.ToLower())
                                {
                                    case "catalog_creditor_id":
                                        break;
                                    case "company_vat_reg_no":
                                        if (api.CompanyEntity._CountryId == CountryCode.Iceland)
                                            creditorNo = Regex.Replace(header.value, @"\D", "");
                                        else
                                            creditorNo = header.value;

                                        journalLine.Account = creditorNo;
                                        break;
                                    case "payment_code_id":
                                        paymentCodeId = header.value;
                                        break;
                                    case "total_amount_excl_vat":
                                        if (header.value != "")
                                            lineTotalExclVat = -NumberConvert.ToDoubleNoThousandSeperator(header.value);
                                        break;
                                    case "total_amount_incl_vat":
                                        if (header.value != "")
                                        {
                                            lineTotalInclVat = -NumberConvert.ToDoubleNoThousandSeperator(header.value);
                                            journalLine.Amount = lineTotalInclVat;
                                        }
                                        break;
                                    case "voucher_number":
                                        Int64 voucherNumber;
                                        if (Int64.TryParse(Regex.Replace(header.value, @"\D", ""), out voucherNumber))
                                        {
                                            journalLine.Invoice = Convert.ToInt64(Regex.Replace(header.value, @"\D", ""));
                                        }
                                        break;
                                    case "order_number":
                                        break;
                                    case "catalog_debitor_id":
                                        break;
                                    case "payment_date":
                                        string dd = header.value;
                                        journalLine.DueDate = (dd == "" ? DateTime.Today : StringSplit.DateParse(dd, DateFormat.ymd));
                                        break;
                                    case "payment_id":
                                        paymentId = header.value;
                                        break;
                                    case "country":
                                        switch (header.value)
                                        {
                                            case "DK":
                                            default:
                                                countryCode = CountryCode.Denmark;
                                                break;
                                        }
                                        break;
                                    case "company_name":
                                        creditorName = header.value;
                                        break;
                                    case "total_vat_amount_scanned":
                                        break;
                                    case "creditor_number":
                                        break;
                                    case "invoice_date":
                                        string dt = header.value;
                                        journalLine.Date = (dt == "" ? DateTime.Today : StringSplit.DateParse(dt, DateFormat.ymd));
                                        break;
                                    case "danish_industry_code":
                                        break;
                                    case "voucher_type":
                                        break;
                                    case "joint_payment_id":
                                        break;
                                    case "qpr_name":
                                        break;
                                    case "currency":
                                        journalLine.Currency = header.value;
                                        break;
                                    case "qpr_number":
                                        break;
                                    case "payment_iban":
                                        ibanNo = header.value;
                                        break;
                                }
                            }
                        }

                        if (paymentMethod == PaymentMethods.FIK)
                        {
                            switch (paymentCodeId)
                            {
                                case "71":
                                    journalLine._PaymentMethod = PaymentTypes.PaymentMethod3;
                                    break;
                                case "73":
                                    journalLine._PaymentMethod = PaymentTypes.PaymentMethod4;
                                    break;
                                case "75":
                                    journalLine._PaymentMethod = PaymentTypes.PaymentMethod5;
                                    break;
                                case "04":
                                    journalLine._PaymentMethod = PaymentTypes.PaymentMethod6;
                                    break;
                                default:
                                    journalLine._PaymentMethod = PaymentTypes.VendorBankAccount;
                                    break;
                            }

                            if (!string.IsNullOrEmpty(paymentId))
                            {
                                var paymentId1 = paymentId.Substring(0, paymentId.Length - 8);
                                var paymentId2 = paymentId.Substring(paymentId.Length - 8);

                                journalLine._PaymentId = string.Format("{0}+{1}", paymentId1, paymentId2);
                            }
                        }
                        else
                        {
                            journalLine._PaymentMethod = PaymentTypes.IBAN;
                            journalLine._PaymentId = ibanNo;
                        }

                        if (hint != null)
                        {
                            journalLine._AccountType = 2; // Kreditor
                            journalLine.Account = creditorNo;
                            if (hint.CreditorAccount != null)
                                journalLine.Account = hint.CreditorAccount;
                            if (hint.Amount != 0)
                                journalLine.Amount = hint.Amount;
                            journalLine.Approved = hint.Approved;
                            if (hint.Currency != null)
                                journalLine.Currency = hint.Currency;
                            if (hint.DocumentDate != DateTime.MinValue)
                                journalLine.Date = hint.DocumentDate;
                            journalLine.DCPostType = "";
                            if (hint.Dimension1 != null)
                                journalLine.Dimension1 = hint.Dimension1;
                            if (hint.Dimension2 != null)
                                journalLine.Dimension2 = hint.Dimension2;
                            if (hint.Dimension3 != null)
                                journalLine.Dimension3 = hint.Dimension3;
                            if (hint.Dimension4 != null)
                                journalLine.Dimension4 = hint.Dimension4;
                            if (hint.Dimension5 != null)
                                journalLine.Dimension5 = hint.Dimension5;
                            journalLine.DocumentRef = hint.RowId;
                            if (hint.DocumentDate != null)
                                journalLine.DocumentDate = hint.DocumentDate;
                            if (hint.DueDate != null)
                                journalLine.DueDate = hint.DueDate;
                            if (hint.PayAccount != null)
                                journalLine.Payment = hint.PayAccount;
                            if (hint.PaymentId != null)
                            {
                                journalLine.PaymentId = hint.PaymentId;
                                journalLine.PaymentMethod = hint.PaymentMethod;
                            }
                            if (hint.PrCategory != null)
                                journalLine.PrCategory = hint.PrCategory;
                            if (hint.Project != null)
                                journalLine.Project = hint.Project;
                            if (string.IsNullOrEmpty(journalLine.Text))
                                journalLine.Text = hint.Text;
                            if (hint.Voucher != 0)
                                journalLine.Voucher = hint.Voucher;
                        }

                        if (journalLine.Date == DateTime.MinValue)
                            journalLine.Date = DateTime.Today;

                        if (journalLine.DueDate == DateTime.MinValue)
                            journalLine.DueDate = journalLine.Date;

                        if (journalLine.DocumentDate == DateTime.MinValue)
                            journalLine.DocumentDate = journalLine.Date;

                        journalLine.SettleValue = "Voucher";

                        if ((journalLine.Amount == 0) && (lineTotalExclVat != 0))
                        {
                            journalLine.Amount = lineTotalExclVat;
                        }
                        journalLine.AmountCur = journalLine.Amount;

                        var creditors = credCache.GetRecords as Uniconta.DataModel.Creditor[];
                        Uniconta.DataModel.Creditor creditor = null;

                        foreach (var c in creditors)
                        {
                            if (c != null)
                            {
                                if (c._LegalIdent == journalLine.Account)
                                {
                                    creditor = c;
                                    break;
                                }
                            }
                        }

                        //var creditor = creditors.Where(cr => cr._LegalIdent == journalLine.Account).FirstOrDefault();

                        if (creditor == null && !string.IsNullOrEmpty(journalLine.Account))
                        {
                            foreach (var c in creditors)
                            {
                                if (c != null)
                                {
                                    if (c._LegalIdent == journalLine.Account.Remove(0, 2))
                                    {
                                        creditor = c;
                                        creditorNo = creditorNo.Remove(0, 2);
                                        journalLine.Account = journalLine.Account.Remove(0, 2);
                                        break;
                                    }
                                }
                            }
                            //creditor = creditors.Where(cr => cr._LegalIdent == journalLine.Account.Remove(0, 2)).FirstOrDefault();
                            //creditorNo = creditorNo.Remove(0, 2);
                            //journalLine.Account = journalLine.Account.Remove(0, 2);
                        }

                        if (creditor == null)
                        {
                            var newCreditor = new CreditorClient()
                            {
                                Account = creditorNo,
                                CompanyRegNo = creditorNo,
                                Name = creditorName,
                                _PaymentMethod = journalLine._PaymentMethod
                            };

                            CompanyInfo companyInformation = null;
                            try
                            {
                                companyInformation = await CVR.CheckCountry(creditorNo, countryCode);
                            }
                            catch (Exception ex)
                            {
                                UnicontaMessageBox.Show(ex.Message, Uniconta.ClientTools.Localization.lookup("Exception"), MessageBoxButton.OK);
                                return;
                            }
                            if (companyInformation != null)
                            {
                                if (string.IsNullOrEmpty(creditorName))
                                    newCreditor.Name = companyInformation.life.name;

                                if (companyInformation.address != null)
                                {
                                    newCreditor.Address1 = companyInformation.address.CompleteStreet;
                                    newCreditor.ZipCode = companyInformation.address.zipcode;
                                    newCreditor.City = companyInformation.address.cityname;
                                    newCreditor.Country = companyInformation.address.Country;
                                }

                                if (companyInformation.contact != null)
                                {
                                    newCreditor.Phone = companyInformation.contact.phone;
                                    newCreditor.ContactEmail = companyInformation.contact.email;
                                    newCreditor.Www = companyInformation.contact.www;
                                }

                                switch (postingType)
                                {
                                    case BilagscanVoucherType.Invoice:
                                        journalLine.Text = Uniconta.ClientTools.Localization.lookup("Invoice") + ": " + newCreditor.Name;
                                        break;
                                    case BilagscanVoucherType.Creditnote:
                                        journalLine.Text = Uniconta.ClientTools.Localization.lookup("Creditnote") + ": " + newCreditor.Name;
                                        break;
                                    case BilagscanVoucherType.Receipt:
                                        journalLine.Text = Uniconta.ClientTools.Localization.lookup("Receipt") + ": " + newCreditor.Name;
                                        break;
                                }
                            }

                            await api.Insert(newCreditor);
                            journalLine.Account = creditorNo;
                        }
                        else
                        {
                            if (string.IsNullOrEmpty(creditor._PostingAccount))
                            {
                                journalLine.Vat = creditor._Vat;
                            }
                            else
                            {
                                var account = (GLAccountClient)offsetCache.Get(creditor._PostingAccount);

                                if (!string.IsNullOrEmpty(account.Vat))
                                {
                                    var dailyJournal = (GLDailyJournalClient)master[0];
                                    if (dailyJournal.TwoVatCodes)
                                        journalLine._OffsetVat = account.Vat;
                                    else
                                        journalLine._Vat = account.Vat;
                                }

                                journalLine._OffsetAccount = creditor._PostingAccount;
                            }
                            if (creditor._Payment != string.Empty)
                            {
                                var paymentTermsCache = api.CompanyEntity.GetCache(typeof(PaymentTerm)) ?? await api.CompanyEntity.LoadCache(typeof(PaymentTerm), api);
                                var paymentTerm = (PaymentTerm)paymentTermsCache.Get(creditor._Payment);

                                if (paymentTerm != null)
                                    journalLine._DueDate = paymentTerm.GetDueDate(journalLine.DueDate);
                            }

                            switch (postingType)
                            {
                                case BilagscanVoucherType.Invoice:
                                    journalLine.Text = Uniconta.ClientTools.Localization.lookup("Invoice") + ": " + creditor._Name;
                                    break;
                                case BilagscanVoucherType.Creditnote:
                                    journalLine.Text = Uniconta.ClientTools.Localization.lookup("Creditnote") + ": " + creditor._Name;
                                    break;
                                case BilagscanVoucherType.Receipt:
                                    journalLine.Text = Uniconta.ClientTools.Localization.lookup("Receipt") + ": " + creditor._Name;
                                    break;
                            }
                        }

                        journalLine.SetMaster(master[0]);
                        newLines.Add(journalLine);

                        if (processLines)
                        {
                            double lineamountexVat = 0;

                            foreach (var lineItem in voucher.line_items)
                            {
                                var journalDetails = new GLDailyJournalLineClient()
                                {
                                    Approved = false,
                                    ForceSettlement = false
                                };

                                foreach (var field in lineItem.fields)
                                {
                                    switch (field.code.ToLower())
                                    {
                                        case "quantity":
                                            break;
                                        case "description":
                                            journalDetails.Text = field.value;
                                            break;
                                        case "amount":
                                            break;
                                        case "incl_vat_amount":
                                            journalDetails.Amount = Convert.ToDouble(field.value.Replace('.', ','));
                                            break;
                                        case "ex_vat_amount":
                                            lineamountexVat = Convert.ToDouble(field.value.Replace('.', ','));
                                            break;
                                    }
                                }

                                if (hint != null)
                                {
                                    journalDetails._AccountType = 2; // kerditor
                                    journalDetails.Approved = hint.Approved;
                                    if (journalDetails.Currency == null)
                                        journalDetails.Currency = hint.Currency;
                                    if (hint.DocumentDate != DateTime.MinValue)
                                        journalDetails.Date = hint.DocumentDate;
                                    journalDetails.DCPostType = "";
                                    if (hint.Dimension1 != null)
                                        journalDetails.Dimension1 = hint.Dimension1;
                                    if (hint.Dimension2 != null)
                                        journalDetails.Dimension2 = hint.Dimension2;
                                    if (hint.Dimension3 != null)
                                        journalDetails.Dimension3 = hint.Dimension3;
                                    if (hint.Dimension4 != null)
                                        journalDetails.Dimension4 = hint.Dimension4;
                                    if (hint.Dimension5 != null)
                                        journalDetails.Dimension5 = hint.Dimension5;
                                    journalDetails.DocumentRef = hint.RowId;
                                    if (hint.DocumentDate != null)
                                        journalDetails.DocumentDate = hint.DocumentDate;
                                    if (hint.DueDate != null)
                                        journalDetails.DueDate = hint.DueDate;
                                    if (hint.PayAccount != null)
                                        journalDetails.Payment = hint.PayAccount;
                                    if (hint.PaymentId != null)
                                        journalDetails.PaymentId = hint.PaymentId;
                                    if (hint.PaymentMethod != null)
                                        journalDetails.PaymentMethod = hint.PaymentMethod;
                                    if (hint.PrCategory != null)
                                        journalDetails.PrCategory = hint.PrCategory;
                                    if (hint.Project != null)
                                        journalDetails.Project = hint.Project;
                                    if (string.IsNullOrEmpty(journalDetails.Text))
                                        journalDetails.Text = hint.Text;
                                    if (hint.Voucher != 0)
                                        journalDetails.Voucher = hint.Voucher;
                                }
                                journalDetails.AmountCur = journalDetails.Amount;

                                if (creditor != null)
                                {
                                    if (creditor._PostingAccount != null)
                                    {
                                        var offsetAccount = (GLAccountClient)offsetCache.Get(creditor._PostingAccount.ToString());
                                    }
                                }
                                journalDetails.SetMaster(master[0]);
                                newLines.Add(journalDetails);
                            }
                        }
                        n += 1;
                    }

                    var errorCode = await api.Insert(newLines);

                    if (vouchersSeen.Count != 0)
                    {
                        // Mark voucher as seen
                        string serializedRequest = "{ \"vouchers\": [ " + vouchersSeen.ToString() + " ] }";
                        var vContent = new StringContent(serializedRequest, Encoding.UTF8, "application/json");
                        response = await client.PostAsync($"https://api.bilagscan.dk/v1/organizations/" + orgNo.ToString() + "/vouchers/seen", vContent);
                        var res = await response.Content.ReadAsStringAsync();
                    }
                }

                if (n == 0)
                    UnicontaMessageBox.Show(string.Format("{0} {1}", Uniconta.ClientTools.Localization.lookup("StillProcessingTryAgain"), Uniconta.ClientTools.Localization.lookup("Vouchers")), Uniconta.ClientTools.Localization.lookup("BilagscanGet"), MessageBoxButton.OK, MessageBoxImage.Information);
                else
                {
                    var messageText = string.Concat(string.Format("{0} {1}", Uniconta.ClientTools.Localization.lookup("NumberOfImportedVouchers"), n),
                    Environment.NewLine, Environment.NewLine,
                    string.Format(Uniconta.ClientTools.Localization.lookup("GoTo"), Uniconta.ClientTools.Localization.lookup("Journallines")), "?");
                    if (UnicontaMessageBox.Show(messageText, Uniconta.ClientTools.Localization.lookup("BilagscanRead"), MessageBoxButton.OKCancel, MessageBoxImage.Information) == MessageBoxResult.OK)
                    {
                        AddDockItem(TabControls.GL_DailyJournalLine, journal, null, null, true);
                    }
                }
                readingFromBilagscan = false;
            }
#endif
        }
    }
    
    internal enum BilagscanVoucherType
    {
        Invoice,
        Creditnote,
        Receipt
    }

    internal enum PaymentMethods
    {
        FIK,
        IBAN
    }
}
