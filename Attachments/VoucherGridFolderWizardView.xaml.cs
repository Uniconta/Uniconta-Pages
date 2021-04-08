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
using UnicontaClient.Pages;
using Uniconta.API.System;
using Uniconta.ClientTools.DataModel;
using Uniconta.Common;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    /// <summary>
    /// Interaction logic for VoucherFolderWizard.xaml
    /// </summary>
    public partial class VoucherGridFolderWizard : UnicontaWizardControl
    {
        private CrudAPI _api;
        public VoucherGridFolderWizard(CrudAPI api)
        {
            InitializeComponent();
            DataContext = this;
            dgVouchersGrid.api = _api = api;
            dgVouchersGrid.View.DataControl.CurrentItemChanged += dgVouchersGrid_CurrentItemChanged;
            documentViewer.Children.Add(Uniconta.ClientTools.Util.UtilDisplay.LoadMessage(Uniconta.ClientTools.Localization.lookup("NoRecords")));
            BindGrid();
        }

        async private void dgVouchersGrid_CurrentItemChanged(object sender, DevExpress.Xpf.Grid.CurrentItemChangedEventArgs e)
        {
            VoucherExtendedClient selectedVoucherClient = e.NewItem as VoucherExtendedClient;
            if (selectedVoucherClient != null)
            {
                busyIndicator.IsBusy = true;

                if (selectedVoucherClient._Data == null)
                    await _api.Read(selectedVoucherClient);

                documentViewer.Children.Clear();

                try
                {
                    if (selectedVoucherClient.Fileextension == FileextensionsTypes.UNK)
                        documentViewer.Children.Add(Uniconta.ClientTools.Util.UtilDisplay.LoadMessage(Uniconta.ClientTools.Localization.lookup("InvalidDocSave")));
                    else
                        documentViewer.Children.Add(Uniconta.ClientTools.Util.UtilDisplay.LoadControl(selectedVoucherClient, false, false));
                }
                catch (Exception ex)
                {
                    documentViewer.Children.Add(Uniconta.ClientTools.Util.UtilDisplay.LoadDefaultControl(string.Format("{0}. \n{1} : {2}", Uniconta.ClientTools.Localization.lookup("InvalidDocSave"),
                    Uniconta.ClientTools.Localization.lookup("ViewerFailed"), ex.Message)));
                }
                finally { busyIndicator.IsBusy = false; }

            }
        }

        async private void BindGrid()
        {
            busyIndicator.IsBusy = true;
            dgVouchersGrid.UpdateMaster(new DocumentNoRef());
            await dgVouchersGrid.Filter(new [] { PropValuePair.GenereteWhereElements("Envelope", typeof(bool), "0") } );
            busyIndicator.IsBusy = false;
        }

        public override string Header
        {
            get
            {
                return string.Format("{0} {1}", Uniconta.ClientTools.Localization.lookup("Select"), Uniconta.ClientTools.Localization.lookup("Vouchers"));
            }
        }

        public override bool IsLastView { get { return false; } }

        public override void NavigateView(NavigationFrame frameView)
        {
            var vouchersSelected = ((IEnumerable<VoucherExtendedClient>)dgVouchersGrid.ItemsSource).Where(p => p.IsAdded == true).ToList();

            if (vouchersSelected.Count > 0)
            {
                WizardData = vouchersSelected;
                frameView.Navigate(new VoucherSaveFolderWizardView(WizardData));
            }
            else
                UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("NoSettlementSelected"), Uniconta.ClientTools.Localization.lookup("Warning"),
                    MessageBoxButton.OK);
        }
    }
}
