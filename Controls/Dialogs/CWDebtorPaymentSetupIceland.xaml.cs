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

namespace UnicontaClient.Controls.Dialogs
{    
    public partial class CWDebtorPaymentSetupIceland : ChildWindow
    {
        public DebtorPaymentFormatClientIceland paymentFormatIceland { get; set; }
        CrudAPI Capi;
        
        public CWDebtorPaymentSetupIceland(CrudAPI api, DebtorPaymentFormatClient paymentFormat)
        {
            Capi = api;
            this.DataContext = this;
            InitializeComponent();

            paymentFormatIceland = new DebtorPaymentFormatClientIceland();
            StreamingManager.Copy(paymentFormat, paymentFormatIceland);
            cmbBank.ItemsSource = Enum.GetValues(typeof(IcelandBank));
            this.DataContext = paymentFormatIceland;
            LeRecievedDefaultInterestAccount.api = LeCapitalAccount.api = LeClaimFeeAccount.api = api;
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
            switch (paymentFormatIceland.Bank)
            {
                case IcelandBank.Arion:
                case IcelandBank.Landsbanki:
                case IcelandBank.Íslandbanki:
                case IcelandBank.Sparisjóðir:
                    break;
                default:
                    break;
            }
        }
    }
}

