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

namespace UnicontaClient.Pages
{
    /// <summary>
    /// Interaction logic for CWRecordsPerTransaction.xaml
    /// </summary>
    public partial class CWRecordsPerTransaction : ChildWindow
    {
        private int _records;
        public int Records { get { return _records; } set { _records = value; } }

        public CWRecordsPerTransaction()
        {
            _records = 100;
            InitializeComponent();
            DataContext = this;
            this.Loaded += CWRecordsPerTransaction_Loaded;
        }

        private void CWRecordsPerTransaction_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (string.IsNullOrWhiteSpace(txtRecods.Text) || txtRecods.Text == "0")
                    txtRecods.Focus();
                else
                    OKButton.Focus();
            }));
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            if (_records <= 0)
                _records = 100;
            else if (_records > 1000)
                _records = 1000;
            SetDialogResult(true);
        }
    }
}
