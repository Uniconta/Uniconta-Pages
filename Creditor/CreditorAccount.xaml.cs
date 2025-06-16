using Uniconta.API.Service;
using UnicontaClient.Models;
using UnicontaClient.Utilities;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Uniconta.ClientTools.Controls;
using Uniconta.Common.Utility;
using Uniconta.API.DebtorCreditor;
using Uniconta.DataModel;
using System.Linq;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class CreditorAccountGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(CreditorClient); } }
        public override bool SingleBufferUpdate { get { return false; } }
    }
    public partial class CreditorAccount : GridBasePage
    {
        SQLCache interestCache, productsCache;
        public override string NameOfControl
        {
            get { return TabControls.CreditorAccount.ToString(); }
        }
        public CreditorAccount(BaseAPI API)
            : base(API, string.Empty)
        {
            InitPage();
        }

        private void InitPage()
        {
            LoadNow(typeof(Uniconta.DataModel.CreditorGroup));
            InitializeComponent();
            dgCreditorAccountGrid.RowDoubleClick += dgCreditorAccountGrid_RowDoubleClick;
            dgCreditorAccountGrid.BusyIndicator = busyIndicator;
            dgCreditorAccountGrid.api = api;
            LayoutControl = detailControl.layoutItems;
            SetRibbonControl(localMenu, dgCreditorAccountGrid);
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            ribbonControl.DisableButtons(new string[] { "AddLine", "CopyRow", "DeleteRow", "UndoDelete", "SaveGrid" });
            var Comp = api.CompanyEntity;
            if (Comp.RoundTo100)
                CurBalance.HasDecimals = Overdue.HasDecimals = false;
            dgCreditorAccountGrid.ShowTotalSummary();

            this.PreviewKeyDown += RootVisual_KeyDown;
            this.BeforeClose += CreditorAccount_BeforeClose;

            Interests.Visible = Interests.ShowInColumnChooser = Comp.CRM;
            Products.Visible = Products.ShowInColumnChooser = Comp.CRM;

            if (!Comp.AllowApproval)
                localMenu.DisableButtons("BankApprove");
        }

        private void CreditorAccount_BeforeClose()
        {
            this.PreviewKeyDown -= RootVisual_KeyDown;
        }

        private void RootVisual_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.F8 && Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                ribbonControl.PerformRibbonAction("CreditorTran");
                e.Handled = true;
            }
            else if (e.Key == Key.F8)
            {
                ribbonControl.PerformRibbonAction("OpenTran");
                e.Handled = true;
            }
        }

        protected override void OnLayoutCtrlLoaded()
        {
            detailControl.api = api;
        }
        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            UnicontaClient.Utilities.Utility.SetDimensionsGrid(api, cldim1, cldim2, cldim3, cldim4, cldim5);
            if (!api.CompanyEntity.DeliveryAddress)
            {
                DeliveryName.Visible = false;
                DeliveryAddress1.Visible = false;
                DeliveryAddress2.Visible = false;
                DeliveryAddress3.Visible = false;
                DeliveryZipCode.Visible = false;
                DeliveryCity.Visible = false;
                DeliveryCountry.Visible = false;
            }
            if (!api.CompanyEntity.CRM)
            {
                CrmGroup.Visible = false;
                Interests.Visible = false;
                Products.Visible = false;
            }
            dgCreditorAccountGrid.Readonly = true;
        }

        void dgCreditorAccountGrid_RowDoubleClick()
        {
            ribbonControl.PerformRibbonAction("CreditorTran");
        }
        public CreditorAccount(BaseAPI api, string lookupKey)
            : base(api, lookupKey)
        {
            InitPage();
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgCreditorAccountGrid.SelectedItem as CreditorClient;
            var selectedItems = dgCreditorAccountGrid.SelectedItems;
            switch (ActionType)
            {
                case "EditAll":
                    if (dgCreditorAccountGrid.Visibility == Visibility.Visible)
                        EditAll();
                    break;
                case "AddLine":
                    dgCreditorAccountGrid.AddRow();
                    break;
                case "CopyRow":
                    if (selectedItem != null)
                    {
                        if (copyRowIsEnabled)
                            dgCreditorAccountGrid.CopyRow();
                        else
                            CopyRecord(selectedItem);
                    }
                    break;
                case "DeleteRow":
                    dgCreditorAccountGrid.DeleteRow();
                    break;
                case "SaveGrid":
                    Save();
                    break;
                case "AddRow":
                    object[] param = new object[2];
                    param[0] = api;
                    param[1] = null;
                    AddDockItem(TabControls.CreditorAccountPage2, param, Uniconta.ClientTools.Localization.lookup("Creditorsaccount"), "Add_16x16");
                    break;
                case "EditRow":
                    if (selectedItem != null)
                        AddDockItem(TabControls.CreditorAccountPage2, new object[2] { selectedItem, true }, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Creditorsaccount"), selectedItem.Account));
                    break;
                case "CreditorTran":
                    if (selectedItem != null)
                        AddDockItem(TabControls.CreditorTransactions, dgCreditorAccountGrid.syncEntity,
                            string.Concat(Uniconta.ClientTools.Localization.lookup("CreditorTransactions"), "/", selectedItem._Account));
                    break;
                case "OpenTran":
                    if (selectedItem != null)
                        AddDockItem(TabControls.CreditorOpenTransactions, dgCreditorAccountGrid.syncEntity, Util.ConcatParenthesis(Uniconta.ClientTools.Localization.lookup("OpenTransaction"), selectedItem._Name));
                    break;
                case "Contacts":
                    if (selectedItem != null)
                        AddDockItem(TabControls.ContactPage, dgCreditorAccountGrid.syncEntity);
                    break;
                case "ClientItemNumber":
                    if (selectedItem?._ItemNameGroup != null)
                        AddDockItem(TabControls.LanguageItemTextPage, selectedItem);
                    else
                        UtilDisplay.ShowFieldMissing("ItemNameGroup");
                    break;
                case "Prices":
                    if (selectedItem?._PriceList != null)
                        AddDockItem(TabControls.CustomerPriceListLinePage, selectedItem);
                    else
                        UtilDisplay.ShowFieldMissing("PriceList");
                    break;
                case "Orders":
                    if (dgCreditorAccountGrid.syncEntity != null)
                        AddDockItem(TabControls.CreditorOrders, dgCreditorAccountGrid.syncEntity);
                    break;
                case "FollowUp":
                    if (selectedItem != null)
                    {
                        var followUpHeader = string.Format("{0}:{2} {1}", Uniconta.ClientTools.Localization.lookup("FollowUp"), selectedItem._Name, Uniconta.ClientTools.Localization.lookup("Accounts"));
                        AddDockItem(TabControls.CrmFollowUpPage, dgCreditorAccountGrid.syncEntity, followUpHeader);
                    }
                    break;
                case "AddNote":
                    if (selectedItem != null)
                    {
                        string hdr = string.Format("{0} : {1}", Uniconta.ClientTools.Localization.lookup("UserNotesInfo"), selectedItem._Account);
                        AddDockItem(TabControls.UserNotesPage, dgCreditorAccountGrid.syncEntity, hdr);
                    }
                    break;
                case "AddDoc":
                    if (selectedItem != null)
                        AddDockItem(TabControls.UserDocsPage, dgCreditorAccountGrid.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Documents"), selectedItem._Name));
                    break;
                case "InvTran":
                    if (selectedItem != null)
                        AddDockItem(TabControls.CreditorInvoiceLine, dgCreditorAccountGrid.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("InvTransactions"), selectedItem._Account));
                    break;
                case "Invoices":
                    if (selectedItem == null) return;
                    string header = string.Format("{0}/{1}", Uniconta.ClientTools.Localization.lookup("CreditorInvoice"), selectedItem._Account);
                    AddDockItem(TabControls.CreditorInvoice, dgCreditorAccountGrid.syncEntity, header);
                    break;
                case "RefreshGrid":
                    if (gridControl.Visibility == Visibility.Visible)
                        gridRibbon_BaseActions(ActionType);
                    break;
                case "AccountStat":
                    if (selectedItem != null)
                        AddDockItem(TabControls.InventoryTotals, selectedItem, string.Format("{0}:{1}", Uniconta.ClientTools.Localization.lookup("CreditorAccountStat"), selectedItem._Account));
                    break;
                case "CreditorStatement":
                    if (selectedItem != null)
                        AddDockItem(TabControls.CreditorStatement, dgCreditorAccountGrid.syncEntity);
                    break;
                case "DeliveryAddresses":
                    if (selectedItem != null)
                    {
                        var title = string.Format("{0}: {1}, {2}", Uniconta.ClientTools.Localization.lookup("DeliveryAddresses"), selectedItem._Account, selectedItem._Name);
                        AddDockItem(TabControls.WorkInstallationPage, dgCreditorAccountGrid.syncEntity, title);
                    }
                    break;
                case "CopyRecord":
                    CopyRecord(selectedItem);
                    break;
                case "UndoDelete":
                    dgCreditorAccountGrid.UndoDeleteRow();
                    break;
                /*
                 * Code from John which is not completed
                case "CreditorPrices":
                    if (selectedItem != null)
                        AddDockItem(TabControls.CreditorPrices, dgCreditorAccountGrid.syncEntity);
                    break;
                */
                case "CreditorTransPivot":
                    if (selectedItem != null)
                        AddDockItem(TabControls.CreditorInvoiceLinesPivotReport, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Pivot"), selectedItem._Name));
                    break;
                case "CreditorBankAccount":
                    CreditorBankDetails(selectedItem);
                    break;
                case "BankApprove":
                    BankApprove();
                    break;
                case "ValidateAddress":
                    if (selectedItems != null)
                    {
                        var selectedItemsArr = dgCreditorAccountGrid.SelectedItems.Cast<CreditorClient>().ToArray();
                        var msg = string.Format(Uniconta.ClientTools.Localization.lookup("MarkedControlFunction"), selectedItemsArr.Length);
                        var result = UnicontaMessageBox.Show(msg, Uniconta.ClientTools.Localization.lookup("Validate"), UnicontaMessageBox.YesNo, MessageBoxImage.Question);
                        if (result != UnicontaMessageBox.Yes)
                            return;

                        AddDockItem(TabControls.UpdateDebAddressViaCvr, selectedItemsArr, null, null, true, null, new[] { new BasePage.ValuePair(null, "Creditor") });
                    }
                    break;

                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
        async void CreditorBankDetails(CreditorClient selectedItem)
        {
            if (selectedItem != null)
            {
                CreditorPaymentAccountClient creditorPaymentAct = new CreditorPaymentAccountClient();
                creditorPaymentAct.SetMaster(selectedItem);
                var res = await api.Read(creditorPaymentAct);
                if (res == 0)
                {
                    AddDockItem(TabControls.CreditorPaymentAccountPage, creditorPaymentAct, string.Format("{0}: {1}, {2}", Uniconta.ClientTools.Localization.lookup("BankDetails"), selectedItem._Account, selectedItem._Name));
                }
                else
                {
                    var pr = new object[] { api, selectedItem };
                    AddDockItem(TabControls.CreditorPaymentAccountPage, pr, string.Format("{0}: {1}, {2}", Uniconta.ClientTools.Localization.lookup("BankDetails"), selectedItem._Account, selectedItem._Name));
                }
            }
        }

        async void BankApprove()
        {
            var selectedItems = dgCreditorAccountGrid.SelectedItems.Cast<Uniconta.ClientTools.DataModel.CreditorClient>();
            if (selectedItems != null)
            {
                var msg = string.Format(Uniconta.ClientTools.Localization.lookup("VendorsMarkedBankDetails"), selectedItems.Count());
                var result = UnicontaMessageBox.Show(msg, Uniconta.ClientTools.Localization.lookup("BankDetails"), UnicontaMessageBox.YesNo, MessageBoxImage.Question);
                if (result != UnicontaMessageBox.Yes)
                    return;
               
               busyIndicator.IsBusy = true;
               busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("BusyMessage");

               TransactionAPI tranApi = new TransactionAPI(this.api);

               int cntUpd = 0;
               foreach (var acc in selectedItems)
               {
                   if (acc.CreditorPaymentAccountRef == null || acc.CreditorPaymentAccountRef._Approved)
                       continue;
                   cntUpd++;
                   var err = await tranApi.ApprovePaymentAccount(acc.Account);

                   if (err != 0)
                   {
                       UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup(err.ToString()), Uniconta.ClientTools.Localization.lookup("Error"), MessageBoxButton.OK);
                       break;
                   }
                   acc.CreditorPaymentAccountRef._Approved = true;
                   acc.NotifyPropertyChanged("CreditorBankApprovement");
               }

               busyIndicator.IsBusy = false;
            }
        }

        void CopyRecord(CreditorClient selectedItem)
        {
            if (selectedItem == null)
                return;
            var creditor = Activator.CreateInstance(selectedItem.GetType()) as CreditorClient;
            CorasauDataGrid.CopyAndClearRowId(selectedItem, creditor);
            creditor._Created = DateTime.MinValue;
            creditor._D2CAccount = null;
            creditor._lastPayment = DateTime.MinValue;
            creditor._LastInvoice = DateTime.MinValue;
            AddDockItem(TabControls.CreditorAccountPage2, new object[2] { creditor, IdObject.get(false) }, Uniconta.ClientTools.Localization.lookup("Creditorsaccount"), "Add_16x16");
        }

        bool copyRowIsEnabled = false;
        bool editAllChecked;
        private void EditAll()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            var ibase = UtilDisplay.GetMenuCommandByName(rb, "EditAll");
            if (ibase == null)
                return;
            if (dgCreditorAccountGrid.Readonly)
            {
                api.AllowBackgroundCrud = false;
                dgCreditorAccountGrid.MakeEditable();
                UserFieldControl.MakeEditable(dgCreditorAccountGrid);
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
                                var err = await dgCreditorAccountGrid.SaveData();
                                if (err != 0)
                                {
                                    api.AllowBackgroundCrud = true;
                                    return;
                                }
                                break;
                            case CWConfirmationBox.ConfirmationResultEnum.No:
                                dgCreditorAccountGrid.CancelChanges(); 
                                break;
                        }
                        editAllChecked = true;
                        dgCreditorAccountGrid.Readonly = true;
                        dgCreditorAccountGrid.tableView.CloseEditor();
                        ibase.Caption = Uniconta.ClientTools.Localization.lookup("EditAll");
                        ribbonControl.DisableButtons(new string[] { "AddLine", "CopyRow", "DeleteRow", "UndoDelete", "SaveGrid" });
                        copyRowIsEnabled = false;
                    };
                    confirmationDialog.Show();
                }
                else
                {
                    dgCreditorAccountGrid.Readonly = true;
                    dgCreditorAccountGrid.tableView.CloseEditor();
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
                return editAllChecked ? false : dgCreditorAccountGrid.HasUnsavedData;
            }
        }
        private async void Save()
        {
            SetBusy();
            var err = await dgCreditorAccountGrid.SaveData();
            if (err != ErrorCodes.Succes)
                api.AllowBackgroundCrud = true;
            ClearBusy();
        }
        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.CreditorAccountPage2)
                dgCreditorAccountGrid.UpdateItemSource(argument);
            if (dgCreditorAccountGrid.Visibility == Visibility.Collapsed)
            {
                detailControl.Refresh(argument, dgCreditorAccountGrid.SelectedItem);
            }
            if (screenName == TabControls.UserNotesPage || screenName == TabControls.UserDocsPage && argument != null)
                dgCreditorAccountGrid.UpdateItemSource(argument);

        }

        async protected override void LoadCacheInBackGround()
        {
            var Comp = api.CompanyEntity;
            var lst = new List<Type>(12) { typeof(Uniconta.DataModel.Employee), typeof(Uniconta.DataModel.GLVat), typeof(Uniconta.DataModel.CreditorGroup), typeof(Uniconta.DataModel.PaymentTerm), typeof(Uniconta.DataModel.CreditorLayoutGroup) };
            if (Comp.CreditorPrice)
                lst.Add(typeof(Uniconta.DataModel.CreditorPriceList));
            if (Comp.NumberOfDimensions >= 1)
                lst.Add(typeof(Uniconta.DataModel.GLDimType1));
            if (Comp.NumberOfDimensions >= 2)
                lst.Add(typeof(Uniconta.DataModel.GLDimType2));
            if (Comp.NumberOfDimensions >= 3)
                lst.Add(typeof(Uniconta.DataModel.GLDimType3));
            if (Comp.NumberOfDimensions >= 4)
                lst.Add(typeof(Uniconta.DataModel.GLDimType4));
            if (Comp.NumberOfDimensions >= 5)
                lst.Add(typeof(Uniconta.DataModel.GLDimType5));
            if (Comp.Shipments)
            {
                lst.Add(typeof(Uniconta.DataModel.ShipmentType));
                lst.Add(typeof(Uniconta.DataModel.DeliveryTerm));
            }
            LoadType(lst);

            if (Comp.CRM)
            {
                interestCache = Comp.GetCache(typeof(Uniconta.DataModel.CrmInterest)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.CrmInterest), api).ConfigureAwait(false);
                productsCache = Comp.GetCache(typeof(Uniconta.DataModel.CrmProduct)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.CrmProduct), api).ConfigureAwait(false);
            }
        }

        private void HasDocImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var creditorAccount = (sender as System.Windows.Controls.Image).Tag as CreditorClient;
            if (creditorAccount != null)
                AddDockItem(TabControls.UserDocsPage, dgCreditorAccountGrid.syncEntity);
        }

        private void HasNoteImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var creditorAccount = (sender as System.Windows.Controls.Image).Tag as CreditorClient;
            if (creditorAccount != null)
                AddDockItem(TabControls.UserNotesPage, dgCreditorAccountGrid.syncEntity);
        }

        private void HasEmailImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var creditorAccount = (sender as TextBlock).Tag as CreditorClient;
            if (creditorAccount != null)
            {
                var mail = string.Concat("mailto:", creditorAccount._ContactEmail);
                System.Diagnostics.Process proc = new System.Diagnostics.Process();
                proc.StartInfo.FileName = mail;
                proc.Start();
            }
        }

        private void HasWebsite_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var creditorAccount = (sender as TextBlock).Tag as CreditorClient;
            Utility.OpenWebSite(creditorAccount?._Www);
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
