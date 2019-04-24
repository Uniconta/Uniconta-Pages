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

        CrudAPI Capi;

        public CWDirectDebit(CrudAPI api, string title)
        {
            Capi = api;
            this.DataContext = this;
            InitializeComponent();
            debPaymentFormat.api = api;
            SetPaymentFormat();
#if !SILVERLIGHT
            this.Title = title;
#endif
#if SILVERLIGHT
            Utility.SetThemeBehaviorOnChildWindow(this);
#endif
            this.Loaded += CW_Loaded;
        }

        private async void SetPaymentFormat()
        {
            var paymentFormats = await Capi.Query<DebtorPaymentFormatClient>();
            debPaymentFormat.ItemsSource = paymentFormats;
        }


        private void CW_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => { OKButton.Focus(); }));
            SetDefaultPaymentFormat();
        }
        async void SetDefaultPaymentFormat()
        {
            var cache = Capi.CompanyEntity.GetCache(typeof(Uniconta.DataModel.DebtorPaymentFormat));
            if (cache == null)
                cache = await Capi.CompanyEntity.LoadCache(typeof(Uniconta.DataModel.DebtorPaymentFormat), Capi);
            foreach (var r in cache.GetRecords)
            {
                var rec = r as DebtorPaymentFormat;
                if (rec != null && rec._Default)
                {
                    debPaymentFormat.SelectedItem = rec;
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
            PaymentFormat = debPaymentFormat.SelectedItem as DebtorPaymentFormatClient;

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

