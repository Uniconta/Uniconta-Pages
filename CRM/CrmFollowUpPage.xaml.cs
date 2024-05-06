using UnicontaClient.Models;
using UnicontaClient.Pages;
using UnicontaClient.Utilities;
using DevExpress.Xpf.Grid;
using System;
using System.Collections;
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
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;
using UnicontaClient.Pages;
using Uniconta.ClientTools.Controls;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class CrmFollowUpPageGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(CrmFollowUpClient); } }
        public override IComparer GridSorting { get { return new CrmFollowUpSort(); } }
        protected override bool RenderAllColumns { get { return true; } }
    }

    public partial class CrmFollowUpPage : GridBasePage
    {
        public override string NameOfControl
        {
            get { return TabControls.CrmFollowUpPage.ToString(); }
        }

        public CrmFollowUpPage(BaseAPI API)
            : base(API, string.Empty)
        {
            InitPage(null);
        }
        public CrmFollowUpPage(UnicontaBaseEntity master)
            : base(master)
        {
            InitPage(master);
        }

        public CrmFollowUpPage(SynchronizeEntity syncEntity) : base(syncEntity,true)
        {
            InitPage(syncEntity.Row);
            SetHeader();
        }

        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            dgCrmFollowUpGrid.UpdateMaster(args);
            SetHeader();
            InitQuery();
        }

        private void SetHeader()
        {
            SetHeader(GetHeaderName(dgCrmFollowUpGrid.masterRecord));
        }

        private void InitPage(UnicontaBaseEntity master)
        {
            InitializeComponent();
            var Comp = api.CompanyEntity;
            LayoutControl = crmDetailControl.layoutItems;
            dgCrmFollowUpGrid.UpdateMaster(master);
            ((TableView)dgCrmFollowUpGrid.View).RowStyle = Application.Current.Resources["StyleRow"] as Style;
            localMenu.dataGrid = dgCrmFollowUpGrid;
            dgCrmFollowUpGrid.api = api;
            dgCrmFollowUpGrid.BusyIndicator = busyIndicator;
            SetRibbonControl(localMenu, dgCrmFollowUpGrid);
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            if (master == null)
                LoadType(new Type[] { typeof(Uniconta.DataModel.CrmProspect), typeof(Uniconta.DataModel.Debtor) });

            DebtorCache = api.GetCache(typeof(Uniconta.DataModel.Debtor));
            CreditorCache = api.GetCache(typeof(Uniconta.DataModel.Creditor));
            CrmProspectCache = api.GetCache(typeof(Uniconta.DataModel.CrmProspect));
            ContactCache = api.GetCache(typeof(Uniconta.DataModel.Contact));
            ProjectCache = api.GetCache(typeof(Uniconta.DataModel.Project));

            ribbonControl.DisableButtons(new string[] { "AddLine", "CopyRow", "DeleteRow", "UndoDelete", "SaveGrid" });
            dgCrmFollowUpGrid.View.DataControl.CurrentItemChanged += DataControl_CurrentItemChanged;
            dgCrmFollowUpGrid.ShowTotalSummary();
        }

        protected override void OnLayoutCtrlLoaded()
        {
            crmDetailControl.api = api;
        }

        Filter defaultFilter;
        protected override Filter[] DefaultFilters()
        {
            if (defaultFilter!= null)
                return new Filter[] { defaultFilter };
            else
                return null;
        }

        public override void SetParameter(IEnumerable<ValuePair> Parameters)
        {
            string employee = null;
            foreach (var rec in Parameters)
            {
                if (string.Compare(rec.Name, "Employee", StringComparison.CurrentCultureIgnoreCase) == 0)
                    employee = rec.Value;
            }
            if (employee != null)
            {
                defaultFilter= new Filter();
                defaultFilter.name = "Employee";
                defaultFilter.value = employee;
                SetHeader();
            }

            base.SetParameter(Parameters);
        }

        void DataControl_CurrentItemChanged(object sender, DevExpress.Xpf.Grid.CurrentItemChangedEventArgs e)
        {
            var oldselectedItem = e.OldItem as CrmFollowUpClient;
            if (oldselectedItem != null)
                oldselectedItem.PropertyChanged -= CrmFollowUp_PropertyChanged;
            var selectedItem = e.NewItem as CrmFollowUpClient;
            if (selectedItem != null)
                selectedItem.PropertyChanged += CrmFollowUp_PropertyChanged;
        }

        private void CrmFollowUp_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var rec = sender as CrmFollowUpClient;
            switch (e.PropertyName)
            {
                case "DCType":
                    SetAccountSource(rec);
                break;
            }
        }

        SQLCache DebtorCache, CreditorCache, CrmProspectCache, ContactCache, ProjectCache;
        protected override async System.Threading.Tasks.Task LoadCacheInBackGroundAsync()
        {
            var api = this.api;
            if (DebtorCache == null)
                DebtorCache = api.GetCache(typeof(Uniconta.DataModel.Debtor)) ?? await api.LoadCache(typeof(Uniconta.DataModel.Debtor)).ConfigureAwait(false);
            if (CreditorCache == null)
                CreditorCache = api.GetCache(typeof(Uniconta.DataModel.Creditor)) ?? await api.LoadCache(typeof(Uniconta.DataModel.Creditor)).ConfigureAwait(false);
            if (CrmProspectCache == null)
                CrmProspectCache = api.GetCache(typeof(Uniconta.DataModel.CrmProspect)) ?? await api.LoadCache(typeof(Uniconta.DataModel.CrmProspect)).ConfigureAwait(false);
            if (ContactCache == null)
                ContactCache = api.GetCache(typeof(Uniconta.DataModel.Contact)) ?? await api.LoadCache(typeof(Uniconta.DataModel.Contact)).ConfigureAwait(false);
            if (ProjectCache == null)
                ProjectCache = api.GetCache(typeof(Uniconta.DataModel.Project)) ?? await api.LoadCache(typeof(Uniconta.DataModel.Project)).ConfigureAwait(false);
        }

        private void SetAccountSource(CrmFollowUpClient record)
        {
            SQLCache cache;
            if (record != null)
            {
                switch (record._DCType)
                {
                    case CrmCampaignMemberType.Debtor: cache = DebtorCache; break;
                    case CrmCampaignMemberType.Creditor: cache = CreditorCache; break;
                    case CrmCampaignMemberType.Prospect: cache = CrmProspectCache; break;
                    case CrmCampaignMemberType.Contact: cache = ContactCache; break;
                    case CrmCampaignMemberType.Project: cache = ProjectCache; break;
                    default: cache = null; break;
                }
                record.accntSource = cache;
                record.NotifyPropertyChanged("AccountSource");
                record.NotifyPropertyChanged("DCAccount");
            }
        }

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.CrmFollowUpPage2)
                dgCrmFollowUpGrid.UpdateItemSource(argument);
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            bool showFields = (dgCrmFollowUpGrid.masterRecords == null);
            DCAccount.Visible = showFields;
            DCType.Visible = showFields;
            Name.Visible = showFields;
        }

        string GetHeaderName(UnicontaBaseEntity masterRecord)
        {
            string lookupStr;
            if (masterRecord is CrmProspectClient)
                lookupStr = "Prospects";
            else if (masterRecord is DebtorClient)
                lookupStr = "Debtors";
            else if (masterRecord is CrmCampaignMemberClient)
                lookupStr = "EmailList";
            else if (masterRecord is CrmCampaignClient)
                lookupStr = "Campaign";
            else if (masterRecord is ContactClient)
                lookupStr = "Contacts";
            else if (masterRecord is CreditorClient)
                lookupStr = "Creditors";
            else if (masterRecord is DebtorOfferClient)
                lookupStr = "Offers";
            else if (masterRecord is ProjectClient)
                lookupStr = "Projects";
            else
                return Uniconta.ClientTools.Localization.lookup("FollowUp");
            return string.Format("{0}:{2} {1}", Uniconta.ClientTools.Localization.lookup("FollowUp"), Utility.GetHeaderString(masterRecord), Uniconta.ClientTools.Localization.lookup(lookupStr));
        }

        void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgCrmFollowUpGrid.SelectedItem as CrmFollowUpClient;

            switch (ActionType)
            {
                case "EditAll":
                    if (dgCrmFollowUpGrid.Visibility == Visibility.Visible)
                        EditAll();
                    break;
                case "AddRow":
                    var newItem = dgCrmFollowUpGrid.GetChildInstance();
                    object[] param = new object[3];
                    param[0] = newItem;
                    param[1] = false;
                    param[2] = dgCrmFollowUpGrid.masterRecord;
                    AddDockItem(TabControls.CrmFollowUpPage2, param, Uniconta.ClientTools.Localization.lookup("FollowUp"), "Add_16x16");
                    break;
                case "EditRow":
                    if (selectedItem == null)
                        return;
                    object[] para = new object[3];
                    para[0] = selectedItem;
                    para[1] = true;
                    para[2] = dgCrmFollowUpGrid.masterRecord;
                    AddDockItem(TabControls.CrmFollowUpPage2, para, selectedItem.UserName, null, true);
                    break;
                case "AddNote":
                    if (selectedItem != null)
                        AddDockItem(TabControls.UserNotesPage, dgCrmFollowUpGrid.syncEntity);
                    break;
                case "AddDoc":
                    if (selectedItem != null)
                        AddDockItem(TabControls.UserDocsPage, dgCrmFollowUpGrid.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Documents"), selectedItem.PrimaryKeyId));
                    break;
                case "OtherFollupups":
                    if (selectedItem != null)
                        OpenCrmFollowUp(selectedItem);
                    break;
                case "AddLine":
                    dgCrmFollowUpGrid.AddRow();
                    break;
                case "CopyRow":
                    dgCrmFollowUpGrid.CopyRow();
                    break;
                case "DeleteRow":
                    dgCrmFollowUpGrid.DeleteRow();
                    break;
                case "SaveGrid":
                    saveGrid();
                    break;
                case "CopyRecord":
                    if (selectedItem != null)
                        CopyRecord(selectedItem);
                    break;
                case "CloseActivity":
                    if (selectedItem != null)
                    {
                        selectedItem._Ended = BasePage.GetSystemDefaultDate();
                        api.Update(selectedItem);
                        dgCrmFollowUpGrid.UpdateItemSource(2, selectedItem);
                    }
                    break;
                case "Contacts":
                    if (selectedItem != null)
                        AddDockItem(TabControls.ContactPage, dgCrmFollowUpGrid.syncEntity);
                    break;
                case "UndoDelete":
                    dgCrmFollowUpGrid.UndoDeleteRow();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        void CopyRecord(CrmFollowUpClient selectedItem)
        {
            var followUp = new CrmFollowUpClient();
            CorasauDataGrid.CopyAndClearRowId(selectedItem, followUp);
            followUp.dc = selectedItem.dc;
            followUp.pros = selectedItem.pros;
            followUp.cont = selectedItem.cont;
            followUp._Created = DateTime.MinValue;
            followUp._Ended = DateTime.MinValue;
            if (followUp._Action == Uniconta.DataModel.FollowUpAction.Lost)
                followUp._Action = 0;
            var parms = new object[3] { followUp, false, dgCrmFollowUpGrid.masterRecord };
            AddDockItem(TabControls.CrmFollowUpPage2, parms, Uniconta.ClientTools.Localization.lookup("FollowUp"), "Add_16x16");
        }

        bool editAllChecked;
        private void EditAll()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            var ibase = UtilDisplay.GetMenuCommandByName(rb, "EditAll");
            if (ibase == null)
                return;
            if (dgCrmFollowUpGrid.Readonly)
            {
                api.AllowBackgroundCrud = false;
                dgCrmFollowUpGrid.MakeEditable();
                UserFieldControl.MakeEditable(dgCrmFollowUpGrid);
                ibase.Caption = Uniconta.ClientTools.Localization.lookup("LeaveEditAll");
                ribbonControl.EnableButtons(new string[] { "AddLine", "CopyRow", "DeleteRow", "UndoDelete", "SaveGrid" });
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
                                var err = await dgCrmFollowUpGrid.SaveData();
                                if (err != 0)
                                {
                                    api.AllowBackgroundCrud = true;
                                    return;
                                }
                                break;
                            case CWConfirmationBox.ConfirmationResultEnum.No:
                                dgCrmFollowUpGrid.CancelChanges(); 
                                break;
                        }
                        editAllChecked = true;
                        dgCrmFollowUpGrid.Readonly = true;
                        dgCrmFollowUpGrid.tableView.CloseEditor();
                        ibase.Caption = Uniconta.ClientTools.Localization.lookup("EditAll");
                        ribbonControl.DisableButtons(new string[] { "AddLine", "CopyRow", "DeleteRow", "UndoDelete", "SaveGrid" });
                    };
                    confirmationDialog.Show();
                }
                else
                {
                    dgCrmFollowUpGrid.Readonly = true;
                    dgCrmFollowUpGrid.tableView.CloseEditor();
                    ibase.Caption = Uniconta.ClientTools.Localization.lookup("EditAll");
                    ribbonControl.DisableButtons(new string[] { "AddLine", "CopyRow", "DeleteRow", "UndoDelete", "SaveGrid" });
                }
            }

        }
        public override bool IsDataChaged
        {
            get
            {
                return editAllChecked ? false : dgCrmFollowUpGrid.HasUnsavedData;
            }
        }

        CorasauGridLookupEditorClient prevDCAccount;
        private void DCAccount_GotFocus(object sender, RoutedEventArgs e)
        {
            CrmFollowUpClient selectedItem = dgCrmFollowUpGrid.SelectedItem as CrmFollowUpClient;
            if (selectedItem != null)
            {
                SetAccountSource(selectedItem);
                if (prevDCAccount != null)
                    prevDCAccount.isValidate = false;
                var editor = (CorasauGridLookupEditorClient)sender;
                prevDCAccount = editor;
                editor.isValidate = true;
            }
        }

        void OpenCrmFollowUp(CrmFollowUpClient selectedItem)
        {
            var dcType = selectedItem._DCType;
            switch (dcType)
            {
                case CrmCampaignMemberType.Debtor:
                    if (selectedItem.Debtor == null) return;
                    AddDockItem(TabControls.CrmFollowUpPage, selectedItem, GetHeaderName(selectedItem.Debtor));
                    break;
                case CrmCampaignMemberType.Creditor:
                    if (selectedItem.Creditor == null) return;
                    AddDockItem(TabControls.CrmFollowUpPage, selectedItem, GetHeaderName(selectedItem.Creditor));
                    break;
                case CrmCampaignMemberType.Prospect:
                    if (selectedItem.Prospect == null) return;
                    AddDockItem(TabControls.CrmFollowUpPage, selectedItem, GetHeaderName(selectedItem.Prospect));
                    break;
                case CrmCampaignMemberType.Contact:
                    var header = string.Format("{0}:{2} {1}", Uniconta.ClientTools.Localization.lookup("FollowUp"), selectedItem._Account, Uniconta.ClientTools.Localization.lookup("Contact"));
                    AddDockItem(TabControls.CrmFollowUpPage, selectedItem, header);
                    break;
                case CrmCampaignMemberType.Project:
                    if (selectedItem.Project == null) return;
                    AddDockItem(TabControls.CrmFollowUpPage, selectedItem, GetHeaderName(selectedItem.Project));
                    break;
            }
        }

        public override bool CheckIfBindWithUserfield(out bool isReadOnly, out bool useBinding)
        {
            isReadOnly = useBinding = false;
            return true;
        }

        protected override LookUpTable HandleLookupOnLocalPage(LookUpTable lookup, CorasauDataGrid dg)
        {
            var followup = dg.SelectedItem as CrmFollowUpClient;
            if (followup == null)
                return lookup;
            if (dg.CurrentColumn?.Name == "DCAccount")
            {
                switch (followup._DCType)
                {
                    case CrmCampaignMemberType.Contact:
                        lookup.TableType = typeof(Uniconta.DataModel.Contact);
                        break;
                    case CrmCampaignMemberType.Creditor:
                        lookup.TableType = typeof(Uniconta.DataModel.Creditor);
                        break;
                    case CrmCampaignMemberType.Debtor:
                        lookup.TableType = typeof(Uniconta.DataModel.Debtor);
                        break;
                    case CrmCampaignMemberType.Prospect:
                        lookup.TableType = typeof(Uniconta.DataModel.CrmProspect);
                        break;
                    case CrmCampaignMemberType.Project:
                        lookup.TableType = typeof(Uniconta.DataModel.Project);
                        break;
                }
            }
            return lookup;
        }

#if !SILVERLIGHT
        private void HasEmailImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var crmFollowUp = (sender as TextBlock).Tag as CrmFollowUpClient;
            if (crmFollowUp != null)
            {
                var mail = string.Concat("mailto:", crmFollowUp.ContactEmail);
                System.Diagnostics.Process proc = new System.Diagnostics.Process();
                proc.StartInfo.FileName = mail;
                proc.Start();
            }
        }

        private void HasWebsite_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var crmFollowUp = (sender as TextBlock).Tag as CrmFollowUpClient;
            Utility.OpenWebSite(crmFollowUp.Www);
        }
#endif
    }
}
