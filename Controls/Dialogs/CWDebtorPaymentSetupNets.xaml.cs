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
    public partial class CWDebtorPaymentSetupNets : ChildWindow
    {
        public DebtorPaymentFormatClientNets paymentFormatNets { get; set; }
        CrudAPI Capi;

        private const string defaultNetsTemplate =
                     "FAKTURANUMMER: %11\r\n" +
                     "FAKTURADATO: %12\r\n\r\n" +
                     "%13\r\n" + //Navn
                     "%14\r\n\r\n" + //Adresse
                     "%15\r\n\r\n" + //Header comment
                     "%21%22\r\n" +
                     "NETTO BELØB %30L\r\n" +
                     "MOMSBELØB %31\r\n" +
                     "TOTAL BELØB %32L";

        private const string defaultNetsTemplateNoLines =
                  "FAKTURANUMMER: %11 FAKTURADATO: %12\r\n" +
                  "BELØB %32";

        public CWDebtorPaymentSetupNets(CrudAPI api, DebtorPaymentFormatClient paymentFormat)
        {
            Capi = api;
            this.DataContext = this;
            InitializeComponent();

            paymentFormatNets = new DebtorPaymentFormatClientNets();
            StreamingManager.Copy(paymentFormat, paymentFormatNets);

            paymentFormatNets.InvoiceTextTemplate = paymentFormatNets.InvoiceTextTemplate ?? defaultNetsTemplate;
            paymentFormatNets.InvoiceTextTemplateNoLines = paymentFormatNets.InvoiceTextTemplateNoLines ?? defaultNetsTemplateNoLines;

            paymentFormatNets.DebtorGroup = paymentFormatNets.DebtorGroup ?? "00001";
            cmbType.ItemsSource = Enum.GetValues(typeof(NetsBSType));

            this.DataContext = paymentFormatNets;
            this.Title = string.Format(Uniconta.ClientTools.Localization.lookup("SetupOBJ"), Uniconta.ClientTools.Localization.lookup("Payment"));
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
    }
}

