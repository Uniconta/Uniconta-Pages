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
using Uniconta.ClientTools.DataModel;

namespace Uniconta.WPFClient.Pages
{
    public partial class CWSimulatedBudgetLines : ChildWindow
    {
        public CWSimulatedBudgetLines(List<GLBudgetLineClient> listSimulatedBudgetLine)
        {
            InitializeComponent();
            this.DataContext = this;
            this.Title = Uniconta.ClientTools.Localization.lookup("Simulate");
            dgSimulatedGLBudgetLine.ItemsSource = listSimulatedBudgetLine;
            dgSimulatedGLBudgetLine.Visibility = Visibility.Visible;
            this.Loaded += CWSimulatedBudgetLines_Loaded;
        }

        private void CWSimulatedBudgetLines_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => { CancelButton.Focus(); }));
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
