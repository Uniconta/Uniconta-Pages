using UnicontaClient.Utilities;
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
using System.Windows.Shapes;
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Util;
using Uniconta.Common;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    /// <summary>
    /// Interaction logic for CWImportDebtorPayment.xaml
    /// </summary>
    public partial class CWImportDebtorPayment : ChildWindow
    {
        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.BankStatement))]
        public string BankAccount { get; set; }

        public string FileName { get; set; }
        public string FileOption { get; set; }
        public string Seperator { get; set; }
        CrudAPI Capi;

        public CWImportDebtorPayment(CrudAPI api)
        {
            Capi = api;
            this.DataContext = this;
            InitializeComponent();
            lookupBanks.api = Capi;
            SetBanks();
            this.Title = Uniconta.ClientTools.Localization.lookup("CreatePaymentsFile");
            this.Loaded += CW_Loaded;
        }

        private void CW_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => { cbImportOption.Focus(); }));
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
                if (CancelButton.IsFocused)
                {
                    SetDialogResult(false);
                    return;
                }
                OKButton_Click(null, null);
            }
        }
        void SetBanks()
        {
            string[] arryOption = new string[]
            {
                Uniconta.ClientTools.Localization.lookup("CSVFile"),
                Uniconta.ClientTools.Localization.lookup("Bank1"),
                Uniconta.ClientTools.Localization.lookup("Bank2")
            };
            cbImportOption.ItemsSource = arryOption;
            FileOption = arryOption[0];
            Seperator = UtilFunctions.GetDefaultDeLimiter().ToString();
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
