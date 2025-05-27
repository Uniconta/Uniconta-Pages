using FromXSDFile.OIOUBL.ExportImport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.Common.Utility;
using Uniconta.DataModel;
using Localization = Uniconta.ClientTools.Localization;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    /// <summary>
    /// Interaction logic for CWChangeCostValue.xaml
    /// </summary>
    public partial class CWChangeCostValue : ChildWindow
    {
        private double originalCostValue;
        InvTransClient invTransInvoice;
        public double CostValue { get; set; }
        public DateTime PostingDate { get; set; }
        public CWChangeCostValue(InvTransClient _invTransInvoice)
        {
            this.DataContext = this;
            invTransInvoice = _invTransInvoice;
            originalCostValue = _invTransInvoice._CostValue;
            CostValue = invTransInvoice._CostValue;
            PostingDate = invTransInvoice._Date;
            InitializeComponent();
            this.Title = string.Format(Uniconta.ClientTools.Localization.lookup("ChangeOBJ"), Uniconta.ClientTools.Localization.lookup("CostValue"));
            this.Loaded += CW_Loaded;
            deQty.Text =  _invTransInvoice.Qty.ToString();
            deCostPrice.Text = _invTransInvoice.CostPrice.ToString("N2");
            deCostPrice.EditValueChanged += DeCostPrice_EditValueChanged;
            deCostValue.EditValueChanged += DeCostValue_EditValueChanged;
        }
        bool changedByCalc = false;
        private void DeCostValue_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            if (!changedByCalc)
            {
                double Qty = NumberConvert.ToDouble(deQty.Text);
                double CostValue = NumberConvert.ToDouble(deCostValue.Text);
                var CostPrice = CostValue / Qty;
                changedByCalc = true;
                deCostPrice.Text = CostPrice.ToString("N2");
            }
            else
                changedByCalc = false;
        }

        private void DeCostPrice_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            if (!changedByCalc)
            {
                double Qty = NumberConvert.ToDouble(deQty.Text);
                double CostPrice = NumberConvert.ToDouble(deCostPrice.Text);
                CostValue = Qty * CostPrice;
                changedByCalc = true;
                deCostValue.Text = CostValue.ToString("N2");
            }
            else
                changedByCalc = false;
        }

        private void CW_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => { OKButton.Focus(); }));
        }

        private void ChildWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                SetDialogResult(false);
            }
            else if (e.Key == Key.Enter)
            {
                if (OKButton.IsFocused)
                    OKButton_Click(null, null);
                else if (CancelButton.IsFocused)
                    SetDialogResult(false);
            }
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            if (!Convert.ToDouble(CostValue).Equals(originalCostValue))
            {
                var message = string.Format(Localization.lookup("ChangeCostValue") + " ?", originalCostValue, deCostValue.EditValue);
                if (UnicontaMessageBox.Show(message, Localization.lookup("Information"), MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    SetDialogResult(true);
                else
                    SetDialogResult(false);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }
    }
}
