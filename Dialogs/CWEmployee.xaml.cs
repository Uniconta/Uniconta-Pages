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
using System.Windows.Shapes;
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.Common;
using UnicontaClient.Controls;

namespace UnicontaClient.Pages
{
    /// <summary>
    /// Interaction logic for CWEmployee.xaml
    /// </summary>
    public partial class CWEmployee : ChildWindow
    {
        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.Employee))]
        [InputFieldData]
        [Display(Name = "Employee", ResourceType = typeof(InputFieldDataText))]
        public string Employee { get; set; }
        [InputFieldData]
        [Display(Name = "Comment", ResourceType = typeof(InputFieldDataText))]
        public string Comment { get; set; }

#if !SILVERLIGHT
        public int DialogTableId { get; set; }
        protected override int DialogId { get { return DialogTableId; } }
        protected override bool ShowTableValueButton { get { return true; } }
#endif
        public CWEmployee(CrudAPI api)
        {
            this.DataContext = this;
            InitializeComponent();
            Title = Uniconta.ClientTools.Localization.lookup("Employee");
#if !SILVERLIGHT
            SizeToContent = SizeToContent.Height;
#else
            Utilities.Utility.SetThemeBehaviorOnChildWindow(this);
#endif
            lookupEmployee.api = api;
            Loaded += delegate { lookupEmployee.Focus(); };
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(true);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }

        private void ChildWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                CancelButton_Click(this, e);
            else if(e.Key == Key.Enter)
            {
                if (OKButton.IsFocused)
                    OKButton_Click(this, e);
                else if (CancelButton.IsFocused)
                    CancelButton_Click(this, e);
            }
        }
    }
}
