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
    /// <summary>
    /// Interaction logic for CWCreateOrderFromProject.xaml
    /// </summary>
    public partial class CWCreateOrderFromProject : ChildWindow
    {
        [InputFieldData]
        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.PrCategory))]
        [Display(Name = "Category", ResourceType = typeof(InputFieldDataText))]
        static public string InvoiceCategory { get; set; }

        [InputFieldData]
        [Display(Name = "Date", ResourceType = typeof(InputFieldDataText))]
        static public DateTime GenrateDate { get; set; }

        [InputFieldData]
        [Display(Name = "FromDate", ResourceType = typeof(InputFieldDataText))]
        static public DateTime FromDate { get; set; }

        [InputFieldData]
        [Display(Name = "ToDate", ResourceType = typeof(InputFieldDataText))]
        static public DateTime ToDate { get; set; }

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

        public CWCreateOrderFromProject(CrudAPI crudApi, bool createOrder) : this(crudApi)
        {
            if (createOrder)
                dpDate.DateTime = DateTime.MinValue;
        }

        public CWCreateOrderFromProject(CrudAPI crudApi, bool createOrder, ProjectClient project, ProjectTaskClient projTask = null, PrWorkSpaceClient prWorkspace = null) : this(crudApi, createOrder)
        {
            if (crudApi.CompanyEntity.ProjectTask && project != null)
            {
                ProjectTask = projTask?._Task;
                setTask(project, projTask);
                lblProjTask.Visibility = leProjTask.Visibility = Visibility.Visible;
            }
            ProjectWorkspace = prWorkspace?._Number;
            leProjWorkspace.SelectedItem = prWorkspace;
        }

        public CWCreateOrderFromProject(CrudAPI crudApi)
        {
            GenrateDate = GenrateDate != DateTime.MinValue ? GenrateDate : Uniconta.ClientTools.Page.BasePage.GetSystemDefaultDate();
            this.DataContext = this;
            InitializeComponent();
            this.Title = string.Format(Uniconta.ClientTools.Localization.lookup("CreateOBJ"), Uniconta.ClientTools.Localization.lookup("Order"));
            dpDate.DateTime = GenrateDate;
            api = crudApi;
            leProjWorkspace.api = cmbCategory.api = crudApi;
            Loaded += CWCreateOrderFromProject_Loaded;
            SetItemSource(crudApi);
        }

        private void CWCreateOrderFromProject_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => { OKButton.Focus(); }));
        }

        void SetItemSource(QueryAPI api)
        {
            var prCache = api.GetCache(typeof(Uniconta.DataModel.PrCategory)) ?? api.LoadCache(typeof(Uniconta.DataModel.PrCategory)).GetAwaiter().GetResult();
            cmbCategory.cacheFilter = new PrCategoryRevenueOnlyFilter(prCache);
        }

        async void setTask(ProjectClient project, ProjectTaskClient projTask)
        {
            leProjTask.ItemsSource = project.Tasks ?? await project.LoadTasks(api);
            leProjTask.SelectedItem = projTask;
            leProjTask.Focus();
        }

        private void ChildWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                SetDialogResult(false);
            }
            else if (e.Key == Key.Enter)
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
            InvoiceCategory = Uniconta.Common.Utility.Util.NotEmptyValue(cmbCategory.SelectedText);
            GenrateDate = dpDate.DateTime;
            FromDate = fromDate.DateTime;
            ToDate = toDate.DateTime;
            SetDialogResult(true);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }
    }
}
