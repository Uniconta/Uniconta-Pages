using UnicontaClient.Models;
using UnicontaClient.Utilities;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Uniconta.API.Service;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class ClosingSheetGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(GLClosingSheetClient); } }
    }
    public partial class ClosingSheet : GridBasePage
    {
        public override string NameOfControl
        {
            get { return TabControls.ClosingSheet.ToString(); }
        }
        public ClosingSheet(BaseAPI API) : base(API, string.Empty)
        {
            Init();
        }
        public ClosingSheet(BaseAPI api, string lookupKey)
            : base(api, lookupKey)
        {
            Init();
        }
        private void Init()
        {
            InitializeComponent();
            SetRibbonControl(localMenu, dgClosingSheet);
            dgClosingSheet.api = api;
            dgClosingSheet.BusyIndicator = busyIndicator;        
            
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgClosingSheet.RowDoubleClick += dgClosingSheet_RowDoubleClick;
        }
        void dgClosingSheet_RowDoubleClick()
        {
            ribbonControl.PerformRibbonAction("Accounts");
        }
        private void Name_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            dgClosingSheet_RowDoubleClick();
        }

        protected override void LoadCacheInBackGround() { LoadType(typeof(Uniconta.DataModel.GLAccount)); }
        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            UnicontaClient.Utilities.Utility.SetDimensionsGrid(api, cldim1, cldim2, cldim3, cldim4, cldim5);
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgClosingSheet.SelectedItem as GLClosingSheetClient;
            switch (ActionType)
            {
                case "AddRow":
                    AddDockItem(TabControls.ClosingSheetPage2, api, Uniconta.ClientTools.Localization.lookup("ClosingSheet"), "Add_16x16");
                    break;
                case "EditRow":
                    if (selectedItem == null)
                        return;
                    AddDockItem(TabControls.ClosingSheetPage2, selectedItem, string.Format("{0} - {1}", Uniconta.ClientTools.Localization.lookup("ClosingSheet"), selectedItem.Name));
                    break;
                case "Accounts":
                    if (selectedItem == null)
                        return;
                    AddDockItem(TabControls.GLAccountClosingSheetPage, selectedItem, string.Format("{0} - {1}", Uniconta.ClientTools.Localization.lookup("Accounts"), selectedItem.Name));
                    break;
                case "AddNote":
                    if (selectedItem != null)
                        AddDockItem(TabControls.UserNotesPage, dgClosingSheet.syncEntity);
                    break;
                case "AddDoc":
                    if (selectedItem != null)
                        AddDockItem(TabControls.UserDocsPage, dgClosingSheet.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Documents"), selectedItem._Name));
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.ClosingSheetPage2)
                dgClosingSheet.UpdateItemSource(argument);
            else if (screenName == TabControls.UserNotesPage || screenName == TabControls.UserDocsPage && argument != null)
                dgClosingSheet.UpdateItemSource(argument);
        }
        private void HasDocImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var debtorAccount = (sender as Image).Tag as GLClosingSheetClient;
            if (debtorAccount != null)
                AddDockItem(TabControls.UserDocsPage, dgClosingSheet.syncEntity);
        }

        private void HasNoteImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var debtorAccount = (sender as Image).Tag as GLClosingSheetClient;
            if (debtorAccount != null)
                AddDockItem(TabControls.UserNotesPage, dgClosingSheet.syncEntity);
        }
    }
}