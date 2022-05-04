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

using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Uniconta.API.Service;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.Util;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class ProjectGroupGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(ProjectGroupClient); } }
    }
    public partial class ProjectGroups : GridBasePage
    {
        public override string NameOfControl
        {
            get { return TabControls.ProjectGroups.ToString(); }
        }
        protected override bool IsLayoutSaveRequired()
        {
            return false;
        }
        public ProjectGroups(BaseAPI api, string lookupKey)
            : base(api, lookupKey)
        {
            Init();
        }
        public ProjectGroups(BaseAPI API) : base(API, string.Empty)
        {
            Init();
        }

        private void Init()
        {
            InitializeComponent();
            SetRibbonControl(localMenu, dgProjectGroupGrid);
            dgProjectGroupGrid.api = api;
            dgProjectGroupGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            ribbonControl.DisableButtons(new string[] { "AddLine", "CopyRow", "DeleteRow", "UndoDelete", "SaveGrid" });
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgProjectGroupGrid.SelectedItem as ProjectGroupClient;
            switch (ActionType)
            {
                case "EditAll":
                    if (dgProjectGroupGrid.Visibility == System.Windows.Visibility.Visible)
                        EditAll();
                    break;
                case "AddRow":
                    AddDockItem(TabControls.ProjectGroupPage2, api, Uniconta.ClientTools.Localization.lookup("ProjectGroup"), "Add_16x16.png");
                    break;
                case "EditRow":
                    if (selectedItem == null)
                        return;
                    AddDockItem(TabControls.ProjectGroupPage2, selectedItem);
                    break;
                case "AddLine":
                    dgProjectGroupGrid.AddRow();
                    break;
                case "CopyRecord":
                    CopyRecord(selectedItem);
                    break;
                case "CopyRow":
                    if (copyRowIsEnabled)
                        dgProjectGroupGrid.CopyRow();
                    else
                        CopyRecord(selectedItem);
                    break;
                case "DeleteRow":
                    dgProjectGroupGrid.DeleteRow();
                    break;
                case "SaveGrid":
                    Save();
                    break;
                case "UndoDelete":
                    dgProjectGroupGrid.UndoDeleteRow();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        void CopyRecord(ProjectGroupClient selectedItem)
        {
            if (selectedItem == null)
                return;
            var projectGrp = Activator.CreateInstance(selectedItem.GetType()) as ProjectGroupClient;
            CorasauDataGrid.CopyAndClearRowId(selectedItem, projectGrp);
            var parms = new object[2] { projectGrp, false };
            AddDockItem(TabControls.ProjectGroupPage2, parms, string.Format(Uniconta.ClientTools.Localization.lookup("CopyOBJ"), selectedItem.Group));
        }

        bool copyRowIsEnabled = false;
        bool editAllChecked;
        private void EditAll()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            var ibase = UtilDisplay.GetMenuCommandByName(rb, "EditAll");
            if (ibase == null)
                return;
            if (dgProjectGroupGrid.Readonly)
            {
                api.AllowBackgroundCrud = false;
                dgProjectGroupGrid.MakeEditable();
                UserFieldControl.MakeEditable(dgProjectGroupGrid);
                ibase.Caption = Uniconta.ClientTools.Localization.lookup("LeaveEditAll");
                ribbonControl.EnableButtons(new string[] { "AddLine", "CopyRow", "DeleteRow", "UndoDelete", "SaveGrid" });
                copyRowIsEnabled = true;
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
                                var err = await dgProjectGroupGrid.SaveData();
                                if (err != 0)
                                {
                                    api.AllowBackgroundCrud = true;
                                    return;
                                }
                                break;
                            case CWConfirmationBox.ConfirmationResultEnum.No:
                                break;
                        }
                        editAllChecked = true;
                        dgProjectGroupGrid.Readonly = true;
                        dgProjectGroupGrid.tableView.CloseEditor();
                        ibase.Caption = Uniconta.ClientTools.Localization.lookup("EditAll");
                        ribbonControl.DisableButtons(new string[] { "AddLine", "CopyRow", "DeleteRow", "UndoDelete", "SaveGrid" });
                        copyRowIsEnabled = false;
                    };
                    confirmationDialog.Show();
                }
                else
                {
                    dgProjectGroupGrid.Readonly = true;
                    dgProjectGroupGrid.tableView.CloseEditor();
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
                return editAllChecked ? false : dgProjectGroupGrid.HasUnsavedData;
            }
        }

        private async void Save()
        {
            SetBusy();
            var err = await dgProjectGroupGrid.SaveData();
            if (err != ErrorCodes.Succes)
                api.AllowBackgroundCrud = true;
            ClearBusy();
        }

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.ProjectGroupPage2)
                dgProjectGroupGrid.UpdateItemSource(argument);
        }
    }
}

