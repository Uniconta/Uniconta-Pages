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
    public partial class CwCreateBudgetTask : ChildWindow
    {

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
        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.PrWorkSpace))]
        [Display(Name = "WorkSpace", ResourceType = typeof(InputFieldDataText))]
        public static string PrWorkSpace { get; set; }

        [InputFieldData]
        [Display(Name = "DeleteBudget", ResourceType = typeof(InputFieldDataText))]
        public bool DeleteBudget { get; set; }

        [InputFieldData]
        [Display(Name = "BudgetTaskPrincip", ResourceType = typeof(InputFieldDataText))]
        public static BudgetTaskPrincip BudgetTaskPrincip { get; set; }

        [InputFieldData]
        [Display(Name = "TaskHours", ResourceType = typeof(InputFieldDataText))]
        public static double TaskHours { get; set; }

#if !SILVERLIGHT
        public int DialogTableId;
        protected override int DialogId { get { return DialogTableId; } }
        protected override bool ShowTableValueButton { get { return true; } }
#endif
        public CwCreateBudgetTask(CrudAPI crudApi, int dialogType = 0)
        {
            this.DataContext = this;
            InitializeComponent();

            this.Title = Uniconta.ClientTools.Localization.lookup("CreateBudgetTask");
            leGroup.api = leEmp.api = lePayroll.api = leWorkspace.api = crudApi;

            cmbBudgetTaskPrincip.ItemsSource = new string[] { Uniconta.ClientTools.Localization.lookup("SumLine"), Uniconta.ClientTools.Localization.lookup("Allocated") };
            cmbBudgetTaskPrincip.SelectedIndex = (byte)BudgetTaskPrincip;
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

        private void cmbBudgetTaskPrincip_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            switch (cmbBudgetTaskPrincip.SelectedIndex)
            {
                case 0: 
                    BudgetTaskPrincip = BudgetTaskPrincip.Princip1;
                    lblTaskHours.IsEnabled = false;
                    deTaskHours.IsEnabled = false;
                    break;
                case 1: 
                    BudgetTaskPrincip = BudgetTaskPrincip.Princip2;
                    lblTaskHours.IsEnabled = true;
                    deTaskHours.IsEnabled = true;
                    break;
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
