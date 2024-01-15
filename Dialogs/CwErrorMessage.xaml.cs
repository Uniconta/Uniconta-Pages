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

namespace UnicontaClient.Controls.Dialogs
{
    public partial class CwErrorMessage : ChildWindow
    {
        public string ErrorMessage { get; set; }
        public CwErrorMessage(string msg)
        {
            ErrorMessage = msg;
            this.DataContext = this;
            InitializeComponent();
            this.Title = Uniconta.ClientTools.Localization.lookup("Error");
            this.Loaded += CW_Loaded;
        }
        void CW_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                OkButton.Focus();
            }));
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }

        private void OpenSubcriptionBtn_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(true);
        }
    }
}
