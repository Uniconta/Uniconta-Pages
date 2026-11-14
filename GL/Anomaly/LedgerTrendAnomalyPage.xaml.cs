using DevExpress.Xpf.Grid;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.AI.Services;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.ClientTools.DataModel;
using UnicontaClient.Models;
using UnicontaClient.Pages.GL.Anomaly;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class LedgerTrendAnomalyGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(LedgerTrendAnomalyResult); } }
        public override bool Readonly { get { return true; } }
    }

    public partial class LedgerTrendAnomalyPage : GridBasePage
    {
        private List<GLAccountClient> accounts;
        private CancellationTokenSource analysisCancellationTokenSource;
        private bool isAnalyzing;
        private LedgerTrendAnomalyParameters acceptedParameters;

        public IReadOnlyList<LedgerTrendAnomalyResult> Results { get; private set; }

        public override string NameOfControl { get { return TabControls.LedgerTrendAnomalyPage; } }

        public static Task<IReadOnlyList<LedgerTrendAnomalyResult>> FindAnomalies(CrudAPI api, LedgerTrendAnomalyParameters parameters, IProgress<LedgerTrendAnomalyProgress> progress)
        {
            return new LedgerTrendAnomalyFinder().FindAnomalies(api, parameters, progress);
        }

        public LedgerTrendAnomalyPage(SynchronizeEntity syncEntity) : base(syncEntity, true)
        {
            InitializePage(null);
            SetupMaster();
        }

        public LedgerTrendAnomalyPage(IEnumerable<GLAccountClient> accounts)
            : base(accounts == null ? null : accounts.FirstOrDefault())
        {
            InitializePage(accounts);
            SetupMaster();
        }

        public LedgerTrendAnomalyPage(object accounts)
            : this(accounts as IEnumerable<GLAccountClient>)
        {
        }

        public override Task InitQuery() { return null; }

        private void InitializePage(IEnumerable<GLAccountClient> accounts)
        {
            this.accounts = accounts == null ? new List<GLAccountClient>() : accounts.Where(a => a != null).ToList();

            InitializeComponent();
            ((TableView)dgResults.View).RowStyle = System.Windows.Application.Current.Resources["GridRowControlCustomHeightStyle"] as Style;
            localMenu.dataGrid = dgResults;
            SetRibbonControl(localMenu, dgResults);

            dgResults.api = api;
            dgResults.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += Ribbon_OnItemClicked;

            var company = api?.CompanyEntity;
            if (company != null && !company.HasDecimals)
                AmountMinus2.HasDecimals = AmountMinus1.HasDecimals = CurrentAmount.HasDecimals =
                PreviousMovement.HasDecimals = CurrentMovement.HasDecimals = false;

            dgResults.Visibility = Visibility.Visible;
            //Uniconta.ClientTools.AI.Services.GPTServiceFactory.Instance.GetActiveService(api)?.Branding?.update(localMenu, "Analyze");
        }

        private async Task LoadAnomaliesAsync(LedgerTrendAnomalyParameters parameters)
        {
            if (!GPTServiceFactory.IsLedgerTrendAnomalyAnalyzeEnabled(Api.CompanyEntity) || isAnalyzing || parameters == null)
                return;

            isAnalyzing = true;
            analysisCancellationTokenSource = new CancellationTokenSource();
            AIProgressBar.Visibility = Visibility.Visible;
            AIProgressBar.Value = 0;
            StatusText.Text = Uniconta.ClientTools.Localization.lookup("AnomalyAnalysisStarting");
            UpdateAmountHeaders(parameters);

            try
            {
                var progress = new Progress<LedgerTrendAnomalyProgress>(update =>
                {
                    AIProgressBar.Value = update.ProgressValue;
                    StatusText.Text = update.StatusText;
                    if (update.Results != null)
                    {
                        SetResults(update.Results);
                        dgResults.RefreshData();
                    }
                });

                var results = await new LedgerTrendAnomalyFinder().FindAnomalies(api, parameters, progress, analysisCancellationTokenSource.Token).ConfigureAwait(true);
                SetResults(results);
            }
            catch (OperationCanceledException)
            {
                StatusText.Text = Uniconta.ClientTools.Localization.lookup("AnomalyAnalysisCancelled");
            }
            catch (Exception ex)
            {
                SetResults(null);
                Uniconta.ClientTools.InfoLog.Log.Warning(ex.Message);
                UtilDisplay.ShowErrorCode(ErrorCodes.Exception);
            }
            finally
            {
                AIProgressBar.Visibility = Visibility.Collapsed;
                analysisCancellationTokenSource.Dispose();
                analysisCancellationTokenSource = null;
                isAnalyzing = false;
            }
        }

        private void SetupMaster()
        {
            acceptedParameters = null;
            SetHeader(Uniconta.ClientTools.Localization.lookup("AnomalyAnalysis"));
            SetResults(null);
        }

        private void SetResults(IEnumerable<LedgerTrendAnomalyResult> results)
        {
            Results = (results ?? Enumerable.Empty<LedgerTrendAnomalyResult>()).ToList();
            dgResults.ItemsSource = Results;
        }

        private void UpdateAmountHeaders(LedgerTrendAnomalyParameters parameters)
        {
            AmountMinus2.Header = FormatPeriodHeader(parameters.PeriodBeforePreviousFrom, parameters.PeriodBeforePreviousTo);
            AmountMinus1.Header = FormatPeriodHeader(parameters.PreviousPeriodFrom, parameters.PreviousPeriodTo);
            CurrentAmount.Header = FormatPeriodHeader(parameters.CurrentPeriodFrom, parameters.CurrentPeriodTo);
        }

        private static string FormatPeriodHeader(DateTime fromDate, DateTime toDate)
        {
            return string.Format("{0:d} - {1:d}", fromDate, toDate);
        }

        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            SetupMaster();
        }

        private void ShowAnalyzeDialog()
        {
            if (isAnalyzing)
                return;

            var dialog = new CWLedgerTrendAnomalyAnalyze(api, accounts);
            dialog.ParametersAccepted += parameters =>
            {
                acceptedParameters = parameters;
                _ = LoadAnomaliesAsync(parameters);
            };
            dialog.Show();
        }

        private async void Ribbon_OnItemClicked(string action)
        {
            switch (action)
            {
                case "Analyze":
                    ShowAnalyzeDialog();
                    break;

                case "RefreshGrid":
                case "Refresh":
                    if (acceptedParameters == null)
                        ShowAnalyzeDialog();
                    else
                        await LoadAnomaliesAsync(acceptedParameters);
                    break;

                case "Cancel":
                    if (analysisCancellationTokenSource != null)
                    {
                        StatusText.Text = Uniconta.ClientTools.Localization.lookup("AnomalyAnalysisCancelling");
                        analysisCancellationTokenSource.Cancel();
                    }
                    break;

                case "SyncPage":
                    InSyncMode = !InSyncMode;
                    break;

                default:
                    gridRibbon_BaseActions(action);
                    break;
            }
        }

        public override void PageClosing()
        {
            if (analysisCancellationTokenSource != null)
                analysisCancellationTokenSource.Cancel();
            base.PageClosing();
        }
    }
}
