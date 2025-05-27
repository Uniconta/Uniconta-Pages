using UnicontaClient.Controls;
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
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class CwCompanyEmailSetup : ChildWindow
    {
        public Company Company { get; set; }
        public CompanySMTPClient EmailSetup { get; set; }
        public CwCompanyEmailSetup()
        {
            this.DataContext = this;
            InitializeComponent();
            this.Title = string.Format(Uniconta.ClientTools.Localization.lookup("CopyOBJ"), Uniconta.ClientTools.Localization.lookup("EmailSetup"));
            this.Loaded += CW_Loaded;
        }

        void CW_Loaded(object sender, RoutedEventArgs e)
        {
            BindCompany();
            Dispatcher.BeginInvoke(new Action(() => { OKButton.Focus(); }));
        }

        private  void BindCompany()
        {
            Company[] companies = CWDefaultCompany.loadedCompanies;
            cbCompany.ItemsSource = companies.ToList();
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
                OKButton_Click(null, null);
            }
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            if (cbEmailSetup.SelectedItem == null)
            {
                Uniconta.ClientTools.Controls.UnicontaMessageBox.Show( string.Format(Uniconta.ClientTools.Localization.lookup("OBJisEmpty"), Uniconta.ClientTools.Localization.lookup("EmailSetup")), Uniconta.ClientTools.Localization.lookup("Error"));
                SetDialogResult(false);
            }
            SetDialogResult(true);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }

        private async void cbCompany_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            if (cbCompany.SelectedIndex == -1)
                return;
           
            busyIndicator.IsBusy = true;
            Company = await BasePage.session.OpenCompany(Company.RowId, true);
            var copyCompanyAPI = new CrudAPI(BasePage.session, Company);
            var lstEntity = await copyCompanyAPI.Query<CompanySMTPClient>();
            if (lstEntity != null && lstEntity.Length > 0)
            {
                cbEmailSetup.ItemsSource = null;
                cbEmailSetup.ItemsSource = lstEntity;
                cbEmailSetup.DisplayMember = "Name";
                cbEmailSetup.SelectedIndex = 0;
            }
            else
                cbEmailSetup.ItemsSource = null;
            busyIndicator.IsBusy = false;
        }
    }
}
