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

namespace UnicontaClient.Pages
{
    public partial class CWShowBankAPILog : ChildWindow
    {
        string data;
        public CWShowBankAPILog(string text)
        {
            InitializeComponent();
            this.DataContext = this;
            data = text;    
            Title = string.Concat(Uniconta.ClientTools.Localization.lookup("FinancialInstitution"), " - ", Uniconta.ClientTools.Localization.lookup("Log"));
            this.Loaded += CW_Loaded;
            ShowText();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }

        void CW_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => { btnClose.Focus(); }));
        }

        void ShowText()
        {
            teText.Text = data;
        }
    }
}
