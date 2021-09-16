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
        [Display(Name = "BudgetMethod", ResourceType = typeof(InputFieldDataText))]
        public static BudgetMethod BudgetMethod { get; set; }

        [InputFieldData]
        [Display(Name = "Name", ResourceType = typeof(InputFieldDataText))]
        public static string BudgetName { get; set; }

        [InputFieldData]
        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.PrWorkSpace))]
        [Display(Name = "WorkSpace", ResourceType = typeof(InputFieldDataText))]
        public static string PrWorkSpace { get; set; }

        [InputFieldData]
        [Display(Name = "DeleteBudget", ResourceType = typeof(InputFieldDataText))]
        public bool DeleteBudget { get; set; }

        [InputFieldData]
        [Display(Name = "InclProjectTasks", ResourceType = typeof(InputFieldDataText))]
        public bool InclProjectTask { get; set; }

#if !SILVERLIGHT
        public int DialogTableId;
        protected override int DialogId { get { return DialogTableId; } }
        protected override bool ShowTableValueButton { get { return true; } }
#endif
        public CwCreateUpdateBudget(CrudAPI crudApi, int dialogType = 0)
        {
            FromDate = FromDate != DateTime.MinValue ? FromDate : Uniconta.ClientTools.Page.BasePage.GetSystemDefaultDate();
            ToDate = ToDate != DateTime.MinValue ? ToDate : Uniconta.ClientTools.Page.BasePage.GetSystemDefaultDate();
            this.DataContext = this;
            InitializeComponent();

            fromDate.DateTime = FromDate;
            toDate.DateTime = ToDate;

            if (!crudApi.CompanyEntity.ProjectTask)
            {
                inclProjectTask.Visibility = Visibility.Collapsed;
                lblInclProjectTask.Visibility = Visibility.Collapsed;
            }

            switch (dialogType)
            {
                case 0:
                    Width = 425;
                    Height = 540;
                    this.Title = Uniconta.ClientTools.Localization.lookup("CreateBudgetRealized");
                    txtName.Visibility = Visibility.Visible;
                    lblTxtName.Visibility = txtName.Visibility;
                    break;
                case 1:
                    Width = 400;
                    Height = 400;
                    this.Title = string.Format(Uniconta.ClientTools.Localization.lookup("UpdateOBJ"), Uniconta.ClientTools.Localization.lookup("Prices"));
                    txtName.Visibility = Visibility.Collapsed;
                    lblTxtName.Visibility = Visibility.Collapsed;
                    layoutGroupSettings.Visibility = Visibility.Collapsed;
                    cmbBudgetMethod.Visibility = Visibility.Collapsed;
                    lblBudgetMethod.Visibility = Visibility.Collapsed;
                    break;
            }

            leGroup.api = leEmp.api = lePayroll.api = lePrCategory.api = leWorkspace.api = crudApi;
            cmbBudgetMethod.ItemsSource = new string[] { Uniconta.ClientTools.Localization.lookup("MonthView"), Uniconta.ClientTools.Localization.lookup("WeekView"), Uniconta.ClientTools.Localization.lookup("DayView") };
            cmbBudgetMethod.SelectedIndex = (byte)BudgetMethod;
        }

        private void ChildWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.DialogResult = false;
            }
            else
                if (e.Key == Key.Enter)
            {
                if (CancelButton.IsFocused)
                {
                    this.DialogResult = false;
                    return;
                }
                OKButton_Click(null, null);
            }
        }

        private void cmbBudgetMethod_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            switch (cmbBudgetMethod.SelectedIndex)
            {
                case 0: BudgetMethod = BudgetMethod.Month; break;
                case 1: BudgetMethod = BudgetMethod.Week; break;
                case 2: BudgetMethod = BudgetMethod.Day; break;
            }
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}
