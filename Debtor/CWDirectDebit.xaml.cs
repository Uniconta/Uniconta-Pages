using UnicontaClient.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class CWDirectDebit : ChildWindow
    {
        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.DebtorPaymentFormat))]
        public DebtorPaymentFormatClient PaymentFormat { get; set; }
        public DirectDebitScheme directDebitScheme { get; set; }
        private bool showSchemeType;
        private bool showActivateWarning;

        CrudAPI Capi;

        public CWDirectDebit(CrudAPI api, string title, bool showSchemeType = false, bool showActivateWarning = false)
        {
            Capi = api;
            this.DataContext = this;
            this.showSchemeType = showSchemeType;
            this.showActivateWarning = showActivateWarning;
            InitializeComponent();

            debPaymentFormat.api = api;
            SetPaymentFormat();
            cmbDirectDebitScheme.ItemsSource = Enum.GetValues(typeof(DirectDebitScheme));

#if !SILVERLIGHT
            this.Title = title;
#endif
#if SILVERLIGHT
            Utility.SetThemeBehaviorOnChildWindow(this);
#endif
            this.Loaded += CW_Loaded;
            SetDefaultPaymentFormat();
            DirectDebitSchemeVisible();
        }

        private async void SetPaymentFormat()
        {
            var paymentFormats = await Capi.Query<DebtorPaymentFormatClient>();
            debPaymentFormat.ItemsSource = paymentFormats;
        }


        private void ShowActivateWarning()
        {
            if (showActivateWarning && this.PaymentFormat != null && ((DebtorPaymFormatType)this.PaymentFormat?._ExportFormat != DebtorPaymFormatType.SEPA))
            {
                txtActivateWarning.Visibility = Visibility.Visible;
                txtActivateWarning.Text = Uniconta.ClientTools.Localization.lookup("DirectDebitActivateWarning");
            }
            else
            {
                rowWarning.Height = new GridLength(0);
                double h = this.Height - 120;
                this.Height = h;
            }
        }

        private void DirectDebitSchemeVisible()
        {
            if (showSchemeType && this.PaymentFormat != null && ((DebtorPaymFormatType)this.PaymentFormat?._ExportFormat == DebtorPaymFormatType.SEPA))
            {
                lblDirectDebitScheme.Visibility = Visibility.Visible;
                cmbDirectDebitScheme.Visibility = Visibility.Visible;

                cmbDirectDebitScheme.SelectedIndex = (int)DirectDebitScheme.CORE;
            }
            else
            {
                lblDirectDebitScheme.Visibility = Visibility.Collapsed;
                cmbDirectDebitScheme.Visibility = Visibility.Collapsed;
            }

            ShowActivateWarning();
        }

        private void CW_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => { OKButton.Focus(); }));
        }
        async void SetDefaultPaymentFormat()
        {
            var cache = Capi.CompanyEntity.GetCache(typeof(Uniconta.DataModel.DebtorPaymentFormat));
            if (cache == null)
                cache = await Capi.CompanyEntity.LoadCache(typeof(Uniconta.DataModel.DebtorPaymentFormat), Capi);
            foreach (var r in cache.GetRecords)
            {
                var rec = r as DebtorPaymentFormatClient;
                if (rec != null && rec._Default)
                {
                    debPaymentFormat.SelectedItem = rec;
                    this.PaymentFormat = rec;
                    break;
                }
            }
        }

        private void ChildWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.DialogResult = false;
            }
            else
                if (e.Key == Key.Enter)
            {
                if (OKButton.IsFocused)
                    OKButton_Click(null, null);
                else if (CancelButton.IsFocused)
                    this.DialogResult = false;
            }
        }


        private void debPaymentFormat_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {

            if (debPaymentFormat.SelectedItem == null || debPaymentFormat.SelectedIndex == -1) return;
            this.PaymentFormat = debPaymentFormat.SelectedItem as DebtorPaymentFormatClient;

            DirectDebitSchemeVisible();
        }


        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            PaymentFormat = debPaymentFormat.SelectedItem as DebtorPaymentFormatClient;

            directDebitScheme = (DirectDebitScheme)cmbDirectDebitScheme.SelectedIndex;

            if (PaymentFormat != null)
            {
                this.DialogResult = true;
            }
            else
            {
                UnicontaMessageBox.Show("Please select a payment format", Uniconta.ClientTools.Localization.lookup("Warning"));
                return;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}

