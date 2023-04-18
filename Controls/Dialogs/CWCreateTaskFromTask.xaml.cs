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

namespace UnicontaClient.Controls.Dialogs
{
    public partial class CWCreateTaskFromTask : ChildWindow
    {

        [InputFieldData]
        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.PrWorkSpace))]
        [Display(Name = "WorkSpace", ResourceType = typeof(InputFieldDataText))]
        public static string FromPrWorkSpace { get; set; }

        [InputFieldData]
        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.Project))]
        [Display(Name = "ProjectTemplate", ResourceType = typeof(InputFieldDataText))]
        public static string ProjectTemplate { get; set; }

        [InputFieldData]
        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.PrWorkSpace))]
        [Display(Name = "WorkSpace", ResourceType = typeof(InputFieldDataText))]
        public static string ToPrWorkSpace { get; set; }

        [InputFieldData]
        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.Project))]
        [Display(Name = "Project", ResourceType = typeof(InputFieldDataText))]
        public string ToProject { get; set; }

        [InputFieldData]
        [Display(Name = "Date", ResourceType = typeof(InputFieldDataText))]
        public DateTime NewDate { get; set; }

        public int DialogTableId;
        protected override int DialogId { get { return DialogTableId; } }
        protected override bool ShowTableValueButton { get { return true; } }
        CrudAPI api;
        public byte BudgetTaskDatePrincip;
        public static int AddYear;

        public CWCreateTaskFromTask(CrudAPI crudApi, string projectNo)
        {
            api = crudApi;
            ToProject = projectNo;
            InitPage();
        }

        public CWCreateTaskFromTask(CrudAPI crudApi)
        {
            api = crudApi;
            BudgetTaskDatePrincip = 2;
            InitPage();
        }

        void InitPage()
        {
            this.DataContext = this;
            InitializeComponent();

            leFromWorkspace.api = leToWorkspace.api = leToProject.api = leProjectTemplate.api = api;
            this.Title = string.Format(Uniconta.ClientTools.Localization.lookup("CopyOBJ"), Uniconta.ClientTools.Localization.lookup("Task"));

            cmbBudgetTaskDatePrincip.ItemsSource = new string[] { Uniconta.ClientTools.Localization.lookup("StartDate"), Uniconta.ClientTools.Localization.lookup("EndDate"), Uniconta.ClientTools.Localization.lookup("Year") };
            cmbBudgetTaskDatePrincip.SelectedIndex = BudgetTaskDatePrincip;
            cmbAddYear.ItemsSource = new string[] { "", "1", "2", "3", "4", "5" };
            

            if (ToProject != null)
            {
                Height = 370;
                lblProjectTemplate.Visibility = Visibility.Collapsed;
                leProjectTemplate.Visibility = Visibility.Collapsed;
            }
            else
            {
                Height = 300;
                lblToProject.Visibility = Visibility.Collapsed;
                leToProject.Visibility = Visibility.Collapsed;
                lblNewDate.Visibility = Visibility.Collapsed;
                leNewDate.Visibility = Visibility.Collapsed;
                lblBudgetTaskDatePrincip.Visibility = Visibility.Collapsed;
                cmbBudgetTaskDatePrincip.Visibility = Visibility.Collapsed;
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

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(true);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }

        private void cmbBudgetTaskDatePrincip_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            BudgetTaskDatePrincip = (byte)cmbBudgetTaskDatePrincip.SelectedIndex;

            if (BudgetTaskDatePrincip == 2)
            {
                leNewDate.IsEnabled = false;
                lblNewDate.IsEnabled = false;
                leNewDate.DateTime = DateTime.MinValue;
                NewDate = DateTime.MinValue;

                cmbAddYear.IsEnabled = true;
                lblAddYear.IsEnabled = true;
                cmbAddYear.SelectedIndex = ToProject == null ? 0 : 1;
            }
            else
            {
                leNewDate.IsEnabled = true;
                lblNewDate.IsEnabled = true;

                cmbAddYear.IsEnabled = false;
                lblAddYear.IsEnabled = false;
                cmbAddYear.SelectedIndex = 0;
            }
        }

        private void cmbAddYear_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            AddYear = cmbAddYear.SelectedIndex;
        }
    }
}
