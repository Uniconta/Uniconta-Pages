using System;
using System.ComponentModel.DataAnnotations;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.Common;
using UnicontaClient.Controls;

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
        [Display(Name = "Group", ResourceType = typeof(InputFieldDataText))]
        public static string Group { get; set; }

        [InputFieldData]
        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.Employee))]
        [Display(Name = "Employee", ResourceType = typeof(InputFieldDataText))]
        public static string Employee { get; set; }

        [InputFieldData]
        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.Project))]
        [Display(Name = "Project", ResourceType = typeof(InputFieldDataText))]
        public static string Project { get; set; }

        [InputFieldData]
        [Display(Name = "Comment", ResourceType = typeof(InputFieldDataText))]
        public static string Comment { get; set; }

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

            switch (dialogType)
            {
                case 0:
                    this.Title = string.Format(Uniconta.ClientTools.Localization.lookup("CreateOBJ"), Uniconta.ClientTools.Localization.lookup("Budget"));
                    txtComment.Visibility = Visibility.Visible;
                    lblTxtCommment.Visibility = txtComment.Visibility;
                    break;
                case 1:
                    this.Title = string.Format(Uniconta.ClientTools.Localization.lookup("UpdateOBJ"), Uniconta.ClientTools.Localization.lookup("Prices"));
                    txtComment.Visibility = Visibility.Collapsed;
                    lblTxtCommment.Visibility = txtComment.Visibility;
                    break;
            }

            leGroup.api = leEmp.api = leProject.api = crudApi;
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
