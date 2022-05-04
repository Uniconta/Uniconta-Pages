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
using System.Windows.Data;
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
    public class CreditorGroupGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(CreditorGroupClient); } }
    }
    public partial class CreditorGroup : GridBasePage
    {
        public override string NameOfControl
        {
            get { return TabControls.CreditorGroup.ToString(); }
        }
        protected override bool IsLayoutSaveRequired()
        {
            return false;
        }
        public CreditorGroup(BaseAPI api, string lookupKey)
            : base(api, lookupKey)
        {
            Init();
        }
        public CreditorGroup(BaseAPI API)
            : base(API, string.Empty)
        {
            Init();
        }

        private void Init()
        {
            InitializeComponent();
            SetRibbonControl(localMenu, dgCreditorGroupGrid);
            dgCreditorGroupGrid.api = api;
            dgCreditorGroupGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            ribbonControl.DisableButtons(new string[] { "AddLine", "CopyRow", "DeleteRow", "UndoDelete", "SaveGrid" });
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgCreditorGroupGrid.SelectedItem as CreditorGroupClient;
            switch (ActionType)
            {
                case "EditAll":
                    if (dgCreditorGroupGrid.Visibility == System.Windows.Visibility.Visible)
                        EditAll();
                    break;
                case "AddRow":
                    AddDockItem(TabControls.CreditorGroupPage2, api, Uniconta.ClientTools.Localization.lookup("CreditorGroup"), "Add_16x16.png");
                    break;
                case "EditRow":
                    if (selectedItem == null)
                        return;
                    object[] EditParam = new object[2];
                    EditParam[0] = selectedItem;
                    EditParam[1] = true;
                    AddDockItem(TabControls.CreditorGroupPage2, EditParam, string.Format("{0}:{1}", Uniconta.ClientTools.Localization.lookup("CreditorGroup"), selectedItem.Name));
                    break;
                case "AddLine":
                    dgCreditorGroupGrid.AddRow();
                    break;
                case "CopyRecord":
                    CopyRecord(selectedItem);
                    break;
                case "CopyRow":
                    if (copyRowIsEnabled)
                        dgCreditorGroupGrid.CopyRow();
                    else
                        CopyRecord(selectedItem);
                    break;
                case "DeleteRow":
                    dgCreditorGroupGrid.DeleteRow();
                    break;
                case "SaveGrid":
                    Save();
                    break;
                case "UndoDelete":
                    dgCreditorGroupGrid.UndoDeleteRow();
                    break;
                case "GroupPosting":
                    if (selectedItem == null) return;
                    string grpPostingHeader = string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("ItemPosting"), selectedItem.Group);
                    AddDockItem(TabControls.CreditorGroupPostingPage, selectedItem, grpPostingHeader);
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        void CopyRecord(CreditorGroupClient selectedItem)
        {
            if (selectedItem == null)
                return;
            var creditorGrp = Activator.CreateInstance(selectedItem.GetType()) as CreditorGroupClient;
            CorasauDataGrid.CopyAndClearRowId(selectedItem, creditorGrp);
            var parms = new object[2] { creditorGrp, false };
            AddDockItem(TabControls.CreditorGroupPage2, parms, Uniconta.ClientTools.Localization.lookup("CreditorGroup"), "Add_16x16.png");
        }

        bool copyRowIsEnabled = false;
        bool editAllChecked;
        private void EditAll()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            var ibase = UtilDisplay.GetMenuCommandByName(rb, "EditAll");
            if (ibase == null)
                return;
            if (dgCreditorGroupGrid.Readonly)
            {
                api.AllowBackgroundCrud = false;
                dgCreditorGroupGrid.MakeEditable();
                UserFieldControl.MakeEditable(dgCreditorGroupGrid);
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
                                var err = await dgCreditorGroupGrid.SaveData();
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
                        dgCreditorGroupGrid.Readonly = true;
                        dgCreditorGroupGrid.tableView.CloseEditor();
                        ibase.Caption = Uniconta.ClientTools.Localization.lookup("EditAll");
                        ribbonControl.DisableButtons(new string[] { "AddLine", "CopyRow", "DeleteRow", "UndoDelete", "SaveGrid" });
                        copyRowIsEnabled = false;
                    };
                    confirmationDialog.Show();
                }
                else
                {
                    dgCreditorGroupGrid.Readonly = true;
                    dgCreditorGroupGrid.tableView.CloseEditor();
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
                return editAllChecked ? false : dgCreditorGroupGrid.HasUnsavedData;
            }
        }

        private async void Save()
        {
            SetBusy();
            var err = await dgCreditorGroupGrid.SaveData();
            if (err != ErrorCodes.Succes)
                api.AllowBackgroundCrud = true;
            ClearBusy();
        }

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.CreditorGroupPage2)
                dgCreditorGroupGrid.UpdateItemSource(argument);
        }
        
        protected override void LoadCacheInBackGround()
        {
            LoadType(new Type[] { typeof(Uniconta.DataModel.GLAccount), typeof(Uniconta.DataModel.GLVat) });
        }

        protected override void OnLayoutCtrlLoaded()
        {
            var Comp = api.CompanyEntity;
            if (!Comp.InvDuty)
                this.ExemptDuty.ShowInColumnChooser = false;
            else
                this.ExemptDuty.ShowInColumnChooser = true;
        }
    }
}
