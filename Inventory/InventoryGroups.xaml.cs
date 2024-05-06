using UnicontaClient.Models;
using UnicontaClient.Utilities;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using System;
using System.Collections;
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
    public class InventoryGroupGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(InvGroupClient); } }
    }
    public partial class InventoryGroups : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.InventoryGroups; } }

        public InventoryGroups(BaseAPI api, string lookupKey) : base(api, lookupKey)
        {
            Init();
        }
        public InventoryGroups(BaseAPI API) : base(API, string.Empty)
        {
            Init();
        }

        private void Init()
        {
            InitializeComponent();
            SetRibbonControl(localMenu, dgInventoryGroupGrid);
            dgInventoryGroupGrid.api = api;
            dgInventoryGroupGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            ribbonControl.DisableButtons(new string[] { "AddLine", "DeleteRow", "CopyRow", "UndoDelete", "SaveGrid" });
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            var Comp = api.CompanyEntity;
            if (!Comp.InvDuty)
                DutyGroup.ShowInColumnChooser = DutyGroup.Visible = false;
            else
                DutyGroup.ShowInColumnChooser = true;
            if (!Comp.Project)
                PrCategory.ShowInColumnChooser = PrCategory.Visible = false;
            else
                PrCategory.ShowInColumnChooser = true;
        }

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.InventoryGroupPage2)
                dgInventoryGroupGrid.UpdateItemSource(argument);
        }
        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgInventoryGroupGrid.SelectedItem as InvGroupClient;
            switch (ActionType)
            {
                case "EditAll":
                    if (dgInventoryGroupGrid.Visibility == System.Windows.Visibility.Visible)
                        EditAll();
                    break;
                case "AddRow":
                    AddDockItem(TabControls.InventoryGroupPage2, api, Uniconta.ClientTools.Localization.lookup("InventoryGroup"), "Add_16x16");
                    break;
                case "AddLine":
                    dgInventoryGroupGrid.AddRow();
                    break;
                case "EditRow":
                    if (selectedItem == null)
                        return;
                    object[] param = new object[2];
                    param[0] = selectedItem;
                    param[1] = true;
                    AddDockItem(TabControls.InventoryGroupPage2, param, string.Format("{0}:{1},{2}", Uniconta.ClientTools.Localization.lookup("InventoryGroup"), selectedItem.Group, selectedItem.Name));
                    break;
                case "CopyRow":
                    if (copyRowIsEnabled)
                        dgInventoryGroupGrid.CopyRow();
                    else
                        CopyRecord(selectedItem);
                    break;
                case "CopyRecord":
                    CopyRecord(selectedItem);
                    break;
                case "UndoDelete":
                    dgInventoryGroupGrid.UndoDeleteRow();
                    break;
                case "DeleteRow":
                    dgInventoryGroupGrid.DeleteRow();
                    break;
                case "SaveGrid":
                    saveGrid();
                    break;
                case "DebtorGroupPosting":
                    if (selectedItem == null) return;

                    string debtPostingHeader = string.Format("{0}: {1}", string.Format(Uniconta.ClientTools.Localization.lookup("GroupPostingOBJ"), Uniconta.ClientTools.Localization.lookup("Debtor")),
                      selectedItem.Group);
                    AddDockItem(TabControls.DebtorGroupPostingPage, selectedItem, debtPostingHeader);
                    break;
                case "CreditorGroupPosting":
                    if (selectedItem == null) return;

                    string credPostingHeader = string.Format("{0}: {1}", string.Format(Uniconta.ClientTools.Localization.lookup("GroupPostingOBJ"), Uniconta.ClientTools.Localization.lookup("Creditor")),
                      selectedItem.Group);
                    AddDockItem(TabControls.CreditorGroupPostingPage, selectedItem, credPostingHeader);
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        void CopyRecord(InvGroupClient selectedItem)
        {
            if (selectedItem == null)
                return;
            var invGroup = Activator.CreateInstance(selectedItem.GetType()) as InvGroupClient;
            CorasauDataGrid.CopyAndClearRowId(selectedItem, invGroup);
            object[] copyParam = new object[2];
            copyParam[0] = invGroup;
            copyParam[1] = false;
            string header = string.Format(Uniconta.ClientTools.Localization.lookup("CopyOBJ"), selectedItem.Group);
            AddDockItem(TabControls.InventoryGroupPage2, copyParam, header);
        }

        bool copyRowIsEnabled = false;
        bool editAllChecked;
        private void EditAll()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            var ibase = UtilDisplay.GetMenuCommandByName(rb, "EditAll");
            if (ibase == null)
                return;
            if (dgInventoryGroupGrid.Readonly)
            {
                api.AllowBackgroundCrud = false;
                dgInventoryGroupGrid.MakeEditable();
                UserFieldControl.MakeEditable(dgInventoryGroupGrid);
                ibase.Caption = Uniconta.ClientTools.Localization.lookup("LeaveEditAll");
                ribbonControl.EnableButtons(new string[] { "AddLine", "DeleteRow", "CopyRow", "UndoDelete", "SaveGrid" });
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
                                var err = await dgInventoryGroupGrid.SaveData();
                                if (err != 0)
                                {
                                    api.AllowBackgroundCrud = true;
                                    return;
                                }
                                break;
                            case CWConfirmationBox.ConfirmationResultEnum.No:
                                dgInventoryGroupGrid.CancelChanges();
                                break;
                        }
                        editAllChecked = true;
                        dgInventoryGroupGrid.Readonly = true;
                        dgInventoryGroupGrid.tableView.CloseEditor();
                        ibase.Caption = Uniconta.ClientTools.Localization.lookup("EditAll");
                        ribbonControl.DisableButtons(new string[] { "AddLine", "DeleteRow", "CopyRow", "UndoDelete", "SaveGrid" });
                        copyRowIsEnabled = false;
                    };
                    confirmationDialog.Show();
                }
                else
                {
                    dgInventoryGroupGrid.Readonly = true;
                    dgInventoryGroupGrid.tableView.CloseEditor();
                    ibase.Caption = Uniconta.ClientTools.Localization.lookup("EditAll");
                    ribbonControl.DisableButtons(new string[] { "AddLine", "DeleteRow", "CopyRow", "UndoDelete", "SaveGrid" });
                    copyRowIsEnabled = false;
                }
            }
        }
        public override bool IsDataChaged
        {
            get
            {
                return editAllChecked ? false : dgInventoryGroupGrid.HasUnsavedData;
            }
        }
    }
}
