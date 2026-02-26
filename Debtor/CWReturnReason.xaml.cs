using FromXSDFile.OIOUBL.ExportImport;
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
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.Common.Utility;
using Uniconta.DataModel;
using Localization = Uniconta.ClientTools.Localization;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    /// <summary>
    /// Interaction logic for CWChangeCostValue.xaml
    /// </summary>
    public partial class CWReturnReason : ChildWindow
    {
        public InvTransReasonClient Reason;
        public CWReturnReason(InvTransReasonClient reason, CrudAPI api)
        {
            this.Reason = reason;
            this.DataContext = Reason;
            InitializeComponent();
            this.Title = Uniconta.ClientTools.Localization.lookup("ReturnReason");
            this.Loaded += CW_Loaded;
            leReason.api = api;
        }
        private void CW_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => { OKButton.Focus(); }));
        }

        private void ChildWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                SetDialogResult(false);
            }
            else if (e.Key == Key.Enter)
            {
                if (OKButton.IsFocused)
                    OKButton_Click(null, null);
                else if (CancelButton.IsFocused)
                    SetDialogResult(false);
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
