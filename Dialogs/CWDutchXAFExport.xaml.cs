using DevExpress.Xpf.Data.Native;
using DevExpress.Xpf.WindowsUI.Navigation;
using DevExpress.XtraPrinting.Native;
using NPOI.Util;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Xml;
using System.Xml.Serialization;
using Uniconta.API.System;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.Common;
using Uniconta.DataModel;
using UnicontaClient.Controls.Dialogs.DutchXafExport;
using UnicontaClient.Controls.Dialogs.DutchXafExport.Model;
using UnicontaClient.Pages;

namespace UnicontaClient.Controls.Dialogs
{
    /// <summary>
    /// Interaction logic for CWDutchXAFExport.xaml
    /// </summary>
    public partial class CWDutchXAFExport : Uniconta.ClientTools.ChildWindow
    {
        private CrudAPI _api;

        private CWDutchXAFExportViewModel ViewModel => DataContext as CWDutchXAFExportViewModel;

        public CWDutchXAFExport()
        {
            InitializeComponent();
            DataContext = new CWDutchXAFExportViewModel();
        }

        private void BrowseXmlFile_Click(object sender, RoutedEventArgs e)
        {
            var vm = ViewModel;
            if (vm == null)
                return;

            var dlg = new System.Windows.Forms.SaveFileDialog
            {
                Title = "XAF output file",
                Filter = "Auditfile (*.xaf)|*.xaf|All files (*.*)|*.*",
                FileName = Path.GetFileName(vm.XmlFileName),
                InitialDirectory = GetInitialDirectory(vm.XmlFileName),
                OverwritePrompt = false
            };

            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                vm.XmlFileName = dlg.FileName;
        }

        //private void BrowseExcelFile_Click(object sender, RoutedEventArgs e)
        //{
        //    var vm = ViewModel;
        //    if (vm == null)
        //        return;
        //
        //    var dlg = new System.Windows.Forms.SaveFileDialog
        //    {
        //        Title = "Select Excel output file",
        //        Filter = "Excel (*.xlsx)|*.xlsx|All files (*.*)|*.*",
        //        FileName = Path.GetFileName(vm.ExcelFileName),
        //        InitialDirectory = GetInitialDirectory(vm.ExcelFileName),
        //        OverwritePrompt = false
        //    };
        //
        //    if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        //        vm.ExcelFileName = dlg.FileName;
        //}

        private static string GetInitialDirectory(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            var dir = Path.GetDirectoryName(fileName);
            return !string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir)
                ? dir
                : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        }

        private async void OKButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel == null)
                return;

            var existingFiles = new List<string>();

            if (!string.IsNullOrWhiteSpace(ViewModel.XmlFileName) && File.Exists(ViewModel.XmlFileName))
                existingFiles.Add(ViewModel.XmlFileName);

            //if (!string.IsNullOrWhiteSpace(vm.ExcelFileName) && File.Exists(vm.ExcelFileName))
            //    existingFiles.Add(vm.ExcelFileName);

            if (existingFiles.Count > 0)
            {
                var message = "The following file(s) already exist:\n\n"
                              + string.Join("\n", existingFiles)
                              + "\n\nDo you want to overwrite them?";

                var result = UnicontaMessageBox.Show(
                    message,
                    "Files already exist",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                    return;
            }

            // Copy everything needed before closing the dialog
            var fromDate = ViewModel.FromDate;
            var toDate = ViewModel.ToDate;
            var xmlFileName = ViewModel.XmlFileName;
            var api = _api;

            var journalDict = ViewModel.JournalExportCodes
                .Where(x => !string.IsNullOrWhiteSpace(x.Journal) &&
                            !string.IsNullOrWhiteSpace(x.ExportCode))
                .GroupBy(x => x.Journal)
                .ToDictionary(g => g.Key, g => g.Last().ExportCode);

            SetDialogResult(true);

            try
            {
                var builder = new CWDutchXAFModelBuilder(new CWDutchXAFModelBuilder.BuilderParms
                {
                    FromDate = fromDate,
                    ToDate = toDate,
                    Api = api,
                    Taxonomy = false,
                    JournalTypeDict = journalDict
                });

                if (!string.IsNullOrEmpty(xmlFileName))
                {
                    var model = await builder.BuildAuditfile();

                    using (var stream = new MemoryStream())
                    {
                        var settings = new XmlWriterSettings
                        {
                            Indent = true,
                            Async = false,
                            CloseOutput = false
                        };

                        var namespaces = new XmlSerializerNamespaces();
                        namespaces.Add(string.Empty, XmlAuditfileNamespaces.Namespace);
                        namespaces.Add("xsi", "http://www.w3.org/2001/XMLSchema-instance");

                        var serializer = new XmlSerializer(typeof(Auditfile));

                        using (var writer = XmlWriter.Create(stream, settings))
                        {
                            serializer.Serialize(writer, model, namespaces);
                        }

                        stream.Position = 0;

                        using (var fileStream = File.Create(xmlFileName))
                        {
                            await stream.CopyToAsync(fileStream);
                        }
                    }

                    Uniconta.ClientTools.InfoLog.Log.Info($"Export completed: '{xmlFileName}'");

                }
            }
            catch (Exception ex)
            {
                Uniconta.ClientTools.InfoLog.Log.Error("Export failed");
                Uniconta.ClientTools.InfoLog.Log.Error(ex);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e) => SetDialogResult(false);

        private void ChildWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                CancelButton_Click(sender, null);
            else if (e.Key == Key.Enter)
                OKButton_Click(sender, null);
        }

        public static void LoadAndShow(CrudAPI api)
        {
            try
            {
                var window = new CWDutchXAFExport
                {
                    _api = api
                };
                window.ViewModel.LoadAsync(api);
                window.Show();
            }
            catch (Exception ex)
            {
                Uniconta.ClientTools.InfoLog.Log.Error(ex);
            }
        }
    }

    public class CWDutchXAFExportViewModel
    {
        public string XmlFileName { get; set; }
        //public string ExcelFileName { get; set; }

        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        public List<JournalExportCodeLine> JournalExportCodes { get; set; }

        internal void LoadAsync(CrudAPI api)
        {
            var journals = api.LoadCache<GLDailyJournalClient>().Result;

            JournalExportCodes = journals.Select(j => new JournalExportCodeLine
            {
                Journal = j.Journal
            }).ToList();

            var lastYear = DateTime.Today.Year - 1;
            var fromDate = new DateTime(lastYear, 1, 1);
            var toDate = new DateTime(lastYear, 12, 31);

            var documentsFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var xmlFileName = Path.Combine(documentsFolder, $"{api.CompanyEntity.Name}_{lastYear}_xaf4_0.xaf");

            XmlFileName = xmlFileName;
            //ExcelFileName = Path.ChangeExtension(xmlFileName, ".xlsx");
            FromDate = fromDate;
            ToDate = toDate;
        }

        public sealed class JournalExportCodeLine
        {
            public string Journal { get; set; }
            public string ExportCode { get; set; }
        }
    }

    public static class APIExtensions
    {
        public static Task QueryPerMonthAsync<T>(this CrudAPI crudApi, string dateField, DateTime startDate, DateTime endDate, Action<T[]> thingToDo)
            where T : class, UnicontaBaseEntity, new()
        {
            if (thingToDo == null)
                throw new ArgumentNullException(nameof(thingToDo));

            return QueryPerMonthAsync<T>(
                crudApi,
                dateField,
                startDate,
                endDate,
                result =>
                {
                    thingToDo(result);
                    return Task.CompletedTask;
                });
        }

        public static async Task QueryPerMonthAsync<T>(this CrudAPI crudApi, string dateField, DateTime startDate, DateTime endDate, Func<T[], Task> thingToDo)
            where T : class, UnicontaBaseEntity, new()
        {
            if (crudApi == null)
                throw new ArgumentNullException(nameof(crudApi));

            if (string.IsNullOrWhiteSpace(dateField))
                throw new ArgumentNullException(nameof(dateField));

            if (thingToDo == null)
                throw new ArgumentNullException(nameof(thingToDo));

            await ForEachMonthAsync(startDate, endDate, async (rangeStart, rangeEnd) =>
            {
                var result = await QueryRangeAsync<T>(crudApi, dateField, rangeStart, rangeEnd).ConfigureAwait(false);
                await thingToDo(result).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        public static async Task QueryPerMonthAsync<T1, T2>(this CrudAPI crudApi, string dateField1, string dateField2, DateTime startDate, DateTime endDate, Action<T1[], T2[], DateTime, DateTime> thingToDo)
            where T1 : class, UnicontaBaseEntity, new()
            where T2 : class, UnicontaBaseEntity, new()
        {
            if (crudApi == null)
                throw new ArgumentNullException(nameof(crudApi));

            if (string.IsNullOrWhiteSpace(dateField1))
                throw new ArgumentNullException(nameof(dateField1));

            if (string.IsNullOrWhiteSpace(dateField2))
                throw new ArgumentNullException(nameof(dateField2));

            if (thingToDo == null)
                throw new ArgumentNullException(nameof(thingToDo));

            await ForEachMonthAsync(startDate, endDate, async (rangeStart, rangeEnd) =>
            {
                var task1 = QueryRangeAsync<T1>(crudApi, dateField1, rangeStart, rangeEnd);
                var task2 = QueryRangeAsync<T2>(crudApi, dateField2, rangeStart, rangeEnd);

                await Task.WhenAll(task1, task2).ConfigureAwait(false);

                thingToDo(task1.Result, task2.Result, rangeStart, rangeEnd);
            }).ConfigureAwait(false);
        }

        private static async Task ForEachMonthAsync(DateTime startDate, DateTime endDate, Func<DateTime, DateTime, Task> perMonthAsync)
        {
            if (perMonthAsync == null)
                throw new ArgumentNullException(nameof(perMonthAsync));

            if (endDate < startDate)
                return;

            var currentMonthStart = new DateTime(startDate.Year, startDate.Month, 1);
            var finalDate = endDate.Date;

            while (currentMonthStart <= finalDate)
            {
                var currentMonthEnd = new DateTime(
                    currentMonthStart.Year,
                    currentMonthStart.Month,
                    DateTime.DaysInMonth(currentMonthStart.Year, currentMonthStart.Month));

                var rangeStart = currentMonthStart < startDate.Date ? startDate.Date : currentMonthStart;
                var rangeEnd = currentMonthEnd > finalDate ? finalDate : currentMonthEnd;

                await perMonthAsync(rangeStart, rangeEnd).ConfigureAwait(false);

                currentMonthStart = currentMonthStart.AddMonths(1);
            }
        }

        public static async Task<T[]> QueryRangeAsync<T>(this CrudAPI crudApi, string dateField, DateTime rangeStart, DateTime rangeEnd)
            where T : class, UnicontaBaseEntity, new()
        {
            var entity = new T();

            var filter = new List<PropValuePair>
            {
                PropValuePair.GenereteWhereElements(dateField, rangeStart.AddSeconds(-1), CompareOperator.GreaterThan, typeof(DateTime)),
                PropValuePair.GenereteWhereElements(dateField, rangeEnd.AddDays(1), CompareOperator.LessThan, typeof(DateTime))
                //PropValuePair.GenereteWhereElements(
                  //  dateField,
                    //typeof(DateTime),
                    //string.Format("{0:yyyy-MM-dd}..{1:yyyy-MM-dd}", rangeStart, rangeEnd))
            };




            var result = await crudApi.Query(entity, null, filter).ConfigureAwait(false);
            return result ?? new T[0];
        }
    }
}

namespace UnicontaClient.Controls.Dialogs.DutchXafExport
{

    using UnicontaClient.Controls.Dialogs.DutchXafExport.Model;

    public class CWDutchXAFModelBuilder
    {
        private readonly BuilderParms Parms;



        public CWDutchXAFModelBuilder(BuilderParms parms)
        {
            Parms = parms;
            //Version = AuditFileCreator.AuditFileVersion.V4_0;
            Parms = parms;
        }

        ///// <summary>
        ///// Preserves the original interface contract by writing XML to the provided stream,
        ///// but internally builds the strongly typed XmlSerializer model first.
        ///// </summary>
        //public async Task WriteAuditFileXml(Stream outputStream)
        //{
        //    var model = await BuildAuditfile();
        //    Serialize(outputStream, model);
        //}

        ///// <summary>
        ///// Writes an Excel-compatible workbook (SpreadsheetML 2003 XML) to the provided stream.
        ///// Every major auditfile node is written to its own worksheet and a totals sheet is included.
        ///// </summary>
        //public async Task WriteAuditFileExcel(Stream outputStream)
        //{
        //    var model = await BuildAuditfile();
        //    WriteAuditFileExcel(outputStream, model);
        //}

        /// <summary>
        /// Builds the strongly typed auditfile model.
        /// </summary>
        public async Task<Auditfile> BuildAuditfile()
        {
            await EnsureContextLoaded();

            return new Auditfile
            {
                Header = BuildHeader(),
                Company = await BuildCompany()
            };
        }

        //public void Serialize(Stream outputStream, Auditfile model)
        //    => new AuditfileModelWriter4_0().WriteXml(outputStream, model);

        //public void WriteAuditFileExcel(Stream outputStream, Auditfile model)
            //=> new AuditfileModelWriter4_0().WriteExcel(outputStream, model);

        private async Task EnsureContextLoaded()
        {
            if (Parms.Api == null)
                throw new InvalidOperationException("CrudApi must be assigned before building the auditfile.");

            await Parms.Api.LoadCache<CreditorClient>(true);
            await Parms.Api.LoadCache<DebtorOrderClient>(true);
        }

        protected virtual AuditfileHeader BuildHeader()
        {
            var fiscalYearText = Parms.FromDate.Year != Parms.ToDate.Year
                ? string.Format(CultureInfo.InvariantCulture, "{0}-{1}", Parms.FromDate.Year, Parms.ToDate.Year)
                : Parms.FromDate.Year.ToString(CultureInfo.InvariantCulture);

            return new AuditfileHeader
            {
                FiscalYear = fiscalYearText,
                StartDate = Parms.FromDate.Date,
                EndDate = Parms.ToDate.Date,
                CurCode = ToIso4217(Parms.CurrentCompany._CurrencyId),
                DateCreated = DateTime.Now.Date,
                SoftwareDesc = "Uniconta",
                SoftwareVersion = Parms.Api.session.ServerVersion.ToString(),
                RgsVersion = "RGS-1"
            };
        }

        private async Task<AuditfileCompany> BuildCompany()
        {
            return new AuditfileCompany
            {
                Commercenr = NullIfDashOrEmpty(Parms.CurrentCompany._Duns, "-"),
                CompanyName = Parms.CurrentCompany.Name,
                TaxRegistrationCountry = SafeToString(Parms.CurrentCompany._CountryId),
                TaxRegIdent = NullIfDashOrEmpty(Parms.CurrentCompany._VatNumber, "-"),
                StreetAddress = BuildCompanyStreetAddresses(),
                CustomersSuppliers = await BuildCustomersSuppliers(),
                GeneralLedger = await BuildGeneralLedger(),
                VatCodes = await BuildVatCodes(),
                Periods = BuildPeriods(),
                OpeningBalance = await BuildOpeningBalance(),
                Transactions = await BuildTransactions()
            };
        }

        protected virtual List<StreetAddress> BuildCompanyStreetAddresses()
            => new List<StreetAddress>
            {
                new StreetAddress
                {
                    Streetname = Parms.CurrentCompany._Address1,
                    PostalCode = Left(Parms.CurrentCompany._Address2, 10),
                    City = Parms.CurrentCompany._Address3,
                    Country = SafeToString(Parms.CurrentCompany._CountryId)
                }
            };

        private async Task<HashSet<string>> GetDCAccounts<T>()
            where T : DCTransClient, UnicontaBaseEntity, new()
        {
            var distinctAccounts = new HashSet<string>();

            await Parms.Api.QueryPerMonthAsync<T>(nameof(DCTransClient.Date), Parms.FromDate, Parms.ToDate, dataPerMonth =>
            {
                foreach (var row in dataPerMonth)
                {
                    if (!string.IsNullOrWhiteSpace(row._Account))
                        distinctAccounts.Add(row._Account);
                }
            });

            return distinctAccounts;
        }


        private async Task<CustomersSuppliers> BuildCustomersSuppliers()
        {
            var retVal = new CustomersSuppliers
            {
                CustomerSupplier = new List<CustomerSupplier>()
            };

            await BuildDC<DebtorClient>(retVal, await GetDCAccounts<DebtorTransClient>(), await GetStartEndBalances<DebtorTotalClient>());
            await BuildDC<CreditorClient>(retVal, await GetDCAccounts<CreditorTransClient>(), await GetStartEndBalances<CreditorTotalClient>());

            return retVal.CustomerSupplier.Count > 0 ? retVal : null;
        }

        private async Task<Dictionary<string, (double BalanceStart, double BalanceEnd)>> GetStartEndBalances<T>()
            where T : DCAgeTotalClient, UnicontaBaseEntity, new()
        {
            var startBalances = await GetTotals<T>(Parms.FromDate.AddDays(-1));
            var endBalances = await GetTotals<T>(Parms.ToDate);

            var result = new Dictionary<string, (double BalanceStart, double BalanceEnd)>();

            foreach (var row in startBalances)
            {
                result[row._Account] = (row.Amount0, 0d);
            }

            foreach (var row in endBalances)
            {
                if (result.TryGetValue(row._Account, out var existing))
                    result[row._Account] = (existing.BalanceStart, row.Amount0);
                else
                    result[row._Account] = (0d, row.Amount0);
            }

            return result;
        }

        private async Task<IEnumerable<T>> GetTotals<T>(DateTime dateTime)
            where T : DCAgeTotalClient, UnicontaBaseEntity, new()
        {
            var entity = new T();
            var filters = new List<PropValuePair>
            {
                PropValuePair.GenereteParameter("PrDate", typeof(DateTime), String.Format("{0:d}", dateTime)),
                PropValuePair.GenereteParameter("Interval", typeof(Int32), "1"),  // TODO: validate
                PropValuePair.GenereteParameter("Count", typeof(Int32), "1")
            };

            return await Parms.Api.Query(entity, null, filters);
        }

        private async Task BuildDC<T>(CustomersSuppliers result, HashSet<string> accountNumbersWithTransactions, Dictionary<string, (double BalanceStart, double BalanceEnd)> totals)
            where T : DCAccount, UnicontaBaseEntity, IdKey, new()
        {
            var dcType = typeof(T) == typeof(DebtorClient) ? CustomerSupplierCode.C : CustomerSupplierCode.S;
            var creditType = typeof(T) == typeof(DebtorClient) ? DebitCreditType.C : DebitCreditType.D;
            var debitType = typeof(T) == typeof(DebtorClient) ? DebitCreditType.D : DebitCreditType.C;

            if (accountNumbersWithTransactions.Any())
            {
                var allDcAccounts = await Parms.Api.LoadCache<T>();
                var dcAccountsWithTransactions = allDcAccounts.Where(c => accountNumbersWithTransactions.Contains(c._Account)).ToArray();

                foreach (var account in dcAccountsWithTransactions)
                {
                    (double opBal, double ClBal) total;

                    totals.TryGetValue(account._Account, out total);

                    SignedAmount opening = SignedAmount.From(total.opBal);
                    SignedAmount closing = SignedAmount.From(total.ClBal);

                    result.CustomerSupplier.Add(new CustomerSupplier
                    {
                        CustSupId = account._Account,
                        CustSupName = account._Name,
                        EMail = account._ContactEmail,
                        CommerceNr = NullIfDashOrEmpty(account._LegalIdent, "-"),
                        TaxRegistrationCountry = SafeToString(account._Country),
                        TaxRegIdent = NullIfDashOrEmpty(account._VatNumber, "-"),
                        CustSupTp = dcType,
                        CustSupTpSpecified = true,
                        OpBalDesc = opening.Amount,
                        OpBalDescSpecified = true,
                        OpBalTp = opening.IsDebit ? debitType : creditType,
                        OpBalTpSpecified = true,
                        ClBalDesc = closing.Amount,
                        ClBalDescSpecified = true,
                        ClBalTp = closing.IsCredit ? creditType : debitType,
                        ClBalTpSpecified = true,
                        StreetAddress = new List<StreetAddress>
                        {
                            new StreetAddress
                            {
                                Streetname = account.Address,
                                City = account._City,
                                PostalCode = account._ZipCode,
                                Country = SafeToString((account._Country))
                            }
                        }
                    });
                }
            }
        }

        private async Task<GeneralLedger> BuildGeneralLedger()
        {
            var accounts = await Parms.Api.LoadCache<GLAccountClient>();
            if (accounts == null || accounts.Count == 0)
                return null;

            return new GeneralLedger
            {
                LedgerAccount = accounts.Select(account => new LedgerAccount
                {
                    AccId = account._Account,
                    AccDesc = account._Name,
                    AccTp = ConvertAccountType(account.AccountTypeEnum),
                    RgsCode = string.IsNullOrWhiteSpace(account.ExternalNo) ? "0000" : account.ExternalNo
                }).ToList()
            };
        }

        private async Task<VatCodes> BuildVatCodes()
        {
            var vatCodes = await Parms.Api.LoadCache<GLVatClient>();
            if (vatCodes == null || vatCodes.Count == 0)
                return null;

            return new VatCodes
            {
                VatCode = vatCodes.Select(code => new VatCode
                {
                    VatId = code._Vat,
                    VatDesc = code._Name,
                    VatToPayAccId = code.Vat,
                    VatToClaimAccId = code.Vat
                }).ToList()
            };
        }

        protected virtual Periods BuildPeriods()
        {
            var period = new DateTime(Parms.FromDate.Year, Parms.FromDate.Month, 1);
            var periods = new List<Period>();

            while (period <= Parms.ToDate)
            {
                periods.Add(new Period
                {
                    PeriodNumber = period.Month,
                    StartDatePeriod = period.Date,
                    EndDatePeriod = new DateTime(period.Year, period.Month, DateTime.DaysInMonth(period.Year, period.Month))
                });

                // Legacy writer emitted periodDesc, but the current XSD/model does not contain that field.
                // Intentionally omitted: mfi.GetMonthName(period.Month) + " " + period.Year
                period = period.AddMonths(1);
            }

            return periods.Count > 0
                ? new Periods { Period = periods }
                : null;
        }

        private async Task<OpeningBalance> BuildOpeningBalance()
        {
            var transactions = await Parms.Api.Query<GLTransClient>(new List<PropValuePair>
            {
                PropValuePair.GenereteWhereElements(
                    nameof(GLTransClient.Date),
                    typeof(DateTime),
                    string.Format("{0:yyyy-MM-dd}", Parms.FromDate))
            });

            var lines = transactions
                .Where(t => t.Date == Parms.FromDate
                    && t._Origin.IsDutchOpeningBalance());

            var retVal = new OpeningBalance
            {
                ObLine = new List<OpeningBalanceLine>()
            };

            var lineNo = 1;
            foreach (var trans in lines)
            {
                var signedAmount = SignedAmount.From(trans.Amount);

                retVal.ObLine.Add(new OpeningBalanceLine
                {
                    Nr = SafeToString(lineNo),
                    AccId = trans.Account,
                    Amnt = signedAmount.Amount,
                    AmntTp = signedAmount.DebitCreditType
                });

                lineNo++;

                if (signedAmount.IsDebit)
                    retVal.TotalDebit += signedAmount.Amount;

                if (signedAmount.IsCredit)
                    retVal.TotalCredit += signedAmount.Amount;

            }

            retVal.LinesCount = lineNo;

            return retVal;
        }
        private async Task<Transactions> BuildTransactions()
        {
            var retVal = new Transactions
            {
                Journal = new List<Journal>()
            };

            var transactionsWithoutJournal = new List<GLTransClient>();

            int lines = 0;

            await Parms.Api.QueryPerMonthAsync<GLTransClient, GLDailyJournalPostedClient>(
                nameof(GLTransClient.Date),
                nameof(GLDailyJournalPostedClient.Posted),
                Parms.FromDate,
                Parms.ToDate,
                (glTransMonthData, glJournalMonthData, fromDate, toDate) =>
                {
                    var filteredTransactions = glTransMonthData
                        .Where(t => !t._Origin.IsDutchOpeningBalance())
                        .ToList();

                    var postedJournals = glJournalMonthData?.ToList() ?? new List<GLDailyJournalPostedClient>();
                    var matchedJournalIds = new HashSet<long>();

                    foreach (var journal in postedJournals)
                    {
                        matchedJournalIds.Add(journal.JournalPostedId);

                        var journalTransactions = filteredTransactions
                            .Where(t => t.JournalPostedId == journal.JournalPostedId)
                            .ToList();

                        var mappedJournal = BuildJournal(
                            journalTransactions,
                            CreateJournalHeader(journal),
                            retVal,
                            ref lines);

                        if (mappedJournal != null)
                            retVal.Journal.Add(mappedJournal);
                    }

                    var unmatchedTransactions = filteredTransactions
                        .Where(t => t.JournalPostedId == 0 || !matchedJournalIds.Contains(t.JournalPostedId))
                        .ToList();

                    if (unmatchedTransactions?.Count > 0)
                    {
                        transactionsWithoutJournal.AddRange(unmatchedTransactions);
                    }
                });

            if (transactionsWithoutJournal.Count > 0)
            {
                var fallbackJournals = transactionsWithoutJournal
                    .GroupBy(t => t.JournalPostedId)
                    .Select(g => new
                    {
                        Journal = BuildJournal(g.ToList(),
                                               CreateFallbackJournalHeader(g.Key.ToString()),
                                               retVal,
                                               ref lines)
                    })
                    .Where(x => x.Journal != null)
                    .Select(x => x.Journal)
                    .ToList();

                retVal.Journal.AddRange(fallbackJournals);
            }

            retVal.LinesCount = lines;
            return retVal;
        }

        private Journal BuildJournal(
            List<GLTransClient> sourceLines,
            Journal journal,
            Transactions totals,
            ref int lines)
        {
            if (sourceLines == null || sourceLines.Count == 0)
                return null;

            journal.Transaction = new List<Transaction>();

            var transactionGroups = sourceLines
                .GroupBy(t => (Voucher: t.Voucher, Text: t.Text, Date: t.Date.Date));

            foreach (var group in transactionGroups)
            {
                var mappedTransaction = BuildTransaction(group, totals, ref lines);

                if (mappedTransaction.TrLine.Count > 0)
                    journal.Transaction.Add(mappedTransaction);
            }

            return journal.Transaction.Count > 0 ? journal : null;
        }

        private Transaction BuildTransaction(
            IGrouping<(int Voucher, string Text, DateTime Date), GLTransClient> group,
            Transactions totals,
            ref int lines)
        {
            var mappedTransaction = new Transaction
            {
                Nr = SafeToString(group.Key.Voucher),
                Desc = group.Key.Text,
                PeriodNumber = group.Key.Date.Month,
                TrDt = group.Key.Date,
                TrLine = new List<TransactionLine>()
            };

            int groupLineNo = 1;

            foreach (var line in group)
            {
                var signedAmount = SignedAmount.From(line.Amount);

                mappedTransaction.TrLine.Add(new TransactionLine
                {
                    Nr = groupLineNo.ToString(CultureInfo.InvariantCulture),
                    AccId = line.Account,
                    DocRef = SafeToString(line.DocumentRef),
                    EffDate = line.Date.Date,
                    SettDate = line.Date.Date,
                    SettDateSpecified = true,
                    Desc = line.Text,
                    Amnt = signedAmount.Amount,
                    AmntTp = signedAmount.DebitCreditType,
                    CustSupId = line.Account,
                    InvRef = SafeToString(line.Invoice)
                });

                groupLineNo++;
                lines++;

                if (signedAmount.IsDebit)
                    totals.TotalDebit += signedAmount.Amount;

                if (signedAmount.IsCredit)
                    totals.TotalCredit += signedAmount.Amount;
            }

            return mappedTransaction;
        }

        private Journal CreateJournalHeader(GLDailyJournalPostedClient journal)
        {
            return new Journal
            {
                JrnId = journal.JournalPostedId.ToString(CultureInfo.InvariantCulture),
                Desc = journal.NumberSerie,
                JrnTp = GetJournalType(journal),
                JrnTpSpecified = true,
                OffsetAccId = "9999"
            };
        }

        private Journal CreateFallbackJournalHeader(string journalId)
        {
            return new Journal
            {
                JrnId = journalId, 
                Desc = "Transactions without matching JournalPostedId",
                JrnTp = JournalType.Z,
                JrnTpSpecified = true,
                OffsetAccId = "9999"
            };
        }

        private static AccountType ConvertAccountType(GLAccountTypes accountType) => accountType == GLAccountTypes.BalanceSheet ? AccountType.B : AccountType.P;

        private JournalType GetJournalType(GLDailyJournalPostedClient postedJournal)
        {
            switch (postedJournal._ReferenceType)
            {
                case GLDailyJournalPostedReference.External:
                    return JournalType.Z;

                case GLDailyJournalPostedReference.DailyJournal:
                    {
                        var journalType = !string.IsNullOrEmpty(postedJournal.ReferenceName)
                                          && Parms.JournalTypeDict != null
                                          && Parms.JournalTypeDict.ContainsKey(postedJournal.ReferenceName)
                            ? Parms.JournalTypeDict[postedJournal.ReferenceName]
                            : JournalType.Z.ToString();

                        return Enum.TryParse<JournalType>(journalType, out var res) ? res : JournalType.M;
                    }

                case GLDailyJournalPostedReference.DebtorInvoice:
                    return JournalType.P;

                case GLDailyJournalPostedReference.CreditorInvoice:
                    return JournalType.S;

                case GLDailyJournalPostedReference.ClosingSheet:
                    return JournalType.M;

                case GLDailyJournalPostedReference.InventoryJournal:
                    return JournalType.Z;

                case GLDailyJournalPostedReference.ProjectJournal:
                    return JournalType.Z;

                case GLDailyJournalPostedReference.RestateInventory:
                    return JournalType.Z;

                case GLDailyJournalPostedReference.Production:
                    return JournalType.T;

                case GLDailyJournalPostedReference.TimeRegistration:
                    return JournalType.Z;

                case GLDailyJournalPostedReference.FixedAsset:
                    return JournalType.Z;

                case GLDailyJournalPostedReference.ProjectWIP:
                    return JournalType.Z;

                case GLDailyJournalPostedReference.ProjectOrder:
                    return JournalType.Z;

                default:
                    throw new ArgumentOutOfRangeException("postedJournal");
            }
        }

        protected static string Left(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }

        protected static string NullIfDashOrEmpty(string value, string legacyFallback)
        {
            if (string.IsNullOrWhiteSpace(value))
                return legacyFallback;

            return value;
        }

        private static string SafeToString(object value) => value == null ? null : Convert.ToString(value, CultureInfo.InvariantCulture);

        /// <summary>
        /// Converts the custom Currencies enum to a current ISO 4217 currency code.
        /// Throws when the value is custom, crypto, or cannot be mapped safely.
        /// </summary>
        public static string ToIso4217(Currencies value)
        {
            switch (value)
            {
                // No currency
                case Currencies.XXX:
                    return "XXX";

                // Historic -> current replacements
                case Currencies.AFA: return "AFN";
                case Currencies.AOK: return "AOA";
                case Currencies.AON: return "AOA";
                case Currencies.AOR: return "AOA";
                case Currencies.BAD: return "BAM";
                case Currencies.BTR: return "BTN";
                case Currencies.BYR: return "BYN";
                case Currencies.ECS: return "USD";
                case Currencies.GHC: return "GHS";
                case Currencies.HRK: return "EUR";
                case Currencies.KTS: return "PGK";
                case Currencies.MGF: return "MGA";
                case Currencies.MRO: return "MRU";
                case Currencies.MZM: return "MZN";
                case Currencies.SDD: return "SDG";
                case Currencies.SIT: return "EUR";
                case Currencies.STD: return "STN";
                case Currencies.TJR: return "TJS";
                case Currencies.TMM: return "TMT";
                case Currencies.TPE: return "TWD";
                case Currencies.UGS: return "UGX";
                case Currencies.VEB: return "VES";
                case Currencies.ZRN: return "CDF";
                case Currencies.ZWD: return "USD";

                // Special recent case: Netherlands Antillean guilder
                // Keep as-is only if your business wants the legacy code rejected instead.
                // If you want "strict current only", change this to throw instead.
                case Currencies.ANG: return "XCG";

                // Explicitly non-ISO / crypto / internal custom values
                case Currencies.BTC:
                case Currencies.BCH:
                case Currencies.BTG:
                case Currencies.ETH:
                case Currencies.BSV:
                case Currencies.LTC:
                case Currencies.CR1:
                case Currencies.CR2:
                case Currencies.CR3:
                case Currencies.CR4:
                case Currencies.CR5:
                case Currencies.CR6:
                case Currencies.CR7:
                case Currencies.CR8:
                case Currencies.CR9:
                case Currencies.CRA:
                case Currencies.CRB:
                case Currencies.CRD:
                case Currencies.CRE:
                case Currencies.CRF:
                case Currencies.CRG:
                case Currencies.CRH:
                case Currencies.CRI:
                case Currencies.CRJ:
                case Currencies.CRK:
                case Currencies.CRM:
                case Currencies.CRN:
                case Currencies.CRO:
                case Currencies.CRP:
                    throw new ArgumentOutOfRangeException(
                        nameof(value),
                        value,
                        string.Format("Value '{0}' does not map to an ISO 4217 currency code.", value));

                default:
                    return value.ToString();
            }
        }

        public sealed class BuilderParms
        {
            public CrudAPI Api { get; set; }
            public DateTime FromDate { get; set; }
            public DateTime ToDate { get; set; }
            public bool Taxonomy { get; set; }
            public Company CurrentCompany => Api.CompanyEntity;
            public Dictionary<string, string> JournalTypeDict { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    public struct SignedAmount
    {
        public decimal RawAmount { get; }
        public decimal Amount { get; }
        public DebitCreditType DebitCreditType { get; }

        public bool IsDebit => DebitCreditType == DebitCreditType.D;
        public bool IsCredit => DebitCreditType == DebitCreditType.C;

        public SignedAmount(decimal rawAmount)
        {
            RawAmount = rawAmount;
            Amount = Math.Abs(rawAmount);
            DebitCreditType = rawAmount < 0 ? DebitCreditType.C : DebitCreditType.D;
        }

        public static SignedAmount From(double amount) => new SignedAmount((decimal)amount);
    }
}

namespace UnicontaClient.Controls.Dialogs.DutchXafExport.Model
{ 
    public static class XmlAuditfileNamespaces
    {
        public const string Namespace = "http://www.odb.belastingdienst.nl/Belastingdienst/BCPP/1.1/structures/XmlauditfileXAF_4.0";
    }

    [Serializable]
    [XmlType(Namespace = XmlAuditfileNamespaces.Namespace)]
    [XmlRoot("auditfile", Namespace = XmlAuditfileNamespaces.Namespace, IsNullable = false)]
    /// <summary>
    /// Root van de XML Auditfile Financieel 4.0. Bevat de berichtheader en de ondernemingsgegevens.
    /// English: Root of the XML Auditfile Financial 4.0. Contains the message header and the company data.
    /// </summary>
    public class Auditfile
    {
        /// <summary>
        /// Berichtheader.
        /// English: Message header.
        /// </summary>
        [XmlElement("header")]
        public AuditfileHeader Header { get; set; }

        /// <summary>
        /// Gegevens van de onderneming die de auditfile aanlevert.
        /// English: Data of the company that provides the audit file.
        /// </summary>
        [XmlElement("company")]
        public AuditfileCompany Company { get; set; }
    }

    [Serializable]
    [XmlType("header", Namespace = XmlAuditfileNamespaces.Namespace)]
    /// <summary>
    /// Berichtheader met algemene kenmerken van de auditfile, zoals boekjaar, valuta en gebruikte software.
    /// English: Message header with general characteristics of the audit file, such as fiscal year, currency, and software used.
    /// </summary>
    public class AuditfileHeader
    {
        /// <summary>
        /// Aanduiding van het boekjaar (EEJJ), bijvoorbeeld 2024. Bij een gebroken boekjaar: 2023-2024.
        /// English: Indication of the fiscal year (YYYY), for example 2024. For a broken fiscal year: 2023-2024.
        /// </summary>
        [XmlElement("fiscalYear")]
        public string FiscalYear { get; set; }

        /// <summary>
        /// Startdatum van het boekjaar in W3C date-formaat, bijvoorbeeld 2024-01-01.
        /// English: Start date of the fiscal year in W3C date format, for example 2024-01-01.
        /// </summary>
        [XmlElement("startDate", DataType = "date")]
        public DateTime StartDate { get; set; }

        /// <summary>
        /// Einddatum van het boekjaar in W3C date-formaat, bijvoorbeeld 2024-12-31.
        /// English: End date of the fiscal year in W3C date format, for example 2024-12-31.
        /// </summary>
        [XmlElement("endDate", DataType = "date")]
        public DateTime EndDate { get; set; }

        /// <summary>
        /// ISO-valutacode (ISO 4217) van de lokale valuta van de administratie, bijvoorbeeld EUR.
        /// English: ISO currency code (ISO 4217) of the local currency of the administration, for example EUR.
        /// </summary>
        [XmlElement("curCode")]
        public string CurCode { get; set; }

        /// <summary>
        /// Datum waarop de auditfile is aangemaakt.
        /// English: Date on which the audit file was created.
        /// </summary>
        [XmlElement("dateCreated", DataType = "date")]
        public DateTime DateCreated { get; set; }

        /// <summary>
        /// Naam van het softwarepakket waarmee de auditfile is gegenereerd.
        /// English: Name of the software package used to generate the audit file.
        /// </summary>
        [XmlElement("softwareDesc")]
        public string SoftwareDesc { get; set; }

        /// <summary>
        /// Versie van het softwarepakket waarmee de auditfile is gegenereerd.
        /// English: Version of the software package used to generate the audit file.
        /// </summary>
        [XmlElement("softwareVersion")]
        public string SoftwareVersion { get; set; }

        /// <summary>
        /// Gebruikte RGS-versie in de administratie.
        /// English: RGS version used in the administration.
        /// </summary>
        [XmlElement("RGSVersion")]
        public string RgsVersion { get; set; }
    }

    [Serializable]
    [XmlType("company", Namespace = XmlAuditfileNamespaces.Namespace)]
    /// <summary>
    /// Gegevens van de onderneming die de auditfile aanlevert, inclusief stamgegevens en transacties.
    /// English: Data of the company providing the audit file, including master data and transactions.
    /// </summary>
    public class AuditfileCompany
    {
        /// <summary>
        /// KvK-nummer van de onderneming.
        /// English: Chamber of Commerce registration number of the company.
        /// </summary>
        [XmlElement("Commercenr")]
        public string Commercenr { get; set; }

        /// <summary>
        /// Juridische naam van de onderneming.
        /// English: Legal name of the company.
        /// </summary>
        [XmlElement("companyName")]
        public string CompanyName { get; set; }

        /// <summary>
        /// ISO-landcode (ISO 3166) van het land waarin de onderneming of relatie fiscaal is geregistreerd.
        /// English: ISO country code (ISO 3166) of the country in which the company or relation is fiscally registered.
        /// </summary>
        [XmlElement("taxRegistrationCountry")]
        public string TaxRegistrationCountry { get; set; }

        /// <summary>
        /// BTW-identificatienummer van de onderneming of relatie.
        /// English: VAT identification number of the company or relation.
        /// </summary>
        [XmlElement("taxRegIdent")]
        public string TaxRegIdent { get; set; }

        /// <summary>
        /// Een of meer adresregels.
        /// English: One or more address entries.
        /// </summary>
        [XmlElement("streetAddress")]
        public List<StreetAddress> StreetAddress { get; set; }

        /// <summary>
        /// Debiteuren- en crediteurenstamgegevens.
        /// English: Customer and supplier master data.
        /// </summary>
        [XmlElement("customersSuppliers")]
        public CustomersSuppliers CustomersSuppliers { get; set; }

        /// <summary>
        /// Grootboekstamgegevens.
        /// English: General ledger master data.
        /// </summary>
        [XmlElement("generalLedger")]
        public GeneralLedger GeneralLedger { get; set; }

        /// <summary>
        /// Gebruikte btw-codes in de administratie.
        /// English: VAT codes used in the administration.
        /// </summary>
        [XmlElement("vatCodes")]
        public VatCodes VatCodes { get; set; }

        /// <summary>
        /// Gehanteerde perioden in de administratie.
        /// English: Periods used in the administration.
        /// </summary>
        [XmlElement("periods")]
        public Periods Periods { get; set; }

        /// <summary>
        /// Openingsbalans of beginbalans.
        /// English: Opening balance or starting balance.
        /// </summary>
        [XmlElement("openingBalance")]
        public OpeningBalance OpeningBalance { get; set; }

        /// <summary>
        /// Grootboektransacties.
        /// English: General ledger transactions.
        /// </summary>
        [XmlElement("transactions")]
        public Transactions Transactions { get; set; }
    }

    [Serializable]
    [XmlType(Namespace = XmlAuditfileNamespaces.Namespace)]
    /// <summary>
    /// Adresgegevens van een onderneming, debiteur of crediteur.
    /// English: Address details of a company, customer, or supplier.
    /// </summary>
    public class StreetAddress
    {
        /// <summary>
        /// Straatnaam. Als straat, huisnummer en toevoeging niet gesplitst kunnen worden, geef alles hier door.
        /// English: Street name. If street, house number, and addition cannot be split, provide everything here.
        /// </summary>
        [XmlElement("streetname")]
        public string Streetname { get; set; }

        /// <summary>
        /// Huisnummer.
        /// English: House number.
        /// </summary>
        [XmlElement("number")]
        public string Number { get; set; }

        /// <summary>
        /// Huisnummertoevoeging.
        /// English: House number addition.
        /// </summary>
        [XmlElement("numberExtension")]
        public string NumberExtension { get; set; }

        /// <summary>
        /// Plaats.
        /// English: City.
        /// </summary>
        [XmlElement("city")]
        public string City { get; set; }

        /// <summary>
        /// Postcode.
        /// English: Postal code.
        /// </summary>
        [XmlElement("postalCode")]
        public string PostalCode { get; set; }

        /// <summary>
        /// Landcode volgens ISO 3166.
        /// English: Country code according to ISO 3166.
        /// </summary>
        [XmlElement("country")]
        public string Country { get; set; }
    }

    [Serializable]
    [XmlType("customersSuppliers", Namespace = XmlAuditfileNamespaces.Namespace)]
    /// <summary>
    /// Verzameling van debiteuren- en crediteurenstamgegevens.
    /// English: Collection of customer and supplier master data.
    /// </summary>
    public class CustomersSuppliers
    {
        /// <summary>
        /// Debiteur of crediteur.
        /// English: Customer or supplier.
        /// </summary>
        [XmlElement("customerSupplier")]
        public List<CustomerSupplier> CustomerSupplier { get; set; }
    }

    [Serializable]
    [XmlType(Namespace = XmlAuditfileNamespaces.Namespace)]
    /// <summary>
    /// Algemene gegevens van een debiteur of crediteur.
    /// English: General data of a customer or supplier.
    /// </summary>
    public class CustomerSupplier
    {
        /// <summary>
        /// Uniek debiteuren- of crediteurennummer.
        /// English: Unique customer or supplier number.
        /// </summary>
        [XmlElement("custSupID")]
        public string CustSupId { get; set; }

        /// <summary>
        /// Juridische naam van de debiteur of crediteur.
        /// English: Legal name of the customer or supplier.
        /// </summary>
        [XmlElement("custSupName")]
        public string CustSupName { get; set; }

        /// <summary>
        /// E-mailadres.
        /// English: Email address.
        /// </summary>
        [XmlElement("eMail")]
        public string EMail { get; set; }

        /// <summary>
        /// KvK-nummer.
        /// English: Chamber of Commerce number.
        /// </summary>
        [XmlElement("commerceNr")]
        public string CommerceNr { get; set; }

        /// <summary>
        /// ISO-landcode (ISO 3166) van het land waarin de onderneming of relatie fiscaal is geregistreerd.
        /// English: ISO country code (ISO 3166) of the country in which the company or relation is fiscally registered.
        /// </summary>
        [XmlElement("taxRegistrationCountry")]
        public string TaxRegistrationCountry { get; set; }

        /// <summary>
        /// BTW-identificatienummer van de onderneming of relatie.
        /// English: VAT identification number of the company or relation.
        /// </summary>
        [XmlElement("taxRegIdent")]
        public string TaxRegIdent { get; set; }

        /// <summary>
        /// Code die aangeeft of het om een debiteur, crediteur of beide gaat.
        /// English: Code indicating whether it concerns a customer, a supplier, or both.
        /// </summary>
        [XmlElement("custSupTp")]
        public CustomerSupplierCode CustSupTp { get; set; }

        [XmlIgnore]
        public bool CustSupTpSpecified { get; set; }

        /// <summary>
        /// Openstaand bedrag in lokale valuta bij de startdatum van het boekjaar.
        /// English: Outstanding amount in local currency at the start date of the fiscal year.
        /// </summary>
        [XmlElement("opBalDesc")]
        public decimal OpBalDesc { get; set; }

        [XmlIgnore]
        public bool OpBalDescSpecified { get; set; }

        /// <summary>
        /// Indicatie of het openingssaldo debet of credit is.
        /// English: Indication whether the opening balance is debit or credit.
        /// </summary>
        [XmlElement("opBalTp")]
        public DebitCreditType OpBalTp { get; set; }

        [XmlIgnore]
        public bool OpBalTpSpecified { get; set; }

        /// <summary>
        /// Openstaand bedrag in lokale valuta bij de einddatum van het boekjaar.
        /// English: Outstanding amount in local currency at the end date of the fiscal year.
        /// </summary>
        [XmlElement("clBalDesc")]
        public decimal ClBalDesc { get; set; }

        [XmlIgnore]
        public bool ClBalDescSpecified { get; set; }

        /// <summary>
        /// Indicatie of het eindsaldo debet of credit is.
        /// English: Indication whether the closing balance is debit or credit.
        /// </summary>
        [XmlElement("clBalTp")]
        public DebitCreditType ClBalTp { get; set; }

        [XmlIgnore]
        public bool ClBalTpSpecified { get; set; }

        /// <summary>
        /// Een of meer adresregels.
        /// English: One or more address entries.
        /// </summary>
        [XmlElement("streetAddress")]
        public List<StreetAddress> StreetAddress { get; set; }
    }

    [Serializable]
    [XmlType("generalLedger", Namespace = XmlAuditfileNamespaces.Namespace)]
    /// <summary>
    /// Grootboekstamgegevens van de administratie.
    /// English: General ledger master data of the administration.
    /// </summary>
    public class GeneralLedger
    {
        /// <summary>
        /// Grootboekrekening uit het rekeningschema.
        /// English: General ledger account from the chart of accounts.
        /// </summary>
        [XmlElement("ledgerAccount")]
        public List<LedgerAccount> LedgerAccount { get; set; }
    }

    [Serializable]
    [XmlType(Namespace = XmlAuditfileNamespaces.Namespace)]
    /// <summary>
    /// Definitie van een grootboekrekening uit het rekeningschema.
    /// English: Definition of a general ledger account from the chart of accounts.
    /// </summary>
    public class LedgerAccount
    {
        /// <summary>
        /// Grootboekrekeningcode zoals gedefinieerd in het grootboek.
        /// English: General ledger account code as defined in the ledger.
        /// </summary>
        [XmlElement("accID")]
        public string AccId { get; set; }

        /// <summary>
        /// Grootboekrekeningnaam.
        /// English: General ledger account name.
        /// </summary>
        [XmlElement("accDesc")]
        public string AccDesc { get; set; }

        /// <summary>
        /// Soort grootboekrekening: balansrekening of resultaatrekening.
        /// English: Type of general ledger account: balance sheet account or profit and loss account.
        /// </summary>
        [XmlElement("accTp")]
        public AccountType AccTp { get; set; }

        /// <summary>
        /// Gebruikte RGS-code die aan de grootboekrekening is gekoppeld.
        /// English: RGS code used and linked to the general ledger account.
        /// </summary>
        [XmlElement("RGScode")]
        public string RgsCode { get; set; }
    }

    [Serializable]
    [XmlType("vatCodes", Namespace = XmlAuditfileNamespaces.Namespace)]
    /// <summary>
    /// Verzameling van in de administratie gebruikte btw-codes.
    /// English: Collection of VAT codes used in the administration.
    /// </summary>
    public class VatCodes
    {
        /// <summary>
        /// BTW-code.
        /// English: VAT code.
        /// </summary>
        [XmlElement("vatCode")]
        public List<VatCode> VatCode { get; set; }
    }

    [Serializable]
    [XmlType(Namespace = XmlAuditfileNamespaces.Namespace)]
    /// <summary>
    /// Definitie van een btw-code en de gekoppelde grootboekrekeningen.
    /// English: Definition of a VAT code and the linked general ledger accounts.
    /// </summary>
    public class VatCode
    {
        /// <summary>
        /// BTW-code of verwijzing naar een gedefinieerde btw-code.
        /// English: VAT code or reference to a defined VAT code.
        /// </summary>
        [XmlElement("vatID")]
        public string VatId { get; set; }

        /// <summary>
        /// Omschrijving van de btw-code.
        /// English: Description of the VAT code.
        /// </summary>
        [XmlElement("vatDesc")]
        public string VatDesc { get; set; }

        /// <summary>
        /// Balansrekening waarop af te dragen btw wordt geboekt.
        /// English: Balance sheet account to which VAT payable is posted.
        /// </summary>
        [XmlElement("vatToPayAccID")]
        public string VatToPayAccId { get; set; }

        /// <summary>
        /// Balansrekening waarop te vorderen btw wordt geboekt.
        /// English: Balance sheet account to which VAT receivable is posted.
        /// </summary>
        [XmlElement("vatToClaimAccID")]
        public string VatToClaimAccId { get; set; }
    }

    [Serializable]
    [XmlType("periods", Namespace = XmlAuditfileNamespaces.Namespace)]
    /// <summary>
    /// Verzameling van in de administratie gehanteerde perioden.
    /// English: Collection of periods used in the administration.
    /// </summary>
    public class Periods
    {
        /// <summary>
        /// Periode.
        /// English: Period.
        /// </summary>
        [XmlElement("period")]
        public List<Period> Period { get; set; }
    }

    [Serializable]
    [XmlType(Namespace = XmlAuditfileNamespaces.Namespace)]
    /// <summary>
    /// Definitie van een periode binnen het boekjaar.
    /// English: Definition of a period within the fiscal year.
    /// </summary>
    public class Period
    {
        /// <summary>
        /// Nummer van de periode zoals gedefinieerd in de administratie.
        /// English: Number of the period as defined in the administration.
        /// </summary>
        [XmlElement("periodNumber")]
        public int PeriodNumber { get; set; }

        /// <summary>
        /// Startdatum van de periode.
        /// English: Start date of the period.
        /// </summary>
        [XmlElement("startDatePeriod", DataType = "date")]
        public DateTime StartDatePeriod { get; set; }

        /// <summary>
        /// Einddatum van de periode.
        /// English: End date of the period.
        /// </summary>
        [XmlElement("endDatePeriod", DataType = "date")]
        public DateTime EndDatePeriod { get; set; }
    }

    [Serializable]
    [XmlType("openingBalance", Namespace = XmlAuditfileNamespaces.Namespace)]
    /// <summary>
    /// Openingsbalans of beginbalans. De bedragen worden in de lokale valuta opgenomen en uitsluitend onder dit element.
    /// English: Opening balance or starting balance. The amounts are recorded in the local currency and only under this element.
    /// </summary>
    public class OpeningBalance
    {
        /// <summary>
        /// Aantal aanwezige regels binnen dit blok.
        /// English: Number of lines present within this block.
        /// </summary>
        [XmlElement("linesCount")]
        public int LinesCount { get; set; }

        /// <summary>
        /// Totaalsom van de debetbedragen binnen dit blok.
        /// English: Total sum of the debit amounts within this block.
        /// </summary>
        [XmlElement("totalDebit")]
        public decimal TotalDebit { get; set; }

        /// <summary>
        /// Totaalsom van de creditbedragen binnen dit blok.
        /// English: Total sum of the credit amounts within this block.
        /// </summary>
        [XmlElement("totalCredit")]
        public decimal TotalCredit { get; set; }

        /// <summary>
        /// Regel van de openingsbalans.
        /// English: Opening balance line.
        /// </summary>
        [XmlElement("obLine")]
        public List<OpeningBalanceLine> ObLine { get; set; }
    }

    [Serializable]
    [XmlType(Namespace = XmlAuditfileNamespaces.Namespace)]
    /// <summary>
    /// Regel van de openingsbalans.
    /// English: Opening balance line.
    /// </summary>
    public class OpeningBalanceLine
    {
        /// <summary>
        /// Uniek nummer van de regel of transactie binnen de betreffende context.
        /// English: Unique number of the line or transaction within the relevant context.
        /// </summary>
        [XmlElement("nr")]
        public string Nr { get; set; }

        /// <summary>
        /// Grootboekrekeningcode zoals gedefinieerd in het grootboek.
        /// English: General ledger account code as defined in the ledger.
        /// </summary>
        [XmlElement("accID")]
        public string AccId { get; set; }

        /// <summary>
        /// Bedrag in lokale valuta.
        /// English: Amount in local currency.
        /// </summary>
        [XmlElement("amnt")]
        public decimal Amnt { get; set; }

        /// <summary>
        /// Indicatie of het bedrag debet of credit is.
        /// English: Indication whether the amount is debit or credit.
        /// </summary>
        [XmlElement("amntTp")]
        public DebitCreditType AmntTp { get; set; }
    }

    [Serializable]
    [XmlType("transactions", Namespace = XmlAuditfileNamespaces.Namespace)]
    /// <summary>
    /// Grootboektransacties van de administratie.
    /// English: General ledger transactions of the administration.
    /// </summary>
    public class Transactions
    {
        /// <summary>
        /// Aantal aanwezige regels binnen dit blok.
        /// English: Number of lines present within this block.
        /// </summary>
        [XmlElement("linesCount")]
        public int LinesCount { get; set; }

        /// <summary>
        /// Totaalsom van de debetbedragen binnen dit blok.
        /// English: Total sum of the debit amounts within this block.
        /// </summary>
        [XmlElement("totalDebit")]
        public decimal TotalDebit { get; set; }

        /// <summary>
        /// Totaalsom van de creditbedragen binnen dit blok.
        /// English: Total sum of the credit amounts within this block.
        /// </summary>
        [XmlElement("totalCredit")]
        public decimal TotalCredit { get; set; }

        /// <summary>
        /// Dagboek.
        /// English: Journal.
        /// </summary>
        [XmlElement("journal")]
        public List<Journal> Journal { get; set; }
    }

    [Serializable]
    [XmlType(Namespace = XmlAuditfileNamespaces.Namespace)]
    /// <summary>
    /// Dagboek met transacties en optioneel een vaste tegenrekening.
    /// English: Journal with transactions and optionally a fixed contra account.
    /// </summary>
    public class Journal
    {
        /// <summary>
        /// Unieke dagboekcode.
        /// English: Unique journal code.
        /// </summary>
        [XmlElement("jrnID")]
        public string JrnId { get; set; }

        /// <summary>
        /// Omschrijving van het betreffende object, dagboek, transactie of transactieregel.
        /// English: Description of the relevant object, journal, transaction, or transaction line.
        /// </summary>
        [XmlElement("desc")]
        public string Desc { get; set; }

        /// <summary>
        /// Dagboeksoort.
        /// English: Journal type.
        /// </summary>
        [XmlElement("jrnTp")]
        public JournalType JrnTp { get; set; }

        [XmlIgnore]
        public bool JrnTpSpecified { get; set; }

        /// <summary>
        /// Vaste tegenrekening van het dagboek.
        /// English: Fixed contra account of the journal.
        /// </summary>
        [XmlElement("offsetAccID")]
        public string OffsetAccId { get; set; }

        /// <summary>
        /// Transactie binnen een dagboek.
        /// English: Transaction within a journal.
        /// </summary>
        [XmlElement("transaction")]
        public List<Transaction> Transaction { get; set; }
    }

    [Serializable]
    [XmlType(Namespace = XmlAuditfileNamespaces.Namespace)]
    /// <summary>
    /// Transactiedetails binnen een dagboek.
    /// English: Transaction details within a journal.
    /// </summary>
    public class Transaction
    {
        /// <summary>
        /// Uniek nummer van de regel of transactie binnen de betreffende context.
        /// English: Unique number of the line or transaction within the relevant context.
        /// </summary>
        [XmlElement("nr")]
        public string Nr { get; set; }

        /// <summary>
        /// Omschrijving van het betreffende object, dagboek, transactie of transactieregel.
        /// English: Description of the relevant object, journal, transaction, or transaction line.
        /// </summary>
        [XmlElement("desc")]
        public string Desc { get; set; }

        /// <summary>
        /// Nummer van de periode zoals gedefinieerd in de administratie.
        /// English: Number of the period as defined in the administration.
        /// </summary>
        [XmlElement("periodNumber")]
        public int PeriodNumber { get; set; }

        /// <summary>
        /// Boekingsdatum: datum waarop de transactie is verwerkt of geboekt in het systeem.
        /// English: Posting date: the date on which the transaction was processed or posted in the system.
        /// </summary>
        [XmlElement("trDt", DataType = "date")]
        public DateTime TrDt { get; set; }

        /// <summary>
        /// Identificatie van de bronapplicatie die de transactie heeft gecreeerd.
        /// English: Identification of the source application that created the transaction.
        /// </summary>
        [XmlElement("Source")]
        public string Source { get; set; }

        /// <summary>
        /// Identificatie van de gebruiker die de transactie heeft ingevoerd.
        /// English: Identification of the user who entered the transaction.
        /// </summary>
        [XmlElement("User")]
        public string User { get; set; }

        /// <summary>
        /// Journaalpostregel.
        /// English: Journal entry line.
        /// </summary>
        [XmlElement("trLine")]
        public List<TransactionLine> TrLine { get; set; }
    }

    [Serializable]
    [XmlType(Namespace = XmlAuditfileNamespaces.Namespace)]
    /// <summary>
    /// Journaalpostregel binnen een transactie.
    /// English: Journal entry line within a transaction.
    /// </summary>
    public class TransactionLine
    {
        /// <summary>
        /// Uniek nummer van de regel of transactie binnen de betreffende context.
        /// English: Unique number of the line or transaction within the relevant context.
        /// </summary>
        [XmlElement("nr")]
        public string Nr { get; set; }

        /// <summary>
        /// Grootboekrekeningcode zoals gedefinieerd in het grootboek.
        /// English: General ledger account code as defined in the ledger.
        /// </summary>
        [XmlElement("accID")]
        public string AccId { get; set; }

        /// <summary>
        /// Documentreferentie, bijvoorbeeld een boekstuknummer of verwijzing naar een brondocument.
        /// English: Document reference, for example a voucher number or a reference to a source document.
        /// </summary>
        [XmlElement("docRef")]
        public string DocRef { get; set; }

        /// <summary>
        /// Datum waarop de factuur is uitgereikt.
        /// English: Date on which the invoice was issued.
        /// </summary>
        [XmlElement("effDate", DataType = "date")]
        public DateTime EffDate { get; set; }

        /// <summary>
        /// Datum waarop goederen of diensten zijn geleverd, of datum van een vooruitbetaling.
        /// English: Date on which goods or services were supplied, or date of an advance payment.
        /// </summary>
        [XmlElement("settDate", DataType = "date")]
        public DateTime SettDate { get; set; }

        [XmlIgnore]
        public bool SettDateSpecified { get; set; }

        /// <summary>
        /// Omschrijving van het betreffende object, dagboek, transactie of transactieregel.
        /// English: Description of the relevant object, journal, transaction, or transaction line.
        /// </summary>
        [XmlElement("desc")]
        public string Desc { get; set; }

        /// <summary>
        /// Bedrag in lokale valuta.
        /// English: Amount in local currency.
        /// </summary>
        [XmlElement("amnt")]
        public decimal Amnt { get; set; }

        /// <summary>
        /// Indicatie of het bedrag debet of credit is.
        /// English: Indication whether the amount is debit or credit.
        /// </summary>
        [XmlElement("amntTp")]
        public DebitCreditType AmntTp { get; set; }

        /// <summary>
        /// Uniek debiteuren- of crediteurennummer.
        /// English: Unique customer or supplier number.
        /// </summary>
        [XmlElement("custSupID")]
        public string CustSupId { get; set; }

        /// <summary>
        /// Uniek factuurnummer.
        /// English: Unique invoice number.
        /// </summary>
        [XmlElement("invRef")]
        public string InvRef { get; set; }

        /// <summary>
        /// Verwijzing naar uniek nummer of code van de ontvangstbon.
        /// English: Reference to the unique number or code of the receipt note.
        /// </summary>
        [XmlElement("receivingDocRef")]
        public string ReceivingDocRef { get; set; }

        /// <summary>
        /// Verwijzing naar uniek nummer of code van het verzenddocument.
        /// English: Reference to the unique number or code of the shipping document.
        /// </summary>
        [XmlElement("shipDocRef")]
        public string ShipDocRef { get; set; }

        /// <summary>
        /// Verwijzing naar uniek nummer of code van de kostenplaats.
        /// English: Reference to the unique number or code of the cost center.
        /// </summary>
        [XmlElement("cost")]
        public string Cost { get; set; }

        /// <summary>
        /// Verwijzing naar uniek nummer of code van het product of artikel.
        /// English: Reference to the unique number or code of the product or item.
        /// </summary>
        [XmlElement("product")]
        public string Product { get; set; }

        /// <summary>
        /// Verwijzing naar uniek nummer of code van het project.
        /// English: Reference to the unique number or code of the project.
        /// </summary>
        [XmlElement("project")]
        public string Project { get; set; }

        /// <summary>
        /// Verwijzing naar de wijze van behandeling voor de Werkkostenregeling (WKR).
        /// English: Reference to the treatment method for the Work-Related Costs Scheme (WKR).
        /// </summary>
        [XmlElement("workCostArrRef")]
        public string WorkCostArrRef { get; set; }

        /// <summary>
        /// Eigen bankrekeningnummer voor ontvangsten en betalingen per bank.
        /// English: Own bank account number for receipts and bank payments.
        /// </summary>
        [XmlElement("bankAccNr")]
        public string BankAccNr { get; set; }

        /// <summary>
        /// Tegenrekening waarvandaan of waarnaar een betaling plaatsvindt.
        /// English: Contra account from which or to which a payment is made.
        /// </summary>
        [XmlElement("offsetBankAccNr")]
        public string OffsetBankAccNr { get; set; }

        /// <summary>
        /// Specificatie van btw op de transactieregel.
        /// English: VAT specification on the transaction line.
        /// </summary>
        [XmlElement("vat")]
        public List<TransactionLineVat> Vat { get; set; }

        /// <summary>
        /// Specificatie van een vreemde valuta op de transactieregel.
        /// English: Foreign currency specification on the transaction line.
        /// </summary>
        [XmlElement("currency")]
        public TransactionLineCurrency Currency { get; set; }
    }

    [Serializable]
    [XmlType(Namespace = XmlAuditfileNamespaces.Namespace)]
    /// <summary>
    /// Specificatie van btw op een transactieregel.
    /// English: Specification of VAT on a transaction line.
    /// </summary>
    public class TransactionLineVat
    {
        /// <summary>
        /// BTW-code of verwijzing naar een gedefinieerde btw-code.
        /// English: VAT code or reference to a defined VAT code.
        /// </summary>
        [XmlElement("vatID")]
        public string VatId { get; set; }

        /// <summary>
        /// BTW-percentage.
        /// English: VAT percentage.
        /// </summary>
        [XmlElement("vatPerc")]
        public decimal VatPerc { get; set; }

        /// <summary>
        /// BTW-bedrag.
        /// English: VAT amount.
        /// </summary>
        [XmlElement("vatAmnt")]
        public decimal VatAmnt { get; set; }

        /// <summary>
        /// Indicatie of het btw-bedrag debet of credit is.
        /// English: Indication whether the VAT amount is debit or credit.
        /// </summary>
        [XmlElement("vatAmntTp")]
        public DebitCreditType VatAmntTp { get; set; }
    }

    [Serializable]
    [XmlType(Namespace = XmlAuditfileNamespaces.Namespace)]
    /// <summary>
    /// Specificatie van een vreemde valutawaarde op een transactieregel.
    /// English: Specification of a foreign currency value on a transaction line.
    /// </summary>
    public class TransactionLineCurrency
    {
        /// <summary>
        /// ISO-valutacode (ISO 4217) van de lokale valuta van de administratie, bijvoorbeeld EUR.
        /// English: ISO currency code (ISO 4217) of the local currency of the administration, for example EUR.
        /// </summary>
        [XmlElement("curCode")]
        public string CurCode { get; set; }

        /// <summary>
        /// Bedrag in vreemde valuta.
        /// English: Amount in foreign currency.
        /// </summary>
        [XmlElement("curAmnt")]
        public decimal CurAmnt { get; set; }
    }

    [Serializable]
    [XmlType(Namespace = XmlAuditfileNamespaces.Namespace)]
    /// <summary>
    /// Code die aangeeft of een relatie debiteur, crediteur of beide is.
    /// English: Code indicating whether a relation is a customer, a supplier, or both.
    /// </summary>
    public enum CustomerSupplierCode
    {
        /// <summary>
        /// Zowel debiteur als crediteur.
        /// English: Both customer and supplier.
        /// </summary>
        [XmlEnum("B")]
        B,

        /// <summary>
        /// Debiteur.
        /// English: Customer.
        /// </summary>
        [XmlEnum("C")]
        C,

        /// <summary>
        /// Crediteur.
        /// English: Supplier.
        /// </summary>
        [XmlEnum("S")]
        S
    }

    [Serializable]
    [XmlType(Namespace = XmlAuditfileNamespaces.Namespace)]
    /// <summary>
    /// Indicatie of een bedrag debet of credit is.
    /// English: Indication whether an amount is debit or credit.
    /// </summary>
    public enum DebitCreditType
    {
        /// <summary>
        /// Credit.
        /// English: Credit.
        /// </summary>
        [XmlEnum("C")]
        C,

        /// <summary>
        /// Debet.
        /// English: Debit.
        /// </summary>
        [XmlEnum("D")]
        D
    }

    [Serializable]
    [XmlType(Namespace = XmlAuditfileNamespaces.Namespace)]
    /// <summary>
    /// Dagboeksoort. De meeste betekenissen komen uit het functiedocument; voor waarde O ontbreekt daar een toelichting.
    /// English: Journal type. Most meanings come from the functional document; value O is not explained there.
    /// </summary>
    public enum JournalType
    {
        /// <summary>
        /// Bank.
        /// English: Bank.
        /// </summary>
        [XmlEnum("B")]
        B,

        /// <summary>
        /// Kas.
        /// English: Cash.
        /// </summary>
        [XmlEnum("C")]
        C,

        /// <summary>
        /// Goederen (ontvangen/verzonden).
        /// English: Goods (received/shipped).
        /// </summary>
        [XmlEnum("G")]
        G,

        /// <summary>
        /// Memoriaal / dagboek.
        /// English: Memorial / journal.
        /// </summary>
        [XmlEnum("M")]
        M,

        /// <summary>
        /// Overig
        /// English: Other.
        /// </summary>
        [XmlEnum("O")]
        O,

        /// <summary>
        /// Inkoop.
        /// English: Purchase.
        /// </summary>
        [XmlEnum("P")]
        P,

        /// <summary>
        /// Verkoop.
        /// English: Sales.
        /// </summary>
        [XmlEnum("S")]
        S,

        /// <summary>
        /// Productie.
        /// English: Production.
        /// </summary>
        [XmlEnum("T")]
        T,

        /// <summary>
        /// Loon.
        /// English: Payroll.
        /// </summary>
        [XmlEnum("Y")]
        Y,

        /// <summary>
        /// Overig.
        /// English: Other.
        /// </summary>
        [XmlEnum("Z")]
        Z
    }

    [Serializable]
    [XmlType(Namespace = XmlAuditfileNamespaces.Namespace)]
    /// <summary>
    /// Soort grootboekrekening.
    /// English: Type of general ledger account.
    /// </summary>
    public enum AccountType
    {
        /// <summary>
        /// Balansrekening.
        /// English: Balance sheet account.
        /// </summary>
        [XmlEnum("B")]
        B,

        /// <summary>
        /// Resultaatrekening (Profit and Loss).
        /// English: Profit and loss account.
        /// </summary>
        [XmlEnum("P")]
        P
    }


}


namespace UnicontaClient.Controls.Dialogs.DutchXafExport.Writer
{
    using UnicontaClient.Controls.Dialogs.DutchXafExport.Model;

    public abstract class StreamWriterBase
    {
        public abstract void Write(Stream outputStream, Auditfile model);

        protected static void ValidateArguments(Stream outputStream, Auditfile model)
        {
            if (outputStream == null)
                throw new ArgumentNullException("outputStream");
            if (model == null)
                throw new ArgumentNullException("model");
        }

        protected static IEnumerable<string[]> FlattenTransactions(Transactions transactions)
        {
            if (transactions?.Journal == null)
                yield break;

            foreach (var journal in transactions.Journal)
            {
                if (journal.Transaction == null)
                    continue;

                foreach (var transaction in journal.Transaction)
                {
                    if (transaction.TrLine == null)
                        continue;

                    foreach (var line in transaction.TrLine)
                    {
                        yield return new[]
                        {
                        SafeExcel(journal.JrnId),
                        SafeExcel(transaction.Nr),
                        SafeExcel(line.Nr),
                        SafeExcel(line.AccId),
                        SafeExcel(line.Desc),
                        FormatDecimal(line.Amnt),
                        line.AmntTp.ToString(),
                        SafeExcel(line.DocRef),
                        SafeExcel(line.CustSupId),
                        SafeExcel(line.InvRef),
                        SafeExcel(line.Project),
                        SafeExcel(line.Product),
                        SafeExcel(transaction.Source),
                        SafeExcel(transaction.User)
                    };
                    }
                }
            }
        }

        protected static void WriteWorksheet(XmlWriter writer, string name, IEnumerable<string[]> rows)
        {
            writer.WriteStartElement("Worksheet", "urn:schemas-microsoft-com:office:spreadsheet");
            writer.WriteAttributeString("ss", "Name", "urn:schemas-microsoft-com:office:spreadsheet", SafeWorksheetName(name));
            writer.WriteStartElement("Table", "urn:schemas-microsoft-com:office:spreadsheet");

            foreach (var row in rows)
                WriteRow(writer, row);

            writer.WriteEndElement();
            writer.WriteEndElement();
        }

        protected static void WriteTableWorksheet(XmlWriter writer, string name, string[] headers, IEnumerable<string[]> dataRows)
        {
            WriteWorksheet(writer, name, new[] { headers }.Concat(dataRows));
        }

        protected static void WriteRow(XmlWriter writer, IEnumerable<string> cells)
        {
            writer.WriteStartElement("Row", "urn:schemas-microsoft-com:office:spreadsheet");

            foreach (var cell in cells)
            {
                writer.WriteStartElement("Cell", "urn:schemas-microsoft-com:office:spreadsheet");
                writer.WriteStartElement("Data", "urn:schemas-microsoft-com:office:spreadsheet");
                writer.WriteAttributeString("ss", "Type", "urn:schemas-microsoft-com:office:spreadsheet", "String");
                writer.WriteString(cell ?? string.Empty);
                writer.WriteEndElement();
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }

        protected static string SafeWorksheetName(string name)
        {
            var invalidChars = new[] { '[', ']', ':', '*', '?', '/', '\\' };
            var cleaned = string.Join(string.Empty, (name ?? string.Empty).Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
            if (string.IsNullOrWhiteSpace(cleaned))
                cleaned = "Sheet";
            return cleaned.Length > 31 ? cleaned.Substring(0, 31) : cleaned;
        }

        protected static string SafeExcel(string value)
        {
            return value ?? string.Empty;
        }

        protected static string FormatDate(DateTime? value)
        {
            return value.HasValue ? value.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : string.Empty;
        }

        protected static string FormatDecimal(decimal? value)
        {
            return value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : string.Empty;
        }
    }
}