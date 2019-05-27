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
using Uniconta.ClientTools;
using UnicontaClient.Utilities;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    
    public partial class CWShowAmount : ChildWindow
    {
    
        public string AmountTypeOption { get; set; }
        public CWShowAmount()
        {
            InitializeComponent();
            SetAmountType();
            this.DataContext = this;
#if !SILVERLIGHT
            this.Title = Uniconta.ClientTools.Localization.lookup("ShowAmount");
#endif
#if SILVERLIGHT
            Utility.SetThemeBehaviorOnChildWindow(this);
#endif
            this.Loaded += CWShowAmount_Loaded;
        }

        private void CWShowAmount_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                cmbAmountType.Focus();
            }));
        }

        void SetAmountType()
        {
            string[] arryOption = new string[]
            {
                Uniconta.ClientTools.Localization.lookup("All"),
                Uniconta.ClientTools.Localization.lookup("Debit"),
                Uniconta.ClientTools.Localization.lookup("Credit")
            };
            cmbAmountType.ItemsSource = arryOption;
            AmountTypeOption = arryOption[0];
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }
    }
}
