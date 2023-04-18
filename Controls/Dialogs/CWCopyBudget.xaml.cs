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
using System.Windows.Threading;
using Uniconta.ClientTools;

namespace UnicontaClient.Controls.Dialogs
{
    public partial class CWCopyBudget : ChildWindow
    {
        public string BudgetName { get; set; }
        public int Months { get; set; }
        public double Pct { get; set; }

        public CWCopyBudget(string name)
        {
            InitializeComponent();
            BudgetName = name + Uniconta.ClientTools.Localization.lookup("Copy");
            this.DataContext = this;
            this.Title = Uniconta.ClientTools.Localization.lookup("Budget");
            this.Loaded += CWCopyBuget_Loaded;
        }

        private void CWCopyBuget_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => { txtName.Focus(); }));
        }

        private void ChildWindow_KeyDown(object sender, KeyEventArgs e)
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

        private async void OKButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(true);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }
    }
}
