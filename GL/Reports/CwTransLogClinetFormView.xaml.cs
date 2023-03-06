using DevExpress.Xpf.LayoutControl;
using System;
using System.Collections.Generic;
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
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Util;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class CWGLTransLogClientFormView : ChildWindow
    {
        public CWGLTransLogClientFormView(GLTransLogClient glTransLogClient)
        {
            this.DataContext = this;
            InitializeComponent();
            Title = string.Format(Uniconta.ClientTools.Localization.lookup("ViewOBJ"), UnicontaClient.Utilities.UtilCommon.ClientTypeTableAttributeName(typeof(GLTransLogClient)));
            layoutItems.DataContext = glTransLogClient;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void LayoutRoot_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close();
        }
    }
}
