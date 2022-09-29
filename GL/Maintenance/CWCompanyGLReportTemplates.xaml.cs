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
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.DataModel;
using UnicontaClient.Controls;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    /// <summary>
    /// Interaction logic for CWCompanyGLReportTemplates.xaml
    /// </summary>
    public partial class CWCompanyGLReportTemplates : ChildWindow
    {
        public Company Company { get; set; }
        public GLReportTemplate GLReportTemplate { get; set; }
        public GLReportLine[] GLReportTemplateLines { get; set; }

        private CrudAPI companyAPI;
        public CWCompanyGLReportTemplates()
        {
            this.DataContext = this;
            InitializeComponent();
            this.Title = string.Format(Uniconta.ClientTools.Localization.lookup("CopyOBJ"), Uniconta.ClientTools.Localization.lookup("CompanyAccountTemplate"));
            this.Loaded += CW_Loaded;
        }

        void CW_Loaded(object sender, RoutedEventArgs e)
        {
            BindCompany();
            Dispatcher.BeginInvoke(new Action(() => { OKButton.Focus(); }));
        }

        private void BindCompany()
        {
            Company[] companies = CWDefaultCompany.loadedCompanies;
            cbCompany.ItemsSource = companies.ToList();
        }

        private void ChildWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                SetDialogResult(true);
            else if (e.Key == Key.Enter)
                SetDialogResult(false);
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(true);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }

        async private void cbCompany_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            busyIndicator.IsBusy = true;
            var comp = cbCompany.SelectedItem as Company;
            if (comp == null)
                return;

            Company = await BasePage.session.OpenCompany(comp.RowId, true);
            companyAPI = new CrudAPI(BasePage.session, Company);
            var lstEntity = await companyAPI.Query<GLReportTemplate>();
            cbGLReportTemplate.ItemsSource = null;
            if (lstEntity != null && lstEntity.Length > 0)
                cbGLReportTemplate.ItemsSource = lstEntity;
            busyIndicator.IsBusy = false;
        }

        async private void cbGLReportTemplate_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            busyIndicator.IsBusy = true;
            var glReportTemplate = cbGLReportTemplate.SelectedItem as GLReportTemplate;

            if (glReportTemplate == null)
                return;

            GLReportTemplate = glReportTemplate;
            var glReportLines = await companyAPI.Query<GLReportLine>(GLReportTemplate);
            if (glReportLines == null || glReportLines.Length == 0)
            {
                UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("NoLinesFound"), Uniconta.ClientTools.Localization.lookup("Warning"));
                return;
            }

            GLReportTemplateLines = glReportLines;
            busyIndicator.IsBusy = false;
        }
    }
}
