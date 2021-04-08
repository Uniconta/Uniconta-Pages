using UnicontaClient.Controls;
using UnicontaClient.Models;
using UnicontaClient.Pages;
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
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class UserCompaniesGridClient : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(CompanyClient); } }
        public override bool Readonly { get { return true; } }
#if SILVERLIGHT
        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                this.Page?.ribbonControl?.PerformRibbonAction("JumpTo");
            }
            else
                base.OnPreviewKeyDown(e);
        }
#endif
    }

    /// <summary>
    /// Interaction logic for UserCompanies.xaml
    /// </summary>
    public partial class UserCompaniesGridPage : GridBasePage
    {
        public override string NameOfControl { get { return Uniconta.ClientTools.UnicontaTabs.UserCompaniesGridPage; } }

        UnicontaBaseEntity selectedCompany;
        bool queryLocal;
        public UserCompaniesGridPage(UnicontaBaseEntity baseEntity) : base(null)
        {
            InitPage(baseEntity);
        }

        public UserCompaniesGridPage(CrudAPI api) : base(api, string.Empty)
        {
            InitPage(null);
        }

        void InitPage(UnicontaBaseEntity baseEntity)
        {
            this.DataContext = this;
            InitializeComponent();
            queryLocal = true;
            selectedCompany = baseEntity;
            dgUserCompaniesGridClient.api = api;
            dgUserCompaniesGridClient.BusyIndicator = busyIndicator;
            SetRibbonControl(localMenu, dgUserCompaniesGridClient);
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
            dgUserCompaniesGridClient.RowDoubleClick += DgUserCompaniesGridClient_RowDoubleClick;
            dgUserCompaniesGridClient.Loaded += DgUserCompaniesGridClient_Loaded;
#if SILVERLIGHT
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            var Comp = api.CompanyEntity;
            UtilDisplay.RemoveMenuCommand(rb, "OpenNewWindow");
#endif
        }
#if SILVERLIGHT
        public override bool CheckIfBindWithUserfield(out bool isReadOnly, out bool useBinding)
        {
            isReadOnly = false;
            useBinding = false;

            return false;
        }
#endif

#if !SILVERLIGHT
        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                LocalMenu_OnItemClicked("JumpTo");
            }
            else
                base.OnPreviewKeyDown(e);
        }
#endif
        private void DgUserCompaniesGridClient_Loaded(object sender, RoutedEventArgs e)
        {
            localMenu.SearchControl.KeyUp += SearchControl_KeyUp;
            localMenu.SearchControl.Focus();
        }

        private void SearchControl_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Down)
            {
                if (dgUserCompaniesGridClient.VisibleRowCount > 0)
                {
                    dgUserCompaniesGridClient.Focus();
                    dgUserCompaniesGridClient.View.FocusedRowHandle = 0;
                    e.Handled = true;
                }
            }
        }

        private void DgUserCompaniesGridClient_RowDoubleClick()
        {
            LocalMenu_OnItemClicked("JumpTo");
        }

        public override Task InitQuery()
        {
            if (queryLocal)
            {
                GetCompanies();
                return null;
            }
            return base.InitQuery();
        }

        private async void GetCompanies(bool OnGridRefresh = false)
        {
            busyIndicator.IsBusy = true;
            dgUserCompaniesGridClient.Visibility = Visibility.Visible;
            dgUserCompaniesGridClient.ItemsSource = null;
            Company[] loadedCompanies;
            if (OnGridRefresh)
            {
                loadedCompanies = await BasePage.session.GetCompanies();
                UnicontaClient.Controls.CWDefaultCompany.loadedCompanies = loadedCompanies;
            }
            else
                loadedCompanies = CWDefaultCompany.loadedCompanies;

            var companies = new CompanyClient[loadedCompanies.Length];

            int i = 0;
            foreach (var comp in CWDefaultCompany.loadedCompanies)
            {
                var cmp = comp as CompanyClient;
                if (cmp == null)
                {
                    cmp = new CompanyClient();
                    StreamingManager.Copy(comp, cmp);
                }
                companies[i] = cmp;
                i++;
            }

            dgUserCompaniesGridClient.SetSource(companies);
            dgUserCompaniesGridClient.SelectedItem = selectedCompany;
            busyIndicator.IsBusy = false;
        }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgUserCompaniesGridClient.SelectedItem as CompanyClient;
            switch (ActionType)
            {
                case "JumpTo":
                    if (selectedItem != null)
                    {
                        if (!selectedItem._Delete)
                        {
                            globalEvents?.NotifyCompanyChange(this, new Uniconta.ClientTools.Util.CompanyEventArgs(selectedItem));
                            CloseDockItem();
                        }
                        else
                            UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("CompanyDeleted"), Uniconta.ClientTools.Localization.lookup("Warning"), MessageBoxButton.OK);
                    }
                    break;
#if !SILVERLIGHT
                case "OpenNewWindow":
                    if (selectedItem != null)
                    {
                        if (!selectedItem._Delete)
                        {
                            var mainWindow = new HomePage(selectedItem.CompanyId, null);
                            mainWindow.Show();
                            CloseDockItem();
                        }
                        else
                            UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("CompanyDeleted"), Uniconta.ClientTools.Localization.lookup("Warning"), MessageBoxButton.OK);
                    }
                    break;
#endif
                case "Cancel":
                    CloseDockItem();
                    break;
                case "RefreshGrid":
                    GetCompanies(true);
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
    }
}
