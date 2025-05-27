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
using Uniconta.Common;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class CWCalculateSalesPrice : ChildWindow
    {
        public double costPrice { get; set; }
        public double salesPrice1 { get; set; }
        public double salesPrice2 { get; set; }
        public double salesPrice3 { get; set; }

        public CWCalculateSalesPrice(double p0, double p1, double p2, double p3, Uniconta.DataModel.InvItem item)
        {
            costPrice = p0;

            if (!double.IsNaN(p1))
                salesPrice1 = p1;
            if (!double.IsNaN(p2))
                salesPrice2 = p2;
            if (!double.IsNaN(p3))
                salesPrice3 = p3;
            this.DataContext = this;
            InitializeComponent();
            var str = Uniconta.ClientTools.Localization.lookup("SalesPrice");
            txtCostPrice.Text = Uniconta.ClientTools.Localization.lookup("CostPrice");
            txtSalPrice1.Text = String.Format("{0} 1 {1}", str, AppEnums.Currencies.ToString(item._Currency1));
            txtSalPrice2.Text = String.Format("{0} 2 {1}", str, AppEnums.Currencies.ToString(item._Currency2));
            txtSalPrice3.Text = String.Format("{0} 3 {1}", str, AppEnums.Currencies.ToString(item._Currency3));

            this.Title = Uniconta.ClientTools.Localization.lookup("SalesPrices");

        }

        void CW_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => {
                if (string.IsNullOrWhiteSpace(dbCostPrice.Text) || double.Parse(dbCostPrice.Text) == 0)
                    dbCostPrice.Focus();
                else
                    OKButton.Focus();
            }));
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
    }
}
