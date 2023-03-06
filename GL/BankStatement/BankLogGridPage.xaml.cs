using DevExpress.Xpf.Grid;
using DevExpress.Xpf.Grid.Filtering;
using dk.gov.oiosi.logging;
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
    public class BankLogDataGridPageClient : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(BankLogClient); } }
        public override bool Readonly { get { return true; } }
        public override IComparer GridSorting { get { return new BankLogDateSort(); } }
    }

    public partial class BankLogGridPage : GridBasePage
    {
        public BankLogGridPage(BaseAPI Api) : base(Api, string.Empty)
        {
            InitPage();
        }

        public BankLogGridPage(SynchronizeEntity syncEntity)
            : base(syncEntity, true)
        {
            this.syncEntity = syncEntity;
            InitPage(syncEntity.Row);
            SetHeader();
        }

        public BankLogGridPage(UnicontaBaseEntity master) : base(master)
        {
            InitPage(master);
        }
        private void InitPage(UnicontaBaseEntity master = null)
        {
            InitializeComponent();
            ((TableView)dgBankLogDataGrid.View).RowStyle = Application.Current.Resources["StyleRow"] as Style;

            dgBankLogDataGrid.UpdateMaster (master);

            SetRibbonControl(localMenu, dgBankLogDataGrid);
            dgBankLogDataGrid.BusyIndicator = busyIndicator;
            dgBankLogDataGrid.api = api;
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
        }

        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            dgBankLogDataGrid.UpdateMaster(args);
            SetHeader();
            InitQuery();
        }

        void SetHeader()
        {
            UnicontaBaseEntity masterClient = dgBankLogDataGrid.masterRecord;
            if (masterClient is BankStatementClient)
            {
                var bankStatementMaster = masterClient as BankStatementClient;
                SetHeader(string.Format("{0} {1}/{2}", Uniconta.ClientTools.Localization.lookup("BankAccount"), Uniconta.ClientTools.Localization.lookup("Log"), bankStatementMaster._Account));
            }
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            bool showFields = (dgBankLogDataGrid.masterRecords == null);
            Account.Visible = showFields;
        }
        public override string NameOfControl { get { return TabControls.BankLogGridPage; } }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgBankLogDataGrid.SelectedItem as BankLogClient;
            var selectedItems = dgBankLogDataGrid.SelectedItems;
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

        async void ViewFile(BankLogClient selectedItem)
        {
            if (selectedItem._Xml == null)
                await api.Read(selectedItem);

            if (selectedItem._Xml != null)
            {
                var cw = new CWPreviewXMLViewer(selectedItem._Xml, selectedItem.CamtFormat);
                cw.Show();
            }
        }
    }
}
