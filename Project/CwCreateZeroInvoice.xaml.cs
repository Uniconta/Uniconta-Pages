using UnicontaClient.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
using Uniconta.ClientTools.DataModel;
using Uniconta.Common;
using UnicontaClient.Controls;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{

    public partial class CwCreateZeroInvoice : ChildWindow
    {
        [InputFieldData]
        [Display(Name = "Simulation", ResourceType = typeof(InputFieldDataText))]
        public bool Simulate { get; set; }

        [InputFieldData]
        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.PrCategory))]
        [Display(Name = "Category", ResourceType = typeof(InputFieldDataText))]
        public string InvoiceCategory { get; set; }

        [InputFieldData]
        [Display(Name = "InvoiceDate", ResourceType = typeof(InputFieldDataText))]
        public DateTime InvoiceDate { get; set; }

        [InputFieldData]
        [Display(Name = "ToDate", ResourceType = typeof(InputFieldDataText))]
        public DateTime ToDate { get; set; }

        [Display(Name = "AdjustmentCategory", ResourceType = typeof(InputFieldDataText))]
        public string AdjustmentCategory { get; set; }

        [InputFieldData]
        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.Employee))]
        [Display(Name = "Employee", ResourceType = typeof(InputFieldDataText))]
        public string Employee { get; set; }

        [InputFieldData]
        [Display(Name = "CreateInvoiceProposal", ResourceType = typeof(InputFieldDataText))]
        public bool IsCreateInvoiceProposal { get; set; }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.ProjectTask))]
        [Display(Name = "Task", ResourceType = typeof(InputFieldDataText))]
        public string ProjectTask { get; set; }

        [InputFieldData]
        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.PrWorkSpace))]
        [Display(Name = "WorkSpace", ResourceType = typeof(InputFieldDataText))]
        public string ProjectWorkspace { get; set; }

        CrudAPI api;
        public int DialogTableId;
        protected override int DialogId { get { return DialogTableId; } }
        protected override bool ShowTableValueButton { get { return true; } }

        public CwCreateZeroInvoice(CrudAPI crudApi, ProjectClient project, PrWorkSpaceClient prWorkspace = null) : this(crudApi)
        {
            if (crudApi.CompanyEntity.ProjectTask && project != null)
            {
                setTask(project);
                lblProjTask.Visibility = leProjTask.Visibility = Visibility.Visible;
            }
            ProjectWorkspace = prWorkspace?._Number;
            leProjWorkspace.SelectedItem = prWorkspace;
        }
        public CwCreateZeroInvoice(CrudAPI crudApi)
        {
            InvoiceDate = InvoiceDate != DateTime.MinValue ? InvoiceDate : Uniconta.ClientTools.Page.BasePage.GetSystemDefaultDate();
            this.DataContext = this;
            InitializeComponent();
            this.Title = string.Format(Uniconta.ClientTools.Localization.lookup("CreateOBJ"), Uniconta.ClientTools.Localization.lookup("ZeroInvoice"));
            txtCreatInvoiceProposal.Text = string.Format(Uniconta.ClientTools.Localization.lookup("CreateOBJ"), Uniconta.ClientTools.Localization.lookup("InvoiceProposal").ToLower());
            invoiceDate.DateTime = InvoiceDate;
            api = crudApi;
            leProjWorkspace.api = cmbCategory.api = cmbEmployee.api = crudApi;
            SetItemSource(api);
        }
        async void SetItemSource(QueryAPI api)
        {
            var prCache = api.GetCache(typeof(Uniconta.DataModel.PrCategory)) ?? await api.LoadCache(typeof(Uniconta.DataModel.PrCategory));
            //  cmbCategory.ItemsSource = new PrCategoryRevenueFilter(prCache);
            cmbRegCategory.ItemsSource = new PrCategoryRegulationFilter(prCache);
        }

        async void setTask(ProjectClient project)
        {
            leProjTask.ItemsSource = project.Tasks ?? await project.LoadTasks(api);
            leProjTask.Focus();
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
                if (CancelButton.IsFocused)
                {
                    SetDialogResult(false);
                    return;
                }
                OKButton_Click(null, null);
            }
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(true);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }

    }
}
