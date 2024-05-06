using UnicontaClient.Models;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Input;
using Uniconta.DataModel;
using System.Collections;
using Uniconta.ClientTools.Controls;
using Uniconta.API.Service;
using System.Windows;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class ContactGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(ContactClient); } }
        public override IComparer GridSorting { get { return new ContactClientSort(); } }
    }
    public partial class ContactPage : GridBasePage
    {
        SQLCache CrmProspectCache, DebtorCache, CreditorCache, interestCache, productsCache;
        bool hasPageMaster;

        public override string NameOfControl
        {
            get { return TabControls.ContactPage.ToString(); }
        }
        public ContactPage(BaseAPI API)
            : base(API, string.Empty)
        {
            Init(null);
        }
        public ContactPage(UnicontaBaseEntity master)
            : base(master)
        {
            Init(master);
        }

        public ContactPage(SynchronizeEntity syncEntity)
            : base(syncEntity, true)
        {
            this.syncEntity = syncEntity;
            Init(syncEntity.Row);
            SetHeader();
        }

        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            dgContactGrid.UpdateMaster(args);
            SetHeader();
            InitQuery();
        }
        void SetHeader()
        {
            string header;
            var syncMaster = dgContactGrid.masterRecord as DCAccount;
            if (syncMaster != null)
                header = string.Format("{0}: {1}, {2}", Uniconta.ClientTools.Localization.lookup("Contacts"), syncMaster._Account, syncMaster._Name);
            else
            {
                var syncMaster2 = dgContactGrid.masterRecord as CrmProspect;
                if (syncMaster2 != null)
                    header = string.Concat(Uniconta.ClientTools.Localization.lookup("Contacts"), ": ", syncMaster2._Name);
                else
                    return;
            }
            SetHeader(header);
        }
        private void Init(UnicontaBaseEntity master)
        {
            InitializeComponent();
            if (master != null)
            {
                hasPageMaster = true;
                dgContactGrid.UpdateMaster(master);
            }
            localMenu.dataGrid = dgContactGrid;
            dgContactGrid.api = api;
            dgContactGrid.BusyIndicator = busyIndicator;
            SetRibbonControl(localMenu, dgContactGrid);
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            var load = new List<Type>();
            if (api.CompanyEntity.CRM)
            {
                CrmProspectCache = api.GetCache(typeof(Uniconta.DataModel.CrmProspect));
                load.Add(typeof(Uniconta.DataModel.CrmInterest));
                load.Add(typeof(Uniconta.DataModel.CrmProduct));
                if (master == null)
                    load.Add(typeof(Uniconta.DataModel.CrmProspect));
            }
            else
            {
                RibbonBase rb = (RibbonBase)localMenu.DataContext;
                UtilDisplay.RemoveMenuCommand(rb, "FollowUp");
            }
            if (master == null)
            {
                load.Add(typeof(Uniconta.DataModel.Debtor));
                load.Add(typeof(Uniconta.DataModel.Creditor));
            }
            if (load.Count != 0)
                LoadType(load.ToArray());

            DebtorCache = api.GetCache(typeof(Uniconta.DataModel.Debtor));
            CreditorCache = api.GetCache(typeof(Uniconta.DataModel.Creditor));

            dgContactGrid.SelectedItemChanged += DgContactGrid_SelectedItemChanged;
            dgContactGrid.View.DataControl.CurrentItemChanged += DataControl_CurrentItemChanged;
            ribbonControl.DisableButtons(new string[] { "AddLine", "CopyRow", "DeleteRow", "UndoDelete", "SaveGrid" });

            Interests.Visible = Interests.ShowInColumnChooser = api.CompanyEntity.CRM;
            Products.Visible = Products.ShowInColumnChooser = api.CompanyEntity.CRM;

            this.PreviewKeyDown += RootVisual_KeyDown;
            this.BeforeClose += ContactPage_BeforeClose;
        }

        public override void SetParameter(IEnumerable<ValuePair> Parameters)
        {
            foreach (var rec in Parameters)
            {
                if (rec.Name == null || rec.Name == "Master")
                {
                    DCAccount master;
                    if (rec.Value == "Debtor")
                        master = new Uniconta.DataModel.Debtor();
                    else if (rec.Value == "Creditor")
                        master = new Uniconta.DataModel.Creditor();
                    else
                        continue;

                    master.SetMaster(api.CompanyEntity);
                    dgContactGrid.UpdateMaster(master as UnicontaBaseEntity);
                    var header = string.Concat(Uniconta.ClientTools.Localization.lookup("Contacts"), ": ", Uniconta.ClientTools.Localization.lookup(master.GetType().Name));
                    SetHeader(header);
                }
            }
            base.SetParameter(Parameters);
        }

        private void DataControl_CurrentItemChanged(object sender, DevExpress.Xpf.Grid.CurrentItemChangedEventArgs e)
        {
            ContactClient oldSelectedItem = e.OldItem as ContactClient;
            if (oldSelectedItem != null)
            {
                oldSelectedItem.PropertyChanged -= ContactClient_PropertyChanged;
                var cache = GetCache(oldSelectedItem._DCType);
                if (cache != null && cache.Count > 1000)
                {
                    oldSelectedItem.accntSource = null;
                    oldSelectedItem.NotifyPropertyChanged("AccountSource");
                }
            }
            ContactClient selectedItem = e.NewItem as ContactClient;
            if (selectedItem != null)
                selectedItem.PropertyChanged += ContactClient_PropertyChanged;
        }

        private void ContactClient_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var rec = sender as ContactClient;
            switch (e.PropertyName)
            {
                case "DCType":
                    SetAccountSource(rec);
                    break;
            }
        }

        private void ContactPage_BeforeClose()
        {
            this.PreviewKeyDown -= RootVisual_KeyDown;
        }

        private void RootVisual_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.F6 && (dgContactGrid.CurrentColumn == DCType || dgContactGrid.CurrentColumn == DCAccount))
            {
                var currentRow = dgContactGrid.SelectedItem as ContactClient;
                if (currentRow != null)
                {
                    var lookupTable = new LookUpTable();
                    lookupTable.api = this.api;
                    lookupTable.KeyStr = Convert.ToString(currentRow._DCAccount);
                    if (currentRow._DCType == 1)
                    {
                        lookupTable.TableType = typeof(Uniconta.DataModel.Debtor);
                        this.LookUpTable(lookupTable, Uniconta.ClientTools.Localization.lookup("Lookup"), TabControls.DebtorAccount);
                    }
                    if (currentRow._DCType == 2)
                    {
                        lookupTable.TableType = typeof(Uniconta.DataModel.Creditor);
                        this.LookUpTable(lookupTable, Uniconta.ClientTools.Localization.lookup("Lookup"), TabControls.CreditorAccount);
                    }
                    if (currentRow._DCType == 3)
                    {
                        lookupTable.TableType = typeof(Uniconta.DataModel.CrmProspect);
                        this.LookUpTable(lookupTable, Uniconta.ClientTools.Localization.lookup("Lookup"), TabControls.CrmProspectPage);
                    }
                }

            }
        }

        private void DgContactGrid_SelectedItemChanged(object sender, DevExpress.Xpf.Grid.SelectedItemChangedEventArgs e)
        {
            var selectedItem = dgContactGrid.SelectedItem as ContactClient;
            if (selectedItem == null)
                return;
            var ribbonControl = this.ribbonControl;
            if (ribbonControl == null)
                return;
            if (selectedItem._DCType == 2)
            {
                ribbonControl.EnableButtons("CreditorOrders");
                ribbonControl.DisableButtons("DebtorOrders");
                ribbonControl.DisableButtons("Offers");
            }
            else if (selectedItem._DCType == 1)
            {
                ribbonControl.DisableButtons("CreditorOrders");
                ribbonControl.EnableButtons("DebtorOrders");
                ribbonControl.DisableButtons("Offers");
            }
            else if (selectedItem._DCType == 3)
            {
                ribbonControl.DisableButtons("CreditorOrders");
                ribbonControl.DisableButtons("DebtorOrders");
                ribbonControl.EnableButtons("Offers");
            }
            else
            {
                ribbonControl.DisableButtons("CreditorOrders");
                ribbonControl.DisableButtons("DebtorOrders");
                ribbonControl.DisableButtons("Offers");
            }
        }

        protected override void OnLayoutLoaded()
        {
            if (hasPageMaster)
            {
                AccountName.Visible = DCType.Visible = DCAccount.Visible = false;
                AccountName.ShowInColumnChooser = DCType.ShowInColumnChooser = DCAccount.ShowInColumnChooser = false;
            }

            if (!api.CompanyEntity.CRM)
            {
                Interests.Visible = false;
                Products.Visible = false;
            }
            base.OnLayoutLoaded();
        }

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.ContactPage2)
                dgContactGrid.UpdateItemSource(argument);
            if (screenName == TabControls.UserNotesPage || screenName == TabControls.UserDocsPage && argument != null)
                dgContactGrid.UpdateItemSource(argument);
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgContactGrid.SelectedItem as ContactClient;
            switch (ActionType)
            {
                case "AddRow":
                    var newItem = dgContactGrid.GetChildInstance();
                    object[] param = new object[3];
                    param[0] = newItem;
                    param[1] = false;
                    param[2] = dgContactGrid.masterRecord;
                    AddDockItem(TabControls.ContactPage2, param, Uniconta.ClientTools.Localization.lookup("Contacts"), "Add_16x16");
                    break;
                case "EditRow":
                    if (selectedItem == null)
                        return;
                    object[] para = new object[3];
                    para[0] = selectedItem;
                    para[1] = true;
                    para[2] = dgContactGrid.masterRecord;
                    AddDockItem(TabControls.ContactPage2, para, selectedItem.Name, null, true);
                    break;
                case "FollowUp":
                    if (selectedItem != null)
                    {
                        var header = string.Format("{0}:{2} {1}", Uniconta.ClientTools.Localization.lookup("FollowUp"), selectedItem._Name, Uniconta.ClientTools.Localization.lookup("Contact"));
                        AddDockItem(TabControls.CrmFollowUpPage, dgContactGrid.syncEntity, header);
                    }
                    break;
                case "AddNote":
                    if (selectedItem != null)
                        AddDockItem(TabControls.UserNotesPage, dgContactGrid.syncEntity);
                    break;
                case "AddDoc":
                    if (selectedItem != null)
                        AddDockItem(TabControls.UserDocsPage, dgContactGrid.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Documents"), selectedItem._Name));
                    break;
                case "DebtorOrders":
                    if (selectedItem != null)
                        AddDockItem(TabControls.DebtorOrders, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("DebtorOrders"), selectedItem._Name));
                    break;
                case "Offers":
                    if (selectedItem != null)
                        AddDockItem(TabControls.DebtorOffers, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Offers"), selectedItem._Name));
                    break;
                case "CreditorOrders":
                    if (selectedItem != null)
                        AddDockItem(TabControls.CreditorOrders, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("CreditorOrders"), selectedItem._Name));
                    break;
                case "EditAll":
                    if (dgContactGrid.Visibility == Visibility.Visible)
                        EditAll();
                    break;
                case "AddLine":
                    dgContactGrid.AddRow();
                    break;
                case "CopyRow":
                    if (copyRowIsEnabled)
                        dgContactGrid.CopyRow();
                    break;
                case "DeleteRow":
                    dgContactGrid.DeleteRow();
                    break;
                case "UndoDelete":
                    dgContactGrid.UndoDeleteRow();
                    break;
                case "SaveGrid":
                    Save();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        async protected override void LoadCacheInBackGround()
        {
            var api = this.api;
            if (DebtorCache == null)
                DebtorCache = api.GetCache(typeof(Debtor)) ?? await api.LoadCache(typeof(Debtor)).ConfigureAwait(false);
            if (CreditorCache == null)
                CreditorCache = api.GetCache(typeof(Uniconta.DataModel.Creditor)) ?? await api.LoadCache(typeof(Uniconta.DataModel.Creditor)).ConfigureAwait(false);
            if (CrmProspectCache == null && api.CompanyEntity.CRM)
                CrmProspectCache = api.GetCache(typeof(CrmProspect)) ?? await api.LoadCache(typeof(CrmProspect)).ConfigureAwait(false);

            if (api.CompanyEntity.CRM)
            {
                interestCache = api.CompanyEntity.GetCache(typeof(Uniconta.DataModel.CrmInterest)) ?? await api.CompanyEntity.LoadCache(typeof(Uniconta.DataModel.CrmInterest), api).ConfigureAwait(false);
                productsCache = api.GetCache(typeof(Uniconta.DataModel.CrmProduct)) ?? await api.CompanyEntity.LoadCache(typeof(Uniconta.DataModel.CrmProduct), api).ConfigureAwait(false);
            }
        }
        private async void Save()
        {
            SetBusy();
            var err = await dgContactGrid.SaveData();
            if (err != ErrorCodes.Succes)
                api.AllowBackgroundCrud = true;
            ClearBusy();
        }

        bool copyRowIsEnabled = false;
        bool editAllChecked;
        private void EditAll()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            var ibase = UtilDisplay.GetMenuCommandByName(rb, "EditAll");
            if (ibase == null)
                return;
            if (dgContactGrid.Readonly)
            {
                api.AllowBackgroundCrud = false;
                dgContactGrid.MakeEditable();
                UserFieldControl.MakeEditable(dgContactGrid);
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
                                var err = await dgContactGrid.SaveData();
                                if (err != 0)
                                {
                                    api.AllowBackgroundCrud = true;
                                    return;
                                }
                                break;
                            case CWConfirmationBox.ConfirmationResultEnum.No:
                                dgContactGrid.CancelChanges();
                                break;
                        }
                        editAllChecked = true;
                        dgContactGrid.Readonly = true;
                        dgContactGrid.tableView.CloseEditor();
                        ibase.Caption = Uniconta.ClientTools.Localization.lookup("EditAll");
                        ribbonControl.DisableButtons(new string[] { "AddLine", "CopyRow", "DeleteRow", "UndoDelete", "SaveGrid" });
                        copyRowIsEnabled = false;
                    };
                    confirmationDialog.Show();
                }
                else
                {
                    dgContactGrid.Readonly = true;
                    dgContactGrid.tableView.CloseEditor();
                    ibase.Caption = Uniconta.ClientTools.Localization.lookup("EditAll");
                    ribbonControl.DisableButtons(new string[] { "AddLine", "CopyRow", "DeleteRow", "UndoDelete", "SaveGrid" });
                    copyRowIsEnabled = false;
                }
            }
        }

        public override bool IsDataChaged { get { return editAllChecked ? false : dgContactGrid.HasUnsavedData; } }
        public override bool CheckIfBindWithUserfield(out bool isReadOnly, out bool useBinding)
        {
            isReadOnly = true;
            useBinding = true;
            return true;
        }

        private void HasDocImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var contactClient = (sender as Image).Tag as ContactClient;
            if (contactClient != null)
                AddDockItem(TabControls.UserDocsPage, dgContactGrid.syncEntity);
        }

        private void HasNoteImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var contactClient = (sender as Image).Tag as ContactClient;
            if (contactClient != null)
                AddDockItem(TabControls.UserNotesPage, dgContactGrid.syncEntity);
        }

        private void SetAccountSource(ContactClient rec)
        {
            var cache = GetCache(rec._DCType);
            if (cache != null)
            {
                rec.accntSource = cache.GetNotNullArray;
                if (rec.accntSource != null)
                    rec.NotifyPropertyChanged("AccountSource");
            }
        }

        SQLCache GetCache(byte AccountType)
        {
            switch (AccountType)
            {
                case 1:
                    return DebtorCache;
                case 2:
                    return CreditorCache;
                case 3:
                    return CrmProspectCache;
                default: return null;
            }
        }

        CorasauGridLookupEditorClient prevAccount;
        private void Account_GotFocus(object sender, RoutedEventArgs e)
        {
            var selectedItem = dgContactGrid.SelectedItem as ContactClient;
            if (selectedItem != null)
            {
                SetAccountSource(selectedItem);
                if (prevAccount != null)
                    prevAccount.isValidate = false;
                var editor = (CorasauGridLookupEditorClient)sender;
                prevAccount = editor;
                editor.isValidate = true;
            }
        }

        private void HasEmailImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var contact = (sender as TextBlock).Tag as ContactClient;
            if (contact != null)
            {
                var mail = string.Concat("mailto:", contact._Email);
                System.Diagnostics.Process proc = new System.Diagnostics.Process();
                proc.StartInfo.FileName = mail;
                proc.Start();
            }
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
