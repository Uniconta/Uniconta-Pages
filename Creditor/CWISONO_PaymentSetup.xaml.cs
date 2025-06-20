using UnicontaClient.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{    
    public partial class CWISONO_PaymentSetup : ChildWindow
    {
        public CreditorPaymentFormatClientISONO paymentFormatISONO { get; set; }
        CrudAPI Capi;

        public CWISONO_PaymentSetup(CrudAPI api, CreditorPaymentFormatClient paymentFormat)
        {
            Capi = api;
            this.DataContext = this;
            InitializeComponent();
            paymentFormatISONO = new CreditorPaymentFormatClientISONO();
            StreamingManager.Copy(paymentFormat, paymentFormatISONO);
            cmbBank.ItemsSource = Enum.GetValues(typeof(noBank));


            this.DataContext = paymentFormatISONO;
            this.Title = string.Format(Uniconta.ClientTools.Localization.lookup("SetupOBJ"), Uniconta.ClientTools.Localization.lookup("Payment"));
            this.Loaded += CW_Loaded;
        }
        private void CW_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => { cmbBank.Focus(); }));
        }
        private void ChildWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                SetDialogResult(false);
            }
            else
                if (e.Key == Key.Enter)
            {
                if (OKButton.IsFocused)
                    OKButton_Click(null, null);
                else if (CancelButton.IsFocused)
                    SetDialogResult(false);
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
            switch (paymentFormatISONO.Bank)
            {
                case noBank.Danske_Bank:
                    lblTestMarked.Visibility = ceTestMarked.Visibility = Visibility.Visible;
                    lblBatchBook.Visibility = ceBatchBooking.Visibility = Visibility.Visible;
                    lblCode.Visibility = txtCode.Visibility = Visibility.Collapsed;
                    break;
                default:
                    lblTestMarked.Visibility = ceTestMarked.Visibility = Visibility.Collapsed;
                    lblBatchBook.Visibility = ceBatchBooking.Visibility = Visibility.Collapsed;
                    lblCode.Visibility = txtCode.Visibility = Visibility.Collapsed;
                    break;
            }
        }


    }
}

