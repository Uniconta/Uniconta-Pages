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
using Uniconta.Common.Utility;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.Maintenance
{   
    public partial class CWCreatePrimo : ChildWindow
    {
        public string BalanceName { get; set; }
        public string PLText { get; set; }
        public int Voucher { get; set; }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.NumberSerie))]
        public string NumberserieText { get; set; }

        public CWCreatePrimo(CrudAPI api, CompanyFinanceYearClient financeYearClient)
        {
            this.DataContext = this;
            InitializeComponent();


            lookupNumberserie.api = api;
#if !SILVERLIGHT
            this.Title = Uniconta.ClientTools.Localization.lookup("Primo");
#else
            Utility.SetThemeBehaviorOnChildWindow(this);
#endif
            txtBalance.Text = financeYearClient.BalanceName;
            txtPL.Text = financeYearClient.PLText;
            txtVoucher.Text = Convert.ToString(financeYearClient.Voucher);
            this.BalanceName = financeYearClient.BalanceName;
            this.PLText = financeYearClient.PLText;
            this.NumberserieText = financeYearClient.NumberserieText;
            this.Voucher = financeYearClient.Voucher;
            this.Loaded += CW_Loaded;
        }

        void CW_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => 
            {
                if (string.IsNullOrWhiteSpace(txtBalance.Text))
                    txtBalance.Focus();
                else
                    SaveButton.Focus();
            }));
        }

        private void txtTransType_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                OKButton_Click(sender, e);
            else if (e.Key == Key.Escape)
                CancelButton_Click(sender, e);
        }
        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            BalanceName = txtBalance.Text;
            PLText = txtPL.Text;
            if (!string.IsNullOrEmpty(txtVoucher.Text))
                Voucher = (int)NumberConvert.ToInt(txtVoucher.Text);
            else
                Voucher = 0;
            this.DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}

