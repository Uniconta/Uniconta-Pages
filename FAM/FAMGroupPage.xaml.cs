using UnicontaClient.Models;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Uniconta.API.Service;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    /// <summary>
    /// Interaction logic for FAMPage.xaml
    /// </summary>
    public class FAMGroupGrid : CorasauDataGridClient
    {
        public override Type TableType
        {
            get { return typeof(FAMGroupClient); }
        }

        public override bool SingleBufferUpdate
        {
            get { return false; }
        }
    }

    public partial class FAMGroupPage : GridBasePage
    {
        public override string NameOfControl
        {
            get { return TabControls.FAMGroupPage.ToString(); }
        }

        public FAMGroupPage(BaseAPI API) : base(API, string.Empty)
        {
            InitPage();
        }

        public FAMGroupPage(BaseAPI api, string lookupKey) : base(api, lookupKey)
        {
            InitPage();
        }

        private void InitPage()
        {
            InitializeComponent();
            dgFAMGroupGrid.RowDoubleClick += dgFAMGroupGrid_RowDoubleClick;
            dgFAMGroupGrid.BusyIndicator = busyIndicator;
            dgFAMGroupGrid.api = api;
            SetRibbonControl(localMenu, dgFAMGroupGrid);

            localMenu.OnItemClicked += localMenu_OnItemClicked;

            //var Comp = api.CompanyEntity;
            //if (Comp.RoundTo100)
            //    CurBalance.HasDecimals = Overdue.HasDecimals = false;

            dgFAMGroupGrid.ShowTotalSummary();
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgFAMGroupGrid.SelectedItem as FAMGroupClient;
            switch (ActionType)
            {
                case "AddRow":
                    AddDockItem(TabControls.FAMGroupPage2, api, Uniconta.ClientTools.Localization.lookup("AssetGroup"), "Add_16x16");
                    break;
                case "EditRow":
                    if (selectedItem == null)
                        return;
                    object[] EditParam = new object[2];
                    EditParam[0] = selectedItem;
                    EditParam[1] = true;
                    AddDockItem(TabControls.FAMGroupPage2, EditParam, string.Format("{0}:{1}", Uniconta.ClientTools.Localization.lookup("AssetGroup"), selectedItem._Group));
                    break;
                case "CopyRow":
                    if (selectedItem == null)
                        return;
                    object[] copyParam = new object[2];
                    copyParam[0] = selectedItem;
                    copyParam[1] = false;
                    string header = string.Format(Uniconta.ClientTools.Localization.lookup("CopyOBJ"), selectedItem._Group);
                    AddDockItem(TabControls.FAMGroupPage2, copyParam, header);
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
            if (dgFAMGroupGrid.Readonly)
            {
                api.AllowBackgroundCrud = false;
                dgFAMGroupGrid.MakeEditable();
                UserFieldControl.MakeEditable(dgFAMGroupGrid);
                ibase.Caption = Uniconta.ClientTools.Localization.lookup("LeaveEditAll");
                ribbonControl.EnableButtons(new string[] { "AddLine", "CopyRow", "DeleteRow", "SaveGrid" });
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
                                var err = await dgFAMGroupGrid.SaveData();
                                if (err != 0)
                                {
                                    api.AllowBackgroundCrud = true;
                                    return;
                                }
                                break;
                            case CWConfirmationBox.ConfirmationResultEnum.No:
                                dgFAMGroupGrid.CancelChanges(); 
                                break;
                        }
                        editAllChecked = true;
                        dgFAMGroupGrid.Readonly = true;
                        dgFAMGroupGrid.tableView.CloseEditor();
                        ibase.Caption = Uniconta.ClientTools.Localization.lookup("EditAll");
                        ribbonControl.DisableButtons(new string[] { "AddLine", "CopyRow", "DeleteRow", "SaveGrid" });
                    };
                    confirmationDialog.Show();
                }
                else
                {
                    dgFAMGroupGrid.Readonly = true;
                    dgFAMGroupGrid.tableView.CloseEditor();
                    ibase.Caption = Uniconta.ClientTools.Localization.lookup("EditAll");
                    ribbonControl.DisableButtons(new string[] { "AddLine", "CopyRow", "DeleteRow", "SaveGrid" });

                }
            }
        }

        public override bool IsDataChaged
        {
            get { return editAllChecked ? false : dgFAMGroupGrid.HasUnsavedData; }
        }

        private async void Save()
        {
            SetBusy();
            var err = await dgFAMGroupGrid.SaveData();
            if (err == ErrorCodes.Succes)
                BindGrid();
            else
                api.AllowBackgroundCrud = true;
            ClearBusy();
        }

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.FAMGroupPage2)
                dgFAMGroupGrid.UpdateItemSource(argument);
        }

        private Task BindGrid()
        {
            return dgFAMGroupGrid.Filter(null);
        }

        private void dgFAMGroupGrid_RowDoubleClick()
        {
            ribbonControl.PerformRibbonAction("FAMTrans");
        }
    }
}
