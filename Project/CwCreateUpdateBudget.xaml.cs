using System;
using System.ComponentModel.DataAnnotations;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.Common;
using Uniconta.DataModel;
using UnicontaClient.Controls;
using UnicontaClient.Utilities;
using System.Linq;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class CwCreateUpdateBudget : ChildWindow
    {
        [InputFieldData]
        [Display(Name = "FromDate", ResourceType = typeof(InputFieldDataText))]
        public static DateTime FromDate { get; set; }

        [InputFieldData]
        [Display(Name = "ToDate", ResourceType = typeof(InputFieldDataText))]
        public static DateTime ToDate { get; set; }

        [InputFieldData]
        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.ProjectBudgetGroup))]
        [Display(Name = "BudgetGroup", ResourceType = typeof(InputFieldDataText))]
        public static string Group { get; set; }

        [InputFieldData]
        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.Employee))]
        [Display(Name = "Employee", ResourceType = typeof(InputFieldDataText))]
        public static string Employee { get; set; }

        [InputFieldData]
        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.EmpPayrollCategory))]
        [Display(Name = "Payroll", ResourceType = typeof(InputFieldDataText))]
        public static string Payroll { get; set; }

        [InputFieldData]
        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.PrCategory))]
        [Display(Name = "ProjectCategory", ResourceType = typeof(InputFieldDataText))]
        public static string PrCategory { get; set; }

        [InputFieldData]
        [Display(Name = "Name", ResourceType = typeof(InputFieldDataText))]
        public static string BudgetName { get; set; }

        [InputFieldData]
        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.PrWorkSpace))]
        [Display(Name = "WorkSpace", ResourceType = typeof(InputFieldDataText))]
        public static string PrWorkSpace { get; set; }

        [InputFieldData]
        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.PrWorkSpace))]
        [Display(Name = "WorkSpace", ResourceType = typeof(InputFieldDataText))]
        public static string PrWorkSpaceNew { get; set; }


        [InputFieldData]
        [Display(Name = "DeleteBudget", ResourceType = typeof(InputFieldDataText))]
        public bool DeleteBudget { get; set; }

        [InputFieldData]
        [Display(Name = "InclProjectTasks", ResourceType = typeof(InputFieldDataText))]
        public bool InclProjectTask { get; set; }


        public int DialogTableId;
        protected override int DialogId { get { return DialogTableId; } }
        protected override bool ShowTableValueButton { get { return true; } }
        public static byte BudgetMethod;

        SQLTableCache<Uniconta.DataModel.ProjectBudgetGroup> budgetGrpCache;
        CrudAPI api;

        public CwCreateUpdateBudget(CrudAPI crudApi, int dialogType = 0)
        {
            FromDate = FromDate != DateTime.MinValue ? FromDate : Uniconta.ClientTools.Page.BasePage.GetSystemDefaultDate();
            ToDate = ToDate != DateTime.MinValue ? ToDate : Uniconta.ClientTools.Page.BasePage.GetSystemDefaultDate();
            this.DataContext = this;
            InitializeComponent();

            fromDate.DateTime = FromDate;
            toDate.DateTime = ToDate;
            api = crudApi;

            if (!crudApi.CompanyEntity.ProjectTask)
            {
                inclProjectTask.Visibility = Visibility.Collapsed;
                lblInclProjectTask.Visibility = Visibility.Collapsed;
            }

            switch (dialogType)
            {
                case 0:
                    Width = 425;
                    Height = 550;
                    this.Title = Uniconta.ClientTools.Localization.lookup("CreateBudgetRealized");
                    txtName.Visibility = Visibility.Visible;
                    lblTxtName.Visibility = txtName.Visibility;
                    leWorkspaceNew.Visibility = Visibility.Visible;
                    lblWorkspaceNew.Visibility = Visibility.Visible;
                    break;
                case 1:
                    Width = 400;
                    Height = 450;
                    this.Title = string.Format(Uniconta.ClientTools.Localization.lookup("UpdateOBJ"), Uniconta.ClientTools.Localization.lookup("Prices"));
                    txtName.Visibility = Visibility.Collapsed;
                    lblTxtName.Visibility = Visibility.Collapsed;
                    lblGroup.Visibility = Visibility.Visible;
                    leGroup.Visibility = Visibility.Visible;
                    lblTxtName.Visibility = Visibility.Collapsed;
                    txtName.Visibility = Visibility.Collapsed;
                    lblDeleteBudget.Visibility = Visibility.Collapsed;
                    deleteBudget.Visibility = Visibility.Collapsed;
                    cmbBudgetMethod.Visibility = Visibility.Collapsed;
                    lblBudgetMethod.Visibility = Visibility.Collapsed;
                    leWorkspaceNew.Visibility = Visibility.Collapsed;
                    lblWorkspaceNew.Visibility = Visibility.Collapsed;
                    break;
            }

            leGroup.api = leEmp.api = lePayroll.api = lePrCategory.api = leWorkspace.api = leWorkspaceNew.api = crudApi;
            cmbBudgetMethod.ItemsSource = new string[]
            {
                string.Concat(Uniconta.ClientTools.Localization.lookup("MonthView"), " (", Uniconta.ClientTools.Localization.lookup("Start"),")"),
                string.Concat(Uniconta.ClientTools.Localization.lookup("MonthView"), " (", Uniconta.ClientTools.Localization.lookup("End"), ")"),
                Uniconta.ClientTools.Localization.lookup("WeekView"), 
                Uniconta.ClientTools.Localization.lookup("DayView") 
            };
            cmbBudgetMethod.SelectedIndex = BudgetMethod;

            budgetGrpCache = crudApi.GetCache<Uniconta.DataModel.ProjectBudgetGroup>();
            SetDefaultValues();
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
                if (CancelButton.IsFocused)
                {
                    SetDialogResult(false);
                    return;
                }
                OKButton_Click(null, null);
            }
        }

        async private void SetDefaultValues()
        {
            if (budgetGrpCache == null)
                budgetGrpCache = await api.LoadCache<Uniconta.DataModel.ProjectBudgetGroup>();

            if (budgetGrpCache != null)
                leGroup.SelectedItem = budgetGrpCache.FirstOrDefault(s => s._Default);
        }

        private void cmbBudgetMethod_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            BudgetMethod = (byte)cmbBudgetMethod.SelectedIndex;
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
