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
        [Display(Name = "TaskHours", ResourceType = typeof(InputFieldDataText))]
        public static double TaskHours { get; set; }


        public int DialogTableId;
        protected override int DialogId { get { return DialogTableId; } }
        protected override bool ShowTableValueButton { get { return true; } }

        public static byte BudgetTaskPrincip;
        SQLTableCache<Uniconta.DataModel.ProjectBudgetGroup> budgetGrpCache;
        SQLTableCache<Uniconta.DataModel.PrWorkSpace> workspaceCache;
        CrudAPI api;

        public CwCreateBudgetTask(CrudAPI crudApi, int dialogType = 0, int cntTasks = 0)
        {
            api = crudApi;
            this.DataContext = this;
            InitializeComponent();

            this.Title = Uniconta.ClientTools.Localization.lookup("CreateBudgetTask");

            leGroup.api = leEmp.api = lePayroll.api = leWorkspace.api = crudApi;

            cmbBudgetTaskPrincip.ItemsSource = new string[] { Uniconta.ClientTools.Localization.lookup("SumLine"), Uniconta.ClientTools.Localization.lookup("Allocated"), Uniconta.ClientTools.Localization.lookup("NormHours") };
            cmbBudgetTaskPrincip.SelectedIndex = BudgetTaskPrincip;

            if (dialogType == 1)
            {
                this.Title += string.Concat("\r\n",Uniconta.ClientTools.Localization.lookup("QtyMarked"), " ", cntTasks);
                layoutGroupAccountRange.Visibility = Visibility.Collapsed;
                Height = 300;
            }

            budgetGrpCache = crudApi.GetCache<Uniconta.DataModel.ProjectBudgetGroup>();
            workspaceCache = crudApi.GetCache<Uniconta.DataModel.PrWorkSpace>();
            SetDefaultValues();
        }
        async private void SetDefaultValues()
        {
            if (budgetGrpCache == null)
                budgetGrpCache = await api.LoadCache<Uniconta.DataModel.ProjectBudgetGroup>();

            if (workspaceCache == null)
                workspaceCache = await api.LoadCache<Uniconta.DataModel.PrWorkSpace>();

            if (budgetGrpCache != null)
            {
                var budGrp = (ProjectBudgetGroup)budgetGrpCache.FirstOrDefault(s => s._Default);
                leGroup.SelectedItem = budGrp;
                Group = Group ?? budGrp?.Number;
            }

            if (workspaceCache != null)
            {
                var wrkspace = (PrWorkSpace)workspaceCache.FirstOrDefault(s => s._Default);
                leWorkspace.SelectedItem = wrkspace;
                PrWorkSpace = PrWorkSpace ?? wrkspace?.Number;
            }
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

        private void cmbBudgetTaskPrincip_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            BudgetTaskPrincip = (byte)cmbBudgetTaskPrincip.SelectedIndex;
            switch (cmbBudgetTaskPrincip.SelectedIndex)
            {
                case 0:
                case 2:
                    lblTaskHours.IsEnabled = false;
                    deTaskHours.IsEnabled = false;
                    break;
                case 1: 
                    lblTaskHours.IsEnabled = true;
                    deTaskHours.IsEnabled = true;
                    break;
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
