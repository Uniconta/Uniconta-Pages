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
    public partial class CWDebtorPaymentSetupSEPA : ChildWindow
    {
        public DebtorPaymentFormatClientSEPA paymentFormatSEPA { get; set; }
        CrudAPI Capi;

        public CWDebtorPaymentSetupSEPA(CrudAPI api, DebtorPaymentFormatClient paymentFormat)
        {
            Capi = api;
            this.DataContext = this;
            InitializeComponent();

            paymentFormatSEPA = new DebtorPaymentFormatClientSEPA();
            StreamingManager.Copy(paymentFormat, paymentFormatSEPA);
            cmbBank.ItemsSource = Enum.GetValues(typeof(SEPABank));

            this.DataContext = paymentFormatSEPA;
            this.Title = string.Format("{0} {1}", Uniconta.ClientTools.Localization.lookup("Setup"), Uniconta.ClientTools.Localization.lookup("Payment"));
#if SILVERLIGHT
            Utility.SetThemeBehaviorOnChildWindow(this);
#endif
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
            switch (paymentFormatSEPA.Bank)
            {
                case SEPABank.Standard:
                    switch (Capi.CompanyEntity._CountryId)
                    {
                        case CountryCode.Denmark:
                        case CountryCode.Germany:
                        case CountryCode.Netherlands:
                            lblBatchBook.Visibility = ceBatchBooking.Visibility = Visibility.Visible;
                            break;
                        default:
                            lblBatchBook.Visibility = ceBatchBooking.Visibility = Visibility.Collapsed;
                            break;
                    }
                    break;
                default:
                    break;
            }
        }
    }
}

