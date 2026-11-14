using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Util;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.Anomaly
{
    public partial class CWLedgerTrendAnomalyAnalyze : ChildWindow, INotifyPropertyChanged
    {
        private readonly CrudAPI api;
        private readonly IReadOnlyList<string> accountNumbers;

        private LedgerTrendAnomalyParameters parameters;

        private DateTime currentPeriodFrom;
        private DateTime currentPeriodTo;
        private DateTime previousPeriodFrom;
        private DateTime previousPeriodTo;
        private DateTime periodBeforePreviousFrom;
        private DateTime periodBeforePreviousTo;


        public event PropertyChangedEventHandler PropertyChanged;
        public event Action<LedgerTrendAnomalyParameters> ParametersAccepted;

        public CWLedgerTrendAnomalyAnalyze(CrudAPI api)
            : this(api, null)
        {
        }

        public CWLedgerTrendAnomalyAnalyze(CrudAPI api, IEnumerable<GLAccountClient> accounts)
        {
            this.api = api;

            InitializeComponent();

            // Keep the form scrollable on small displays while its actions and help stay visible.
            var workArea = SystemParameters.WorkArea;
            Width = Math.Min(Width, workArea.Width);
            Height = Math.Min(Height, workArea.Height);
            MinWidth = Math.Min(MinWidth, Width);
            MinHeight = Math.Min(MinHeight, Height);

            LayoutRoot.AddHandler(
                Keyboard.GotKeyboardFocusEvent,
                new KeyboardFocusChangedEventHandler(Input_GotKeyboardFocus),
                true);

            accountNumbers = accounts == null
                ? new List<string>()
                : accounts
                    .Where(a => a != null && !string.IsNullOrWhiteSpace(a.Account))
                    .Select(a => a.Account)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

            Title = Uniconta.ClientTools.Localization.lookup("AnomalyAnalysis");

            parameters = LedgerTrendAnomalyParameters.CreateDefault(
                DateTime.Today,
                api?.CompanyEntity);

            ApplyParameters(parameters);

            DataContext = this;
        }

        public DateTime CurrentPeriodFrom
        {
            get { return currentPeriodFrom; }
            set
            {
                value = value.Date;

                if (currentPeriodFrom == value)
                    return;

                currentPeriodFrom = value;
                NotifyPropertyChanged(nameof(CurrentPeriodFrom));
            }
        }

        public DateTime CurrentPeriodTo
        {
            get { return currentPeriodTo; }
            set
            {
                value = value.Date;

                if (currentPeriodTo == value)
                    return;

                currentPeriodTo = value;
                NotifyPropertyChanged(nameof(CurrentPeriodTo));
            }
        }

        public DateTime PreviousPeriodFrom
        {
            get { return previousPeriodFrom; }
            set
            {
                value = value.Date;

                if (previousPeriodFrom == value)
                    return;

                previousPeriodFrom = value;
                NotifyPropertyChanged(nameof(PreviousPeriodFrom));
            }
        }

        public DateTime PreviousPeriodTo
        {
            get { return previousPeriodTo; }
            set
            {
                value = value.Date;

                if (previousPeriodTo == value)
                    return;

                previousPeriodTo = value;
                NotifyPropertyChanged(nameof(PreviousPeriodTo));
            }
        }

        public DateTime PeriodBeforePreviousFrom
        {
            get { return periodBeforePreviousFrom; }
            set
            {
                value = value.Date;

                if (periodBeforePreviousFrom == value)
                    return;

                periodBeforePreviousFrom = value;
                NotifyPropertyChanged(nameof(PeriodBeforePreviousFrom));
            }
        }

        public DateTime PeriodBeforePreviousTo
        {
            get { return periodBeforePreviousTo; }
            set
            {
                value = value.Date;

                if (periodBeforePreviousTo == value)
                    return;

                periodBeforePreviousTo = value;
                NotifyPropertyChanged(nameof(PeriodBeforePreviousTo));
            }
        }

        public int MinimumAbsoluteDifference { get; set; }
        public int MinimumPercentageChange { get; set; }
        public int MaximumAnomalies { get; set; }
        public int MaximumTransactionsPerAnomaly { get; set; }

        private void ApplyParameters(LedgerTrendAnomalyParameters source)
        {
            if (source == null)
                return;

            parameters = source;

            ApplyPeriodValues(source);

            MinimumAbsoluteDifference =
                (int)Math.Round(source.MinimumAbsoluteDifference);

            MinimumPercentageChange =
                (int)Math.Round(source.MinimumPercentageChange);

            MaximumAnomalies =
                source.MaximumAnomalies;

            MaximumTransactionsPerAnomaly =
                source.MaximumTransactionsPerAnomaly;

            NotifyPropertyChanged(nameof(MinimumAbsoluteDifference));
            NotifyPropertyChanged(nameof(MinimumPercentageChange));
            NotifyPropertyChanged(nameof(MaximumAnomalies));
            NotifyPropertyChanged(nameof(MaximumTransactionsPerAnomaly));
        }

        private void ApplyPeriodValues(LedgerTrendAnomalyParameters source)
        {
            CurrentPeriodFrom = source.CurrentPeriodFrom;
            CurrentPeriodTo = source.CurrentPeriodTo;
            PreviousPeriodFrom = source.PreviousPeriodFrom;
            PreviousPeriodTo = source.PreviousPeriodTo;
            PeriodBeforePreviousFrom = source.PeriodBeforePreviousFrom;
            PeriodBeforePreviousTo = source.PeriodBeforePreviousTo;
        }

        private void PreviousMonthButton_Click(object sender, RoutedEventArgs e)
        {
            MovePeriods(-1);
        }

        private void NextMonthButton_Click(object sender, RoutedEventArgs e)
        {
            MovePeriods(1);
        }

        private void MovePeriods(int months)
        {
            Keyboard.ClearFocus();
            SyncPeriodValuesToParameters();
            parameters.MovePeriods(months);
            ApplyPeriodValues(parameters);
        }

        private void SyncPeriodValuesToParameters()
        {
            parameters.CurrentPeriodFrom =
                CurrentPeriodFrom;

            parameters.CurrentPeriodTo =
                CurrentPeriodTo;

            parameters.PreviousPeriodFrom =
                PreviousPeriodFrom;

            parameters.PreviousPeriodTo =
                PreviousPeriodTo;

            parameters.PeriodBeforePreviousFrom =
                PeriodBeforePreviousFrom;

            parameters.PeriodBeforePreviousTo =
                PeriodBeforePreviousTo;
        }

        public LedgerTrendAnomalyParameters CreateParameters()
        {
            if (parameters == null)
                parameters = new LedgerTrendAnomalyParameters();

            parameters.CurrentPeriodFrom =
                CurrentPeriodFrom;

            parameters.CurrentPeriodTo =
                CurrentPeriodTo;

            parameters.PreviousPeriodFrom =
                PreviousPeriodFrom;

            parameters.PreviousPeriodTo =
                PreviousPeriodTo;

            parameters.PeriodBeforePreviousFrom =
                PeriodBeforePreviousFrom;

            parameters.PeriodBeforePreviousTo =
                PeriodBeforePreviousTo;

            parameters.MinimumAbsoluteDifference =
                MinimumAbsoluteDifference;

            parameters.MinimumPercentageChange =
                MinimumPercentageChange;

            parameters.MaximumAnomalies =
                MaximumAnomalies;

            parameters.MaximumTransactionsPerAnomaly =
                MaximumTransactionsPerAnomaly;

            parameters.AccountNumbers =
                accountNumbers;

            return parameters;
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            Keyboard.ClearFocus();

            if (!ValidateDates())
                return;

            var result = CreateParameters();

            LastValue.Pack(
                api?.CompanyEntity,
                result);

            var handler = ParametersAccepted;
            if (handler != null)
                handler(result);

            SetDialogResult(true);
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            if (parameters == null)
                parameters = new LedgerTrendAnomalyParameters();

            parameters.Reset(DateTime.Today);

            ApplyParameters(parameters);
        }

        private bool ValidateDates()
        {
            if (CurrentPeriodTo < CurrentPeriodFrom ||
                PreviousPeriodTo < PreviousPeriodFrom ||
                PeriodBeforePreviousTo < PeriodBeforePreviousFrom)
            {
                Uniconta.ClientTools.InfoLog.Log.Warning(
                    Uniconta.ClientTools.Localization.lookup("AnomalyEachLedgerAnomalyPeriodMustHaveAFrom"));

                return false;
            }

            return true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }

        private void Input_GotKeyboardFocus(
            object sender,
            KeyboardFocusChangedEventArgs e)
        {
            var element = e.NewFocus as FrameworkElement;

            while (element != null)
            {
                if (element.Tag is string key &&
                    key.StartsWith("AnomalyHelp", StringComparison.Ordinal))
                {
                    HelpStatusText.Text =
                        Uniconta.ClientTools.Localization.lookup(key);

                    return;
                }

                element =
                    (element.Parent ??
                     VisualTreeHelper.GetParent(element))
                    as FrameworkElement;
            }
        }

        private void ChildWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                SetDialogResult(false);
        }

        private void NotifyPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;

            if (handler != null)
                handler(
                    this,
                    new PropertyChangedEventArgs(propertyName));
        }
    }
}