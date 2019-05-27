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
using Uniconta.API.System;
using Uniconta.ClientTools;


using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class CWProductionOrderLine : ChildWindow
    {
        public int storage;
        public double quantity { get; set; }
        private bool isQtyVisible = true;
        public int Decimals { get; set; }
        public CWProductionOrderLine(CrudAPI api, bool isQuantityVisible = false, string title = null, int Decimal = 0)
        {
            Decimals = Decimal;
            this.DataContext = this;
            InitializeComponent();
            this.Title = string.IsNullOrEmpty(title) ? Uniconta.ClientTools.Localization.lookup("ProductionLines") : title;
#if SILVERLIGHT
            UnicontaClient.Utilities.Utility.SetThemeBehaviorOnChildWindow(this);
#endif
            if (!isQuantityVisible)
            {
                isQtyVisible = false;
                rowQuantity.Height = new GridLength(0.0d);
            }
            var prodReg = AppEnums.ProductionRegister.ToString((int)api.CompanyEntity?._OrderLineStorage);
            storageType.ItemsSource = AppEnums.ProductionRegister.Values;
            storageType.SelectedItem = prodReg;
            this.Loaded += CW_Loaded;
        }

        void CW_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => { storageType.Focus(); }));
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
            quantity = isQtyVisible ? Convert.ToDouble(qtyEditor.Text) : 0.0d;
            storage = AppEnums.ProductionRegister.IndexOf(Convert.ToString(storageType.EditValue));
            this.DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}
