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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Uniconta.ClientTools;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    /// <summary>
    /// Interaction logic for CWCollectionLetter.xaml
    /// </summary>
    public partial class CWCollectionLetter : ChildWindow
    {
        public string Result { get; set; }
        public CWCollectionLetter()
        {
            InitializeComponent();
            this.DataContext = this;
            this.Title = string.Format("{0} {1}", Uniconta.ClientTools.Localization.lookup("Select"), Uniconta.ClientTools.Localization.lookup("Options"));
            Loaded += CWCollectionLetter_Loaded;

            cmbCollectionLtr.ItemsSource = Utility.GetDebtorCollectionLetters();
            cmbCollectionLtr.SelectedIndex = 0;
        }

       

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            if (cmbCollectionLtr.SelectedItem == null)
                SetDialogResult(false);
            else
            {
                Result = cmbCollectionLtr.SelectedItem.ToString();
                SetDialogResult(true);
            }
        }

        void CWCollectionLetter_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => { cmbCollectionLtr.Focus(); }));
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
    }
}
