using UnicontaClient.Utilities;
using System;
using System.Collections.Generic;
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
using Uniconta.ClientTools.DataModel;
using Uniconta.Common;
using System.Text;
using Uniconta.ClientTools.Controls;
using Uniconta.Common.Utility;

namespace UnicontaClient.Controls.Dialogs
{
    public partial class CWDebtorOpenTransactions : ChildWindow
    {
        private List<long> selectedInvoices;
        private List<long> selectedVouchers;
        public string settlement;

        public CWDebtorOpenTransactions(CrudAPI api, UnicontaBaseEntity source)
        {
            this.DataContext = this;
            InitializeComponent();
#if SILVERLIGHT
            Utility.SetThemeBehaviorOnChildWindow(this);
#endif
            this.Title = Uniconta.ClientTools.Localization.lookup("SettleOpenTransactions");
            settlement = string.Empty;
            selectedInvoices = new List<long>();
            selectedVouchers = new List<long>();
            dgDebtorTransOpen.api = api;
            dgDebtorTransOpen.UpdateMaster(source);
            dgDebtorTransOpen.Filter(null);
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedInvoices != null && selectedVouchers != null && selectedInvoices.Count > 0 && selectedVouchers.Count > 0)
            {
                var sb = StringBuilderReuse.Create();
                if (!selectedInvoices.Contains(0))
                {
                    sb.Append("I:");
                    foreach (var inv in selectedInvoices)
                        sb.AppendNum(inv).Append(';');
                }
                else
                {
                    sb.Append("V:");
                    foreach (var vouc in selectedVouchers)
                        sb.AppendNum(vouc).Append(';');
                }
                sb.Length--; // remove the last ;
                settlement = sb.ToStringAndRelease();
                SetDialogResult(true);
            }
            else
                UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("NoSettlementSelected"), Uniconta.ClientTools.Localization.lookup("Warning"), MessageBoxButton.OK);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            settlement = null;
            selectedInvoices = null;
            selectedVouchers = null;
            SetDialogResult(false);
        }

#if SILVERLIGHT
        private void ChildWindow_KeyDown(object sender, KeyEventArgs e)
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
#endif
        public double RemainingAmt = 0d;
        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            var chkBox = sender as CheckBox;
            var row = chkBox.Tag as DebtorTransOpenClient;
            if (row == null)
                return;
            if (chkBox.IsChecked == true)
            {
                selectedInvoices.Add(row.Invoice);
                selectedVouchers.Add(row.Voucher);
                RemainingAmt += row.AmountOpen;
            }
            else
            {
                selectedInvoices.Remove(row.Invoice);
                selectedVouchers.Remove(row.Voucher);
                RemainingAmt -= row.AmountOpen;
            }
        }
    }
}

