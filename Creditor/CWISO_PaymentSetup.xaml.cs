using System;
using System.Windows;
using System.Windows.Input;
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.Common;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    /// <summary>
    /// Common setup dialog for all ISO 20022 payment formats
    /// </summary>
    public partial class CWISO_PaymentSetup : ChildWindow
    {
        public CreditorPaymentFormatClient paymentFormatISO { get; private set; }
        readonly ExportFormatType exportFormat;

        public CWISO_PaymentSetup(CrudAPI api, CreditorPaymentFormatClient paymentFormat)
        {
            exportFormat = (ExportFormatType)paymentFormat._ExportFormat;
            paymentFormatISO = CreatePaymentFormat(exportFormat);
            StreamingManager.Copy(paymentFormat, paymentFormatISO);

            InitializeComponent();
            this.DataContext = paymentFormatISO;
            cmbBank.ItemsSource = Enum.GetValues(BankEnumType(exportFormat));

            txtBatchBookingHelp.Text = HelpText("BatchBookingHelp");
            txtTestMarkedHelp.Text = HelpText("TestMarkedHelp");
            var pain09Help = HelpText("Pain09Help");
            var pain09HelpCountry = HelpText("Pain09Help" + exportFormat.ToString().Replace("ISO20022_", "")); //Country specific text, e.g. Pain09HelpDK
            if (pain09HelpCountry != null)
                pain09Help = pain09Help != null ? string.Concat(pain09Help, " ", pain09HelpCountry) : pain09HelpCountry;
            txtNewPaymentFormatHelp.Text = pain09Help;
            liNewPaymentFormatHelp.Visibility = pain09Help != null ? Visibility.Visible : Visibility.Collapsed;

            this.Title = string.Format(Uniconta.ClientTools.Localization.lookup("SetupOBJ"), Uniconta.ClientTools.Localization.lookup("Payment"));
            this.Loaded += CW_Loaded;
        }

        static CreditorPaymentFormatClient CreatePaymentFormat(ExportFormatType exportFormat)
        {
            switch (exportFormat)
            {
                case ExportFormatType.ISO20022_DK: return new CreditorPaymentFormatClientISODK();
                case ExportFormatType.ISO20022_NO: return new CreditorPaymentFormatClientISONO();
                case ExportFormatType.ISO20022_SE: return new CreditorPaymentFormatClientISOSE();
                case ExportFormatType.ISO20022_NL: return new CreditorPaymentFormatClientISONL();
                case ExportFormatType.ISO20022_DE: return new CreditorPaymentFormatClientISODE();
                case ExportFormatType.ISO20022_EE: return new CreditorPaymentFormatClientISOEE();
                case ExportFormatType.ISO20022_UK: return new CreditorPaymentFormatClientISOUK();
                case ExportFormatType.ISO20022_LT: return new CreditorPaymentFormatClientISOLT();
                case ExportFormatType.ISO20022_CH: return new CreditorPaymentFormatClientISOCH();
                default: return null;
            }
        }

        static Type BankEnumType(ExportFormatType exportFormat)
        {
            switch (exportFormat)
            {
                case ExportFormatType.ISO20022_DK: return typeof(dkBank);
                case ExportFormatType.ISO20022_NO: return typeof(noBank);
                case ExportFormatType.ISO20022_SE: return typeof(seBank);
                case ExportFormatType.ISO20022_NL: return typeof(nlBank);
                case ExportFormatType.ISO20022_DE: return typeof(deBank);
                case ExportFormatType.ISO20022_EE: return typeof(eeBank);
                case ExportFormatType.ISO20022_UK: return typeof(ukBank);
                case ExportFormatType.ISO20022_LT: return typeof(ltBank);
                case ExportFormatType.ISO20022_CH: return typeof(chBank);
                default: return null;
            }
        }

        /// <summary>
        /// Must match BankSpecificSettings*.BatchBooking()
        /// </summary>
        static bool ShowBatchBooking(ExportFormatType exportFormat, object bank)
        {
            switch (exportFormat)
            {
                case ExportFormatType.ISO20022_DK: return (dkBank)bank == dkBank.Danske_Bank || (dkBank)bank == dkBank.Nordea;
                case ExportFormatType.ISO20022_NO: return (noBank)bank == noBank.Danske_Bank;
                case ExportFormatType.ISO20022_SE: return (seBank)bank == seBank.Danske_Bank;
                case ExportFormatType.ISO20022_DE: return (deBank)bank == deBank.Commerzbank;
                case ExportFormatType.ISO20022_UK: return (ukBank)bank == ukBank.Danske_Bank;
                case ExportFormatType.ISO20022_CH: return (chBank)bank != chBank.None;
                case ExportFormatType.ISO20022_NL: return true;
                default: return false;
            }
        }

        /// <summary>
        /// Must match BankSpecificSettings*.TestMarked()
        /// </summary>
        static bool ShowTestMarked(ExportFormatType exportFormat, object bank)
        {
            switch (exportFormat)
            {
                case ExportFormatType.ISO20022_DK: return (dkBank)bank == dkBank.Danske_Bank;
                case ExportFormatType.ISO20022_NO: return (noBank)bank == noBank.Danske_Bank;
                case ExportFormatType.ISO20022_SE: return (seBank)bank == seBank.Danske_Bank;
                default: return false;
            }
        }

        static string HelpText(string label)
        {
            var text = Uniconta.ClientTools.Localization.lookup(label);
            return string.IsNullOrEmpty(text) || text == label ? null : text; //Hide help text until the label exists
        }

        void UpdateVisibility()
        {
            var bank = cmbBank.SelectedItem;
            var showBatchBooking = bank != null && ShowBatchBooking(exportFormat, bank);
            var showTestMarked = bank != null && ShowTestMarked(exportFormat, bank);

            liBatchBooking.Visibility = showBatchBooking ? Visibility.Visible : Visibility.Collapsed;
            liBatchBookingHelp.Visibility = showBatchBooking && !string.IsNullOrEmpty(txtBatchBookingHelp.Text) ? Visibility.Visible : Visibility.Collapsed;
            liTestMarked.Visibility = showTestMarked ? Visibility.Visible : Visibility.Collapsed;
            liTestMarkedHelp.Visibility = showTestMarked && !string.IsNullOrEmpty(txtTestMarkedHelp.Text) ? Visibility.Visible : Visibility.Collapsed;
            lgProcessing.Visibility = showBatchBooking || showTestMarked ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// pain.001.001.03 is the default format and is used when pain.001.001.09 isn't selected
        /// </summary>
        void UpdatePain03()
        {
            cePain03.IsChecked = ceNewPaymentFormat.IsChecked != true;
        }

        private void ceNewPaymentFormat_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            cePain03.IsChecked = !(e.NewValue is bool newValue && newValue);
        }

        private void CW_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateVisibility();
            UpdatePain03();
            Dispatcher.BeginInvoke(new Action(() => { cmbBank.Focus(); }));
        }

        private void ChildWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                SetDialogResult(false);
            }
            else if (e.Key == Key.Enter)
            {
                if (CancelButton.IsFocused)
                {
                    SetDialogResult(false);
                    return;
                }
                OKButton_Click(null, null);
            }
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(true);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }

        private void cmbBank_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            UpdateVisibility();
        }
    }
}
