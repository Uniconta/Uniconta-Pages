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
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.Common;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    /// <summary>
    /// Interaction logic for CWCreateOrderFromQuickInvoice.xaml
    /// </summary>
    public partial class CWCreateOrderFromQuickInvoice : ChildWindow
    {
        public string Account { get; set; }

        public CWCreateOrderFromQuickInvoice(CrudAPI api, string account)
        {
            this.DataContext = this;
            InitializeComponent();
            this.Title = String.Format(Uniconta.ClientTools.Localization.lookup("CopyOBJ"), Uniconta.ClientTools.Localization.lookup("Invoice"));
            this.Loaded += CW_Loaded;
            dgCreateOrderGrid.BusyIndicator = busyIndicator;
            dgCreateOrderGrid.api = api;
            localMenu.dataGrid = dgCreateOrderGrid;
            this.Account = account;
            if (!this.dgCreateOrderGrid.ReuseCache(typeof(Uniconta.DataModel.DCInvoice)))
                BindGrid();
        }

        void CW_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => { CreateButton.Focus(); }));
        }

        private void ChildWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.DialogResult = false;
            }
            else if (e.Key == Key.Enter)
            {
                if (CreateButton.IsFocused)
                    CreateButton_Click(null, null);
                else if (CancelButton.IsFocused)
                    this.DialogResult = false;
            }
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            if (dgCreateOrderGrid.SelectedItem == null)
            {
                UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("RecordNotSelected"), Uniconta.ClientTools.Localization.lookup("Warning"));
                return;
            }
            else
                this.DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private Task Filter(IEnumerable<PropValuePair> propValuePair)
        {
            if(!string.IsNullOrWhiteSpace(Account))
                propValuePair = new List<PropValuePair>(){ PropValuePair.GenereteWhereElements("Account", Account, CompareOperator.Equal) };
            
            return dgCreateOrderGrid.Filter(propValuePair);
        }

        private void BindGrid()
        {
            var t = Filter(null);
        }
    }
}
