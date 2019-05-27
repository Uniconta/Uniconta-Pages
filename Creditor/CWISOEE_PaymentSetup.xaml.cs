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
    public partial class CWISOEE_PaymentSetup : ChildWindow
    {
        public CreditorPaymentFormatClientISOEE paymentFormatISOEE { get; set; }
        CrudAPI Capi;

        public CWISOEE_PaymentSetup(CrudAPI api, CreditorPaymentFormatClient paymentFormat)
        {
            Capi = api;
            this.DataContext = this;
            InitializeComponent();
            paymentFormatISOEE = new CreditorPaymentFormatClientISOEE();
            StreamingManager.Copy(paymentFormat, paymentFormatISOEE);
            cmbBank.ItemsSource = Enum.GetValues(typeof(eeBank));


            this.DataContext = paymentFormatISOEE;
            this.Title = string.Format(Uniconta.ClientTools.Localization.lookup("SetupOBJ"), Uniconta.ClientTools.Localization.lookup("Payment"));
#if SILVERLIGHT
            Utility.SetThemeBehaviorOnChildWindow(this);
#endif
            this.Loaded += CW_Loaded;
        }
        private void CW_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => { cmbBank.Focus(); }));
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
            this.DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void cmbBank_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {

        }
    }
}

