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
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;

namespace UnicontaClient.Controls.Dialogs
{
    public partial class CWAccountants : ChildWindow
    {
        public AccountantClient selectedAccountant;
        public CWAccountants(CrudAPI api, AccountantClient selectedAccountant)
        {
            this.selectedAccountant = selectedAccountant;
            this.DataContext = this;
            InitializeComponent();
            dgAccountants.api = api;
            dgAccountants.View.ShowSearchPanel(false);
            this.Title = Uniconta.ClientTools.Localization.lookup("Accountant");
            BindGrid(api);
            dgAccountants.View.Loaded += View_Loaded;
            dgAccountants.View.ShowSearchPanelMode = DevExpress.Xpf.Grid.ShowSearchPanelMode.Always;
        }

        async void BindGrid(CrudAPI api)
        {
            var acc = await new Corasau.API.Admin.UserAPI(api).GetPartners(new AccountantClient(), api.CompanyEntity._CountryId) as AccountantClient[];
            dgAccountants.SetSource(acc);
            dgAccountants.SelectedItem = selectedAccountant;
        }
        private void View_Loaded(object sender, RoutedEventArgs e)
        {
            if (dgAccountants.View.SearchControl != null)
                dgAccountants.View.SearchControl.Focus();
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            var accountant = dgAccountants.SelectedItem as AccountantClient;
            if (accountant != null)
            {
                selectedAccountant = accountant;
            }
            SetDialogResult(true);
        }
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            selectedAccountant = null;
            SetDialogResult(true);
        }
    }
}
