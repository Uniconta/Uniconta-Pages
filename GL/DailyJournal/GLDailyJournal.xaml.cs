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
using Localization = Uniconta.ClientTools.Localization;
#if !SILVERLIGHT
using Bilagscan;
using System.Net.Http;
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
           // dgGldailyJournal.PreviewMouseDown += DgGldailyJournal_PreviewMouseDown;
            RemoveMenuItem();
        }

        //private void DgGldailyJournal_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        //{
        //    if (dgGldailyJournal.CurrentColumn == dgGldailyJournal.Columns["Name"])
        //    {
        //        var selectedItem = dgGldailyJournal.SelectedItem as GLDailyJournalClient;
        //        if (selectedItem != null)
        //            AddDockItem(TabControls.GL_DailyJournalLine, selectedItem);
        //    }
        //}

        void RemoveMenuItem()
        {
            var Comp = api.CompanyEntity;
            var rb = (RibbonBase)localMenu.DataContext;
            if (Comp._CountryId != CountryCode.Denmark && Comp._CountryId != CountryCode.Iceland && Comp._CountryId != CountryCode.Netherlands && Comp._CountryId != CountryCode.Austria)
                UtilDisplay.RemoveMenuCommand(rb, "BilagscanReadVouchers");
            if (Comp._CountryId != CountryCode.Iceland)
                UtilDisplay.RemoveMenuCommand(rb, "Iceland_PSPSettlements");
        }

        void dgGldailyJournal_RowDoubleClick()
        {
            var selectedItem = dgGldailyJournal.SelectedItem as GLDailyJournalClient;
            if (selectedItem != null)
                AddDockItem(TabControls.GL_DailyJournalLine, selectedItem);
        }

        protected override void LoadCacheInBackGround()
        {
            var lst = new List<Type>(15) { typeof(Uniconta.DataModel.GLAccount), typeof(Uniconta.DataModel.GLVat), typeof(Uniconta.DataModel.Debtor), typeof(Uniconta.DataModel.Creditor), typeof(Uniconta.DataModel.GLTransType), typeof(Uniconta.DataModel.NumberSerie), typeof(Uniconta.DataModel.PaymentTerm), typeof(Uniconta.DataModel.GLTransType), typeof(Uniconta.DataModel.GLChargeGroup), typeof(Uniconta.DataModel.PaymentTerm) };
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
            if (api.CompanyEntity.Project)
            {
                lst.Add(typeof(Uniconta.DataModel.PrCategory));
                lst.Add(typeof(Uniconta.DataModel.PrWorkSpace));
                lst.Add(typeof(Uniconta.DataModel.Project));
            }
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
                    AddDockItem(TabControls.GLDailyJournalPage2, api, Localization.lookup("Posting"), "Add_16x16");
                    break;
                case "EditRow":
                    if (selectedItem != null)
                        AddDockItem(TabControls.GLDailyJournalPage2, selectedItem);
                    break;
                case "GLDailyJournalLine":
                    if (selectedItem != null)
                        AddDockItem(TabControls.GL_DailyJournalLine, selectedItem);
                    break;
                case "GLDailyJournalPosted":
                    if (selectedItem != null)
                        AddDockItem(TabControls.PostedJournals, selectedItem);
                    break;
                case "MatchVoucherToJournalLines":
                    if (selectedItem != null)
                        AddDockItem(TabControls.MatchPhysicalVoucherToGLDailyJournalLines, selectedItem);
                    break;
                case "DeleteAllJournalLines":
                    if (selectedItem == null)
                        return;
                    var text = string.Format(Localization.lookup("JournalContainsLines"), selectedItem._Name);
                    EraseYearWindow EraseYearWindowDialog = new EraseYearWindow(text, false);
                    EraseYearWindowDialog.Closed += async delegate
                    {
                        if (EraseYearWindowDialog.DialogResult == true)
                        {
                            CloseDockItem(TabControls.GL_DailyJournalLine, selectedItem);
                            PostingAPI postApi = new PostingAPI(api);
                            var res = await postApi.DeleteJournalLines(selectedItem);
                            UtilDisplay.ShowErrorCode(res);
                            selectedItem.NumberOfLines = 0;
                        }
                    };
                    EraseYearWindowDialog.Show();
                    break;
                case "ImportBankStatement":
                    if (selectedItem != null)
                        AddDockItem(TabControls.ImportGLDailyJournal, selectedItem, string.Concat(string.Format(Localization.lookup("ImportOBJ"),
                            Localization.lookup("BankStatement")), " : ", selectedItem._Journal), null, true);
                    break;
                case "TransType":
                    AddDockItem(TabControls.GLTransTypePage, null, Localization.lookup("TransTypes"));
                    break;
                case "AppendTransType":
                    CWAppendEnumeratedLabel dialog = new CWAppendEnumeratedLabel(api);
                    dialog.Show();
                    break;
                case "ImportData":
                    if (selectedItem != null)
                        OpenImportDataPage(selectedItem);
                    break;
                case "GLOffSetAccountTemplate":
                    AddDockItem(TabControls.GLOffsetAccountTemplate, null, Localization.lookup("OffsetAccountTemplates"));
                    break;
                case "AccountActivity":
                    if (selectedItem != null)
                        AddDockItem(TabControls.GLTransLogPage, selectedItem, string.Format("{0}: {1}", Localization.lookup("GLTransLog"), selectedItem._Journal));
                    break;
                case "BilagscanReadVouchers":
                    ReadFromBilagscan(selectedItem);
                    break;
                case "MoveJournalLines":
                    if (selectedItem != null)
                        MoveJournalLines(selectedItem);
                    break;
                case "Iceland_PSPSettlements":
                    if (selectedItem != null)
                        AddDockItem(TabControls.Iceland_ImportPSPSettlements, selectedItem, string.Concat("Sækja uppgjör færsluhirðis", " : ", selectedItem._Journal), null, true);
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        void MoveJournalLines(GLDailyJournalClient journal)
        {
            CwMoveJournalLines cwWin = new CwMoveJournalLines(api);
            cwWin.Closed += async delegate
            {
                if (cwWin.DialogResult == true && cwWin.GLDailyJournal != null)
                {
                    busyIndicator.IsBusy = true;
                    var result = await (new Uniconta.API.GeneralLedger.PostingAPI(api)).MoveJournalLines(journal, cwWin.GLDailyJournal);
                    busyIndicator.IsBusy = false;
                    UtilDisplay.ShowErrorCode(result);
                    if (result == 0)
                    {
                        foreach (var lin in (IEnumerable<GLDailyJournalClient>)dgGldailyJournal.ItemsSource)
                            if (lin.RowId == cwWin.GLDailyJournal.RowId)
                            {
                                lin.NumberOfLines = lin._NumberOfLines + journal._NumberOfLines;
                                journal.NumberOfLines = 0;
                                break;
                            }
                    }
                }
            };
            cwWin.Show();
        }

        void OpenImportDataPage(GLDailyJournalClient selectedItem)
        {
            var header = selectedItem._Journal;            
            var baseEntityArray = new UnicontaBaseEntity[2] { api.CompanyEntity.CreateUserType<GLDailyJournalLineClient>(), selectedItem };
            AddDockItem(TabControls.ImportPage, new object[] { baseEntityArray, header }, string.Format("{0}: {1}", Localization.lookup("Import"), header));
        }

        bool readingFromBilagscan;
        void ReadFromBilagscan(UnicontaBaseEntity selectedItem)
        {
            if (readingFromBilagscan)
                UnicontaMessageBox.Show(Localization.lookup("UpdateInBackground"), Localization.lookup("Information"), MessageBoxButton.OK);
            else
                _ReadFromBilagscan(selectedItem);
        }
        async void _ReadFromBilagscan(UnicontaBaseEntity selectedItem)
        {
            var journal = dgGldailyJournal.SelectedItem as GLDailyJournalClient;
            var noOfVouchers = 0;
            try
            {
                readingFromBilagscan = true;

                busyIndicator.IsBusy = true;
                CompanySettingsClient companySettings = new CompanySettingsClient();
                await api.Read(companySettings);
                var orgNo = NumberConvert.ToStringNull(companySettings._OrgNumber);
                if (orgNo == null)
                {
                    busyIndicator.IsBusy = false;
                    UnicontaMessageBox.Show(string.Format(Localization.lookup("CannotBeBlank"), Localization.lookup("OrgNumber")), Localization.lookup("Paperflow"), MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }
                var accessToken = Bilagscan.Account.GetBilagscanAccessToken(api);

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                    var response = await client.GetAsync($"https://api.bilagscan.dk/v1/organizations/" + orgNo + "/vouchers?seen=false&count=100&offset=0&sorts=-upload_date&status=successful");
                    var content = await response.Content.ReadAsStringAsync();
                    var vouchers = Bilagscan.Voucher.GetVouchers(content);

                    var credCache = api.CompanyEntity.GetCache(typeof(Uniconta.DataModel.Creditor)) ?? await api.CompanyEntity.LoadCache(typeof(Uniconta.DataModel.Creditor), api);
                    var offsetCache = api.CompanyEntity.GetCache(typeof(Uniconta.DataModel.GLAccount)) ?? await api.CompanyEntity.LoadCache(typeof(Uniconta.DataModel.GLAccount), api);

                    if (vouchers?.data != null && vouchers.data.Length > 0)
                    {
                        var creditors = credCache.GetKeyStrRecords as Uniconta.DataModel.Creditor[];

                        var updateLines = new List<GLDailyJournalLineClient>(vouchers.data.Length);
                        List<Uniconta.DataModel.Creditor> CreList = null;

                        foreach (var voucher in vouchers.data)
                        {
                            var journalLine = new GLDailyJournalLineClient();
                            journalLine.SetMaster(journal);

                            var postingType = BilagscanVoucherType.Invoice;
                            var hint = Bilagscan.Voucher.GetHint(voucher.note);

                            var bilagscanRefID = voucher.id;
                            journalLine._ReferenceNumber = bilagscanRefID != 0 ? NumberConvert.ToString(bilagscanRefID) : null;

                            var bsItem = voucher.header_fields.Where(hf => string.Compare(hf.code, "voucher_number", StringComparison.OrdinalIgnoreCase) == 0).FirstOrDefault();
                            if (bsItem != null)
                                journalLine._Invoice = bsItem.value;

                            bsItem = voucher.header_fields.Where(hf => string.Compare(hf.code, "voucher_type", StringComparison.OrdinalIgnoreCase) == 0).FirstOrDefault();
                            if (bsItem != null)
                            {
                                switch (bsItem.value)
                                {
                                    case "invoice": postingType = BilagscanVoucherType.Invoice; break;
                                    case "creditnote": postingType = BilagscanVoucherType.Creditnote; break;
                                    case "receipt": postingType = BilagscanVoucherType.Receipt; break;
                                }
                            }

                            bsItem = voucher.header_fields.Where(hf => string.Compare(hf.code, "company_vat_reg_no", StringComparison.OrdinalIgnoreCase) == 0).FirstOrDefault();
                            var creditorCVR = bsItem?.value ?? string.Empty;

                            bsItem = voucher.header_fields.Where(hf => string.Compare(hf.code, "total_amount_incl_vat", StringComparison.OrdinalIgnoreCase) == 0).FirstOrDefault();
                            if (bsItem != null)
                            {
                                journalLine.Amount = Math.Abs(NumberConvert.ToDoubleNoThousandSeperator(bsItem.value));

                                if (postingType != BilagscanVoucherType.Creditnote)
                                    journalLine.Amount = -journalLine.Amount;
                            }

                            CountryCode countryCode = CountryCode.Denmark;
                            bsItem = voucher.header_fields.Where(hf => string.Compare(hf.code, "country", StringComparison.OrdinalIgnoreCase) == 0).FirstOrDefault();
                            if (bsItem != null)
                            {
                                CountryISOCode countryISO;
                                countryCode = CountryCode.Denmark; //default
                                if (Enum.TryParse(bsItem.value, true, out countryISO))
                                    countryCode = (CountryCode)countryISO;
                            }

                            bsItem = voucher.header_fields.Where(hf => string.Compare(hf.code, "invoice_date", StringComparison.OrdinalIgnoreCase) == 0).FirstOrDefault();
                            if (bsItem != null)
                            {
                                var invoiceDate = bsItem.value == string.Empty ? DateTime.MinValue : StringSplit.DateParse(bsItem.value, DateFormat.ymd);
                                journalLine._Date = invoiceDate != DateTime.MinValue ? invoiceDate : GetSystemDefaultDate();
                            }

                            bsItem = voucher.header_fields.Where(hf => string.Compare(hf.code, "payment_date", StringComparison.OrdinalIgnoreCase) == 0).FirstOrDefault();
                            if (bsItem != null)
                                journalLine._DueDate = bsItem.value == string.Empty ? DateTime.MinValue : StringSplit.DateParse(bsItem.value, DateFormat.ymd);

                            bsItem = voucher.header_fields.Where(hf => string.Compare(hf.code, "currency", StringComparison.OrdinalIgnoreCase) == 0).FirstOrDefault();
                            if (bsItem != null)
                            {
                                Currencies currencyISO;
                                if (!Enum.TryParse(bsItem.value, true, out currencyISO))
                                    currencyISO = Currencies.DKK; //default

                                journalLine._Currency = (byte)currencyISO;
                            }

                            bsItem = voucher.header_fields.Where(hf => string.Compare(hf.code, "payment_account_number", StringComparison.OrdinalIgnoreCase) == 0).FirstOrDefault();
                            var bbanAcc = bsItem?.value;

                            bsItem = voucher.header_fields.Where(hf => string.Compare(hf.code, "payment_reg_number", StringComparison.OrdinalIgnoreCase) == 0).FirstOrDefault();
                            var bbanRegNum = bsItem?.value;

                            bsItem = voucher.header_fields.Where(hf => string.Compare(hf.code, "payment_iban", StringComparison.OrdinalIgnoreCase) == 0).FirstOrDefault();
                            var ibanNo = bsItem?.value;

                            bsItem = voucher.header_fields.Where(hf => string.Compare(hf.code, "payment_swift_bic", StringComparison.OrdinalIgnoreCase) == 0).FirstOrDefault();
                            var swiftNo = bsItem?.value;

                            string paymentCodeId = null;
                            bsItem = voucher.header_fields.Where(hf => string.Compare(hf.code, "payment_code_id", StringComparison.OrdinalIgnoreCase) == 0).FirstOrDefault();
                            if (bsItem != null)
                                paymentCodeId = bsItem.value;

                            bsItem = voucher.header_fields.Where(hf => string.Compare(hf.code, "payment_id", StringComparison.OrdinalIgnoreCase) == 0).FirstOrDefault();
                            var paymentId = bsItem?.value;

                            bsItem = voucher.header_fields.Where(hf => string.Compare(hf.code, "joint_payment_id", StringComparison.OrdinalIgnoreCase) == 0).FirstOrDefault();
                            var jointPaymentId = bsItem?.value;

                            var paymentMethod = PaymentTypes.VendorBankAccount;
                            switch (paymentCodeId)
                            {
                                case "71": paymentMethod = PaymentTypes.PaymentMethod3; break;
                                case "73": paymentMethod = PaymentTypes.PaymentMethod4; break;
                                case "75": paymentMethod = PaymentTypes.PaymentMethod5; break;
                                case "04":
                                case "4": paymentMethod = PaymentTypes.PaymentMethod6; break;
                            }

                            if (paymentMethod != PaymentTypes.VendorBankAccount && (paymentId != null || jointPaymentId != null))
                            {
                                journalLine._PaymentMethod = paymentMethod;
                                journalLine._PaymentId = string.Format("{0} +{1}", paymentId, jointPaymentId);
                            }
                            else if (bbanRegNum != null && bbanAcc != null)
                            {
                                journalLine._PaymentMethod = PaymentTypes.VendorBankAccount;
                                journalLine._PaymentId = string.Format("{0}-{1}", bbanRegNum, bbanAcc);
                            }
                            else if (swiftNo != null && ibanNo != null)
                            {
                                journalLine._PaymentMethod = PaymentTypes.IBAN;
                                journalLine._PaymentId = ibanNo;
                            }

                            journalLine._SettleValue = SettleValueType.Voucher;

                            Uniconta.DataModel.Creditor creditor = null;

                            if (hint != null)
                            {
                                journalLine._DocumentRef = hint.RowId;
                                //if (hint.CreditorAccount != null)
                                //    creditor = (Uniconta.DataModel.Creditor)credCache.Get(hint.CreditorAccount);
                                //if (hint.Amount != 0)
                                //    journalLine.Amount = hint.Amount;
                                //if (hint.Currency != null && hint.Currency != "-")
                                //    journalLine.Currency = hint.Currency;
                                //if (hint.PaymentId != null)
                                //{
                                //    journalLine._PaymentId = hint.PaymentId;
                                //    journalLine.PaymentMethod = hint.PaymentMethod;
                                //}
                            }

                            journalLine._AccountType = 2;

                            var creditorCVRNum = Regex.Replace(creditorCVR, "[^0-9]", string.Empty);
                            if (creditorCVRNum != string.Empty)
                                creditor = creditors.Where(s => (Regex.Replace(s._LegalIdent ?? string.Empty, "[^0-9.]", "") == creditorCVRNum)).FirstOrDefault();

                            if (creditorCVRNum == string.Empty)
                            {
                                journalLine._Text = Localization.lookup("NotValidVatNo");
                            }
                            else if (creditor == null)
                            {
                                var newCreditor = new CreditorClient()
                                {
                                    _Account = creditorCVR,
                                    _LegalIdent = creditorCVR,
                                    _PaymentMethod = journalLine._PaymentMethod,
                                    _PaymentId = journalLine._PaymentId,
                                    _SWIFT = swiftNo
                                };

                                CompanyInfo companyInformation = null;
                                try
                                {
                                    companyInformation = await CVR.CheckCountry(creditorCVR, countryCode);
                                }
                                catch { }

                                if (companyInformation != null)
                                {
                                    if (companyInformation.life != null)
                                        newCreditor._Name = companyInformation.life.name;

                                    if (companyInformation.address != null)
                                    {
                                        newCreditor._Address1 = companyInformation.address.CompleteStreet;                                      
                                        newCreditor._Address2 = companyInformation.address.street2;                                        
                                        newCreditor._ZipCode = companyInformation.address.zipcode;
                                        newCreditor._City = companyInformation.address.cityname;
                                        newCreditor._Country = companyInformation.address.Country;
                                    }

                                    if (companyInformation.contact != null)
                                    {
                                        newCreditor._Phone = companyInformation.contact.phone;
                                        newCreditor._ContactEmail = companyInformation.contact.email;
                                    }

                                    journalLine.Text = newCreditor.Name;
                                }
                                else
                                {
                                    newCreditor.Name = Localization.lookup("NotValidVatNo");
                                }

                                await api.Insert(newCreditor);
                                journalLine._Account = creditorCVR;
                            }
                            else
                            {
                                string vat;
                                if (creditor._PostingAccount != null)
                                {
                                    journalLine._OffsetAccount = creditor._PostingAccount;
                                    vat = ((GLAccount)offsetCache.Get(creditor._PostingAccount))?._Vat;
                                }
                                else
                                    vat = creditor._Vat;
                                if (vat != null)
                                {
                                    if (journal._TwoVatCodes)
                                        journalLine._OffsetVat = vat;
                                    else
                                        journalLine._Vat = vat;
                                }

                                if (journalLine._DueDate == DateTime.MinValue && creditor._Payment != string.Empty)
                                {
                                    var paymentTermsCache = api.GetCache(typeof(PaymentTerm)) ?? await api.LoadCache(typeof(PaymentTerm));
                                    var paymentTerm = (PaymentTerm)paymentTermsCache.Get(creditor._Payment);

                                    if (paymentTerm != null)
                                        journalLine._DueDate = paymentTerm.GetDueDate(journalLine._DueDate);
                                }

                                journalLine._Account = creditor._Account;
                                if (creditor._SWIFT == null && !string.IsNullOrEmpty(swiftNo))
                                {
                                    creditor._SWIFT = swiftNo;
                                    if (CreList == null)
                                        CreList = new List<Uniconta.DataModel.Creditor>();
                                    CreList.Add(creditor);
                                }
                            }

                            updateLines.Add(journalLine);
                        }

                        noOfVouchers = updateLines.Count;
                        var errorCode = await api.Insert(updateLines);
                        if (CreList != null)
                            api.UpdateNoResponse(CreList);

                        var sb = StringBuilderReuse.Create("{ \"vouchers\": [ ");
                        foreach (var voucher in vouchers.data)
                            sb.AppendNum(voucher.id).Append(',');
                        sb.Length = sb.Length - 1; // remove last comma
                        sb.Append(" ] }");
                        var vContent = new StringContent(sb.ToStringAndRelease(), Encoding.UTF8, "application/json");
                        response = await client.PostAsync($"https://api.bilagscan.dk/v1/organizations/" + orgNo + "/vouchers/seen", vContent);
                        await response.Content.ReadAsStringAsync();
                    }
                }
            }
            finally
            {
                readingFromBilagscan = false;
                busyIndicator.IsBusy = false;
            }

            if (noOfVouchers == 0)
                UnicontaMessageBox.Show(string.Format(Localization.lookup("StillProcessingTryAgain"), Localization.lookup("Bilagscan")), Localization.lookup("Bilagscan"), MessageBoxButton.OK, MessageBoxImage.Information);
            else
            {
                var messageText = string.Concat(Localization.lookup("NumberOfImportedVouchers"), ": ", NumberConvert.ToString(noOfVouchers), Environment.NewLine, Environment.NewLine,
                        string.Format(Localization.lookup("GoTo"), Localization.lookup("Journallines")), "?");
                if (UnicontaMessageBox.Show(messageText, Localization.lookup("BilagscanRead"), MessageBoxButton.OKCancel, MessageBoxImage.Information) == MessageBoxResult.OK)
                {
                    AddDockItem(TabControls.GL_DailyJournalLine, journal, null, null, true);
                }
            }
        }

        private void TextBlock_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            busyIndicator.IsBusy = true;
            var selectedItem = dgGldailyJournal.SelectedItem as GLDailyJournalClient;
            if (selectedItem != null)
                AddDockItem(TabControls.GL_DailyJournalLine, selectedItem);
            busyIndicator.IsBusy = false;
        }
    }

    internal enum BilagscanVoucherType
    {
        Invoice,
        Creditnote,
        Receipt
    }
}
