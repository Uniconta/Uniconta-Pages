using DevExpress.Xpf.Grid.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Uniconta.API.Service;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.Common.Utility;
using Uniconta.DataModel;
using UnicontaClient.Models;
using UnicontaClient.Pages.GL.Reports.SIESwedenImportExport.Helpers;
using UnicontaClient.Pages.GL.Reports.SIESwedenImportExport.SIEModels;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class SIEGLTransGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(GLTransClient); } }
        public SIEGLTransGrid(IDataControlOriginationElement dataControlOriginationElement) : base(dataControlOriginationElement) { }
        public SIEGLTransGrid() { }
    }

    public partial class SIEExportPage : GridBasePage
    {
        private SQLTableCache<GLAccountClient> _accounts;
        private IEnumerable<CompanyFinanceYearClient> _accountingYears;

        //The range of the last search. The export uses it, so that balances and vouchers cover the same dates as the grid.
        private DateTime _searchFromDate;
        private DateTime _searchToDate;

        /// <summary>
        /// SIE file types offered for export. 4I is the transaction import format (vouchers only), 4E the
        /// full export format (balances and vouchers).
        /// </summary>
        public static string[] SieFormatTypeList = new string[]
        {
            "1",
            "2",
            "3",
            "4I",
            "4E"
        };

        public SIEExportPage(BaseAPI API) : base(API, string.Empty)
        {
            Init();
        }

        public SIEExportPage(SynchronizeEntity syncEntity) : base(syncEntity, true)
        {
            Init();
        }

        //This ensures that no initial results are loaded before "Search" is clicked.
        public override Task InitQuery()
        {
            return Task.CompletedTask;
        }

        private void Init()
        {
            InitializeComponent();
            SetRibbonControl(localMenu, dgGLTransactions);
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;

            DataContext = this;

            cbSIECompanyType.ItemsSource = SIEHelpers.GetOrganisationTypes();
            cbSIEFormatType.ItemsSource = SieFormatTypeList;

            //Default date from and to will be running month
            deDateTo.DateTime = DateTime.Today;
            deDateFrm.DateTime = DateTime.Today.AddMonths(-1);

            dgGLTransactions.RowDoubleClick += DgGLTransactions_RowDoubleClick;
            dgGLTransactions.ShowTotalSummary();

            _loadBudgetsTask = LoadBudgetsAsync();
        }

        private GLBudgetClient[] _budgets;
        private Task _loadBudgetsTask;

        /// <summary>
        /// Fills the budget list. #PBUDGET is written from the one budget chosen; the active budget is suggested
        /// when there is exactly one.
        /// </summary>
        private async Task LoadBudgetsAsync()
        {
            try
            {
                var budgets = await api.Query<GLBudgetClient>();
                _budgets = (budgets ?? new GLBudgetClient[0]).Where(b => !String.IsNullOrEmpty(b.KeyStr)).OrderBy(b => b.KeyStr).ToArray();
                cbBudget.ItemsSource = _budgets.Select(b => b.KeyStr).ToList();

                var activeBudgets = _budgets.Where(b => b._Active).ToList();
                if (activeBudgets.Count == 1)
                    cbBudget.SelectedItem = activeBudgets[0].KeyStr;
            }
            catch (Exception ex)
            {
                api.ReportException(ex, "SIE export: load budgets");
            }
        }

        protected override async Task LoadCacheInBackGroundAsync()
        {
            await EnsureCacheLoadedAsync();
        }

        private async Task EnsureCacheLoadedAsync()
        {
            if (_accounts == null)
                _accounts = await api.LoadCache<GLAccountClient>();

            if (_accountingYears == null)
                _accountingYears = await api.Query<CompanyFinanceYearClient>();
        }

        private void DgGLTransactions_RowDoubleClick()
        {
            ribbonControl.PerformRibbonAction("VoucherTransactions");
        }

        //Do not show user defined fields
        public override bool CheckIfBindWithUserfield(out bool isReadOnly, out bool useBinding)
        {
            isReadOnly = false;
            useBinding = false;
            return false;
        }

        private async void LocalMenu_OnItemClicked(string ActionType)
        {
            try
            {
                switch (ActionType)
                {
                    case "ExportData":
                        await ExportDataAsync();
                        break;
                    case "VoucherTransactions":
                        var selItem = dgGLTransactions.SelectedItem as GLTransClient;
                        if (selItem != null)
                            AddDockItem(TabControls.AccountsTransaction, dgGLTransactions.syncEntity, Util.ConcatParenthesis(Uniconta.ClientTools.Localization.lookup("VoucherTransactions"), selItem._Voucher));
                        break;
                    case "Search":
                        await LoadTransAsync();
                        break;
                    default:
                        gridRibbon_BaseActions(ActionType);
                        break;
                }
            }
            catch (Exception ex)
            {
                busyIndicator.IsBusy = false;
                Uniconta.ClientTools.Controls.UnicontaMessageBox.Show(ex);
            }
        }

        /// <summary>
        /// Filter GL transactions according to the provided date range
        /// </summary>
        private async Task LoadTransAsync()
        {
            var fromDate = deDateFrm.DateTime.Date;
            var toDate = deDateTo.DateTime.Date;

            if (fromDate > toDate)
            {
                UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("ToDateTooSmall"), Uniconta.ClientTools.Localization.lookup("Error"));
                return;
            }

            busyIndicator.IsBusy = true;
            try
            {
                var filteredTransactions = await QueryTransactionsAsync(fromDate, toDate);
                if (filteredTransactions == null)
                    return;

                dgGLTransactions.ItemsSource = filteredTransactions;
                dgGLTransactions.Visibility = Visibility.Visible;

                _searchFromDate = fromDate;
                _searchToDate = toDate;
            }
            finally
            {
                busyIndicator.IsBusy = false;
            }
        }

        /// <summary>
        /// Queries posted GL transactions in a date range. Returns null (and shows the error) when the query fails.
        /// </summary>
        private async Task<GLTransClient[]> QueryTransactionsAsync(DateTime fromDate, DateTime toDate)
        {
            //Add filters according to the selected dates
            var propValuePairs = new List<PropValuePair>();
            var dateFilterStr = String.Empty;

            if (fromDate != DateTime.MinValue &&
                toDate != DateTime.MinValue)
                dateFilterStr = $"{fromDate.ToShortDateString()}..{toDate.ToShortDateString()}";
            else if (fromDate != DateTime.MinValue)
                dateFilterStr = $"{fromDate.ToShortDateString()}..";
            else if (toDate != DateTime.MinValue)
                dateFilterStr = $"..{toDate.ToShortDateString()}";

            if (!string.IsNullOrEmpty(dateFilterStr))
                propValuePairs.Add(PropValuePair.GenereteWhereElements(nameof(GLTransClient.Date), typeof(DateTime), dateFilterStr));

            var transactions = await api.Query<GLTransClient>(propValuePairs);

            //In case of error, short-circuit
            if (transactions == null &&
                api.LastError != ErrorCodes.Succes)
            {
                Uniconta.ClientTools.Util.UtilDisplay.ShowErrorCode(api.LastError);
                return null;
            }

            return transactions ?? new GLTransClient[0];
        }

        /// <summary>
        /// Export Gl transactions from grid according to the selected SIE file type
        /// </summary>
        private async Task ExportDataAsync()
        {
            var sieType = cbSIEFormatType.EditValue?.ToString();
            if (String.IsNullOrEmpty(sieType))
            {
                UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("PleaseSelectOBJ"), Uniconta.ClientTools.Localization.lookup("Type")), Uniconta.ClientTools.Localization.lookup("Error"));
                return;
            }

            var sieCompanyType = cbSIECompanyType.EditValue?.ToString();
            if (String.IsNullOrEmpty(sieCompanyType))
            {
                UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("PleaseSelectOBJ"), Uniconta.ClientTools.Localization.lookup("SIECompanyType")), Uniconta.ClientTools.Localization.lookup("Error"));
                return;
            }

            //#PBUDGET is compulsory in types 2 and 3 when the company has a budget (SIE 4B 5.17-5.18), optional in
            //4E and not allowed in 1 and 4I (chapter 6). The budget list is awaited for 4E as well, so that a budget
            //suggested by the list is used, also right after the page has been opened.
            if (sieType == "2" || sieType == "3" || sieType == "4E")
            {
                if (_loadBudgetsTask != null)
                    await _loadBudgetsTask;

                if (sieType != "4E" && _budgets != null && _budgets.Length > 0 && cbBudget.SelectedItem == null)
                {
                    UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("PleaseSelectOBJ"), Uniconta.ClientTools.Localization.lookup("Budget")) + Environment.NewLine + Uniconta.ClientTools.Localization.lookup("SIEMissingBudget"), Uniconta.ClientTools.Localization.lookup("Error"));
                    return;
                }
            }

            var gridTransactions = dgGLTransactions.ItemsSource as IEnumerable<GLTransClient>;
            if (gridTransactions == null)
            {
                UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("SearchFirst"), Uniconta.ClientTools.Localization.lookup("Error"));
                return;
            }

            //The vouchers of a type 4 file should belong to one financial year: #RAR 0, the year of the to-date
            if (sieType == "4I" || sieType == "4E")
            {
                await EnsureCacheLoadedAsync();

                var currentYear = GetYears(_searchToDate)[0];
                if (SIEHelpers.SpansFinancialYears(_searchFromDate, currentYear.Start))
                {
                    var spanConfirmation = UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("SIEDateRangeSpansYears"), currentYear.Start.Value.ToShortDateString(), currentYear.End.Value.ToShortDateString()) + Environment.NewLine + Uniconta.ClientTools.Localization.lookup("AreYouSureToContinue"),
                                                                   Uniconta.ClientTools.Localization.lookup("Warning"),
                                                                   MessageBoxButton.YesNo);
                    if (spanConfirmation != MessageBoxResult.Yes)
                        return;
                }
            }

            var fileName = GetFileName(sieType);
            if (String.IsNullOrEmpty(fileName))
                return;

            busyIndicator.IsBusy = true;
            try
            {
                await EnsureCacheLoadedAsync();
                _accountsByNumber = null;
                _hasSruField = null;

                var sieDocument = await FillSIEDocumentAsync(sieType, sieCompanyType, gridTransactions.ToList());
                if (sieDocument == null)
                    return;

                var writerOptions = new SIEDocumentWriter.WriteOptions
                {
                    WriteKSUMMA = true
                };

                var writer = new SIEDocumentWriter(sieDocument, writerOptions);
                writer.Write(fileName);

                UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("SavedOBJ"), fileName), Uniconta.ClientTools.Localization.lookup("Succes"));
            }
            finally
            {
                busyIndicator.IsBusy = false;
            }
        }

        /// <summary>
        /// Collect export destination and file name from the user
        /// </summary>
        private string GetFileName(string sieType)
        {
            //Files for import into a financial reporting program (4I) are given the suffix .SI, other files .SE (5.1)
            var extension = sieType == "4I" ? "si" : "se";

            var saveDialog = UtilDisplay.LoadSaveFileDialog;
            saveDialog.FileName = $"{SIEHelpers.SafeFileName(api.CompanyEntity.Name)}_{DateTime.Now.ToString("yyyyMMddHHmm")}_SIE{sieType}.{extension}";
            saveDialog.Filter = $"{Uniconta.ClientTools.Localization.lookup("SIEFiles")} (*.se;*.si;*.sie)|*.se;*.si;*.sie|All files (*.*)|*.*";

            bool? dialogResult = saveDialog.ShowDialog();
            if (dialogResult != true)
                return null;

            return saveDialog.FileName;
        }

        protected override LookUpTable HandleLookupOnLocalPage(LookUpTable lookup, CorasauDataGrid dg)
        {
            if (dg != dgGLTransactions || dg.CurrentColumn?.FieldName != nameof(GLTransClient.Account))
                return base.HandleLookupOnLocalPage(lookup, dg);

            lookup.TableType = typeof(Uniconta.DataModel.GLAccount);
            return lookup;
        }

        #region SIE file functions

        /// <summary>
        /// Prepare SIE document for export according to the type.
        /// Vouchers (type 4) are taken from the searched date range; balances are calculated for the financial
        /// year of the to-date (year 0) and the previous financial year (year -1) up to the to-date (#OMFATTN).
        /// </summary>
        private async Task<SIEDocument> FillSIEDocumentAsync(string sieType, string companyType, List<GLTransClient> gridTransactions)
        {
            var isImportFile = sieType == "4I";
            var sieTypeNumber = sieType == "4I" || sieType == "4E" ? 4 : int.Parse(sieType);
            var cutOff = _searchToDate;

            var sieDocument = new SIEDocument();
            sieDocument.FLAGGA = 0;
            sieDocument.FORMAT = "PC8";
            sieDocument.SIETYP = sieTypeNumber;
            sieDocument.GEN_DATE = DateTime.Now;
            sieDocument.GEN_NAMN = api.session.User._Name;
            sieDocument.FNAMN = GetFnamn(companyType);
            sieDocument.PROGRAM = new List<string>() { "Uniconta", api.session.ServerVersion.ToString() };
            sieDocument.VALUTA = api.CompanyEntity._CurrencyId.ToString();
            sieDocument.KPTYP = "EUBAS97";
            sieDocument.RAR = GetYears(cutOff);
            _yearStarts = new HashSet<DateTime>(sieDocument.RAR.Values.Where(y => y.Start.HasValue).Select(y => y.Start.Value.Date)
                .Concat((_accountingYears ?? Enumerable.Empty<CompanyFinanceYearClient>()).Select(y => y.FromDate.Date)));
            sieDocument.KONTO = GetKonto();
            sieDocument.DIM = sieTypeNumber >= 3 ? await GetDimsAsync() : new Dictionary<string, SIEDimension>();

            if (!isImportFile)
            {
                sieDocument.OMFATTN = cutOff;

                //#TAXAR is not written: SIE-gruppen's file test reports the item as outdated.

                //Balances are calculated from all transactions of the years, independent of the searched range.
                //Years before the oldest #RAR year are added when it has not been opened by a year transfer.
                var rarStart = sieDocument.RAR.Values.Min(y => y.Start.Value);
                var earlierYears = await GetEarlierYearsAsync(sieDocument.RAR);
                if (earlierYears == null)
                    return null;

                var balanceStart = earlierYears.Any() ? earlierYears.Min(y => y.Start.Value) : rarStart;
                var balanceTransactions = _searchFromDate <= balanceStart
                    ? gridTransactions
                    : (await QueryTransactionsAsync(balanceStart, cutOff))?.ToList();

                if (balanceTransactions == null)
                    return null;

                var resultAccount = _accounts.FirstOrDefault(a => a.SystemAccountEnum == SystemAccountTypes.EndYearResultTransfer)?.Account;
                var calculator = new SIEBalanceCalculator(sieDocument.RAR, cutOff, sieDocument.KONTO, IsBalanceAccount, sieDocument.DIM, sieTypeNumber >= 3, resultAccount);
                calculator.Calculate(balanceTransactions.Select(ToTransRecord).ToList(), earlierYears);

                //Without a year-end result account, the result of a year without year transfer stays out of #IB
                if (calculator.ResultNotCarried)
                {
                    var resultConfirmation = UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("SIENoResultAccount") + Environment.NewLine + Uniconta.ClientTools.Localization.lookup("AreYouSureToContinue"),
                                                                     Uniconta.ClientTools.Localization.lookup("Warning"),
                                                                     MessageBoxButton.YesNo);
                    if (resultConfirmation != MessageBoxResult.Yes)
                        return null;
                }

                if (sieTypeNumber >= 2)
                    calculator.CalculateBudget(await GetBudgetRecordsAsync(rarStart, sieDocument.RAR.Values.Max(y => y.End.Value)));

                sieDocument.IB = calculator.IB;
                sieDocument.UB = calculator.UB;
                sieDocument.RES = calculator.RES;
                sieDocument.OIB = calculator.OIB;
                sieDocument.OUB = calculator.OUB;
                sieDocument.PSALDO = calculator.PSALDO;
                sieDocument.PBUDGET = calculator.PBUDGET;
            }

            if (sieTypeNumber == 4)
            {
                //Posting date and user of the vouchers, read once instead of through GLTransClient.JournalPostedRef
                var postedJournals = await api.Query<GLDailyJournalPostedClient>();
                _postedJournals = (postedJournals ?? new GLDailyJournalPostedClient[0]).GroupBy(p => p.RowId).ToDictionary(g => g.Key, g => g.First());

                sieDocument.VER = GetVer(gridTransactions, sieDocument.KONTO, sieDocument.DIM);
            }

            return sieDocument;
        }

        private Dictionary<int, GLDailyJournalPostedClient> _postedJournals;

        private GLDailyJournalPostedClient GetPostedJournal(GLTransClient transaction)
        {
            GLDailyJournalPostedClient posted;
            return transaction._JournalPostedId != 0 && _postedJournals != null && _postedJournals.TryGetValue(transaction._JournalPostedId, out posted) ? posted : null;
        }

        /// <summary>
        /// Financial years before the #RAR years that are needed for the opening balance: when the oldest #RAR year
        /// has no primo transactions (the year transfer has not been run), its opening balance is carried from the
        /// year before, and so on back to a year with primo transactions or the first financial year.
        /// Returns null when a query fails (the error has been shown).
        /// </summary>
        private async Task<List<SIEBookingYear>> GetEarlierYearsAsync(Dictionary<int, SIEBookingYear> rar)
        {
            var earlierYears = new List<SIEBookingYear>();

            var oldest = rar.Values.Where(y => y.Start.HasValue).OrderBy(y => y.Start.Value).FirstOrDefault();
            if (oldest == null)
                return earlierYears;

            var financeYears = (_accountingYears ?? Enumerable.Empty<CompanyFinanceYearClient>()).OrderByDescending(y => y.FromDate).ToList();
            var id = oldest.ID;
            var start = oldest.Start.Value.Date;

            while (true)
            {
                var hasOpeningEntries = await HasOpeningEntriesAsync(start);
                if (hasOpeningEntries == null)
                    return null;
                if (hasOpeningEntries.Value)
                    break;

                var previous = financeYears.FirstOrDefault(y => y.ToDate.Date < start);
                if (previous == null)
                    break;

                var year = new SIEBookingYear() { ID = --id, Start = previous.FromDate.Date, End = previous.ToDate.Date };
                earlierYears.Add(year);
                start = year.Start.Value;
            }

            return earlierYears;
        }

        /// <returns>null when the query fails</returns>
        private async Task<bool?> HasOpeningEntriesAsync(DateTime yearStart)
        {
            var transactions = await QueryTransactionsAsync(yearStart, yearStart);
            if (transactions == null)
                return null;
            return transactions.Any(IsOpeningEntry);
        }

        private Dictionary<string, GLAccountClient> _accountsByNumber;

        private bool IsBalanceAccount(string accountNumber)
        {
            if (_accountsByNumber == null)
                _accountsByNumber = _accounts.Where(a => !String.IsNullOrEmpty(a.Account)).GroupBy(a => a.Account).ToDictionary(g => g.Key, g => g.First());

            GLAccountClient account;
            if (accountNumber != null && _accountsByNumber.TryGetValue(accountNumber, out account))
                return SIEHelpers.IsBalanceAccount(account.AccountTypeEnum);

            //Unknown account: follow the BAS chart of accounts
            return SIEHelpers.IsBalanceAccount(SIEHelpers.ToUnicontaAccountType(null, accountNumber));
        }

        private HashSet<DateTime> _yearStarts;

        /// <summary>
        /// Opening entries (primo and year transfer) belong to #IB, not to the vouchers
        /// </summary>
        private bool IsOpeningEntry(GLTransClient transaction)
        {
            return SIEHelpers.IsOpeningEntry(transaction.PrimoTrans, transaction._YearTransfer, transaction._Origin, transaction.Voucher, transaction.Date, _yearStarts);
        }

        private SieTransRecord ToTransRecord(GLTransClient transaction)
        {
            return new SieTransRecord()
            {
                Account = transaction.Account,
                Date = transaction.Date,
                Amount = (decimal)transaction.Amount,
                Qty = (decimal)transaction.Qty,
                IsPrimo = IsOpeningEntry(transaction),
                Dimensions = GetDimensionValues(transaction.Dimension1, transaction.Dimension2, transaction.Dimension3, transaction.Dimension4, transaction.Dimension5)
            };
        }

        private string[] GetDimensionValues(params string[] values)
        {
            var count = Math.Min(api.CompanyEntity.NumberOfDimensions, values.Length);
            var result = new string[count];
            for (int i = 0; i < count; i++)
                result[i] = values[i];
            return result;
        }

        /// <summary>
        /// Budget amounts per period for #PBUDGET, from the budget chosen on the page. Budgets are not added
        /// together: a budget and a forecast would give doubled figures. Without a chosen budget, no #PBUDGET.
        /// </summary>
        private async Task<List<SieBudgetRecord>> GetBudgetRecordsAsync(DateTime fromDate, DateTime toDate)
        {
            var records = new List<SieBudgetRecord>();

            var budgetId = cbBudget.SelectedItem as string;
            var budget = _budgets?.FirstOrDefault(b => b.KeyStr == budgetId);
            if (budget == null)
                return records;

            var budgetLines = await api.Query<GLBudgetLineClient>(budget);
            if (budgetLines == null)
                return records;

            foreach (var line in budgetLines)
            {
                if (line._Disable || String.IsNullOrEmpty(line._Account))
                    continue;

                foreach (var date in ExpandBudgetDates(line))
                {
                    if (date < fromDate || date > toDate)
                        continue;

                    records.Add(new SieBudgetRecord()
                    {
                        Account = line._Account,
                        Date = date,
                        Amount = (decimal)line._Amount,
                        Qty = (decimal)line._Qty,
                        Dimensions = GetDimensionValues(line._Dim1, line._Dim2, line._Dim3, line._Dim4, line._Dim5)
                    });
                }
            }

            return records;
        }

        /// <summary>
        /// The dates a budget line applies to: once at its date, or repeated according to its recurring method until its to-date
        /// </summary>
        private static IEnumerable<DateTime> ExpandBudgetDates(GLBudgetLineClient line)
        {
            var start = line._Date.Date;
            var end = line._ToDate == DateTime.MinValue ? start : line._ToDate.Date;

            if (line._Recurring == RecurringMethod.Never || end <= start)
            {
                yield return start;
                yield break;
            }

            var name = line._Recurring.ToString();
            int months = 0, days = 0;
            if (name.StartsWith("Month"))
                int.TryParse(name.Substring(5), out months);
            else if (name == "Week")
                days = 7;
            else if (name == "Week2")
                days = 14;

            if (months == 0 && days == 0)
            {
                yield return start;
                yield break;
            }

            for (var date = start; date <= end; date = months > 0 ? date.AddMonths(months) : date.AddDays(days))
                yield return date;
        }

        /// <summary>
        /// Converts Uniconta transactions to SIE verifications. Opening balance postings are not vouchers.
        /// Vouchers are written per series in ascending number order (11. #VER 7).
        /// </summary>
        private List<SIEVoucher> GetVer(List<GLTransClient> transactions, Dictionary<string, SIEAccount> kontos, Dictionary<string, SIEDimension> dims)
        {
            var vers = new List<SIEVoucher>();

            //Postings without a voucher number are grouped per day, so that unrelated postings are not merged
            var transactionsGroupedByVoucher = transactions
                .Where(t => !IsOpeningEntry(t))
                .GroupBy(t => new { Series = t.NumberSerie ?? String.Empty, t.Voucher, Day = t.Voucher == 0 ? t.Date.Date : DateTime.MinValue })
                .OrderBy(g => g.Key.Series, StringComparer.Ordinal)
                .ThenBy(g => g.Key.Voucher)
                .ThenBy(g => g.Key.Day)
                .ToList();

            //Vouchers without a number get running numbers after the highest number of their series
            var nextNumber = transactionsGroupedByVoucher
                .GroupBy(g => g.Key.Series)
                .ToDictionary(s => s.Key, s => s.Max(g => g.Key.Voucher) + 1);

            var fallbackSeries = SIEHelpers.GetFallbackSeries(nextNumber.Keys);

            foreach (var transGroup in transactionsGroupedByVoucher)
            {
                var rows = transGroup.OrderBy(t => t.Date).ThenBy(t => t._VoucherLine).ToList();
                var firstTransaction = rows.First();
                var firstTransactionJournalRef = GetPostedJournal(firstTransaction);

                var ver = new SIEVoucher();

                //Fallback series is picked if one is not present on the voucher
                ver.Series = transGroup.Key.Series.Length > 0 ? transGroup.Key.Series : fallbackSeries;

                ver.Number = (transGroup.Key.Voucher > 0 ? transGroup.Key.Voucher : nextNumber[transGroup.Key.Series]++).ToString();
                ver.VoucherDate = rows.Min(t => t.Date);
                ver.Text = rows.Select(t => t.Text).FirstOrDefault(t => !String.IsNullOrWhiteSpace(t)) ?? String.Empty;
                ver.CreatedDate = firstTransactionJournalRef?.Posted ?? DateTime.MinValue;
                ver.CreatedBy = firstTransactionJournalRef?.PostedBy;
                ver.Rows = GetTrans(rows, kontos, dims);
                ver.Token = "#VER";

                vers.Add(ver);
            }

            return vers;
        }

        /// <summary>
        /// Converts Uniconta transactions to SIE transactions
        /// </summary>
        private List<SIEVoucherRow> GetTrans(List<GLTransClient> voucherRows, Dictionary<string, SIEAccount> kontos, Dictionary<string, SIEDimension> dims)
        {
            var trans = new List<SIEVoucherRow>();

            foreach (var row in voucherRows)
            {
                var voucherRow = new SIEVoucherRow();
                voucherRow.Account = GetOrCreateKonto(kontos, row.Account);
                voucherRow.Objects = GetObjects(row, dims);
                voucherRow.Amount = Math.Round((decimal)row.Amount, 2, MidpointRounding.AwayFromZero);
                voucherRow.Quantity = row.Qty == 0 ? (decimal?)null : Math.Round((decimal)row.Qty, 2, MidpointRounding.AwayFromZero);
                voucherRow.RowDate = row.Date;
                voucherRow.Text = row.Text;
                voucherRow.CreatedBy = GetPostedJournal(row)?.PostedBy;
                voucherRow.Token = "#TRANS";

                trans.Add(voucherRow);
            }

            return trans;
        }

        /// <summary>
        /// Convert the dimension values of a transaction to SIE objects (see SIEHelpers.ToSieDimensionNumber)
        /// </summary>
        private List<SIEObject> GetObjects(GLTransClient transaction, Dictionary<string, SIEDimension> dims)
        {
            var objekts = new List<SIEObject>();
            var values = GetDimensionValues(transaction.Dimension1, transaction.Dimension2, transaction.Dimension3, transaction.Dimension4, transaction.Dimension5);

            for (int i = 0; i < values.Length; i++)
            {
                if (String.IsNullOrEmpty(values[i]))
                    continue;

                var dimNumber = SIEHelpers.ToSieDimensionNumber(i + 1).ToString();
                SIEDimension dim;
                if (!dims.TryGetValue(dimNumber, out dim))
                {
                    dim = new SIEDimension() { Number = dimNumber, Name = GetDimensionName(i + 1) };
                    dims.Add(dimNumber, dim);
                }

                SIEObject obj;
                if (!dim.Objects.TryGetValue(values[i], out obj))
                {
                    obj = new SIEObject() { Dimension = dim, Number = values[i], Name = values[i] };
                    dim.Objects.Add(values[i], obj);
                }

                objekts.Add(obj);
            }

            return objekts;
        }

        private static SIEAccount GetOrCreateKonto(Dictionary<string, SIEAccount> kontos, string accountNumber)
        {
            SIEAccount konto;
            if (!kontos.TryGetValue(accountNumber, out konto))
            {
                konto = new SIEAccount() { Number = accountNumber, Name = "" };
                kontos.Add(accountNumber, konto);
            }
            return konto;
        }

        /// <summary>
        /// Converts Uniconta GL accounts to SIE kontos
        /// </summary>
        private Dictionary<string, SIEAccount> GetKonto()
        {
            var missingSru = false;
            var nonNumeric = new List<string>();
            var kontos = new Dictionary<string, SIEAccount>();

            foreach (var account in _accounts)
            {
                //Do not export header and total accounts
                if (account.AccountTypeEnum <= GLAccountTypes.CalculationExpression)
                    continue;

                if (String.IsNullOrEmpty(account.Account))
                    continue;

                //Account numbers are to be numeric (11. #KONTO 2)
                if (!account.Account.All(char.IsDigit))
                    nonNumeric.Add(account.Account);

                var sieAccount = new SIEAccount()
                {
                    Name = account.Name,
                    Number = account.Account,
                    Type = SIEHelpers.ToSieAccountType(account.AccountTypeEnum)
                };

                var sru = GetSRU(account);
                if (!String.IsNullOrEmpty(sru))
                    sieAccount.SRU.Add(sru);
                else
                    missingSru = true;

                kontos[account.Account] = sieAccount;
            }

            if (missingSru)
                UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("SIEMissingSRU"), Uniconta.ClientTools.Localization.lookup("Warning"));

            if (nonNumeric.Any())
                UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("SIENonNumericAccounts"), string.Join(", ", nonNumeric.Take(20))), Uniconta.ClientTools.Localization.lookup("Warning"));

            return kontos;
        }

        private bool? _hasSruField;

        /// <summary>
        /// The SRU code is kept in a user defined field "SRU" on the account; without the field no #SRU is written
        /// </summary>
        private string GetSRU(GLAccountClient account)
        {
            if (_hasSruField == null)
                _hasSruField = SIEAccountFieldsHelpers.HasSRUField(account);

            return _hasSruField.Value ? SIEAccountFieldsHelpers.GetSRU(account) : null;
        }

        /// <summary>
        /// Financial years for #RAR: year 0 is the financial year of the to-date, year -1 the previous one.
        /// Calendar years are used when the company has no financial year for the date.
        /// </summary>
        private Dictionary<int, SIEBookingYear> GetYears(DateTime cutOff)
        {
            var years = new Dictionary<int, SIEBookingYear>();

            var financeYears = (_accountingYears ?? Enumerable.Empty<CompanyFinanceYearClient>()).OrderBy(y => y.FromDate).ToList();

            var current = financeYears.FirstOrDefault(y => y.FromDate.Date <= cutOff && y.ToDate.Date >= cutOff);
            if (current != null)
            {
                years[0] = new SIEBookingYear() { ID = 0, Start = current.FromDate.Date, End = current.ToDate.Date };

                var previous = financeYears.LastOrDefault(y => y.ToDate.Date < current.FromDate.Date);
                if (previous != null)
                    years[-1] = new SIEBookingYear() { ID = -1, Start = previous.FromDate.Date, End = previous.ToDate.Date };
            }
            else
            {
                years[0] = new SIEBookingYear() { ID = 0, Start = new DateTime(cutOff.Year, 1, 1), End = new DateTime(cutOff.Year, 12, 31) };
                years[-1] = new SIEBookingYear() { ID = -1, Start = new DateTime(cutOff.Year - 1, 1, 1), End = new DateTime(cutOff.Year - 1, 12, 31) };
            }

            return years;
        }

        /// <summary>
        /// Collects company information
        /// </summary>
        private SIECompany GetFnamn(string companyType)
        {
            var companyEntity = api.CompanyEntity;

            var company = new SIECompany();
            company.Name = companyEntity.Name;
            company.Code = companyEntity.CompanyId.ToString();
            company.OrgIdentifier = SIEHelpers.FormatOrgNumber(!String.IsNullOrWhiteSpace(companyEntity._Id) ? companyEntity._Id : companyEntity._VatNumber);
            company.OrgType = companyType;
            company.Phone = companyEntity._Phone;
            company.SNI = SIEHelpers.FormatSniCode(companyEntity._IndustryCode);
            company.Street = companyEntity._Address1;
            company.ZipCity = $"{companyEntity._Address2} {companyEntity._Address3}".Trim();

            return company;
        }

        private string GetDimensionName(int dimension)
        {
            string name = null;
            switch (dimension)
            {
                case 1: name = api.CompanyEntity._Dim1; break;
                case 2: name = api.CompanyEntity._Dim2; break;
                case 3: name = api.CompanyEntity._Dim3; break;
                case 4: name = api.CompanyEntity._Dim4; break;
                case 5: name = api.CompanyEntity._Dim5; break;
            }

            return String.IsNullOrWhiteSpace(name) ? $"Dimension {dimension}" : name;
        }

        /// <summary>
        /// Converts Uniconta dimensions to SIE dimensions (see SIEHelpers.ToSieDimensionNumber)
        /// </summary>
        private async Task<Dictionary<string, SIEDimension>> GetDimsAsync()
        {
            var dims = new Dictionary<string, SIEDimension>();

            for (int i = 1; i <= api.CompanyEntity.NumberOfDimensions; i++)
            {
                IEnumerable<GLDimType> dimensions = null;

                if (i == 1)
                    dimensions = await api.LoadCache<GLDimType1Client>();
                else if (i == 2)
                    dimensions = await api.LoadCache<GLDimType2Client>();
                else if (i == 3)
                    dimensions = await api.LoadCache<GLDimType3Client>();
                else if (i == 4)
                    dimensions = await api.LoadCache<GLDimType4Client>();
                else if (i == 5)
                    dimensions = await api.LoadCache<GLDimType5Client>();

                var dimension = new SIEDimension();
                dimension.IsDefault = false;
                dimension.Name = GetDimensionName(i);
                dimension.Number = SIEHelpers.ToSieDimensionNumber(i).ToString();

                foreach (var obj in dimensions ?? Enumerable.Empty<GLDimType>())
                {
                    if (String.IsNullOrEmpty(obj.KeyStr))
                        continue;

                    dimension.Objects[obj.KeyStr] = new SIEObject()
                    {
                        Dimension = dimension,
                        Number = obj.KeyStr,
                        Name = obj.KeyName
                    };
                }

                dims.Add(dimension.Number, dimension);
            }

            return dims;
        }

        #endregion
    }
}
