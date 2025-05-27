using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.AccessControl;
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

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    /// <summary>
    /// Interaction logic for CWProjects.xaml
    /// </summary>
    public partial class CWProjects : ChildWindow
    {
        [ForeignKeyAttribute(ForeignKeyTable = typeof(ProjectClient))]
        [Display(Name = "Project", ResourceType = typeof(InputFieldDataText))]
        public string Project { get; set; }
        public bool AllLines { get; set; }

        public bool ShowAllLines;

        public string Label;
        public CWProjects(CrudAPI api, string title = null)
        {
            InitializeComponent();
            this.DataContext = this;
            leProject.api = api;
            this.Title = title ?? Uniconta.ClientTools.Localization.lookup("Select");
            lblProject.Text = string.Format(Uniconta.ClientTools.Localization.lookup("ToOBJ"), Uniconta.ClientTools.Localization.lookup("Project"));
            this.Loaded += CW_Loaded;
            chkAllLines.Visibility = txtAllLines.Visibility = Visibility.Collapsed;
        }

        private void CW_Loaded(object sender, RoutedEventArgs e)
        {
            if (ShowAllLines)
                chkAllLines.Visibility = txtAllLines.Visibility = Visibility.Visible;
            if (!string.IsNullOrEmpty(Label))
                lblProject.Text = Label;
            Dispatcher.BeginInvoke(new Action(() => { leProject.Focus(); }));
        }
        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(true);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }

        private void imgClearProject_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            leProject.EditValue = null;
        }
    }
}
