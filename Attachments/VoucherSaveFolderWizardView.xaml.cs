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
using DevExpress.Xpf.WindowsUI;
using Uniconta.ClientTools.Controls;
using Uniconta.Common;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using System.Collections;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    /// <summary>
    /// Interaction logic for VoucherFolderNameWizardView.xaml
    /// </summary>
    public partial class VoucherSaveFolderWizardView : UnicontaWizardControl
    {
        public VoucherSaveFolderWizardView(object wizardData)
        {
            InitializeComponent();
            DataContext = this;
            InitView(wizardData);
        }

        private void InitView(object wizardData)
        {
            dgVoucherFolderGrid.Readonly = false;
            var data = wizardData as IEnumerable<UnicontaClient.Pages.VoucherExtendedClient>;
            dgVoucherFolderGrid.SetSource(data.ToArray());
            cmbContentTypes.ItemsSource = AppEnums.ContentTypes.Values;
            cmbContentTypes.SelectedIndex = 0;
        }

        public override string Header { get { return string.Format("{0} {1}",Uniconta.ClientTools.Localization.lookup("Envelope") ,Uniconta.ClientTools.Localization.lookup("Content")); } }

        public override bool IsLastView { get { return true; } }

        public override void NavigateView(NavigationFrame frameView)
        {
            var selectedVouchers = dgVoucherFolderGrid.ItemsSource;
            var folderName = txtFolder.Text;
            var contentType = cmbContentTypes.SelectedItem;

            WizardData = new object[] { selectedVouchers, folderName, contentType };
        }
    }
}
