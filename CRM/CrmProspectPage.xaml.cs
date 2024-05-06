using UnicontaClient.Models;
using UnicontaClient.Utilities;
using System;
using System.Windows;
using Uniconta.API.Service;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;
using Uniconta.API.Crm;
using System.Windows.Input;
using System.Windows.Controls;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class CrmProspectGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(CrmProspectClient); } }
        public override bool SingleBufferUpdate { get { return false; } }
    }

    public partial class CrmProspectPage : GridBasePage
    {
        SQLCache interestCache, productsCache;
        public override string NameOfControl
        {
            get { return TabControls.CrmProspectPage.ToString(); }
        }
        public CrmProspectPage(BaseAPI API) : base(API, string.Empty)
        {
            InitPage();
        }
        public CrmProspectPage(BaseAPI api, string lookupKey)
            : base(api, lookupKey)
        {
            InitPage();
        }
        private void InitPage()
        {
            InitializeComponent();
            LayoutControl = crmDetailControl.layoutItems;
            dgCrmProspectGrid.api = api;
            dgCrmProspectGrid.BusyIndicator = busyIndicator;
            SetRibbonControl(localMenu, dgCrmProspectGrid);

            localMenu.OnItemClicked += localMenu_OnItemClicked;
            ribbonControl.DisableButtons(new string[] { "AddLine", "CopyRow", "DeleteRow", "UndoDelete", "SaveGrid" });
        }
        protected override void OnLayoutCtrlLoaded()
        {
            crmDetailControl.api = api;
        }
        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            UnicontaClient.Utilities.Utility.SetDimensionsGrid(api, cldim1, cldim2, cldim3, cldim4, cldim5);
            dgCrmProspectGrid.Readonly = true;
        }
        async protected override void LoadCacheInBackGround()
        {
            LoadType(new Type[] { typeof(CrmInterest), typeof(CrmProduct), typeof(Contact) });

            interestCache = api.CompanyEntity.GetCache(typeof(Uniconta.DataModel.CrmInterest)) ?? await api.CompanyEntity.LoadCache(typeof(Uniconta.DataModel.CrmInterest), api).ConfigureAwait(false);
            productsCache = api.CompanyEntity.GetCache(typeof(Uniconta.DataModel.CrmProduct)) ?? await api.CompanyEntity.LoadCache(typeof(Uniconta.DataModel.CrmProduct), api).ConfigureAwait(false);
        }

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.CrmProspectPage2)
                dgCrmProspectGrid.UpdateItemSource(argument);
            if (screenName == TabControls.UserNotesPage || screenName == TabControls.UserDocsPage && argument != null)
                dgCrmProspectGrid.UpdateItemSource(argument);
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgCrmProspectGrid.SelectedItem as CrmProspectClient;
            switch (ActionType)
            {
                case "EditAll":
                    if (dgCrmProspectGrid.Visibility == Visibility.Visible)
                        EditAll();
                    break;
                case "AddRow":
                    object[] param = new object[2];
                    param[0] = api;
                    param[1] = null;
                    AddDockItem(TabControls.CrmProspectPage2, param, Uniconta.ClientTools.Localization.lookup("Prospects"), "Add_16x16");
                    break;
                case "EditRow":
                    if (selectedItem != null)
                    {
                        object[] Params = new object[2] { selectedItem, true };
                        AddDockItem(TabControls.CrmProspectPage2, Params, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Prospects"), selectedItem.Name));
                    }
                    break;
                case "Contacts":
                    if (selectedItem != null)
                        AddDockItem(TabControls.ContactPage, dgCrmProspectGrid.syncEntity);
                    break;
                case "FollowUp":
                    if (selectedItem != null)
                    {
                        var followUpHeader = string.Format("{0}:{2} {1}", Uniconta.ClientTools.Localization.lookup("FollowUp"), selectedItem._Name, Uniconta.ClientTools.Localization.lookup("Prospects"));
                        AddDockItem(TabControls.CrmFollowUpPage, dgCrmProspectGrid.syncEntity, followUpHeader);
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
                case "Offers":
                    if (selectedItem != null)
                        AddDockItem(TabControls.DebtorOffers, selectedItem);
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
                    {
                        if (copyRowIsEnabled)
                            dgCrmProspectGrid.CopyRow();
                        else
                            CopyRecord(selectedItem);
                    }
                    break;
                case "ConvertToDebtor":
                    if (selectedItem != null)
                        ConvertProspectToDebtor(selectedItem);
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
                case "UndoDelete":
                    dgCrmProspectGrid.UndoDeleteRow();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
        void CopyRecord(CrmProspectClient selectedItem)
        {
            if (selectedItem == null)
                return;
            var prospect = Activator.CreateInstance(selectedItem.GetType()) as CrmProspectClient;
            CorasauDataGrid.CopyAndClearRowId(selectedItem, prospect);
            var parms = new object[2] { prospect, false };
            AddDockItem(TabControls.CrmProspectPage2, parms, Uniconta.ClientTools.Localization.lookup("Prospects"), "Add_16x16");
        }

        bool copyRowIsEnabled = false;
        bool editAllChecked;
        private void EditAll()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            var ibase = UtilDisplay.GetMenuCommandByName(rb, "EditAll");
            if (ibase == null)
                return;
            if (dgCrmProspectGrid.Readonly)
            {
                api.AllowBackgroundCrud = false;
                dgCrmProspectGrid.MakeEditable();
                UserFieldControl.MakeEditable(dgCrmProspectGrid);
                ibase.Caption = Uniconta.ClientTools.Localization.lookup("LeaveEditAll");
                ribbonControl.EnableButtons(new string[] { "AddLine", "CopyRow", "DeleteRow", "UndoDelete", "SaveGrid" });
                editAllChecked = false;
                copyRowIsEnabled = true;
            }
            else
            {
                if (IsDataChaged)
                {
                    string message = Uniconta.ClientTools.Localization.lookup("SaveChangesPrompt");
                    CWConfirmationBox confirmationDialog = new CWConfirmationBox(message);
                    confirmationDialog.Closing += async delegate
                    {
                        if (confirmationDialog.DialogResult == null)
                            return;

                        switch (confirmationDialog.ConfirmationResult)
                        {
                            case CWConfirmationBox.ConfirmationResultEnum.Yes:
                                var err = await dgCrmProspectGrid.SaveData();
                                if (err != 0)
                                {
                                    api.AllowBackgroundCrud = true;
                                    return;
                                }
                                break;
                            case CWConfirmationBox.ConfirmationResultEnum.No:
                                dgCrmProspectGrid.CancelChanges();
                                break;
                        }
                        editAllChecked = true;
                        dgCrmProspectGrid.Readonly = true;
                        dgCrmProspectGrid.tableView.CloseEditor();
                        ibase.Caption = Uniconta.ClientTools.Localization.lookup("EditAll");
                        ribbonControl.DisableButtons(new string[] { "AddLine", "CopyRow", "DeleteRow", "UndoDelete", "SaveGrid" });
                        copyRowIsEnabled = false;
                    };
                    confirmationDialog.Show();
                }
                else
                {
                    dgCrmProspectGrid.Readonly = true;
                    dgCrmProspectGrid.tableView.CloseEditor();
                    ibase.Caption = Uniconta.ClientTools.Localization.lookup("EditAll");
                    ribbonControl.DisableButtons(new string[] { "AddLine", "CopyRow", "DeleteRow", "UndoDelete", "SaveGrid" });
                    copyRowIsEnabled = false;
                }
            }
        }
        public override bool IsDataChaged
        {
            get
            {
                return editAllChecked ? false : dgCrmProspectGrid.HasUnsavedData;
            }
        }

        void ConvertProspectToDebtor(CrmProspectClient crmProspect)
        {
            CWConvertProspectToDebtor cwwin = new CWConvertProspectToDebtor(api, crmProspect);
            cwwin.Closing += async delegate
            {
                if (cwwin.DialogResult == true)
                {
                    if (cwwin?.DebtorClient == null) return;
                    CrmAPI crmApi = new CrmAPI(api);
                    var res = await crmApi.ConvertToDebtor(crmProspect, cwwin.DebtorClient);
                    UtilDisplay.ShowErrorCode(res);

                    if (res == ErrorCodes.Succes)
                        InitQuery();
                }
            };
            cwwin.Show();
        }

        private void HasDocImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var prospectClient = (sender as Image).Tag as CrmProspectClient;
            if (prospectClient != null)
                AddDockItem(TabControls.UserDocsPage, dgCrmProspectGrid.syncEntity);
        }

        private void HasNoteImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var prospectClient = (sender as Image).Tag as CrmProspectClient;
            if (prospectClient != null)
                AddDockItem(TabControls.UserNotesPage, dgCrmProspectGrid.syncEntity);
        }

        private void HasEmailImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var prospectClient = (sender as TextBlock).Tag as CrmProspectClient;
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
            var prospect = (sender as TextBlock).Tag as CrmProspectClient;
            Utility.OpenWebSite(prospect._Www);
        }

        private void cmbInterests_GotFocus(object sender, RoutedEventArgs e)
        {
            var cmb = sender as ComboBoxEditor;
            if (cmb != null && interestCache != null)
                cmb.ItemsSource = interestCache.GetKeyList();
        }

        private void cmbProducts_GotFocus(object sender, RoutedEventArgs e)
        {
            var cmb = sender as ComboBoxEditor;
            if (cmb != null)
                cmb.ItemsSource = productsCache.GetKeyList();
        }
    }
}
