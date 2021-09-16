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
    public partial class CwCreateAnchorBudget : ChildWindow
    {
        [InputFieldData]
        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.ProjectBudgetGroup))]
        [Display(Name = "Group", ResourceType = typeof(InputFieldDataText))]
        public static string Group { get; set; }

#if !SILVERLIGHT
        public int DialogTableId;
        protected override int DialogId { get { return DialogTableId; } }
        protected override bool ShowTableValueButton { get { return true; } }
#endif
        public CwCreateAnchorBudget(CrudAPI crudApi, int dialogType = 0)
        {
            this.DataContext = this;
            InitializeComponent();

            leGroup.api = crudApi;
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
