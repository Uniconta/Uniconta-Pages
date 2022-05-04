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
    /// Interaction logic for CWOrderOIOUBLImport.xaml
    /// </summary>
    public partial class CWOrderOIOUBLImport : ChildWindow
    {
        public bool OneOrMultiple { get; set; }

        public CWOrderOIOUBLImport()
        {
            InitializeComponent();
            this.Title = string.Format("{0} {1}", Uniconta.ClientTools.Localization.lookup("Import"), Uniconta.ClientTools.Localization.lookup("OIOUBL"));
        }

        private void oneFileButton_Click(object sender, RoutedEventArgs e)
        {
            OneOrMultiple = true;
            SetDialogResult(true);
        }

        private void multipleFilesButton_Click(object sender, RoutedEventArgs e)
        {
            OneOrMultiple = false;
            SetDialogResult(true);
        }

        private void ChildWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                SetDialogResult(false);
            }
            else
            {
                if (oneFileButton.IsFocused)
                    oneFileButton_Click(null, null);
                else if (multipleFilesButton.IsFocused)
                    multipleFilesButton_Click(null, null);
            }
        }
    }
}
