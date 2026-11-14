using UnicontaClient.Models;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using System;
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
using Uniconta.DataModel;
using Uniconta.ClientTools.Util;
using Uniconta.API.Service;
using Uniconta.API.DebtorCreditor;
using System.Windows;
using Uniconta.Client.Pages;
using Uniconta.Common.Utility;
using UnicontaClient.Utilities;
using DevExpress.Xpf.Editors;
using System.Globalization;
using System.Windows.Data;
using DevExpress.XtraReports.UI;
using NPOI.SS.Formula.Functions;
using System.IO;
using static DevExpress.Utils.Svg.CommonSvgImages;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class GridCreditorTransContr : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(CreditorTransClient); } }
    }
    public partial class CreditorTransactionsContr : GridBasePage
    {
        public override string NameOfControl { get { return "ContractorReportIcelandPage"; } }
        int filterYear;
        static string filterContr = "";

        protected override Filter[] DefaultFilters()
        {
            Filter contrFilter = new Filter() { name = "Account", value = $"{filterContr}" };
            Filter yearFilter = new Filter() { name = "Date", value = $"{filterYear}-01-01..{filterYear}-12-31" };
            Filter typeFilter = new Filter() { name = "PostType", value = "Invoice;CreditNote;Fee;Cancellation;Discount" };//String.Format("{0:d}..", filterDate) };
            if (filterContr != "")
                return new Filter[] { contrFilter, yearFilter, typeFilter };
            else
                return new Filter[] { yearFilter, typeFilter };
        }
        protected override SortingProperties[] DefaultSort()
        {
            SortingProperties dateSort = new SortingProperties("Date") { Ascending = false };
            SortingProperties VoucherSort = new SortingProperties("Voucher");
            return new SortingProperties[] { dateSort, VoucherSort };
        }

        public CreditorTransactionsContr(UnicontaBaseEntity master)
            : base(master)
        {
            Initialize(master);
        }
        public CreditorTransactionsContr(SynchronizeEntity syncEntity)
            : base(syncEntity, true)
        {
            this.syncEntity = syncEntity;
            Initialize(syncEntity.Row);
            SetHeader();
        }
        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            dgCreditorTransContr.UpdateMaster(args);
            SetHeader();
            FilterGrid(gridControl, false, false);
        }
        void SetHeader()
        {
            var syncMaster = dgCreditorTransContr.masterRecord as Uniconta.DataModel.Creditor;
            if (syncMaster == null)
                return;
            string header = string.Format("{0}/{1}", Uniconta.ClientTools.Localization.lookup("CreditorTransactionsContr"), syncMaster._Account);
            SetHeader(header);
        }
        public CreditorTransactionsContr(BaseAPI API)
            : base(API, string.Empty)
        {
            InitializeComponent();
            Initialize(null);
        }

        UnicontaBaseEntity master;
        private void Initialize(UnicontaBaseEntity _master)
        {
            InitializeComponent();
            master = _master;
            dgCreditorTransContr.UpdateMaster(master);
            dgCreditorTransContr.api = api;
            var Comp = api.CompanyEntity;
            filterYear = BasePage.GetSystemDefaultDate().Year - 1;
            txtYearValue.EditValue = DateTime.Parse($"{filterYear}-01-01", CultureInfo.InvariantCulture);
            SetRibbonControl(localMenu, dgCreditorTransContr);
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgCreditorTransContr.BusyIndicator = busyIndicator;
            dgCreditorTransContr.ShowTotalSummary();
            if (Comp.RoundTo100)
                Amount.HasDecimals = Debit.HasDecimals = Credit.HasDecimals = false;
        }

        public override Task InitQuery()
        {
            return FilterGrid(dgCreditorTransContr, master == null, false);
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            bool showFields = (dgCreditorTransContr.masterRecords == null);
            colAccount.Visible = showFields;
            colName.Visible = showFields;
            Open.Visible = !showFields;
            if (!api.CompanyEntity.Project)
                Project.Visible = false;
            Text.ReadOnly = Invoice.ReadOnly = PostType.ReadOnly = TransType.ReadOnly = showFields;
            if (showFields)
            {
                Open.Visible = false;
                RibbonBase rb = (RibbonBase)localMenu.DataContext;
                UtilDisplay.RemoveMenuCommand(rb, "SaveGrid");
            }
            var credMaster = master as Uniconta.DataModel.Creditor;
            if (credMaster != null)
            {
#if !SILVERLIGHT
                FromDebtor.Visible =
#endif
                dgCreditorTransContr.Readonly = (credMaster._D2CAccount != null);
            }
            else
            {
                dgCreditorTransContr.Readonly = showFields;
            }
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            CreditorTransClient selectedItem = dgCreditorTransContr.SelectedItem as CreditorTransClient;
            switch (ActionType)
            {
                case "ViewDownloadRow":
                    if (selectedItem != null)
                        DebtorTransactions.ShowVoucher(dgCreditorTransContr.syncEntity, api, busyIndicator);
                    break;
                case "Settlements":
                    if (selectedItem != null)
                    {
                        string header = Util.ConcatParenthesis(Uniconta.ClientTools.Localization.lookup("Settlements"), selectedItem._Voucher);
                        AddDockItem(TabControls.CreditorSettlements, dgCreditorTransContr.syncEntity, true, header, null, floatingLoc: Utility.GetDefaultLocation());
                    }
                    break;
                case "VoucherTransactions":
                    if (selectedItem != null)
                    {
                        string vheader = Util.ConcatParenthesis(Uniconta.ClientTools.Localization.lookup("VoucherTransactions"), selectedItem._Voucher);
                        AddDockItem(TabControls.AccountsTransaction, dgCreditorTransContr.syncEntity, vheader);
                    }
                    break;
                case "SaveGrid":
                    Save();
                    break;
                case "PostedBy":
                    if (selectedItem != null)
                        JournalPosted(selectedItem);
                    break;
                case "RefreshGrid":
                    FilterGrid(gridControl, master == null, false);
                    break;
                case "Invoices":
                    if (selectedItem != null)
                    {
                        string header = string.Format("{0}/{1}", Uniconta.ClientTools.Localization.lookup("CreditorInvoice"), selectedItem._Account);
                        AddDockItem(TabControls.CreditorInvoice, dgCreditorTransContr.syncEntity, header);
                    }
                    break;
                case "InvoiceLine":
                    if (selectedItem != null)
                        ShowInvoiceLines(selectedItem);
                    break;
                case "ClearFilter":
                    filterContr = "";
                    this.baseRibbon?.FilterGrid?.ClearFilter(false);
                    this.baseRibbon?.SetFilterDefaultFields(DefaultFilters());
                    this.baseRibbon?.FilterGrid.Refresh();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        async void ShowInvoiceLines(CreditorTransClient creditorTrans)
        {
            var creditorInvoice = await api.Query<CreditorInvoiceClient>(new UnicontaBaseEntity[] { creditorTrans }, null);
            if (creditorInvoice != null && creditorInvoice.Length > 0)
            {
                var credInv = creditorInvoice[0];
                AddDockItem(TabControls.CreditorInvoiceLine, credInv, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("InvoiceNumber"), credInv.InvoiceNum));
            }
        }

        async private void JournalPosted(CreditorTransClient selectedItem)
        {
            var result = await api.Query(new GLDailyJournalPostedClient(), new UnicontaBaseEntity[] { selectedItem }, null);
            if (result != null && result.Length == 1)
            {
                CWGLPostedClientFormView cwPostedClient = new CWGLPostedClientFormView(result[0]);
                cwPostedClient.Show();
            }
        }

        async private void Save()
        {
            SetBusy();
            dgCreditorTransContr.BusyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("Saving");
            var err = await dgCreditorTransContr.SaveData();
            ClearBusy();
        }

        private void TxtYearValue_OnEditValueChanged(object sender, EditValueChangedEventArgs e)
        {
            var filterDate = (DateTime)e.NewValue;
            filterYear = filterDate.Year;
            this.baseRibbon?.SetFilterDefaultFields(DefaultFilters());
            this.baseRibbon?.FilterGrid.Refresh();
        }

        static string ContractorLegalID(CreditorClient contractor)
        {
            var ContractorLegalID = "";
            if (contractor.CompanyRegNo != null)
                ContractorLegalID = contractor.CompanyRegNo;
            else
                ContractorLegalID = contractor.Account;
            ContractorLegalID = new string(ContractorLegalID.ToCharArray().Where(o => char.IsDigit(o)).ToArray());
            return ContractorLegalID;
        }

        // one slip per contractor: the year's payments on the visible transactions, cut to whole krónur
        static List<IcelandContractorSlip> Slips(IEnumerable<CreditorTransClient> items, out string error, out string errorAccount)
        {
            error = null;
            errorAccount = null;
            var lst = new List<IcelandContractorSlip>();
            foreach (var grouping in items.GroupBy(o => o.Creditor))
            {
                var creditor = grouping.Key;
                if (creditor == null)
                    continue;
                var total = grouping.Sum(o => o.Amount);
                if (total >= 0)
                    continue;
                var amount = (long)-total;
                if (creditor.Country == CountryCode.Iceland)
                {
                    var kt = ContractorLegalID(creditor);
                    if (kt.Length != 10)
                        error = "Ógild kennitala";
                    else
                        lst.Add(new IcelandContractorSlip { Kennitala = kt, Amount = amount });
                }
                else if (creditor.Country == CountryCode.Unknown)
                    error = "Óþekkt land";
                else
                    lst.Add(new IcelandContractorSlip
                    {
                        Country = ((CountryISOCode)creditor.Country).ToString(),
                        Tin = creditor._VatNumber ?? "",
                        Name = creditor._Name,
                        Street = creditor.Address,
                        City = creditor._City,
                        Amount = amount
                    });
                if (error != null)
                {
                    errorAccount = creditor.Account;
                    return null;
                }
            }
            return lst;
        }

        // RSK's schema messages in words
        static string Readable(string RSKmelding)
        {
            if (string.IsNullOrEmpty(RSKmelding))
                return RSKmelding;
            if (RSKmelding.Contains("List of possible elements expected: 'Tin Gata'.") || RSKmelding.Contains("The 'Tin' element has an invalid value"))
                return "Vantar Tax Identification Number (TIN) hjá verktaka";
            if (RSKmelding.Contains("List of possible elements expected: 'Gata'") || RSKmelding.Contains("The 'Gata' element has an invalid value"))
                return "Vantar heimilisfang hjá verktaka";
            if (RSKmelding.Contains("'ErlendurAdili' has invalid child element 'Land'. List of possible elements expected: 'Borg'.") || RSKmelding.Contains("The 'Borg' element has an invalid value"))
                return "Vantar að tilgreina borg verktaka";
            if (RSKmelding.Contains("Ekki er hægt að skila sömu sendingu aftur."))
                return "Búið að skila miða fyrir verktaka";
            return RSKmelding;
        }

        static string Message(IcelandContractorSlipsResult r)
        {
            var s = Readable(r.Message);
            if (!string.IsNullOrEmpty(r.Details))
                s = string.IsNullOrEmpty(s) ? r.Details : string.Concat(s, "\n\n", r.Details);
            return s;
        }

        private void SendButton_OnClick(object sender, RoutedEventArgs e)
        {
            Send(IcelandContractorSlipAction.New);
        }

        // the server checks the slips at RSK, then files them; RSK's receipt goes to the inbox
        private async void Send(IcelandContractorSlipAction action)
        {
            var veflykill = txtVeflykill.EditText;
            var keys = new Uniconta.API.GeneralLedger.IcelandRskKeysAPI(api);
            var keyState = await keys.State(); // with a veflykill saved for the company, the server uses it when none is typed
            var saved = keyState != null && keyState.SlipsSaved;
            if (string.IsNullOrEmpty(veflykill) && !saved)
            {
                UnicontaMessageBox.Show("Sláðu inn veflykil", "Verktakamiðar");
                return;
            }
            var items = dgCreditorTransContr.VisibleItems.Cast<CreditorTransClient>().ToList();
            var slips = Slips(items, out var error, out var errorAccount);
            if (slips == null)
            {
                filterContr = errorAccount;
                UnicontaMessageBox.Show(error + $" á lykli {errorAccount}", "Verktakamiðar");
                this.baseRibbon?.SetFilterDefaultFields(DefaultFilters());
                this.baseRibbon?.FilterGrid.Refresh();
                return;
            }
            if (slips.Count == 0)
            {
                UnicontaMessageBox.Show("Engir verktakar fundust", "Verktakamiðar");
                return;
            }

            SetBusy();
            dgCreditorTransContr.BusyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("Sending");
            var capi = new IcelandContractorSlipsAPI(api);
            var check = await capi.Check(filterYear, action, veflykill, slips);
            IcelandContractorSlipsResult res = null;
            if (check != null && check.Err == 0 && check.Valid)
                res = await capi.File(filterYear, action, veflykill, slips);
            ClearBusy();

            if (check == null || (res == null && check.Err == 0 && check.Valid))
            {
                UtilDisplay.ShowErrorCode(capi.LastError != 0 ? capi.LastError : ErrorCodes.NoSucces);
                return;
            }
            if (check.Err != 0 || !check.Valid)
            {
                if (check.Err != 0 && string.IsNullOrEmpty(check.Message))
                    UtilDisplay.ShowErrorCode(check.Err);
                else
                    UnicontaMessageBox.Show(Message(check), "Villa");
                return;
            }
            if (res.Err != 0 || !res.Valid)
            {
                if (string.IsNullOrEmpty(res.Message))
                    UtilDisplay.ShowErrorCode(res.Err != 0 ? res.Err : ErrorCodes.NoSucces);
                else
                    UnicontaMessageBox.Show(Message(res), "Villa");
                return;
            }
            if (!string.IsNullOrWhiteSpace(res.Message) || !string.IsNullOrEmpty(res.Details))
                UnicontaMessageBox.Show(Message(res), "Athugasemd");
            if (res.VoucherRowId != 0)
                UnicontaMessageBox.Show("Kvittun hefur verið vistuð í Stafræn fylgiskjöl (innhólf)", "Aðgerð tókst", MessageBoxButton.OK, MessageBoxImage.Information);
            else
                UnicontaMessageBox.Show($"Verktakamiðum skilað, bunki {res.BatchNumber}", "Aðgerð tókst", MessageBoxButton.OK, MessageBoxImage.Information);
            if (!saved && !string.IsNullOrEmpty(veflykill) && UnicontaMessageBox.Show(
                    "Vista veflykil Gagnaskila fyrir fyrirtækið? Hann er geymdur dulkóðaður á þjóninum, og þá þarf ekki að slá hann inn næst.",
                    "Verktakamiðar", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                var s = await keys.Save(Uniconta.API.GeneralLedger.IcelandRskKeyKind.Slips, veflykill);
                if (s == null || s.Err != ErrorCodes.Succes)
                    UnicontaMessageBox.Show(s?.Message ?? "Veflykillinn var ekki vistaður", "Verktakamiðar");
            }
        }
    } //...class CreditorTransactionsContr

    public class GroupConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((Uniconta.DataModel.Creditor)value)._Group;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class Comment
    {
        public string Contr;
        public string Comnt;

        public Comment(string c)
        {
            this.Comnt = c;
        }
    }

}
