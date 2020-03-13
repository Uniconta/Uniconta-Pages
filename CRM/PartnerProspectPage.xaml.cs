using UnicontaClient.Models;
using UnicontaClient.Pages;
using UnicontaClient.Utilities;
using System;
using System.Collections;
using System.Linq;
using System.Windows;
using Uniconta.API.Service;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;
using Uniconta.API.Crm;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Controls;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class PartnerProspectGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(PartnerProspectClient); } }
        public override bool SingleBufferUpdate { get { return false; } }
    }

    public partial class PartnerProspectPage : GridBasePage
    {
        public override string NameOfControl
        {
            get { return TabControls.PartnerProspectPage.ToString(); }
        }
        public PartnerProspectPage(BaseAPI API) : base(API, string.Empty)
        {
            InitPage();
        }
        public PartnerProspectPage(BaseAPI api, string lookupKey)
            : base(api, lookupKey)
        {
            InitPage();
        }
        private void InitPage()
        {
            InitializeComponent();
            dgCrmProspectGrid.api = api;
            dgCrmProspectGrid.BusyIndicator = busyIndicator;
            SetRibbonControl(localMenu, dgCrmProspectGrid);
            localMenu.OnItemClicked += localMenu_OnItemClicked;
        }
       
        protected override void LoadCacheInBackGround()
        {
            LoadType(new Type[] { typeof(CrmInterest), typeof(CrmProduct) });
        }

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.PartnerProspectPage2)
                dgCrmProspectGrid.UpdateItemSource(argument);
            if (screenName == TabControls.UserNotesPage || screenName == TabControls.UserDocsPage && argument != null)
                dgCrmProspectGrid.UpdateItemSource(argument);
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgCrmProspectGrid.SelectedItem as PartnerProspectClient;
            switch (ActionType)
            {
                case "AddRow":
                    object[] param = new object[2];
                    param[0] = api;
                    param[1] = null;
                    AddDockItem(TabControls.PartnerProspectPage2, param, Uniconta.ClientTools.Localization.lookup("PartnerProspects"), "Add_16x16.png");
                    break;
                case "EditRow":
                    if (selectedItem != null)
                    {
                        object[] Params = new object[2] { selectedItem, true };
                        AddDockItem(TabControls.PartnerProspectPage2, Params, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("PartnerProspects"), selectedItem.Name));
                    }
                    break;
                case "FollowUp":
                    if (selectedItem != null)
                    {
                        var followUpHeader = string.Format("{0}:{2} {1}", Uniconta.ClientTools.Localization.lookup("FollowUp"), selectedItem._Name, Uniconta.ClientTools.Localization.lookup("PartnerProspects"));
                        AddDockItem(TabControls.PartnerProspectFollowUpPage, dgCrmProspectGrid.syncEntity, followUpHeader);
                    }
                    break;
                case "AddNote":
                    if (selectedItem != null)
                        AddDockItem(TabControls.UserNotesPage, dgCrmProspectGrid.syncEntity);
                    break;
                case "AddDoc":
                    if (selectedItem != null)
                        AddDockItem(TabControls.UserDocsPage, dgCrmProspectGrid.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Documents"), selectedItem._Name));
                    break;
                case "RefreshGrid":
                    if (gridControl.Visibility == Visibility.Visible)
                        gridRibbon_BaseActions(ActionType);
                    break;
                case "AddLine":
                    dgCrmProspectGrid.AddRow();
                    break;
                case "CopyRow":
                    if (selectedItem != null)
                        CopyRecord(selectedItem);
                    break;
                case "DeleteRow":
                    dgCrmProspectGrid.DeleteRow();
                    break;
                case "SaveGrid":
                    saveGrid();
                    break;
                case "CopyRecord":
                    CopyRecord(selectedItem);
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
        void CopyRecord(PartnerProspectClient selectedItem)
        {
            if (selectedItem == null)
                return;
            var prospect = Activator.CreateInstance(selectedItem.GetType()) as PartnerProspectClient;
            StreamingManager.Copy(selectedItem, prospect);
            var parms = new object[2] { prospect, false };
            AddDockItem(TabControls.PartnerProspectPage2, parms, Uniconta.ClientTools.Localization.lookup("PartnerProspects"), "Add_16x16.png");
        }

        private void HasDocImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var prospectClient = (sender as Image).Tag as PartnerProspectClient;
            if (prospectClient != null)
                AddDockItem(TabControls.UserDocsPage, dgCrmProspectGrid.syncEntity);
        }

        private void HasNoteImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var prospectClient = (sender as Image).Tag as PartnerProspectClient;
            if (prospectClient != null)
                AddDockItem(TabControls.UserNotesPage, dgCrmProspectGrid.syncEntity);
        }
#if !SILVERLIGHT
        private void HasEmailImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var prospectClient = (sender as TextBlock).Tag as PartnerProspectClient;
            if (prospectClient != null)
            {
                var mail = string.Concat("mailto:", prospectClient._ContactEmail);
                System.Diagnostics.Process proc = new System.Diagnostics.Process();
                proc.StartInfo.FileName = mail;
                proc.Start();
            }
        }

        private void HasWebsite_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var prospectClient = (sender as TextBlock).Tag as PartnerProspectClient;
            if (prospectClient != null)
                Utility.OpenWebSite(prospectClient.Www);
        }
#endif
    }
}
