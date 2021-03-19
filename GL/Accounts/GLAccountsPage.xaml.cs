using Uniconta.API.System;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Util;
using System.Collections.ObjectModel;
using DevExpress.Xpf.Grid.LookUp;
using DevExpress.Xpf.Editors.Settings;
using UnicontaClient.Models;
using UnicontaClient.Utilities;
using System.Threading.Tasks;
using System.Collections;
using DevExpress.Xpf.Grid;
using Uniconta.API.Service;
using DevExpress.Xpf.Core;
using DevExpress.Data.Filtering;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class CorasauDataGridGLAccount : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(GLAccountClient); }  }
        public override bool SingleBufferUpdate { get { return false; } }
    }

    public partial class GLAccountsPage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.GLAccountsPage; } }
        public GLAccountsPage(BaseAPI api)
            : base(api, string.Empty)
        {
            InitPage();
        }
       
        public GLAccountsPage(BaseAPI api, string lookupKey)
            : base(api, lookupKey)
        {
            InitPage();
        }
        public GLAccountsPage(UnicontaBaseEntity param)
            : base(null)
        {
            InitPage();
            dgGLTable.SetSource(new UnicontaBaseEntity[] { param });
        }

        private void InitPage()
        {
            InitializeComponent();
            dgGLTable.RowDoubleClick += dgGLTable_RowDoubleClick;
            this.DataContext = this;
            SetRibbonControl(localMenu, dgGLTable);
            dgGLTable.api = api;
            var Comp = api.CompanyEntity;
            if (Comp != null && !Comp.HasDecimals)
                PrevYear.HasDecimals = PrevYearDebit.HasDecimals = PrevYearCredit.HasDecimals = 
                ThisYear.HasDecimals = ThisYearDebit.HasDecimals = ThisYearCredit.HasDecimals = false;
            localMenu.OnItemClicked += localMenu_OnItemClicked;

            if (Comp._CountryId != CountryCode.Germany)
                DATEVAuto.ShowInColumnChooser = false;

            dgGLTable.BusyIndicator = busyIndicator;
            ribbonControl.DisableButtons(new string[] { "AddRow", "CopyRow", "DeleteRow", "UndoDelete" ,"SaveGrid" });
            ((TableView)dgGLTable.View).RowStyle = Application.Current.Resources["RowStyle"] as Style;
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            setDim();
            dgGLTable.Readonly = true;
        }

        void dgGLTable_RowDoubleClick()
        {
            localMenu_OnItemClicked("GLTran");
        }

        void localMenu_OnItemClicked(string ActionType)
        {
            GLAccountClient selectedItem = dgGLTable.SelectedItem as GLAccountClient;
            switch (ActionType)
            {
                case "EditAll":
                    if (dgGLTable.Visibility == Visibility.Visible)
                        EditAll();
                    break;
                case "AddItem":
                    AddDockItem(TabControls.GLAccountPage2, api, Uniconta.ClientTools.Localization.lookup("Accounts"), "Add_16x16.png");
                    break;
                case "EditItem":
                    if (selectedItem == null)
                        return;
                    var param = new object[2] { selectedItem, true };
                    AddDockItem(TabControls.GLAccountPage2, param, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Accounts"), selectedItem.Account));
                    break;
                case "AddRow":
                    if (dgGLTable.Readonly)
                        AddDockItem(TabControls.GLAccountPage2, api, Uniconta.ClientTools.Localization.lookup("Accounts"), "Add_16x16.png");
                    else
                        dgGLTable.AddRow();
                    break;
                case "CopyRow":
                    if (copyRowIsEnabled)
                        dgGLTable.CopyRow();
                    else
                        CopyRecord(selectedItem);
                    break;
                case "DeleteRow":
                    dgGLTable.DeleteRow();
                    break;
                case "SaveGrid":
                    dgGLTable.SaveData();
                    break;
                case "AllocationAccounts":
                    if (selectedItem != null)
                        AddDockItem(TabControls.GLAllocationAccounts, dgGLTable.syncEntity);
                    break;
                case "GLTran":
                    if (selectedItem != null)
                        AddDockItem(TabControls.TransactionReport, dgGLTable.syncEntity);
                    break;
                case "Trans":
                    if (selectedItem != null)
                        AddDockItem(TabControls.AccountsTransaction, dgGLTable.syncEntity, string.Format("{0} ({1})", Uniconta.ClientTools.Localization.lookup("AccountsTransaction"), selectedItem._Name));
                    break;
                case "Budget":
                    if (selectedItem != null)
                        AddDockItem(TabControls.GLBudgetLinePage, dgGLTable.syncEntity);
                    break;
                case "AddNote":
                    if (selectedItem != null)
                        AddDockItem(TabControls.UserNotesPage, dgGLTable.syncEntity);
                    break;
                case "AddDoc":
                    if (selectedItem != null)
                        AddDockItem(TabControls.UserDocsPage, dgGLTable.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Documents"), selectedItem._Name));
                    break;
                case "VatOnClient":
                    if (selectedItem != null)
                        AddDockItem(TabControls.VatOnClientsReport, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("VATprDC"), selectedItem._Name));
                    break;
                case "GLTransSum":
                    if (selectedItem != null)
                        AddDockItem(TabControls.GLTransSumClientPage, dgGLTable.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("AccountTotals"), selectedItem._Account));
                    break;
                case "CopyRecord":
                    CopyRecord(selectedItem);
                    break;
                case "UndoDelete":
                    dgGLTable.UndoDeleteRow();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        void CopyRecord(GLAccountClient selectedItem)
        {
            if (selectedItem == null)
                return;
            var glAccount = Activator.CreateInstance(selectedItem.GetType()) as GLAccountClient;
            CorasauDataGrid.CopyAndClearRowId(selectedItem, glAccount);
            var parms = new object[2] { glAccount, false };
            AddDockItem(TabControls.GLAccountPage2, parms, Uniconta.ClientTools.Localization.lookup("Accounts"), "Add_16x16.png");
        }

        bool copyRowIsEnabled = false;
        bool editAllChecked;
        private void EditAll()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            var ibase = UtilDisplay.GetMenuCommandByName(rb, "EditAll");
            if (ibase == null)
                return;
            if (dgGLTable.Readonly)
            {
                api.AllowBackgroundCrud = false;
                dgGLTable.MakeEditable();
                ibase.Caption = Uniconta.ClientTools.Localization.lookup("LeaveEditAll");
                ribbonControl.EnableButtons(new string[] { "AddRow", "CopyRow", "DeleteRow", "UndoDelete", "SaveGrid" });
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
                                var err = await dgGLTable.SaveData();
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
                        dgGLTable.Readonly = true;
                        dgGLTable.tableView.CloseEditor();
                        ibase.Caption = Uniconta.ClientTools.Localization.lookup("EditAll");
                        ribbonControl.DisableButtons(new string[] { "AddRow", "CopyRow", "DeleteRow", "UndoDelete", "SaveGrid" });
                        copyRowIsEnabled = false;
                    };
                    confirmationDialog.Show();
                }
                else
                {
                    dgGLTable.Readonly = true;
                    dgGLTable.tableView.CloseEditor();
                    ibase.Caption = Uniconta.ClientTools.Localization.lookup("EditAll");
                    ribbonControl.DisableButtons(new string[] { "AddRow", "CopyRow", "DeleteRow", "UndoDelete", "SaveGrid" });
                    copyRowIsEnabled = false;
                }
            }
        }
        public override bool IsDataChaged
        {
            get
            {
                return editAllChecked ? false : dgGLTable.HasUnsavedData;
            }
        }
        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.GLAccountPage2)
                dgGLTable.UpdateItemSource(argument);
            if (screenName == TabControls.UserNotesPage || screenName == TabControls.UserDocsPage && argument != null)
                dgGLTable.UpdateItemSource(argument);
        }

        void setDim()
        {
            UnicontaClient.Utilities.Utility.SetDimensionsGrid(api, cldim1, cldim2, cldim3, cldim4, cldim5);
            UnicontaClient.Utilities.Utility.SetDimensionsGrid(api, clNewDim1, clNewDim2, clNewDim3, clNewDim4, clNewDim5);
        }

        private void HasDocImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var glAccount = (sender as Image).Tag as GLAccountClient;
            if(glAccount!=null)
                AddDockItem(TabControls.UserDocsPage, dgGLTable.syncEntity);
        }

        private void HasNoteImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var glAccount = (sender as Image).Tag as GLAccountClient;
            if (glAccount != null)
                AddDockItem(TabControls.UserNotesPage, dgGLTable.syncEntity);
        }
    }
}
