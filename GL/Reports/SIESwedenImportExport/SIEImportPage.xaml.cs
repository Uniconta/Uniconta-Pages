using DevExpress.Xpf.Grid.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Uniconta.API.GeneralLedger;
using Uniconta.API.Service;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;
using UnicontaClient.Models;
using UnicontaClient.Pages.GL.Reports.SIESwedenImportExport.Helpers;
using UnicontaClient.Pages.GL.Reports.SIESwedenImportExport.Models;
using UnicontaClient.Pages.GL.Reports.SIESwedenImportExport.SIEModels;
using UnicontaClient.Utilities;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class SIEJournalLineGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(SIEGLDailyJournalLine); } }
        public SIEJournalLineGrid(IDataControlOriginationElement dataControlOriginationElement) : base(dataControlOriginationElement) { }
        public SIEJournalLineGrid() { }
    }

    public class SIEGLAccountGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(SIEGLAccount); } }
        public SIEGLAccountGrid(IDataControlOriginationElement dataControlOriginationElement) : base(dataControlOriginationElement) { }
        public SIEGLAccountGrid() { }
    }
    public class SIEDimensionValueGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(SIEDimensionValue); } }
        public SIEDimensionValueGrid(IDataControlOriginationElement dataControlOriginationElement) : base(dataControlOriginationElement) { }
        public SIEDimensionValueGrid() { }
    }

    public partial class SIEImportPage : GridBasePage
    {
        private IDictionary<string, string> _seriesJournalsMap;
        private SIEDocument _sieDocument;
        private string _filePath;

        /// <summary>
        /// SIE dimension number -> Uniconta dimension (1..5), chosen by the user in CWSIEMapDimensions. Only the
        /// dimensions used by #TRANS are mapped, and the objects of a dimension without a mapping are not imported.
        /// </summary>
        private Dictionary<string, int> _dimensionMap;

        private SQLTableCache<GLDailyJournalClient> _glJournals;
        private SQLTableCache<GLAccountClient> _glAccounts;
        private IEnumerable<SIEDimensionValue> _dimValues;

        private PostingAPI _postingAPI;

        private const int MaxShownIssues = 15;

        public SIEImportPage(BaseAPI API)
            : base(API, string.Empty)
        {
            Init();
        }

        public SIEImportPage(SynchronizeEntity syncEntity) : base(syncEntity, true)
        {
            Init();
        }

        //This ensures that no initial results are loaded before "Import" is clicked.
        public override Task InitQuery()
        {
            return Task.CompletedTask;
        }

        void Init()
        {
            DataContext = this;

            InitializeComponent();
            SetRibbonControl(localMenu, dgGLJournalLines);
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;

            _postingAPI = new PostingAPI(api);

            Utility.SetDimensionsGrid(api, cldim1, cldim2, cldim3, cldim4, cldim5);

            dgGLJournalLines.ShowTotalSummary();
            dgGLDimensions.ShowTotalSummary();
            dgGLAccounts.ShowTotalSummary();

            ResetPage();
        }

        private async void LocalMenu_OnItemClicked(string ActionType)
        {
            try
            {
                switch (ActionType)
                {
                    case "ImportData":
                        await EnsureCacheLoadedAsync();
                        await ImportDataAsync();
                        break;
                    case "MapSeriesToJournals":
                        MapSeriesToJournals();
                        break;
                    case "Simulate":
                        Simulate();
                        break;
                    case "SaveData":
                        await SaveAsync();
                        break;
                    case "PostJournal":
                        await SaveAsync(true);
                        break;
                    default:
                        gridRibbon_BaseActions(ActionType);
                        break;
                }
            }
            catch (Exception ex)
            {
                busyIndicator.IsBusy = false;
                UnicontaMessageBox.Show(ex);
            }
        }

        protected override async Task LoadCacheInBackGroundAsync()
        {
            await EnsureCacheLoadedAsync();
        }

        private async Task EnsureCacheLoadedAsync()
        {
            if (_glJournals == null)
                _glJournals = await api.LoadCache<GLDailyJournalClient>();

            if (_glAccounts == null)
                _glAccounts = await api.LoadCache<GLAccountClient>();
        }

        /// <summary>
        /// Reads the dimension values again for every file, so that values created by an earlier import (or by
        /// another user) are not offered for creation again.
        /// </summary>
        private async Task LoadDimensionValuesAsync()
        {
            var dimValues = new List<SIEDimensionValue>();

            if (api.CompanyEntity.NumberOfDimensions >= 1)
            {
                var dim1Values = await api.LoadCache<GLDimType1Client>(true);
                dimValues.AddRange(dim1Values.Select(x => ToDimensionValue(x)));
            }

            if (api.CompanyEntity.NumberOfDimensions >= 2)
            {
                var dim2Values = await api.LoadCache<GLDimType2Client>(true);
                dimValues.AddRange(dim2Values.Select(x => ToDimensionValue(x)));
            }

            if (api.CompanyEntity.NumberOfDimensions >= 3)
            {
                var dim3Values = await api.LoadCache<GLDimType3Client>(true);
                dimValues.AddRange(dim3Values.Select(x => ToDimensionValue(x)));
            }

            if (api.CompanyEntity.NumberOfDimensions >= 4)
            {
                var dim4Values = await api.LoadCache<GLDimType4Client>(true);
                dimValues.AddRange(dim4Values.Select(x => ToDimensionValue(x)));
            }

            if (api.CompanyEntity.NumberOfDimensions >= 5)
            {
                var dim5Values = await api.LoadCache<GLDimType5Client>(true);
                dimValues.AddRange(dim5Values.Select(x => ToDimensionValue(x)));
            }

            _dimValues = dimValues;
        }

        public override void AssignMultipleGrid(List<Uniconta.ClientTools.Controls.CorasauDataGrid> gridCtrls)
        {
            gridCtrls.Add(dgGLDimensions);
            gridCtrls.Add(dgGLAccounts);
            gridCtrls.Add(dgGLJournalLines);
        }

        public override bool CheckIfBindWithUserfield(out bool isReadOnly, out bool useBinding)
        {
            isReadOnly = false;
            useBinding = false;
            return false;
        }

        /// <summary>
        /// Read SIE file and parse data to journal lines
        /// </summary>
        private async Task ImportDataAsync()
        {
            //Read SIE file
            _sieDocument = await GetSieDataAsync();
            if (_sieDocument == null)
                return;

            //Validate before importing. The page was cleared when the file was chosen; a file that is not imported
            //is cleared as well.
            var validationResult = Validate();
            if (!validationResult)
            {
                ResetPage();
                return;
            }

            await LoadDimensionValuesAsync();

            //The user maps the dimensions used by the transactions to Uniconta dimensions, each dimension number once
            var usedDimensions = GetTransactionObjects().Select(o => o.Dimension)
                .GroupBy(d => d.Number).Select(g => g.First())
                .OrderBy(d => { int n; return int.TryParse(d.Number, out n) ? n : int.MaxValue; })
                .ThenBy(d => d.Number)
                .ToList();

            if (!usedDimensions.Any() || api.CompanyEntity.NumberOfDimensions == 0)
            {
                _dimensionMap = new Dictionary<string, int>();
                ShowFileData();
                return;
            }

            var cwMapDimensions = new CWSIEMapDimensions(api.CompanyEntity, usedDimensions);
            cwMapDimensions.Closed += (sender, args) =>
            {
                try
                {
                    if (cwMapDimensions.DialogResult != true)
                    {
                        ResetPage();
                        return;
                    }

                    _dimensionMap = cwMapDimensions.DimensionMap;
                    ShowFileData();
                }
                catch (Exception ex)
                {
                    UnicontaMessageBox.Show(ex);
                }
            };

            cwMapDimensions.Show();
        }

        /// <summary>
        /// The objects used by the #TRANS rows of the file, each once.
        /// </summary>
        private List<SIEObject> GetTransactionObjects()
        {
            return _sieDocument.VER.SelectMany(v => v.Rows)
                .Where(r => r.Objects != null)
                .SelectMany(r => r.Objects)
                .Where(o => o.Dimension != null)
                .GroupBy(o => o.Dimension.Number + "\u0001" + o.Number)
                .Select(g => g.First())
                .ToList();
        }

        /// <summary>
        /// Shows the vouchers, accounts and dimension values of the file in the grids.
        /// </summary>
        private void ShowFileData()
        {
            //Collect all unique series and mappings with journal
            //In case, there is an empty series, they have to be mapped as well
            var numberSeries = _sieDocument.VER.Select(x => x.Series ?? String.Empty).Distinct().ToList();
            _seriesJournalsMap = numberSeries.ToDictionary(x => x, x => String.Empty);

            //Load lines, gl accounts and dimensions from SIE file
            var journalLines = ConvertVERToJournalLines(_sieDocument.VER);

            var glAccounts = new List<SIEGLAccount>();

            foreach (var konto in _sieDocument.KONTO)
                glAccounts.Add(ConvertKontoToGLAccount(konto.Value));

            //Only objects used by the transactions are shown and created
            var dimensions = new List<SIEDimensionValue>();
            foreach (var obj in GetTransactionObjects())
            {
                if (_dimensionMap.ContainsKey(obj.Dimension.Number))
                    dimensions.Add(ConvertSIEDimToUnicontaDim(obj));
            }

            //Set items source and enable ribbon buttons
            dgGLJournalLines.ItemsSource = journalLines;
            dgGLJournalLines.Visibility = Visibility.Visible;

            dgGLAccounts.ItemsSource = glAccounts;
            dgGLAccounts.Visibility = Visibility.Visible;

            dgGLDimensions.ItemsSource = dimensions;
            dgGLDimensions.Visibility = Visibility.Visible;

            localMenu.EnableButtons("MapSeriesToJournals");
            localMenu.EnableButtons("Simulate");
            localMenu.EnableButtons("SaveData");
            localMenu.EnableButtons("PostJournal");

            //Set ribbon information
            var currentYear = _sieDocument.CurrentYear;
            if (currentYear != null &&
                currentYear.Start.HasValue)
                SetRibbonItemValue(Uniconta.ClientTools.Localization.lookup("FromDate"), currentYear.Start.Value.ToShortDateString());

            if (currentYear != null &&
                currentYear.End.HasValue)
                SetRibbonItemValue(Uniconta.ClientTools.Localization.lookup("ToDate"), currentYear.End.Value.ToShortDateString());

            SetRibbonItemValue(Uniconta.ClientTools.Localization.lookup("Type"), _sieDocument.SIETYP.ToString());

            if (_sieDocument.PROGRAM != null &&
                _sieDocument.PROGRAM.Any())
                SetRibbonItemValue(Uniconta.ClientTools.Localization.lookup("Application"), string.Join(", ", _sieDocument.PROGRAM));

            if (_sieDocument.GEN_DATE.HasValue)
                SetRibbonItemValue(Uniconta.ClientTools.Localization.lookup("CreatedBy"), string.Join(", ", new[] { _sieDocument.GEN_NAMN, _sieDocument.GEN_DATE.Value.ToShortDateString() }.Where(s => !String.IsNullOrEmpty(s))));

            SetRibbonItemValue(Uniconta.ClientTools.Localization.lookup("CompanyName"), _sieDocument.FNAMN.Name);
            SetRibbonItemValue(Uniconta.ClientTools.Localization.lookup("CompanyRegNo"), _sieDocument.FNAMN.OrgIdentifier);
            SetRibbonItemValue(Uniconta.ClientTools.Localization.lookup("CompanyId"), _sieDocument.FNAMN.Code);
            SetRibbonItemValue(Uniconta.ClientTools.Localization.lookup("address"), string.Join(" ", _sieDocument.FNAMN.Contact, _sieDocument.FNAMN.Street, _sieDocument.FNAMN.ZipCity).Trim());
            SetRibbonItemValue(Uniconta.ClientTools.Localization.lookup("CurrencyCode"), _sieDocument.VALUTA);
        }

        /// <summary>
        /// Simulate transactions by posting to selected journal
        /// </summary>
        /// <returns></returns>
        private void Simulate()
        {
            var journalLines = dgGLJournalLines.ItemsSource as IEnumerable<SIEGLDailyJournalLine>;
            if (journalLines == null)
                return;

            //Pick journal to simulate on
            var cwPickJournal = new CwMoveJournalLines(api);
            cwPickJournal.Title = string.Format(Uniconta.ClientTools.Localization.lookup("PleaseSelectOBJ"), Uniconta.ClientTools.Localization.lookup("Journal"));

            cwPickJournal.Closed += async (sender, args) =>
            {
                try
                {
                    if (cwPickJournal.DialogResult == false)
                        return;

                    if (String.IsNullOrEmpty(cwPickJournal.Journal))
                        return;

                    if (cwPickJournal.GLDailyJournal == null)
                    {
                        UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("NotFoundOBJ"), Uniconta.ClientTools.Localization.lookup("Journal") + " " + cwPickJournal.Journal), Uniconta.ClientTools.Localization.lookup("Error"));
                        return;
                    }

                    if (!CheckJournals(new[] { cwPickJournal.GLDailyJournal }))
                        return;

                    bool confirmed;
                    if (!CheckMandatoryVat(journalLines, false, out confirmed))
                        return;

                    busyIndicator.IsBusy = true;

                    //The simulation needs the accounts and dimension values; both are only created after confirmation
                    var createMissingGLAccountsResult = await CreateMissingGLAccountsAsync();
                    if (createMissingGLAccountsResult != ErrorCodes.Succes)
                        return;

                    var createMissingDimensionsResult = await CreateMissingDimensionValuesAsync();
                    if (createMissingDimensionsResult != ErrorCodes.Succes)
                        return;

                    //Simulate posting
                    var simulateResult = await _postingAPI.CheckDailyJournal(cwPickJournal.GLDailyJournal, DateTime.Today, true, new GLTransClientTotal(),
                                                                             journalLines.Count(), false, journalLines);
                    if (simulateResult.Err != ErrorCodes.Succes)
                    {
                        Utility.ShowJournalError(simulateResult, dgGLJournalLines);
                        return;
                    }

                    AddDockItem(TabControls.SimulatedTransactions, new object[] { simulateResult.AccountBalance, simulateResult.SimulatedTrans }, Uniconta.ClientTools.Localization.lookup("SimulatedTransactions"), null, true);
                }
                catch (Exception ex)
                {
                    UnicontaMessageBox.Show(ex);
                }
                finally
                {
                    busyIndicator.IsBusy = false;
                }
            };

            cwPickJournal.Show();
        }

        /// <summary>
        /// Transfer lines to the mapped journals and optionally post them
        /// </summary>
        /// <returns></returns>
        private async Task SaveAsync(bool post = false)
        {
            var allLines = dgGLJournalLines.ItemsSource as IEnumerable<SIEGLDailyJournalLine>;
            if (allLines == null)
                return;

            //Perform mapping, if not performed
            if (_seriesJournalsMap.Any(x => String.IsNullOrEmpty(x.Value)))
            {
                MapSeriesToJournals();
                return;
            }

            //All journals have to exist before anything is transferred
            var journals = new Dictionary<string, GLDailyJournalClient>();
            foreach (var journalName in _seriesJournalsMap.Values.Distinct())
            {
                var journal = _glJournals.FirstOrDefault(x => x.Journal == journalName);
                if (journal == null)
                {
                    UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("NotFoundOBJ"), Uniconta.ClientTools.Localization.lookup("Journal") + " " + journalName), Uniconta.ClientTools.Localization.lookup("Error"));
                    return;
                }
                journals[journalName] = journal;
            }

            if (!CheckJournals(journals.Values))
                return;

            //Lines that are only saved can still be corrected in the journal before posting
            bool confirmed;
            if (!CheckMandatoryVat(allLines, !post, out confirmed))
                return;

            if (!confirmed)
            {
                var confirmation = UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("AreYouSureToContinue"),
                                                           Uniconta.ClientTools.Localization.lookup("Confirmation"),
                                                           MessageBoxButton.YesNo);

                if (confirmation != MessageBoxResult.Yes)
                    return;
            }

            //Post or insert series by series. A series that has been transferred is removed from the page when a
            //later series fails or throws, so that it cannot be transferred twice.
            var remainingLines = allLines.ToList();
            var transferredSeries = new List<string>();
            string currentSeries = null;
            var completed = false;

            busyIndicator.IsBusy = true;
            try
            {
                //Create missing GL accounts if requested by user
                var createMissingGLAccountsResult = await CreateMissingGLAccountsAsync();
                if (createMissingGLAccountsResult != ErrorCodes.Succes)
                    return;

                //Create missing dimensions
                var createMissingDimensionsResult = await CreateMissingDimensionValuesAsync();
                if (createMissingDimensionsResult != ErrorCodes.Succes)
                    return;

                var journalLinesGroupedBySeries = remainingLines.GroupBy(x => x.Series ?? String.Empty).ToList();

                foreach (var journalLinesGroup in journalLinesGroupedBySeries)
                {
                    currentSeries = journalLinesGroup.Key;
                    var journal = journals[_seriesJournalsMap[journalLinesGroup.Key]];

                    var groupLines = journalLinesGroup.ToList();
                    foreach (var journalLine in groupLines)
                        journalLine.SetMaster(journal);

                    ErrorCodes result;
                    if (post)
                    {
                        var postResult = await _postingAPI.PostDailyJournal(journal, DateTime.Today, System.IO.Path.GetFileName(_filePath), groupLines.Count, lines: groupLines);
                        result = postResult.Err;
                        if (result != ErrorCodes.Succes)
                        {
                            //The line number of the error counts within this series. It is changed to the line of the
                            //grid as shown after the transferred series have been removed (the grid is sorted by
                            //LineNumber). The grid changes after this message, so it does not go to the line itself.
                            var errorLine = postResult.Counter;
                            if (errorLine > 0 && errorLine <= groupLines.Count)
                            {
                                var shownLines = remainingLines.Where(l => !transferredSeries.Contains(l.Series ?? String.Empty)).OrderBy(l => l.LineNumber).ToList();
                                postResult.Counter = shownLines.IndexOf(groupLines[errorLine - 1]) + 1;
                            }
                            Utility.ShowJournalError(postResult, dgGLJournalLines, false);
                        }
                    }
                    else
                    {
                        result = await InsertLinesAsync(groupLines);
                        if (result != ErrorCodes.Succes)
                            UtilDisplay.ShowErrorCode(result);
                    }

                    if (result != ErrorCodes.Succes)
                        return;

                    transferredSeries.Add(journalLinesGroup.Key);
                }

                completed = true;

                //Set imported flag on the SIE file without touching anything else in it
                var flagSet = SetImportedFlag();

                //Disable menu items and clear fields
                ResetPage();

                if (flagSet)
                    UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("ImportCompleted"), Uniconta.ClientTools.Localization.lookup("Succes"));
            }
            finally
            {
                busyIndicator.IsBusy = false;

                if (!completed && transferredSeries.Any())
                {
                    remainingLines.RemoveAll(l => transferredSeries.Contains(l.Series ?? String.Empty));
                    dgGLJournalLines.ItemsSource = remainingLines;
                    UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("SIEPartiallyTransferred"), string.Join(", ", transferredSeries), currentSeries),
                                            Uniconta.ClientTools.Localization.lookup("Warning"));
                }
            }
        }

        /// <summary>
        /// Checks the journals before anything is created or transferred. The server only uses the dates of the
        /// lines in a journal whose date is "Free"; other journals would post every voucher on one date. A journal
        /// that generates voucher numbers replaces the voucher numbers of the file, which the user has to accept.
        /// </summary>
        /// <returns>true when the import can continue</returns>
        private bool CheckJournals(IEnumerable<Uniconta.DataModel.GLDailyJournal> journals)
        {
            var journalList = journals.ToList();

            var notFreeDate = journalList.Where(j => j._DateFunction != GLJournalDate.Free).Select(j => j._Journal).ToList();
            if (notFreeDate.Any())
            {
                UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("SIEJournalDateNotFree"), string.Join(", ", notFreeDate)),
                                        Uniconta.ClientTools.Localization.lookup("Error"));
                return false;
            }

            var generatesVouchers = journalList.Where(j => j._GenerateVoucher).Select(j => j._Journal).ToList();
            if (generatesVouchers.Any())
            {
                var confirmation = UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("SIEJournalGeneratesVouchers"), string.Join(", ", generatesVouchers)) + Environment.NewLine + Uniconta.ClientTools.Localization.lookup("AreYouSureToContinue"),
                                                           Uniconta.ClientTools.Localization.lookup("Warning"),
                                                           MessageBoxButton.YesNo);
                if (confirmation != MessageBoxResult.Yes)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// The lines have no VAT code, because a SIE file has the VAT as lines of its own. The server rejects a line
        /// without VAT code on an account with mandatory or fixed VAT, so these accounts are reported before
        /// accounts or dimension values are created.
        /// </summary>
        /// <param name="askToContinue">Ask whether to continue anyway (lines that are only saved), instead of stopping</param>
        /// <param name="confirmed">True when the user has been asked and answered Yes</param>
        /// <returns>true when the import can continue</returns>
        private bool CheckMandatoryVat(IEnumerable<SIEGLDailyJournalLine> lines, bool askToContinue, out bool confirmed)
        {
            confirmed = false;
            var lineAccounts = new HashSet<string>(lines.Select(l => l.Account).Where(a => !String.IsNullOrEmpty(a)));
            var vatAccounts = _glAccounts
                .Where(a => lineAccounts.Contains(a.Account) && (a._MandatoryTax == VatOptions.Mandatory || a._MandatoryTax == VatOptions.Fixed))
                .Select(a => a.Account)
                .OrderBy(a => a)
                .ToList();

            if (!vatAccounts.Any())
                return true;

            var shownAccounts = string.Join(", ", vatAccounts.Take(20));
            if (vatAccounts.Count > 20)
                shownAccounts += " ...";
            var message = Uniconta.ClientTools.Localization.lookup("AccountHasMandatoryVat") + ": " + shownAccounts + Environment.NewLine + Uniconta.ClientTools.Localization.lookup("SIEAccountsMandatoryVat");

            if (!askToContinue)
            {
                UnicontaMessageBox.Show(message, Uniconta.ClientTools.Localization.lookup("Error"));
                return false;
            }

            var confirmation = UnicontaMessageBox.Show(message + Environment.NewLine + Environment.NewLine + Uniconta.ClientTools.Localization.lookup("AreYouSureToContinue"),
                                                       Uniconta.ClientTools.Localization.lookup("Warning"),
                                                       MessageBoxButton.YesNo);
            confirmed = confirmation == MessageBoxResult.Yes;
            return confirmed;
        }

        /// <summary>
        /// Insert lines in bulk of 200 lines. When a bulk fails, the bulks already inserted are deleted again,
        /// so that the lines of a series are inserted completely or not at all.
        /// </summary>
        private async Task<ErrorCodes> InsertLinesAsync(List<SIEGLDailyJournalLine> lines)
        {
            var insertedLines = new List<SIEGLDailyJournalLine>();
            for (int i = 0; i < lines.Count; i += 200)
            {
                var partialLines = lines.Skip(i).Take(200).ToList();
                var linesInsertResult = await api.Insert(partialLines);
                if (linesInsertResult != ErrorCodes.Succes)
                {
                    if (insertedLines.Count > 0)
                    {
                        var deleteResult = await api.Delete(insertedLines);
                        if (deleteResult != ErrorCodes.Succes)
                            UtilDisplay.ShowErrorCode(deleteResult);
                    }
                    return linesInsertResult;
                }
                insertedLines.AddRange(partialLines);
            }

            return ErrorCodes.Succes;
        }

        /// <summary>
        /// Sets #FLAGGA 1 in the imported file, as the standard requires from an importing program.
        /// When the flag cannot be set, the user is told and false is returned.
        /// </summary>
        private bool SetImportedFlag()
        {
            string reason = null;
            try
            {
                if (SIEDocument.SetImportedFlag(_filePath))
                    return true;
            }
            catch (Exception ex)
            {
                reason = ex.Message;
            }

            var message = Uniconta.ClientTools.Localization.lookup("SIEFlagNotSet");
            if (reason != null)
                message += Environment.NewLine + reason;
            UnicontaMessageBox.Show(message, Uniconta.ClientTools.Localization.lookup("Warning"));
            return false;
        }

        /// <summary>
        /// Map number series to journals
        /// </summary>
        private void MapSeriesToJournals()
        {
            if (_seriesJournalsMap == null)
                return;

            var cwMapSeriesToJournals = new CWSIEMapSeriesToJournals(api, _seriesJournalsMap);
            cwMapSeriesToJournals.Closed += (sender, args) =>
            {
                if (cwMapSeriesToJournals.DialogResult == false)
                    return;

                _seriesJournalsMap = cwMapSeriesToJournals.SeriesJournalsMap.ToDictionary(x => x.Serie, x => x.Journal);
            };

            cwMapSeriesToJournals.Show();
        }

        /// <summary>
        /// Validates SIE file data against the standard and against Uniconta
        /// </summary>
        /// <returns>true when the import can continue</returns>
        private bool Validate()
        {
            var errors = _sieDocument.ValidationExceptions;

            //Not a SIE file at all
            if (errors.OfType<SIEInvalidFileException>().Any())
            {
                UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("InvalidFile"), Uniconta.ClientTools.Localization.lookup("Error"));
                return false;
            }

            //The file announced a control total but it is missing: the file is truncated (spec 10.6)
            if (errors.OfType<SIETruncatedFileException>().Any())
            {
                UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("SIETruncatedFile"), Uniconta.ClientTools.Localization.lookup("Error"));
                return false;
            }

            //Only type 4 files carry vouchers
            if (_sieDocument.VER.Count == 0)
            {
                UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("SIENoVouchers"), Uniconta.ClientTools.Localization.lookup("Error"));
                return false;
            }

            //If file is already imported, confirm to reimport
            if (_sieDocument.FLAGGA == 1)
            {
                var reimportConfirmation = UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("SIEConfirmReimport"),
                                                                   Uniconta.ClientTools.Localization.lookup("Confirmation"),
                                                                   MessageBoxButton.YesNo);
                if (reimportConfirmation != MessageBoxResult.Yes)
                    return false;
            }

            //A file with a byte order mark is not in PC8. This is asked first, because it can also explain a control
            //total that does not match.
            var encodingIssue = errors.OfType<SIEEncodingException>().FirstOrDefault();
            if (encodingIssue != null)
            {
                var encodingConfirmation = UnicontaMessageBox.Show(GetIssueText(encodingIssue) + Environment.NewLine + Environment.NewLine + Uniconta.ClientTools.Localization.lookup("AreYouSureToContinue"),
                                                                   Uniconta.ClientTools.Localization.lookup("Confirmation"),
                                                                   MessageBoxButton.YesNo);
                if (encodingConfirmation != MessageBoxResult.Yes)
                    return false;
            }

            //If KSUMMA is present, the checksum has been validated while reading
            if (_sieDocument.HasKSUMMA && !_sieDocument.KSUMMAValid)
            {
                var importInvalidCheckSumConfirmation = UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("SIEKSummaNotMatching") + Environment.NewLine + Uniconta.ClientTools.Localization.lookup("AreYouSureToContinue"),
                                                                                Uniconta.ClientTools.Localization.lookup("Confirmation"),
                                                                                MessageBoxButton.YesNo);

                if (importInvalidCheckSumConfirmation != MessageBoxResult.Yes)
                    return false;
            }

            //The voucher date is mandatory: a voucher without one cannot be posted
            var undatedVouchers = _sieDocument.VER.Where(v => v.VoucherDate == DateTime.MinValue).ToList();
            if (undatedVouchers.Any())
            {
                var shownVouchers = string.Join(", ", undatedVouchers.Take(20).Select(v => (v.Series + " " + v.Number).Trim()));
                if (undatedVouchers.Count > 20)
                    shownVouchers += " ...";
                UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("SIEVoucherDateMissing"), shownVouchers), Uniconta.ClientTools.Localization.lookup("Error"));
                return false;
            }

            //Currency of the file should be the currency of the company (SEK is assumed when missing)
            var fileCurrency = String.IsNullOrWhiteSpace(_sieDocument.VALUTA) ? "SEK" : _sieDocument.VALUTA.Trim().ToUpperInvariant();
            var companyCurrency = api.CompanyEntity._CurrencyId.ToString().ToUpperInvariant();
            if (fileCurrency != companyCurrency)
            {
                var currencyConfirmation = UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("SIEDiffersFromCompanyOBJ"), Uniconta.ClientTools.Localization.lookup("Currency"), fileCurrency, companyCurrency) + Environment.NewLine + Uniconta.ClientTools.Localization.lookup("AreYouSureToContinue"),
                                                                   Uniconta.ClientTools.Localization.lookup("Confirmation"),
                                                                   MessageBoxButton.YesNo);
                if (currencyConfirmation != MessageBoxResult.Yes)
                    return false;
            }

            //Corporate identification number of the file should be the one of the company
            var fileOrgNr = SIEHelpers.DigitsOnly(_sieDocument.FNAMN.OrgIdentifier);
            var companyOrgNr = SIEHelpers.DigitsOnly(SIEHelpers.FormatOrgNumber(api.CompanyEntity._Id));
            if (fileOrgNr.Length > 0 && companyOrgNr.Length > 0 && fileOrgNr != companyOrgNr)
            {
                var orgNrConfirmation = UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("SIEDiffersFromCompanyOBJ"), Uniconta.ClientTools.Localization.lookup("CompanyRegNo"), _sieDocument.FNAMN.OrgIdentifier, api.CompanyEntity._Id) + Environment.NewLine + Uniconta.ClientTools.Localization.lookup("AreYouSureToContinue"),
                                                                Uniconta.ClientTools.Localization.lookup("Confirmation"),
                                                                MessageBoxButton.YesNo);
                if (orgNrConfirmation != MessageBoxResult.Yes)
                    return false;
            }

            //Remaining issues (unbalanced vouchers, invalid dates or amounts, ...) are shown and the user decides
            var issues = errors.Where(e => !(e is SIEInvalidChecksumException) && !(e is SIEEncodingException)).Select(GetIssueText).Distinct().ToList();
            if (issues.Any())
            {
                var shown = string.Join(Environment.NewLine, issues.Take(MaxShownIssues));
                if (issues.Count > MaxShownIssues)
                    shown += Environment.NewLine + "...";

                var issuesConfirmation = UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("SIEValidationIssues"), issues.Count) + Environment.NewLine + Environment.NewLine + shown + Environment.NewLine + Environment.NewLine + Uniconta.ClientTools.Localization.lookup("AreYouSureToContinue"),
                                                                 Uniconta.ClientTools.Localization.lookup("Confirmation"),
                                                                 MessageBoxButton.YesNo);
                if (issuesConfirmation != MessageBoxResult.Yes)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// The text shown for a problem found while reading the file: a label with the line or voucher it concerns.
        /// </summary>
        private static string GetIssueText(Exception e)
        {
            var detail = (e as SIEException)?.Detail;
            if (detail == null)
                return e.Message;

            //Generic labels where they exist: the label names the problem, the line or voucher follows
            if (e is SIEInvalidFieldException)
                return string.Format(Uniconta.ClientTools.Localization.lookup("InvalidValue"), detail, String.Empty).TrimEnd();
            if (e is SIEDateException)
                return string.Format(Uniconta.ClientTools.Localization.lookup("InvalidOBJ"), Uniconta.ClientTools.Localization.lookup("Date")) + ": " + detail;
            if (e is SIEMissingMandatoryDateException)
                return string.Format(Uniconta.ClientTools.Localization.lookup("MissingOBJ"), Uniconta.ClientTools.Localization.lookup("Date")) + ": " + detail;
            if (e is SIEVoucherMissmatchException)
                return string.Format(Uniconta.ClientTools.Localization.lookup("VoucherNotBalancedOBJ"), detail);

            string key = null;
            if (e is SIEMissingObjectException)
                key = "SIEIssueInvalidObject";
            else if (e is SIEStructureException)
                key = "SIEIssueStructure";
            else if (e is SIEInvalidFeatureException)
                key = "SIEIssueNotAllowedInType";
            else if (e is SIEEncodingException)
                key = "SIEIssueNotPC8";

            return key == null ? e.Message : string.Format(Uniconta.ClientTools.Localization.lookup(key), detail);
        }

        /// <summary>
        /// Create missing GL accounts
        /// </summary>
        /// <returns></returns>
        private async Task<ErrorCodes> CreateMissingGLAccountsAsync()
        {
            var glAccounts = dgGLAccounts.ItemsSource as IEnumerable<SIEGLAccount>;
            if (glAccounts == null ||
                !glAccounts.Any())
                return ErrorCodes.Succes;

            var missingGLAccounts = glAccounts.Where(x => x.IsNew).ToList();
            if (!missingGLAccounts.Any())
                return ErrorCodes.Succes;

            var missingGLAccountsStr = string.Join("; ", missingGLAccounts.Select(x => x.KeyStr).ToList());
            var createGLAccountsConfirmation = UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("NotExistWillBeCreatedOBJ"), Uniconta.ClientTools.Localization.lookup("Accounts"), missingGLAccountsStr) + Environment.NewLine + Uniconta.ClientTools.Localization.lookup("AreYouSureToContinue"),
                                                                       Uniconta.ClientTools.Localization.lookup("Confirmation"),
                                                                       MessageBoxButton.YesNo);

            if (createGLAccountsConfirmation != MessageBoxResult.Yes)
                return ErrorCodes.NoSucces;

            //The SRU code of the file goes to the company's SRU field, when the company has one
            foreach (var account in missingGLAccounts)
                if (!String.IsNullOrEmpty(account.SRU))
                    SIEAccountFieldsHelpers.SetSRU(account, account.SRU);

            var insertGLAccountsResult = await api.Insert(missingGLAccounts);
            if (insertGLAccountsResult != ErrorCodes.Succes)
            {
                UtilDisplay.ShowErrorCode(insertGLAccountsResult);
                return insertGLAccountsResult;
            }

            foreach (var account in missingGLAccounts)
                account.IsNew = false;

            return ErrorCodes.Succes;
        }

        /// <summary>
        /// Create missing dimension values
        /// Before executing this function, we ensure that the dimensions of the file are mapped to the company dimensions
        /// </summary>
        /// <returns></returns>
        private async Task<ErrorCodes> CreateMissingDimensionValuesAsync()
        {
            var dimensions = dgGLDimensions.ItemsSource as IEnumerable<SIEDimensionValue>;
            if (dimensions == null ||
                !dimensions.Any())
                return ErrorCodes.Succes;

            var missingDimensions = dimensions.Where(x => x.IsNew).ToList();
            if (!missingDimensions.Any())
                return ErrorCodes.Succes;

            var missingDimensionsStr = string.Join("; ", missingDimensions.Take(20).Select(x => x.DimType + " " + x.Dimension));
            if (missingDimensions.Count > 20)
                missingDimensionsStr += " ...";
            var createDimensionsConfirmation = UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("SIEMissingDimensionValues"), missingDimensionsStr) + Environment.NewLine + Uniconta.ClientTools.Localization.lookup("AreYouSureToContinue"),
                                                                       Uniconta.ClientTools.Localization.lookup("Confirmation"),
                                                                       MessageBoxButton.YesNo);

            if (createDimensionsConfirmation != MessageBoxResult.Yes)
                return ErrorCodes.NoSucces;

            var dimensionValuesToInsert = new List<UnicontaBaseEntity>();

            foreach (var dimValue in missingDimensions)
            {
                GLDimType glDim = null;

                if (dimValue.DimType == "1")
                    glDim = ToGLDim<GLDimType1Client>(dimValue);
                else if (dimValue.DimType == "2")
                    glDim = ToGLDim<GLDimType2Client>(dimValue);
                else if (dimValue.DimType == "3")
                    glDim = ToGLDim<GLDimType3Client>(dimValue);
                else if (dimValue.DimType == "4")
                    glDim = ToGLDim<GLDimType4Client>(dimValue);
                else if (dimValue.DimType == "5")
                    glDim = ToGLDim<GLDimType5Client>(dimValue);

                if (glDim != null)
                    dimensionValuesToInsert.Add(glDim as UnicontaBaseEntity);
            }

            if (!dimensionValuesToInsert.Any())
                return ErrorCodes.Succes;

            //Insert new dimensions
            var dimensionValuesInsertResult = await api.Insert(dimensionValuesToInsert);
            if (dimensionValuesInsertResult != ErrorCodes.Succes)
            {
                UtilDisplay.ShowErrorCode(dimensionValuesInsertResult);
                return dimensionValuesInsertResult;
            }

            foreach (var dimValue in missingDimensions)
                dimValue.IsNew = false;

            return ErrorCodes.Succes;
        }

        private static SIEDimensionValue ToDimensionValue(GLDimType dim)
        {
            var dimValue = new SIEDimensionValue();
            dimValue.Dimension = dim.KeyStr;
            dimValue.Name = dim.KeyName;
            dimValue.DimType = dim.__DimType().ToString();

            return dimValue;
        }

        private T ToGLDim<T>(SIEDimensionValue dimValue) where T : GLDimType, UnicontaBaseEntity, new()
        {
            var glDim = new T();
            glDim.CompanyId = api.CompanyId;
            glDim._Dim = dimValue.Dimension;
            glDim._Name = dimValue.Name;

            return glDim;
        }

        /// <summary>
        /// Resets the page grids, document and buttons
        /// </summary>
        private void ResetPage()
        {
            _seriesJournalsMap = null;
            _sieDocument = null;
            _dimensionMap = null;
            _filePath = String.Empty;

            dgGLJournalLines.ItemsSource = null;
            dgGLJournalLines.Visibility = Visibility.Collapsed;

            dgGLAccounts.ItemsSource = null;
            dgGLAccounts.Visibility = Visibility.Collapsed;

            dgGLDimensions.ItemsSource = null;
            dgGLDimensions.Visibility = Visibility.Collapsed;

            localMenu.DisableButtons("MapSeriesToJournals");
            localMenu.DisableButtons("Simulate");
            localMenu.DisableButtons("SaveData");
            localMenu.DisableButtons("PostJournal");

            //Reset ribbon information
            SetRibbonItemValue(Uniconta.ClientTools.Localization.lookup("FromDate"), String.Empty);
            SetRibbonItemValue(Uniconta.ClientTools.Localization.lookup("ToDate"), String.Empty);
            SetRibbonItemValue(Uniconta.ClientTools.Localization.lookup("Type"), String.Empty);
            SetRibbonItemValue(Uniconta.ClientTools.Localization.lookup("Application"), String.Empty);
            SetRibbonItemValue(Uniconta.ClientTools.Localization.lookup("CreatedBy"), String.Empty);
            SetRibbonItemValue(Uniconta.ClientTools.Localization.lookup("CompanyName"), String.Empty);
            SetRibbonItemValue(Uniconta.ClientTools.Localization.lookup("CompanyRegNo"), String.Empty);
            SetRibbonItemValue(Uniconta.ClientTools.Localization.lookup("CompanyId"), String.Empty);
            SetRibbonItemValue(Uniconta.ClientTools.Localization.lookup("address"), String.Empty);
            SetRibbonItemValue(Uniconta.ClientTools.Localization.lookup("CurrencyCode"), String.Empty);
        }

        protected override LookUpTable HandleLookupOnLocalPage(LookUpTable lookup, CorasauDataGrid dg)
        {
            if (dg != dgGLJournalLines || dg.CurrentColumn?.FieldName != nameof(SIEGLDailyJournalLine.Account))
                return base.HandleLookupOnLocalPage(lookup, dg);

            lookup.TableType = typeof(Uniconta.DataModel.GLAccount);
            return lookup;
        }


        private void SetRibbonItemValue(string caption, string statusValue)
        {
            var rb = (RibbonBase)localMenu.DataContext;
            var groups = UtilDisplay.GetMenuCommandsByStatus(rb, true);

            var grpRibbonItem = groups.FirstOrDefault(x => x.Caption == caption);
            if (grpRibbonItem != null)
                grpRibbonItem.StatusValue = statusValue;
        }

        #region SIE Helpers

        /// <summary>
        /// Converts SIE vouchers to journal lines
        /// </summary>
        private List<SIEGLDailyJournalLine> ConvertVERToJournalLines(List<SIEVoucher> vouchers)
        {
            var journalLines = new List<SIEGLDailyJournalLine>();
            var lineNumber = 1;

            //Voucher numbers: use the number from the file when it is numeric and no other series uses it.
            //Lines with the same voucher number are posted as one voucher, so A 1 and B 1 must not share one.
            //Other vouchers (without a usable number, allowed in 4I files, or colliding with an earlier series)
            //get running numbers after the highest number in the file, so that each voucher stays together.
            var voucherNumbers = new Dictionary<SIEVoucher, int>();
            var seriesOfNumber = new Dictionary<int, string>();
            var maxVoucher = 0;
            foreach (var ver in vouchers)
            {
                int voucher;
                if (TryParseVoucherNumber(ver.Number, out voucher))
                    maxVoucher = Math.Max(maxVoucher, voucher);
            }

            foreach (var ver in vouchers)
            {
                int voucher;
                if (!TryParseVoucherNumber(ver.Number, out voucher))
                    continue;

                string series;
                var verSeries = ver.Series ?? String.Empty;
                if (!seriesOfNumber.TryGetValue(voucher, out series))
                    seriesOfNumber[voucher] = verSeries;
                else if (series != verSeries)
                    continue;

                voucherNumbers[ver] = voucher;
            }

            foreach (var ver in vouchers)
            {
                int voucher;
                if (!voucherNumbers.TryGetValue(ver, out voucher))
                {
                    voucher = ++maxVoucher;
                    voucherNumbers[ver] = voucher;
                }

                foreach (var trans in ver.Rows)
                {
                    var journalLine = new SIEGLDailyJournalLine();
                    journalLine.SetMaster(api.CompanyEntity);
                    journalLine.LineNumber = lineNumber++;

                    journalLine.Series = ver.Series;
                    journalLine.Number = ver.Number;

                    journalLine.Voucher = voucher;

                    journalLine.Date = trans.RowDate;
                    journalLine._AccountTypeEnum = GLJournalAccountType.Finans;
                    journalLine.Account = trans.Account.Number;
                    journalLine.SIEAccountName = trans.Account.Name;
                    journalLine.Amount = (double)trans.Amount;
                    journalLine.Text = String.IsNullOrEmpty(trans.Text) ? ver.Text : trans.Text;
                    journalLine.Qty = trans.Quantity.HasValue ? (double)trans.Quantity.Value : 0;
                    journalLine.Note = trans.CreatedBy;

                    //Set dimensions according to the dimension number of each object
                    if (trans.Objects != null)
                    {
                        foreach (var obj in trans.Objects)
                        {
                            int dimIndex;
                            if (!_dimensionMap.TryGetValue(obj.Dimension.Number, out dimIndex))
                                continue;

                            switch (dimIndex)
                            {
                                case 1: journalLine.Dimension1 = obj.Number; break;
                                case 2: journalLine.Dimension2 = obj.Number; break;
                                case 3: journalLine.Dimension3 = obj.Number; break;
                                case 4: journalLine.Dimension4 = obj.Number; break;
                                case 5: journalLine.Dimension5 = obj.Number; break;
                            }
                        }
                    }

                    journalLines.Add(journalLine);
                }
            }

            return journalLines;
        }

        private static bool TryParseVoucherNumber(string number, out int voucher)
        {
            voucher = 0;
            if (String.IsNullOrWhiteSpace(number))
                return false;

            var digits = new string(number.Where(char.IsDigit).ToArray());
            return digits.Length > 0 && int.TryParse(digits, out voucher) && voucher > 0;
        }

        /// <summary>
        /// Converts SIE konto to Uniconta GL account
        /// </summary>
        /// <param name="konto"></param>
        /// <returns></returns>
        private SIEGLAccount ConvertKontoToGLAccount(SIEAccount konto)
        {
            var glAccount = new SIEGLAccount();
            glAccount.SetMaster(api.CompanyEntity);

            glAccount.AccountTypeEnum = SIEHelpers.ToUnicontaAccountType(konto.Type, konto.Number);

            glAccount.Account = konto.Number;
            glAccount.Name = konto.Name;

            //Add a static comment "SIE" to new konto, to be able to identify where it came from
            glAccount.Comment = "SIE";

            if (konto.SRU != null &&
                konto.SRU.Any())
                glAccount.SRU = konto.SRU.First();

            var existing = _glAccounts.FirstOrDefault(x => x.Account == glAccount.Account);
            glAccount.IsNew = existing == null;

            //The standard asks the importing program to point out accounts whose name differs
            if (existing != null &&
                !String.IsNullOrEmpty(konto.Name) &&
                !String.Equals(existing.Name, konto.Name, StringComparison.CurrentCultureIgnoreCase))
                glAccount.ExistingName = existing.Name;

            return glAccount;
        }

        /// <summary>
        /// Converts SIE object to Uniconta dimension value
        /// </summary>
        private SIEDimensionValue ConvertSIEDimToUnicontaDim(SIEObject obj)
        {
            var dim = new SIEDimensionValue();
            dim.Dimension = obj.Number;
            dim.Name = String.IsNullOrEmpty(obj.Name) ? obj.Number : obj.Name;
            dim.DimType = _dimensionMap[obj.Dimension.Number].ToString();
            dim.IsNew = !_dimValues.Any(x => x.DimType == dim.DimType && x.Dimension == dim.Dimension);

            return dim;
        }

        /// <summary>
        /// Load file and SIE document
        /// </summary>
        /// <returns></returns>
        private async Task<SIEDocument> GetSieDataAsync()
        {
            var openFileDialog = UtilDisplay.LoadOpenFileDialog;

            openFileDialog.Title = Uniconta.ClientTools.Localization.lookup("UserDocFile");
            openFileDialog.Filter = $"{Uniconta.ClientTools.Localization.lookup("SIEFiles")} (*.se;*.sie;*.si)|*.se;*.sie;*.si|All files (*.*)|*.*";

            bool? result = openFileDialog.ShowDialog();
            if (result != true)
                return null;

            //The previous file is cleared first, so that its lines cannot be posted with the new file's path when
            //reading or mapping the new file fails
            ResetPage();
            _filePath = openFileDialog.FileName;

            var sieDocument = new SIEDocument();
            sieDocument.ThrowErrors = false;
            //#RTRANS rows are always followed by an identical #TRANS row and #BTRANS rows are removed
            //transactions, so both are ignored as the standard permits (11. #RTRANS 5, #BTRANS 4).
            sieDocument.IgnoreRTRANS = true;
            sieDocument.IgnoreBTRANS = true;
            sieDocument.AllowUnderDimensions = true;

            busyIndicator.IsBusy = true;
            try
            {
                await sieDocument.ReadDocumentAsync(_filePath);
            }
            finally
            {
                busyIndicator.IsBusy = false;
            }

            return sieDocument;
        }

        #endregion
    }
}
