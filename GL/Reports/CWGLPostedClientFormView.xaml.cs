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

namespace Uniconta.Client.Pages
{
    /// <summary>
    /// Interaction logic for CWFormView.xaml
    /// </summary>
    public partial class CWGLPostedClientFormView : ChildWindow
    {

        public CWGLPostedClientFormView(GLDailyJournalPostedClient journalPostedClient)
        {
            this.DataContext = this;
            InitializeComponent();
            Title = string.Format(Uniconta.ClientTools.Localization.lookup("ViewOBJ"), UnicontaClient.Utilities.UtilCommon.ClientTypeTableAttributeName(typeof(GLDailyJournalPostedClient)));
            layoutItems.DataContext = journalPostedClient;
            this.Loaded += CWGLPostedClientFormView_Loaded;
        }

        private void CWGLPostedClientFormView_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (string.IsNullOrWhiteSpace(txtPosted.Text))
                    txtPosted.Focus();
                else
                    btnClose.Focus();
            }));
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void LayoutRoot_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close();
        }
    }
}
