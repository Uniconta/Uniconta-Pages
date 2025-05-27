using UnicontaClient.Models;
using UnicontaClient.Pages;
using UnicontaClient.Utilities;
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
using System.Windows.Shapes;
using Uniconta.API.Service;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class WorkInstallationGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(WorkInstallationClient); } }
    }

    public partial class WorkInstallationPage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.WorkInstallationPage; } }

        public WorkInstallationPage(BaseAPI api, string lookupKey)
            : base(api, lookupKey)
        {
            InitPage(null);
        }

        public WorkInstallationPage(BaseAPI API) : base(API, string.Empty)
        {
            InitPage(null);
        }

        public WorkInstallationPage(SynchronizeEntity syncEntity)
            : base(syncEntity, true)
        {
            this.syncEntity = syncEntity;
            InitPage(syncEntity.Row);
            SetHeader();
        }

        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            dgWorkInstallation.UpdateMaster(args);
            SetHeader();
            InitQuery();
        }

        void SetHeader()
        {
            string header;
            var syncMaster = dgWorkInstallation.masterRecord as DCAccount;
            if (syncMaster != null)
                header = string.Format("{0}: {1}, {2}", Uniconta.ClientTools.Localization.lookup("DeliveryAddresses"), syncMaster._Account, syncMaster._Name);
            else
               return;
            SetHeader(header);
        }

        public WorkInstallationPage(UnicontaBaseEntity master) : base(master)
        {
            InitPage(master);
        }

        void InitPage(UnicontaBaseEntity master)
        {
            InitializeComponent();
            dgWorkInstallation.UpdateMaster(master);
            SetRibbonControl(localMenu, dgWorkInstallation);
            dgWorkInstallation.api = api;
            dgWorkInstallation.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
        }

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.WorkInstallationPage2)
                dgWorkInstallation.UpdateItemSource(argument);
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgWorkInstallation.SelectedItem as WorkInstallationClient;
            switch (ActionType)
            {
                case "AddRow":
                    var newItem = dgWorkInstallation.GetChildInstance();
                    object[] param = new object[3];
                    param[0] = newItem;
                    param[1] = false;
                    param[2] = dgWorkInstallation.masterRecord;
                    var header = Uniconta.ClientTools.Localization.lookup("WorkInstallation");
                    if (dgWorkInstallation.masterRecord != null)
                        header = Uniconta.ClientTools.Localization.lookup("DeliveryAddresses");
                    AddDockItem(TabControls.WorkInstallationPage2, param, header, "Add_16x16");
                    break;
                case "EditRow":
                    if (selectedItem != null)
                    {
                        object[] para = new object[3];
                        para[0] = selectedItem;
                        para[1] = true;
                        para[2] = dgWorkInstallation.masterRecord;
                        AddDockItem(TabControls.WorkInstallationPage2,para, selectedItem.Name, null,true);
                    }
                    break;
                case "AddNote":
                    if (selectedItem != null)
                        AddDockItem(TabControls.UserNotesPage, dgWorkInstallation.syncEntity);
                    break;
                case "AddDoc":
                    if (selectedItem != null)
                        AddDockItem(TabControls.UserDocsPage, dgWorkInstallation.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Documents"), selectedItem._Name));
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        public override bool CheckIfBindWithUserfield(out bool isReadOnly, out bool useBinding)
        {
            isReadOnly = false;
            useBinding = true;
            return true;
        }

        private void HasDocImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var client = (sender as System.Windows.Controls.Image).Tag as WorkInstallationClient;
            if (client != null)
                AddDockItem(TabControls.UserDocsPage, dgWorkInstallation.syncEntity);
        }

        private void HasNoteImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var client = (sender as System.Windows.Controls.Image).Tag as WorkInstallationClient;
            if (client != null)
                AddDockItem(TabControls.UserNotesPage, dgWorkInstallation.syncEntity);
        }
    }
}
