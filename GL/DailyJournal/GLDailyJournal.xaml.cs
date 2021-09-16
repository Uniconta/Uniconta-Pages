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
            var Comp = api.CompanyEntity;
            if (Comp._CountryId != CountryCode.Denmark && Comp._CountryId != CountryCode.Iceland && Comp._CountryId != CountryCode.Netherlands && Comp._CountryId != CountryCode.Austria)
                UtilDisplay.RemoveMenuCommand((RibbonBase)localMenu.DataContext, "BilagscanReadVouchers");
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
                    AddDockItem(TabControls.GLDailyJournalPage2, api, Uniconta.ClientTools.Localization.lookup("Posting"), "Add_16x16.png");
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
                    var text = string.Format(Uniconta.ClientTools.Localization.lookup("JournalContainsLines"), selectedItem._Name);
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
                        AddDockItem(TabControls.ImportGLDailyJournal, selectedItem, string.Concat(string.Format(Uniconta.ClientTools.Localization.lookup("ImportOBJ"),
                            Uniconta.ClientTools.Localization.lookup("BankStatement")), " : ", selectedItem._Journal), null, true);
                    break;
                case "TransType":
                    AddDockItem(TabControls.GLTransTypePage, null, Uniconta.ClientTools.Localization.lookup("TransTypes"));
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
                    AddDockItem(TabControls.GLOffsetAccountTemplate, null, Uniconta.ClientTools.Localization.lookup("OffsetAccountTemplates"));
                    break;
                case "AccountActivity":
                    if (selectedItem != null)
                        AddDockItem(TabControls.GLTransLogPage, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("GLTransLog"), selectedItem._Journal));
                    break;
                case "BilagscanReadVouchers":
                    ReadFromBilagscan(selectedItem);
                    break;
                case "MoveJournalLines":
                    if (selectedItem != null)
                        MoveJournalLines(selectedItem);
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
            string header = selectedItem._Journal;
            UnicontaBaseEntity[] baseEntityArray = new UnicontaBaseEntity[2] { new GLDailyJournalLineClient(), selectedItem };
            AddDockItem(TabControls.ImportPage, new object[] { baseEntityArray, header }, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Import"), header));
        }
         
        private bool readingFromBilagscan;
        private async void ReadFromBilagscan(UnicontaBaseEntity selectedItem)
        {
#if !SILVERLIGHT

            if (!readingFromBilagscan)
            {
                readingFromBilagscan = true;

                bool processLines = false;
                var accessToken = await Bilagscan.Account.GetBilagscanAccessToken(api);
                var noOfVouchers = 0;
                var companySettings = await api.Query<CompanySettingsClient>();
                var orgNo = companySettings.FirstOrDefault()._OrgNumber;

                var journal = dgGldailyJournal.SelectedItem as GLDailyJournalClient;

              //  UnicontaBaseEntity[] baseEntityArray = new UnicontaBaseEntity[1] { selectedItem };

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                    var response = await client.GetAsync($"https://api.bilagscan.dk/v1/organizations/" + orgNo.ToString() + "/vouchers?seen=false&count=100&offset=0&sorts=-upload_date&status=successful");
                    var content = await response.Content.ReadAsStringAsync();
                    var vouchers = Bilagscan.Voucher.GetVouchers(content);

                    var credCache = api.CompanyEntity.GetCache(typeof(Uniconta.DataModel.Creditor)) ?? await api.CompanyEntity.LoadCache(typeof(Uniconta.DataModel.Creditor), api);
                    var offsetCache = api.CompanyEntity.GetCache(typeof(Uniconta.DataModel.GLAccount)) ?? await api.CompanyEntity.LoadCache(typeof(Uniconta.DataModel.GLAccount), api);
                    var vouchersSeen = new CommaDelimitedStringCollection();

                    var master = new List<UnicontaBaseEntity>
                    {
                        journal
                    };

                    var newLines = new List<UnicontaBaseEntity>();
                    var updateCreditor = new List<UnicontaBaseEntity>();

                    if (vouchers?.data != null)
                    {
                        var creditors = credCache.GetKeyStrRecords as Uniconta.DataModel.Creditor[];

                        foreach (var voucher in vouchers.data)
                        {
                            vouchersSeen.Add(NumberConvert.ToString(voucher.id));
                            var journalLine = new GLDailyJournalLineClient()
                            {
                                Approved = false,
                                ForceSettlement = false
                            };

                            var postingType = BilagscanVoucherType.Invoice;

                            var hint = Bilagscan.Voucher.GetHint(voucher.note);

                            var bilagscanRefID = voucher.id;
                            journalLine.ReferenceNumber = bilagscanRefID != 0 ? NumberConvert.ToString(bilagscanRefID) : null;

                            var bsItem = voucher.header_fields.Where(hf => string.Compare(hf.code, "voucher_number", true) == 0).FirstOrDefault();
                            if (bsItem != null)
                                journalLine.Invoice = bsItem.value;

                            bsItem = voucher.header_fields.Where(hf => string.Compare(hf.code, "voucher_type", true) == 0).FirstOrDefault();
                            if (bsItem != null)
                            {
                                switch (bsItem.value)
                                {
                                    case "invoice": postingType = BilagscanVoucherType.Invoice; break;
                                    case "creditnote": postingType = BilagscanVoucherType.Creditnote; break;
                                    case "receipt": postingType = BilagscanVoucherType.Receipt; break;
                                }
                            }

                            var creditorCVR = string.Empty;
                            bsItem = voucher.header_fields.Where(hf => string.Compare(hf.code, "company_vat_reg_no", true) == 0).FirstOrDefault();
                            if (bsItem != null)
                                creditorCVR = bsItem.value;

                            bsItem = voucher.header_fields.Where(hf => string.Compare(hf.code, "total_amount_incl_vat", true) == 0).FirstOrDefault();
                            if (bsItem != null)
                            {
                                journalLine.Amount = Math.Abs(NumberConvert.ToDoubleNoThousandSeperator(bsItem.value));

                                if (postingType != BilagscanVoucherType.Creditnote)
                                    journalLine.Amount = -journalLine.Amount;
                            }

                            CountryCode countryCode = CountryCode.Denmark;
                            bsItem = voucher.header_fields.Where(hf => string.Compare(hf.code, "country", true) == 0).FirstOrDefault();
                            if (bsItem != null)
                            {
                                CountryISOCode countryISO;
                                countryCode = CountryCode.Denmark; //default
                                if (Enum.TryParse(bsItem.value, true, out countryISO))
                                    countryCode = (CountryCode)countryISO;
                            }

                            bsItem = voucher.header_fields.Where(hf => string.Compare(hf.code, "invoice_date", true) == 0).FirstOrDefault();
                            if (bsItem != null)
                            {
                                var invoiceDate = bsItem.value == string.Empty ? GetSystemDefaultDate() : StringSplit.DateParse(bsItem.value, DateFormat.ymd);
                                journalLine.Date = invoiceDate;

                                if (journalLine.Date == DateTime.MinValue)
                                    journalLine.Date = GetSystemDefaultDate();
                            }

                            bsItem = voucher.header_fields.Where(hf => string.Compare(hf.code, "payment_date", true) == 0).FirstOrDefault();
                            if (bsItem != null)
                            {
                                var paymentDate = bsItem.value == string.Empty ? DateTime.MinValue : StringSplit.DateParse(bsItem.value, DateFormat.ymd);
                                journalLine._DueDate = paymentDate;
                            }

                            bsItem = voucher.header_fields.Where(hf => string.Compare(hf.code, "currency", true) == 0).FirstOrDefault();
                            if (bsItem != null)
                            {
                                Currencies currencyISO;
                                if (!Enum.TryParse(bsItem.value, true, out currencyISO))
                                    currencyISO = Currencies.DKK; //default

                                journalLine._Currency = (byte)currencyISO;
                            }

                            string bbanAcc = null;
                            bsItem = voucher.header_fields.Where(hf => string.Compare(hf.code, "payment_account_number", true) == 0).FirstOrDefault();
                            if (bsItem != null)
                                bbanAcc = bsItem.value;

                            string bbanRegNum = null;
                            bsItem = voucher.header_fields.Where(hf => string.Compare(hf.code, "payment_reg_number", true) == 0).FirstOrDefault();
                            if (bsItem != null)
                                bbanRegNum = bsItem.value;

                            string ibanNo = null;
                            bsItem = voucher.header_fields.Where(hf => string.Compare(hf.code, "payment_iban", true) == 0).FirstOrDefault();
                            if (bsItem != null)
                                ibanNo = bsItem.value;

                            string swiftNo = null;
                            bsItem = voucher.header_fields.Where(hf => string.Compare(hf.code, "payment_swift_bic", true) == 0).FirstOrDefault();
                            if (bsItem != null)
                                swiftNo = bsItem.value;

                            string paymentCodeId = null;
                            bsItem = voucher.header_fields.Where(hf => string.Compare(hf.code, "payment_code_id", true) == 0).FirstOrDefault();
                            if (bsItem != null)
                                paymentCodeId = bsItem.value;

                            string paymentId = null;
                            bsItem = voucher.header_fields.Where(hf => string.Compare(hf.code, "payment_id", true) == 0).FirstOrDefault();
                            if (bsItem != null)
                                paymentId = bsItem.value;

                            string jointPaymentId = null;
                            bsItem = voucher.header_fields.Where(hf => string.Compare(hf.code, "joint_payment_id", true) == 0).FirstOrDefault();
                            if (bsItem != null)
                                jointPaymentId = bsItem.value;

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

                            journalLine.SettleValue = "Voucher";

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
                                journalLine.Text = Uniconta.ClientTools.Localization.lookup("NotValidVatNo");
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
                                catch (Exception ex)
                                {
                                    UnicontaMessageBox.Show(ex);
                                    return;
                                }

                                if (companyInformation != null)
                                {
                                    if (companyInformation.life != null)
                                        newCreditor._Name = companyInformation.life.name;

                                    if (companyInformation.address != null)
                                    {
                                        newCreditor._Address1 = companyInformation.address.CompleteStreet;
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
                                    newCreditor.Name = Uniconta.ClientTools.Localization.lookup("NotValidVatNo");
                                }

                                await api.Insert(newCreditor);
                                journalLine.Account = creditorCVR;
                            }
                            else
                            {
                                if (!string.IsNullOrEmpty(creditor._PostingAccount))
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
                                else
                                {
                                    journalLine.Vat = creditor._Vat;
                                }


                                if (journalLine._DueDate == DateTime.MinValue && creditor._Payment != string.Empty)
                                {
                                    var paymentTermsCache = api.GetCache(typeof(PaymentTerm)) ?? await api.LoadCache(typeof(PaymentTerm));
                                    var paymentTerm = (PaymentTerm)paymentTermsCache.Get(creditor._Payment);

                                    if (paymentTerm != null)
                                        journalLine._DueDate = paymentTerm.GetDueDate(journalLine.DueDate);
                                }

                                journalLine.Account = creditor._Account;
                                journalLine.Text = creditor._Name;

                                if (creditor._SWIFT == null && swiftNo != null)
                                {
                                    creditor._SWIFT = swiftNo;
                                    updateCreditor.Add(creditor);
                                }
                            }

                            journalLine.SetMaster(master[0]);
                            newLines.Add(journalLine);
                           
                            noOfVouchers += 1;
                        }
                    }
                    var errorCode = await api.Insert(newLines);
                    api.UpdateNoResponse(updateCreditor);

                    if (vouchersSeen.Count != 0)
                    {
                        // Mark voucher as seen
                        string serializedRequest = "{ \"vouchers\": [ " + vouchersSeen.ToString() + " ] }";
                        var vContent = new StringContent(serializedRequest, Encoding.UTF8, "application/json");
                        response = await client.PostAsync($"https://api.bilagscan.dk/v1/organizations/" + NumberConvert.ToString(orgNo) + "/vouchers/seen", vContent);
                        var res = await response.Content.ReadAsStringAsync();
                    }
                }

                if (noOfVouchers == 0)
                    UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("StillProcessingTryAgain"), Uniconta.ClientTools.Localization.lookup("Bilagscan")), Uniconta.ClientTools.Localization.lookup("Bilagscan"), MessageBoxButton.OK, MessageBoxImage.Information);
                else
                {
                    var messageText = string.Concat(string.Format("{0} {1}", Uniconta.ClientTools.Localization.lookup("NumberOfImportedVouchers"), noOfVouchers),
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
}
