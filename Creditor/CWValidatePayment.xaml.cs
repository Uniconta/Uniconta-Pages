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
    public partial class CWValidatePayment : ChildWindow
    {
        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.CreditorPaymentFormat))]
        public CreditorPaymentFormatClient PaymentFormat { get; set; }

        public string PaymentFormatOption { get; set; }

        CrudAPI Capi;

        public CWValidatePayment(CrudAPI api)
        {
            Capi = api;
            this.DataContext = this;
            InitializeComponent();
            lePaymentFormat.api = api;
            SetPaymentFormat();
#if !SILVERLIGHT
            this.Title = Uniconta.ClientTools.Localization.lookup("Validate");
#endif
#if SILVERLIGHT
            Utility.SetThemeBehaviorOnChildWindow(this);
#endif
            this.Loaded += CW_Loaded;
        }

        private async void SetPaymentFormat()
        {
            var paymentFormats = await Capi.Query<CreditorPaymentFormatClient>();
            lePaymentFormat.ItemsSource = paymentFormats;
        }


        private void CW_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => { lePaymentFormat.Focus(); }));
            SetDefaultPaymentFormat();
        }
        async void SetDefaultPaymentFormat()
        {
            var cache = Capi.CompanyEntity.GetCache(typeof(CreditorPaymentFormat));
            if (cache == null)
                cache = await Capi.CompanyEntity.LoadCache(typeof(CreditorPaymentFormat), Capi);
            foreach (var r in cache.GetRecords)
            {
                var rec = r as CreditorPaymentFormat;
                if (rec != null && rec._Default)
                {
                    lePaymentFormat.SelectedItem = rec;
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

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            PaymentFormat = lePaymentFormat.SelectedItem as CreditorPaymentFormatClient;
            if (PaymentFormat != null)
            {
                if (PaymentFormat._ExportFormat == (byte)ExportFormatType.NETS_Norge)
                {
                    UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("NotActive"), Uniconta.ClientTools.Localization.lookup("Warning"));
                    return;
                }

                PaymentFormatOption = Convert.ToString(lePaymentFormat.SelectedItem);
                this.DialogResult = true;
            }
            else
            {
                UnicontaMessageBox.Show("Please Select a payment format", Uniconta.ClientTools.Localization.lookup("Warning"));
                return;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}

