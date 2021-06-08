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
using Uniconta.ClientTools.Util;
using Uniconta.ClientTools.Controls;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class DebtorGroupGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(DebtorGroupClient); } }
    }
    public partial class DebtorGroup : GridBasePage
    {
        public override string NameOfControl
        {
            get { return TabControls.DebtorGroup.ToString(); }
        }
        protected override bool IsLayoutSaveRequired()
        {
            return false;
        }
        public DebtorGroup(BaseAPI api, string lookupKey)
            : base(api, lookupKey)
        {
            Init();
        }
        public DebtorGroup(BaseAPI API) : base(API, string.Empty)
        {
            Init();
        }

        private void Init()
        {
            InitializeComponent();
            localMenu.dataGrid = dgDebtorGroupGrid;
            SetRibbonControl(localMenu, dgDebtorGroupGrid);
            dgDebtorGroupGrid.api = api;
            dgDebtorGroupGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            ribbonControl.DisableButtons(new string[] { "AddLine", "CopyRow", "DeleteRow", "UndoDelete", "SaveGrid" });
        }

        protected override void OnLayoutCtrlLoaded()
        {
            var Comp = api.CompanyEntity;
            if (!Comp.InvDuty)
                this.ExemptDuty.ShowInColumnChooser = false;
            else
                this.ExemptDuty.ShowInColumnChooser = true;
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgDebtorGroupGrid.SelectedItem as DebtorGroupClient;
            switch (ActionType)
            {
                case "EditAll":
                    if (dgDebtorGroupGrid.Visibility == System.Windows.Visibility.Visible)
                        EditAll();
                    break;
                case "AddRow":
                    AddDockItem(TabControls.DebtorGroupPage2, api, Uniconta.ClientTools.Localization.lookup("DebtorGroup"), "Add_16x16.png");
                    break;
                case "EditRow": 
                    if (selectedItem == null)
                        return;
                    object[] EditParam = new object[2];
                    EditParam[0] = selectedItem;
                    EditParam[1] = true;
                    AddDockItem(TabControls.DebtorGroupPage2, EditParam, string.Format("{0}:{1}", Uniconta.ClientTools.Localization.lookup("DebtorGroup"), selectedItem.Name));
                    break;
                case "AddLine":
                    dgDebtorGroupGrid.AddRow();
                    break;
                case "CopyRecord":
                    CopyRecord(selectedItem);
                    break;
                case "CopyRow":
                    if (copyRowIsEnabled)
                        dgDebtorGroupGrid.CopyRow();
                    else
                        CopyRecord(selectedItem);
                    break;
                case "DeleteRow":
                    dgDebtorGroupGrid.DeleteRow();
                    break;
                case "SaveGrid":
                    Save();
                    break;
                case "UndoDelete":
                    dgDebtorGroupGrid.UndoDeleteRow();
                    break;
                case "GroupPosting":
                    if (selectedItem == null) return;
                    string grpPostingHeader = string.Format("{0}: {1}", string.Format(Uniconta.ClientTools.Localization.lookup("GroupPostingOBJ"), Uniconta.ClientTools.Localization.lookup("Debtor")),
                       selectedItem.Group);
                    AddDockItem(TabControls.DebtorGroupPostingPage, selectedItem, grpPostingHeader);
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        void CopyRecord(DebtorGroupClient selectedItem)
        {
            if (selectedItem == null)
                return;
            var debtorGrp = Activator.CreateInstance(selectedItem.GetType()) as DebtorGroupClient;
            CorasauDataGrid.CopyAndClearRowId(selectedItem, debtorGrp);
            var parms = new object[2] { debtorGrp, false };
            AddDockItem(TabControls.DebtorGroupPage2, parms, Uniconta.ClientTools.Localization.lookup("Debtorgroup"), "Add_16x16.png");
        }

        bool copyRowIsEnabled = false;
        bool editAllChecked;
        private void EditAll()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            var ibase = UtilDisplay.GetMenuCommandByName(rb, "EditAll");
            if (ibase == null)
                return;
            if (dgDebtorGroupGrid.Readonly)
            {
                api.AllowBackgroundCrud = false;
                dgDebtorGroupGrid.MakeEditable();
                UserFieldControl.MakeEditable(dgDebtorGroupGrid);
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
                                var err = await dgDebtorGroupGrid.SaveData();
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
                        dgDebtorGroupGrid.Readonly = true;
                        dgDebtorGroupGrid.tableView.CloseEditor();
                        ibase.Caption = Uniconta.ClientTools.Localization.lookup("EditAll");
                        ribbonControl.DisableButtons(new string[] { "AddLine", "CopyRow", "DeleteRow", "UndoDelete", "SaveGrid" });
                        copyRowIsEnabled = false;
                    };
                    confirmationDialog.Show();
                }
                else
                {
                    dgDebtorGroupGrid.Readonly = true;
                    dgDebtorGroupGrid.tableView.CloseEditor();
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
                return editAllChecked ? false : dgDebtorGroupGrid.HasUnsavedData;
            }
        }

        private async void Save()
        {
            SetBusy();
            var err = await dgDebtorGroupGrid.SaveData();
            if (err != ErrorCodes.Succes)
                api.AllowBackgroundCrud = true;
            ClearBusy();
        }

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.DebtorGroupPage2)
                dgDebtorGroupGrid.UpdateItemSource(argument);
        }
       
        protected override void LoadCacheInBackGround()
        {
            LoadType(new Type[] { typeof(Uniconta.DataModel.GLAccount), typeof(Uniconta.DataModel.GLVat) });
        }
    }
}
