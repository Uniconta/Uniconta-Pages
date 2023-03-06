using DevExpress.Xpf.Grid;
using System;
using System.Collections;
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
using Uniconta.API.Service;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using UnicontaClient.Models;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class DocsSendLogDataGridClient : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(DebtorSendLogClient); } }
        public override bool Readonly { get { return true; } }
        public override IComparer GridSorting { get { return new LogDateSort(); } }
    }

    /// <summary>
    /// Interaction logic for DCLogGridPage.xaml
    /// </summary>
    public partial class DocsSendLogGridPage : GridBasePage
    {
        public DocsSendLogGridPage(BaseAPI Api) : base(Api, string.Empty)
        {
            InitPage();
        }

        public DocsSendLogGridPage(SynchronizeEntity syncEntity)
            : base(syncEntity, true)
        {
            this.syncEntity = syncEntity;
            InitPage(syncEntity.Row);
            SetHeader();
        }

        public DocsSendLogGridPage(UnicontaBaseEntity master) : base(master)
        {
            InitPage(master);
        }
        private void InitPage(UnicontaBaseEntity master = null)
        {
            InitializeComponent();
            ((TableView)dgDocsSendLogDataGrid.View).RowStyle = Application.Current.Resources["StyleRow"] as Style;
            dgDocsSendLogDataGrid.UpdateMaster(master);
            SetRibbonControl(localMenu, dgDocsSendLogDataGrid);
            dgDocsSendLogDataGrid.BusyIndicator = busyIndicator;
            dgDocsSendLogDataGrid.api = api;
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
        }

        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            dgDocsSendLogDataGrid.UpdateMaster(args);
            SetHeader();
            InitQuery();
        }

        void SetHeader()
        {
            UnicontaBaseEntity masterClient = dgDocsSendLogDataGrid.masterRecord;

            if (masterClient is DebtorInvoiceClient)
            {
                var debtInvoiceMaster = masterClient as DebtorInvoiceClient;
                SetHeader(string.Format("{0} {1}/{2}", Uniconta.ClientTools.Localization.lookup("Invoice"), Uniconta.ClientTools.Localization.lookup("Log"), debtInvoiceMaster._InvoiceNumber));
            }
            else if (masterClient is DebtorClient)
            {
                Account.Visible = false;
                Name.Visible = false;
                var debtorMaster = masterClient as DebtorClient;
                SetHeader(string.Format("{0} {1}/{2}", Uniconta.ClientTools.Localization.lookup("Debtor"), Uniconta.ClientTools.Localization.lookup("Log"), debtorMaster.Account));
            }
        }

        public override string NameOfControl { get { return TabControls.DocsSendLogGridPage; } }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgDocsSendLogDataGrid.SelectedItem as DCDocSendLogClient;
            switch (ActionType)
            {
                case "ViewFile":
                    if (selectedItem != null)
                        ViewFile(selectedItem);
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        async void ViewFile(DCDocSendLogClient selectedItem)
        {
            if (selectedItem._File == null)
                await api.Read(selectedItem);

            if (selectedItem._File != null)
            {
                ChildWindow cw = null;
                if (selectedItem._Fileextenson == FileextensionsTypes.XML)
                    cw = new CWPreviewXMLViewer(selectedItem._File);
                else if (selectedItem._Fileextenson == FileextensionsTypes.TXT)
                    cw = new CWPreviewTextViewer(selectedItem._File);

                cw?.Show();
            }
        }

    }
}
