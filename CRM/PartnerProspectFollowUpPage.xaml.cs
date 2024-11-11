using UnicontaClient.Models;
using UnicontaClient.Pages;
using UnicontaClient.Utilities;
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
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;
using UnicontaClient.Pages;
using Uniconta.ClientTools.Controls;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class PartnerProspectFollowUpPageGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(PartnerProspectFollowUpClient); } }
        public override IComparer GridSorting { get { return new PartnerProspectFollowUpSort(); } }
    }

    public partial class PartnerProspectFollowUpPage : GridBasePage
    {
        public override string NameOfControl
        {
            get { return TabControls.PartnerProspectFollowUpPage.ToString(); }
        }

        public PartnerProspectFollowUpPage(BaseAPI API)
            : base(API, string.Empty)
        {
            InitializeComponent();
            InitPage(null);
            RemoveMenu();
        }
        public PartnerProspectFollowUpPage(UnicontaBaseEntity master)
            : base(master)
        {
            InitializeComponent();
            InitPage(master);
        }

        public PartnerProspectFollowUpPage(SynchronizeEntity syncEntity) : base(syncEntity, true)
        {
            InitializeComponent();
            InitPage(syncEntity.Row);
            SetHeader();
        }

        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            dgCrmFollowUpGrid.UpdateMaster(args);
            SetHeader();
            InitQuery();
        }

        private void SetHeader()
        {
            SetHeader(GetHeaderName(dgCrmFollowUpGrid.masterRecord));
        }

        private void InitPage(UnicontaBaseEntity master)
        {
            dgCrmFollowUpGrid.UpdateMaster(master);
            localMenu.dataGrid = dgCrmFollowUpGrid;
            dgCrmFollowUpGrid.api = api;
            dgCrmFollowUpGrid.BusyIndicator = busyIndicator;
            SetRibbonControl(localMenu, dgCrmFollowUpGrid);
            localMenu.OnItemClicked += localMenu_OnItemClicked;
        }

        void RemoveMenu()
        {
            var Comp = api.CompanyEntity;
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            UtilDisplay.RemoveMenuCommand(rb, "AddRow");
        }

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.PartnerProspectFollowUpPage2)
                dgCrmFollowUpGrid.UpdateItemSource(argument);
        }

        string GetHeaderName(UnicontaBaseEntity masterRecord)
        {
            string key = Utility.GetHeaderString(masterRecord);
            string header = string.Empty;
            return  header = string.Format("{0}:{2} {1}", Uniconta.ClientTools.Localization.lookup("FollowUp"), key, Uniconta.ClientTools.Localization.lookup("PartnerProspects"));
        }

        void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgCrmFollowUpGrid.SelectedItem as PartnerProspectFollowUpClient;
            switch (ActionType)
            {
                case "AddRow":
                    var newItem = dgCrmFollowUpGrid.GetChildInstance();
                    object[] param = new object[3];
                    param[0] = newItem;
                    param[1] = false;
                    param[2] = dgCrmFollowUpGrid.masterRecord;
                    AddDockItem(TabControls.PartnerProspectFollowUpPage2, param, Uniconta.ClientTools.Localization.lookup("FollowUp"), "Add_16x16");
                    break;
                case "EditRow":
                    if (selectedItem == null)
                        return;
                    object[] para = new object[3];
                    para[0] = selectedItem;
                    para[1] = true;
                    para[2] = dgCrmFollowUpGrid.masterRecord;
                    AddDockItem(TabControls.PartnerProspectFollowUpPage2, para, selectedItem.UserName, null, true);
                    break;
                case "AddNote":
                    if (selectedItem != null)
                        AddDockItem(TabControls.UserNotesPage, dgCrmFollowUpGrid.syncEntity);
                    break;
                case "AddDoc":
                    if (selectedItem != null)
                        AddDockItem(TabControls.UserDocsPage, dgCrmFollowUpGrid.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Documents"), selectedItem.PrimaryKeyId));
                    break;
                case "AddLine":
                    dgCrmFollowUpGrid.AddRow();
                    break;
                case "CopyRow":
                    if (selectedItem != null)
                        CopyRecord(selectedItem);
                    break;
                case "DeleteRow":
                    dgCrmFollowUpGrid.DeleteRow();
                    break;
                case "SaveGrid":
                    saveGrid();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        void CopyRecord(PartnerProspectFollowUpClient selectedItem)
        {
            if (selectedItem == null)
                return;
            var followUp = Activator.CreateInstance(selectedItem.GetType()) as PartnerProspectFollowUpClient;
            CorasauDataGrid.CopyAndClearRowId(selectedItem, followUp, api);
            var parms = new object[3] { followUp, false, dgCrmFollowUpGrid.masterRecord };
            AddDockItem(TabControls.PartnerProspectFollowUpPage2, parms, Uniconta.ClientTools.Localization.lookup("FollowUp"), "Add_16x16");
        }
    }
}
