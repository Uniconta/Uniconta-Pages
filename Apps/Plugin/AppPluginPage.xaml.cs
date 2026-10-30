using System.Collections.Generic;
using UnicontaClient.Models;
using UnicontaClient.Pages;
using UnicontaClient.Utilities;
using System;
using System.Threading.Tasks;
using Uniconta.API.Service;
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.DataModel;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.Util;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class AppPluginGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(AppPluginClient); } }
    }

    public partial class AppPluginPage : GridBasePage
    {
        public AppPluginPage(BaseAPI API) : base(API, string.Empty)
        {
            InitPage(null);
        }
        public AppPluginPage(UnicontaBaseEntity master) : base(master)
        {
            InitPage(master);
        }

        void InitPage(UnicontaBaseEntity master)
        {
            InitializeComponent();
            if (master != null)
                dgAppPluginGrid.masterRecords = new List<UnicontaBaseEntity>() { master, Utility.GetCurrentApp() };
            else
                dgAppPluginGrid.UpdateMaster(Utility.GetCurrentApp());
            localMenu.dataGrid = dgAppPluginGrid;
            dgAppPluginGrid.api = api;
            SetRibbonControl(localMenu, dgAppPluginGrid);
            dgAppPluginGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            ribbonControl.DisableButtons(new string[] { "DeleteRow", "UndoDelete", "SaveGrid" });
        }

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.AppPluginPage2)
            {
                var args = argument as object[];
                if (args[2] == LoadedRow)
                {
                    dgAppPluginGrid.UpdateItemSource(argument);
                    var qapi = new QueryAPI(api);
                    Plugin.Load(qapi);
                }
            }
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgAppPluginGrid.SelectedItem as AppPluginClient;
            switch (ActionType)
            {
                case "EditAll":
                    if (dgAppPluginGrid.Visibility == System.Windows.Visibility.Visible)
                        EditAll();
                    break;
                case "AddRow":
                    object[] parm = new object[2];
                    parm[0] = api;
                    parm[1] = LoadedRow;
                    AddDockItem(TabControls.AppPluginPage2, parm, Uniconta.ClientTools.Localization.lookup("UserPlugin"), "Add_16x16");
                    break;
                case "EditRow":
                    if (selectedItem != null)
                        AddDockItem(TabControls.AppPluginPage2, selectedItem, string.Format(Uniconta.ClientTools.Localization.lookup("EditOBJ"), selectedItem.Name));
                    break;
                case "SaveGrid":
                    saveGrid();
                    break;
                case "UndoDelete":
                    dgAppPluginGrid.UndoDeleteRow();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        bool editAllChecked;
        private void EditAll()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            var ibase = UtilDisplay.GetMenuCommandByName(rb, "EditAll");
            if (ibase == null)
                return;
            if (dgAppPluginGrid.Readonly)
            {
                api.AllowBackgroundCrud = false;
                dgAppPluginGrid.MakeEditable();
                UserFieldControl.MakeEditable(dgAppPluginGrid);
                ibase.Caption = Uniconta.ClientTools.Localization.lookup("LeaveEditAll");
                ribbonControl.EnableButtons(new string[] { "DeleteRow", "UndoDelete", "SaveGrid" });
                editAllChecked = false;
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
                                var err = await dgAppPluginGrid.SaveData();
                                if (err != 0)
                                {
                                    api.AllowBackgroundCrud = true;
                                    return;
                                }
                                break;
                            case CWConfirmationBox.ConfirmationResultEnum.No:
                                dgAppPluginGrid.CancelChanges();
                                break;
                        }
                        editAllChecked = true;
                        dgAppPluginGrid.Readonly = true;
                        dgAppPluginGrid.tableView.CloseEditor();
                        ibase.Caption = Uniconta.ClientTools.Localization.lookup("EditAll");
                        ribbonControl.DisableButtons(new string[] { "DeleteRow", "UndoDelete", "SaveGrid" });
                    };
                    confirmationDialog.Show();
                }
                else
                {
                    dgAppPluginGrid.Readonly = true;
                    dgAppPluginGrid.tableView.CloseEditor();
                    ibase.Caption = Uniconta.ClientTools.Localization.lookup("EditAll");
                    ribbonControl.DisableButtons(new string[] { "DeleteRow", "UndoDelete", "SaveGrid" });
                }
            }

        }
        public override bool IsDataChaged
        {
            get
            {
                return editAllChecked ? false : dgAppPluginGrid.HasUnsavedData;
            }
        }
    }
}
