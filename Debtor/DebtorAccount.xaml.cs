using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Uniconta.API.Service;
using Uniconta.ClientTools;
using Uniconta.ClientTools.AI;
using Uniconta.ClientTools.AI.Prompts;
using Uniconta.ClientTools.AI.Services.Context;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.Controls.AI;
using Uniconta.ClientTools.Controls.AI.Assistant.Capabilities;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.Common.Enums;
using Uniconta.Common.User;
using Uniconta.Common.Utility;
using Uniconta.DataModel;
using Uniconta.WindowsAPI.ClientTools;
using UnicontaClient.Models;
using UnicontaClient.Utilities;
using static Uniconta.ClientTools.AI.Prompts.PromptDefinition;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class DebtorAccountGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(DebtorClient); } }
        public override bool SingleBufferUpdate { get { return false; } }
    }
    public partial class DebtorAccount : GridBasePage
    {
        SQLCache interestCache, productsCache;
        public override string NameOfControl
        {
            get { return TabControls.DebtorAccount.ToString(); }
        }
        public DebtorAccount(BaseAPI API) : base(API, string.Empty)
        {
            InitPage();
        }
        public DebtorAccount(BaseAPI api, string lookupKey)
            : base(api, lookupKey)
        {
            InitPage();
        }

        private void InitPage()
        {
            var Comp = api.CompanyEntity;
            if (Comp.CRM)
                LoadType(new Type[] { typeof(Uniconta.DataModel.DebtorGroup), typeof(Uniconta.DataModel.CrmInterest), typeof(Uniconta.DataModel.CrmProduct) });
            else
                LoadNow(typeof(Uniconta.DataModel.DebtorGroup));

            InitializeComponent();
            dgDebtorAccountGrid.RowDoubleClick += dgDebtorAccountGrid_RowDoubleClick;
            localMenu.dataGrid = dgDebtorAccountGrid;
            LayoutControl = detailControl.layoutItems;
            dgDebtorAccountGrid.api = api;
            dgDebtorAccountGrid.BusyIndicator = busyIndicator;
            SetRibbonControl(localMenu, dgDebtorAccountGrid);

            localMenu.OnItemClicked += localMenu_OnItemClicked;
            ribbonControl.DisableButtons(new string[] { "AddLine", "CopyRow", "DeleteRow", "UndoDelete", "SaveGrid" });

            if (Comp.RoundTo100)
                CurBalance.HasDecimals = Overdue.HasDecimals = false;

            dgDebtorAccountGrid.ShowTotalSummary();

            this.PreviewKeyDown += RootVisual_KeyDown;
            this.BeforeClose += DebtorAccount_BeforeClose;

            Interests.Visible = Interests.ShowInColumnChooser = Comp.CRM;
            Products.Visible = Products.ShowInColumnChooser = Comp.CRM;
        }

        private void RootVisual_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.F8 && Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                ribbonControl.PerformRibbonAction("DebtorTran");
                e.Handled = true;
            }
            else if (e.Key == Key.F8)
            {
                ribbonControl.PerformRibbonAction("OpenTran");
                e.Handled = true;
            }
        }

        private void DebtorAccount_BeforeClose()
        {
            this.PreviewKeyDown -= RootVisual_KeyDown;
        }

        protected override void OnLayoutCtrlLoaded()
        {
            detailControl.api = api;
        }
        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            UnicontaClient.Utilities.Utility.SetDimensionsGrid(api, cldim1, cldim2, cldim3, cldim4, cldim5);

            var Comp = api.CompanyEntity;
            if (!Comp.CRM)
            {
                CrmGroup.Visible = false;
                Interests.Visible = false;
                Products.Visible = false;
            }
            if (!Comp.DeliveryAddress)
            {
                DeliveryName.Visible = false;
                DeliveryAddress1.Visible = false;
                DeliveryAddress2.Visible = false;
                DeliveryAddress3.Visible = false;
                DeliveryZipCode.Visible = false;
                DeliveryCity.Visible = false;
                DeliveryCountry.Visible = false;
            }
            if (!Comp._DirectDebit)
            {
                PaymentFormat.Visible = false;
            }

            dgDebtorAccountGrid.Readonly = true;
            if (!Comp.InvPackaging)
            {
                PackagingConsumer.Visible = PackagingConsumer.ShowInColumnChooser = false;
                MicroEnterprise.Visible = MicroEnterprise.ShowInColumnChooser = false;
            }
            var rb = (RibbonBase)localMenu.DataContext;
            if (rb != null)
            {
                if (BasePage.session.User._Role == (byte)UserRoles.Standard)
                    UtilDisplay.RemoveMenuCommand(rb, "AccountantClientInfo");
                if (BasePage.session.User._Role != (byte)UserRoles.Accountant)
                    UtilDisplay.RemoveMenuCommand(rb, "ClientInformation");
            }
        }

        void dgDebtorAccountGrid_RowDoubleClick()
        {
            ribbonControl.PerformRibbonAction("DebtorTran");
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgDebtorAccountGrid.SelectedItem as DebtorClient;
            var selectedItems = dgDebtorAccountGrid.SelectedItems;

            switch (ActionType)
            {
                case "EditAll":
                    if (dgDebtorAccountGrid.Visibility == Visibility.Visible)
                        EditAll();
                    break;
                case "AddRow":
                    object[] param = new object[2];
                    param[0] = api;
                    param[1] = null;
                    AddDockItem(TabControls.DebtorAccountPage2, param, Uniconta.ClientTools.Localization.lookup("DebtorAccount"), "Add_16x16");
                    break;
                case "EditRow":
                    if (selectedItem != null)
                        AddDockItem(TabControls.DebtorAccountPage2, new object[2] { selectedItem, true }, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("DebtorAccount"), selectedItem._Account));
                    break;
                case "DebtorTran":
                    if (selectedItem != null)
                        AddDockItem(TabControls.DebtorTransactions, dgDebtorAccountGrid.syncEntity,
                            string.Concat(Uniconta.ClientTools.Localization.lookup("DebtorTransactions"), "/", selectedItem._Account));
                    break;
                case "OpenTran":
                    if (selectedItem != null)
                        AddDockItem(TabControls.DebtorOpenTransactions, dgDebtorAccountGrid.syncEntity, Util.ConcatParenthesis(Uniconta.ClientTools.Localization.lookup("OpenTransaction"), selectedItem._Name));
                    break;
                case "Contacts":
                    if (selectedItem != null)
                        AddDockItem(TabControls.ContactPage, dgDebtorAccountGrid.syncEntity);
                    break;
                case "ClientItemNumber":
                    if (selectedItem != null && selectedItem._ItemNameGroup != null)
                        AddDockItem(TabControls.LanguageItemTextPage, selectedItem);
                    else
                        UtilDisplay.ShowFieldMissing("ItemNameGroup");
                    break;
                case "CreateMandates":
                    if (selectedItems != null)
                        CreateMandates(selectedItems);
                    break;
                case "Prices":
                    if (selectedItem != null && selectedItem._PriceList != null)
                        AddDockItem(TabControls.CustomerPriceListLinePage, selectedItem);
                    else
                        UtilDisplay.ShowFieldMissing("PriceList");
                    break;
                case "DebtorOrders":
                    if (selectedItem != null)
                        AddDockItem(TabControls.DebtorOrders, dgDebtorAccountGrid.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Orders"), selectedItem._Name));
                    break;
                case "FollowUp":
                    if (selectedItem != null)
                    {
                        var header = string.Format("{0}:{2} {1}", Uniconta.ClientTools.Localization.lookup("FollowUp"), selectedItem._Name, Uniconta.ClientTools.Localization.lookup("Accounts"));
                        AddDockItem(TabControls.CrmFollowUpPage, dgDebtorAccountGrid.syncEntity, header);
                    }
                    break;
                case "AddNote":
                    if (selectedItem != null)
                    {
                        string header = string.Format("{0} : {1}", Uniconta.ClientTools.Localization.lookup("UserNotesInfo"), selectedItem._Account);
                        AddDockItem(TabControls.UserNotesPage, dgDebtorAccountGrid.syncEntity, header);
                    }
                    break;
                case "AddDoc":
                    if (selectedItem != null)
                        AddDockItem(TabControls.UserDocsPage, dgDebtorAccountGrid.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Documents"), selectedItem._Name));
                    break;
                case "Orders":
                    if (selectedItem != null)
                        AddDockItem(TabControls.DebtorOrders, dgDebtorAccountGrid.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Orders"), selectedItem._Name));
                    break;
                case "Offers":
                    if (selectedItem != null)
                        AddDockItem(TabControls.DebtorOffers, dgDebtorAccountGrid.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Offer"), selectedItem._Name));
                    break;
                case "Invoices":
                    if (selectedItem != null)
                    {
                        string header = string.Format("{0}/{1}", Uniconta.ClientTools.Localization.lookup("DebtorInvoice"), selectedItem._Account);
                        AddDockItem(TabControls.Invoices, dgDebtorAccountGrid.syncEntity, header);
                    }
                    break;
                case "InvTran":
                    if (selectedItem != null)
                        AddDockItem(TabControls.DebtorInvoiceLines, dgDebtorAccountGrid.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("InvTransactions"), selectedItem._Account));
                    break;
                case "AccountStat":
                    if (selectedItem != null)
                        AddDockItem(TabControls.InventoryTotals, selectedItem, string.Format("{0}:{1}", Uniconta.ClientTools.Localization.lookup("AccountStat"), selectedItem._Account));
                    break;
                case "AccountStatDate":
                    if (selectedItem != null)
                        AddDockItem(TabControls.DebtorAccountStat, new object[2] { selectedItem, true }, string.Format("{0}:{1}", Uniconta.ClientTools.Localization.lookup("AccountStat"), selectedItem._Account));
                    break;
                case "RefreshGrid":
                    if (gridControl.Visibility == Visibility.Visible)
                        gridRibbon_BaseActions(ActionType);
                    break;
                case "AddLine":
                    dgDebtorAccountGrid.AddRow();
                    break;
                case "CopyRow":
                    if (selectedItem != null)
                    {
                        if (copyRowIsEnabled)
                            dgDebtorAccountGrid.CopyRow();
                        else
                            CopyRecord(selectedItem);
                    }
                    break;
                case "DeleteRow":
                    dgDebtorAccountGrid.DeleteRow();
                    break;
                case "SaveGrid":
                    Save();
                    break;
                case "DebtorStatement":
                    if (selectedItem != null)
                        AddDockItem(TabControls.DebtorStatement, dgDebtorAccountGrid.syncEntity);
                    break;
                case "DeliveryAddresses":
                    if (selectedItem != null)
                    {
                        var header = string.Format("{0}: {1}, {2}", Uniconta.ClientTools.Localization.lookup("DeliveryAddresses"), selectedItem._Account, selectedItem._Name);
                        AddDockItem(TabControls.WorkInstallationPage, dgDebtorAccountGrid.syncEntity, header);
                    }
                    break;
                case "QuickInvoice":
                    if (selectedItem != null)
                        AddDockItem(TabControls.CreateInvoicePage, selectedItem);
                    break;
                case "CopyRecord":
                    CopyRecord(selectedItem);
                    break;
                case "DebtorTransPivot":
                    if (selectedItem != null)
                        AddDockItem(TabControls.DebtorInvoiceLinesPivotReport, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Pivot"), selectedItem._Name));
                    break;
                case "Project":
                    if (selectedItem != null)
                        AddDockItem(TabControls.Project, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Project"), selectedItem._Name));
                    break;
                case "UndoDelete":
                    dgDebtorAccountGrid.UndoDeleteRow();
                    break;
                case "DocSendLog":
                    if (selectedItem != null)
                        AddDockItem(TabControls.DocsSendLogGridPage, dgDebtorAccountGrid.syncEntity);
                    break;
                case "TaskGroups":
                    if (selectedItem != null)
                        AddDockItem(TabControls.DebtorProjTaskGroup, dgDebtorAccountGrid.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("TaskGroups"), selectedItem._Name));
                    break;
                case "AccountantClientInfo":
                    if (selectedItem != null)
                        AddDockItem(TabControls.AccountantClientInfo, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("AccountantClientInfo"), selectedItem._Account));
                    break;
                case "Budget":
                    if (selectedItem != null)
                        AddDockItem(TabControls.DebtorBudgetLinePage, dgDebtorAccountGrid.syncEntity);
                    break;
                case "CollectionLetterLog":
                    if (selectedItem != null)
                    {
                        string header = string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("CollectionLetterLog"), selectedItem.Account);
                        AddDockItem(TabControls.DebtorTransCollectPage, dgDebtorAccountGrid.syncEntity, header);
                    }
                    break;
                case "ClientInformation":
                    if (selectedItem != null)
                    {
                        var controlParams = "Dashboard=UC_DK_Std_Univisor_MasterData;sourcetype=UseCurrentRecord";
                        AddDockItem(TabControls.DashBoardViewerPage, selectedItem, string.Concat(Uniconta.ClientTools.Localization.lookup("Dashboard"), ": ", "UC_DK_Std_Univisor_MasterData"), null, true, null, controlParams, null);
                        break;
                    }
                    break;
                case "ValidateAddress":
                    ValidateFunctions(selectedItems, ActionType);
                    break;
                case "ValidateNemhandel":
                    ValidateFunctions(selectedItems, ActionType);
                    break;
                case "VatNumberValidationService":
                    ValidateFunctions(selectedItems, ActionType);
                    break;
                case "VatNumberValidationServiceLog":
                    AddDockItem(TabControls.DebtorFieldLogPage, dgDebtorAccountGrid.syncEntity, true, "Log", null, new System.Windows.Point(100, 200));
                    break;

                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        public override List<IAIAssistantCapability> GetAIAssistantCapabilities(bool hasFileAttached)
        {
            var retval = base.GetAIAssistantCapabilities(hasFileAttached);

            retval.Add(new DeterminePublicProfileCapability());

            return retval;
        }

        void CopyRecord(DebtorClient selectedItem)
        {
            if (selectedItem == null)
                return;
            var debtor = Activator.CreateInstance(selectedItem.GetType()) as DebtorClient;
            CorasauDataGrid.CopyAndClearRowId(selectedItem, debtor);
            debtor._Created = DateTime.MinValue;
            debtor._D2CAccount = null;
            debtor._lastPayment = DateTime.MinValue;
            debtor._LastInvoice = DateTime.MinValue;
            AddDockItem(TabControls.DebtorAccountPage2, new object[2] { debtor, IdObject.get(false) }, Uniconta.ClientTools.Localization.lookup("DebtorAccount"), "Add_16x16");
        }

        void ValidateFunctions(IList selectedItems, string actiontype)
        {
            if (selectedItems != null)
            {
                var selectedItemsArr = dgDebtorAccountGrid.SelectedItems.Cast<DebtorClient>().ToArray();
                var msg = string.Format(Uniconta.ClientTools.Localization.lookup("MarkedControlFunction"), selectedItemsArr.Length);
                var actionstr = actiontype == "ValidateNemhandel" ? "Nemhandel" : actiontype == "ValidateAddress" ? Uniconta.ClientTools.Localization.lookup("Address") : Uniconta.ClientTools.Localization.lookup("VatNumberValidationService");
                var result = UnicontaMessageBox.Show(msg, string.Concat(Uniconta.ClientTools.Localization.lookup("Validate"), " (", actionstr, ")"), UnicontaMessageBox.YesNo, MessageBoxImage.Question);
                if (result != UnicontaMessageBox.Yes)
                    return;

                if (actiontype == "ValidateNemhandel")
                    AddDockItem(TabControls.DebtorNHRLookup, selectedItemsArr, null, null, true, null);
                else if (actiontype == "ValidateAddress")
                    AddDockItem(TabControls.UpdateDebAddressViaCvr, selectedItemsArr, null, null, true, null, new[] { new BasePage.ValuePair(null, "Debtor") });
                else if (actiontype == "VatNumberValidationService")
                    VatNumberValidationService(selectedItemsArr);
            }
        }

        async void VatNumberValidationService(IEnumerable<DebtorClient> debtors)
        {
            try
            {
                var previousContent = busyIndicator.BusyContent;

                busyIndicator.IsBusy = true;
                const int Grouping = 20;
                int cntTotal = debtors.Count();
                int cntPotential = 0;
                int cntAll = 0;
                int cntRejected = 0;
                int cntApproved = 0;
                int cntNotApproved = 0;
                int cntErrorVatNumberFormat = 0;
                int cntErrorValidationService = 0;

                var lst2 = new List<DebtorClient>();
                foreach (var debtor in debtors)
                {
                    cntAll++;
                    if ((debtor._Country == CountryCode.UnitedKingdom && debtor._EORI == null) || (debtor._Country != CountryCode.UnitedKingdom &&
                        (debtor._LegalIdent == null || !Country2Language.IsEU(debtor._Country) || debtor._Country == api.CompanyEntity._CountryId)))
                        cntRejected++;
                    else
                    {
                        cntPotential++;
                        lst2.Add(debtor);
                    }

                    if (lst2.Count > 0 && ((cntPotential % Grouping) == 0 || cntAll == cntTotal))
                    {
                        busyIndicator.BusyContent = string.Concat(Uniconta.ClientTools.Localization.lookup("Total"), ": ", cntTotal, Environment.NewLine,
                                                                  Uniconta.ClientTools.Localization.lookup("Approved"), ": ", cntApproved, " ...", Environment.NewLine,
                                                                  Uniconta.ClientTools.Localization.lookup("NotApproved"), ": ", cntNotApproved, " ...", Environment.NewLine,
                                                                  Uniconta.ClientTools.Localization.lookup("ErrorVatNumberFormat"), ": ", cntErrorVatNumberFormat, " ...", Environment.NewLine,
                                                                  Uniconta.ClientTools.Localization.lookup("ErrorValidationService"), ": ", cntErrorValidationService, " ...", Environment.NewLine,
                                                                  Uniconta.ClientTools.Localization.lookup("Rejected"), ": ", cntRejected, " ...");

                        var viesReportLst = await VIES.CheckVatApprox(lst2, api);
                        if (viesReportLst == null || viesReportLst.Count == 1 && viesReportLst[0].FaultCode == VIES.UC_FAULTCODE_GENERALERROR) //TODO:Måske ændre
                        {
                            busyIndicator.IsBusy = false;
                            busyIndicator.BusyContent = previousContent;
                            UnicontaMessageBox.Show(viesReportLst?[0].FaultString ?? Uniconta.ClientTools.Localization.lookup("NoSucces"), Uniconta.ClientTools.Localization.lookup("Error"), MessageBoxButton.OK);
                            return;
                        }

                        foreach (var x in viesReportLst)
                        {
                            if (x.FaultString != null)
                            {
                                if (x.NoValidVATNo)
                                    cntErrorVatNumberFormat++;
                                else
                                    cntErrorValidationService++;
                                continue;
                            }

                            if (x.IsValid)
                                cntApproved++;
                            else
                                cntNotApproved++;
                        }
                        lst2.Clear();
                    }
                }
                busyIndicator.IsBusy = false;
                busyIndicator.BusyContent = previousContent;

                var sb = StringBuilderReuse.Create(Uniconta.ClientTools.Localization.lookup("VatNumberValidationService")).AppendLine().AppendLine().

                Append(string.Concat(Uniconta.ClientTools.Localization.lookup("Total"), ": ", cntTotal)).AppendLine().
                Append(string.Concat(Uniconta.ClientTools.Localization.lookup("Approved"), ": ", cntApproved)).AppendLine().
                Append(string.Concat(Uniconta.ClientTools.Localization.lookup("NotApproved"), ": ", cntNotApproved));
                if (cntErrorVatNumberFormat > 0)
                    sb.AppendLine().Append(string.Concat(Uniconta.ClientTools.Localization.lookup("ErrorVatNumberFormat"), ": ", cntErrorVatNumberFormat));
                if (cntErrorValidationService > 0)
                    sb.AppendLine().Append(string.Concat(Uniconta.ClientTools.Localization.lookup("ErrorValidationService"), ": ", cntErrorValidationService));
                if (cntRejected > 0)
                    sb.AppendLine().Append(string.Concat(Uniconta.ClientTools.Localization.lookup("Rejected"), ": ", cntRejected));

                UnicontaMessageBox.Show(sb.ToStringAndRelease(), Uniconta.ClientTools.Localization.lookup("Information"), MessageBoxButton.OK);
            }
            catch (Exception ex)
            {
                busyIndicator.IsBusy = false;
                UnicontaMessageBox.Show(ex.Message, Uniconta.ClientTools.Localization.lookup("Exception"), MessageBoxButton.OK);
                return;
            }
        }

        async void CreateMandates(IList debtors)
        {
            var mandateCache = api.GetCache(typeof(Uniconta.DataModel.DebtorPaymentMandate)) ?? await api.LoadCache(typeof(Uniconta.DataModel.DebtorPaymentMandate));

            CWDirectDebit cwwin = new CWDirectDebit(api, string.Format(Uniconta.ClientTools.Localization.lookup("CreateOBJ"), Uniconta.ClientTools.Localization.lookup("Mandates")), true);
            cwwin.Args = new Uniconta.ClientTools.ChildWindowCallerArgs(this, api);
            cwwin.Closing += delegate
            {
                if (cwwin.DialogResult == true)
                {
                    var lstDebtors = debtors.Cast<Uniconta.ClientTools.DataModel.DebtorClient>();
                    var lstInsert = new List<Uniconta.DataModel.DebtorPaymentMandate>();
                    var lstUpdate = new List<Uniconta.DataModel.Debtor>();

                    bool mandateIsDebtor = cwwin.PaymentFormat._UseDebtorAccAsMandate;

                    foreach (var rec in lstDebtors)
                    {
                        var mandate = (Uniconta.DataModel.DebtorPaymentMandate)mandateCache?.Get(rec._Account);
                        if (mandate != null)
                            continue;

                        var newMandate = new Uniconta.DataModel.DebtorPaymentMandate();
                        newMandate.SetMaster(rec);
                        newMandate._AltMandateId = mandateIsDebtor ? rec._Account : null;
                        newMandate._Scheme = cwwin.directDebitScheme;
                        newMandate._StatusInfo = string.Format("({0}) {1}", Uniconta.DirectDebitPayment.Common.GetTimeStamp(), Uniconta.ClientTools.Localization.lookup("Created")); ;
                        lstInsert.Add(newMandate);

                        if (rec.PaymentFormat != cwwin.PaymentFormat._Format)
                        {
                            rec.PaymentFormat = cwwin.PaymentFormat._Format;
                            lstUpdate.Add(rec);
                        }
                    }

                    if (lstInsert.Count > 0)
                    {
                        api.InsertNoResponse(lstInsert);
                        UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("RecordsUpdated"), lstInsert.Count, Uniconta.ClientTools.Localization.lookup("Mandates")), Uniconta.ClientTools.Localization.lookup("Information"));
                    }

                    if (lstUpdate.Count > 0)
                        api.UpdateNoResponse(lstUpdate);
                }
            };
            cwwin.Show();
        }

        bool copyRowIsEnabled = false;
        bool editAllChecked;
        private void EditAll()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            var ibase = UtilDisplay.GetMenuCommandByName(rb, "EditAll");
            if (ibase == null)
                return;
            if (dgDebtorAccountGrid.Readonly)
            {
                api.AllowBackgroundCrud = false;
                dgDebtorAccountGrid.MakeEditable();
                UserFieldControl.MakeEditable(dgDebtorAccountGrid);
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
                                var err = await dgDebtorAccountGrid.SaveData();
                                if (err != 0)
                                {
                                    api.AllowBackgroundCrud = true;
                                    return;
                                }
                                break;
                            case CWConfirmationBox.ConfirmationResultEnum.No:
                                dgDebtorAccountGrid.CancelChanges();
                                break;
                        }
                        editAllChecked = true;
                        dgDebtorAccountGrid.Readonly = true;
                        dgDebtorAccountGrid.tableView.CloseEditor();
                        ibase.Caption = Uniconta.ClientTools.Localization.lookup("EditAll");
                        ribbonControl.DisableButtons(new string[] { "AddLine", "CopyRow", "DeleteRow", "UndoDelete", "SaveGrid" });
                        copyRowIsEnabled = false;
                    };
                    confirmationDialog.Show();
                }
                else
                {
                    dgDebtorAccountGrid.Readonly = true;
                    dgDebtorAccountGrid.tableView.CloseEditor();
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
                return editAllChecked ? false : dgDebtorAccountGrid.HasUnsavedData;
            }
        }
        private async void Save()
        {
            SetBusy();
            var err = await dgDebtorAccountGrid.SaveData();
            if (err != ErrorCodes.Succes)
                api.AllowBackgroundCrud = true;
            ClearBusy();
        }
        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.DebtorAccountPage2)
            {
                dgDebtorAccountGrid.UpdateItemSource(argument);
                if (dgDebtorAccountGrid.Visibility == Visibility.Collapsed)
                    detailControl.Refresh(argument, dgDebtorAccountGrid.SelectedItem);
            }
            if (screenName == TabControls.UserNotesPage || screenName == TabControls.UserDocsPage && argument != null)
                dgDebtorAccountGrid.UpdateItemSource(argument);
        }

        protected override async Task LoadCacheInBackGroundAsync()
        {
            var Comp = api.CompanyEntity;
            var lst = new List<Type>(12) { typeof(Uniconta.DataModel.Employee), typeof(Uniconta.DataModel.GLVat), typeof(Uniconta.DataModel.DebtorGroup), typeof(Uniconta.DataModel.PaymentTerm), typeof(Uniconta.DataModel.DebtorLayoutGroup) };
            if (Comp.InvPrice)
                lst.Add(typeof(Uniconta.DataModel.DebtorPriceList));
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
            var debtorAccount = (sender as System.Windows.Controls.Image).Tag as DebtorClient;
            if (debtorAccount != null)
                AddDockItem(TabControls.UserDocsPage, dgDebtorAccountGrid.syncEntity);
        }

        private void HasNoteImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var debtorAccount = (sender as System.Windows.Controls.Image).Tag as DebtorClient;
            if (debtorAccount != null)
                AddDockItem(TabControls.UserNotesPage, dgDebtorAccountGrid.syncEntity);
        }

        private void HasEmailImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var debtorAccount = (sender as TextBlock).Tag as DebtorClient;
            if (debtorAccount != null)
            {
                if (string.IsNullOrWhiteSpace(debtorAccount._ContactEmail))
                    return;
                OfficeComponentUtil.SendEmailToOutlook(debtorAccount._ContactEmail);
            }
        }

        private void HasWebsite_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var debtorAccount = (sender as TextBlock).Tag as DebtorClient;
            Utility.OpenWebSite(debtorAccount._Www);
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

    public sealed class DeterminePublicProfileCapability : AIAssistantCapabilityBase, IAIAssistantCapabilityPrompt, IAIAssistantCapabilityButton
    {
        public DeterminePublicProfileCapability()
            : base(
                nameof(DeterminePublicProfileCapability),
                "Search the web",
                "Researches the selected account using publicly available internet sources. " +
                "Use this capability when the user asks to find company information, " +
                "research the debtor, determine its public profile, identify what the company does, " +
                "or search the web for commercial information. " +
                "The web search must only start after explicit user confirmation.",
                weight: 80,
                "Determine the public profile",
                "Find information about this account on the web",
                "Find information about this vendor",
                "Search the internet for this debtor",
                "What does this company do?",
                "Research this company based on internet data")
        {
        }

        public Task<AIAssistantCapabilityResult> ExecuteAsync(IAIAssistantCapabilityContext capabilityContext)
        {
            var dcAccount =
                capabilityContext.AssistantConsumer.AIRecordData?.FocussedRow as DCAccount;

            if (dcAccount == null)
            {
                return Task.FromResult(
                    AIAssistantCapabilityResult.Fail(
                        "No account is currently selected."));
            }

            var companyName = !string.IsNullOrWhiteSpace(dcAccount._Name) ? dcAccount._Name : dcAccount._Account;

            return Task.FromResult(
                new AIAssistantCapabilityResult
                {
                    Succeeded = true,
                    Message = $"Do you want to search for public information about '{companyName}'?",
                    Actions = new[]
                    {
                        new AIAssistantCapabilityResult.CapabilityAction
                        {
                            Caption = "Yes, search the web",
                            ExecuteAsync = () => ExecuteWebSearchAsync(capabilityContext,dcAccount)
                        },
                        new AIAssistantCapabilityResult.CapabilityAction
                        {
                            Caption = Uniconta.ClientTools.Localization.lookup("No"),
                            ExecuteAsync = () => Task.FromResult(AIAssistantCapabilityResult.Success("The web search was not performed."))
                        }
                    }
                });
        }

        private async Task<AIAssistantCapabilityResult> ExecuteWebSearchAsync(IAIAssistantCapabilityContext capabilityContext, DCAccount dcAccount)
        {
            var prompt = PromptProvider.From(() => PublicProfilePrompt)
                .Replace("DebtorContext", CreateAccountContext(dcAccount));

            var conversation =
                capabilityContext.UserRequest.GetConversationContext(prompt);

            var report = await capabilityContext.GPTService.GetResponse<PublicProfileReport>(conversation);

            if (string.IsNullOrWhiteSpace(report?.Report))
            {
                return AIAssistantCapabilityResult.Fail("No public company profile could be created.");
            }

            return AIAssistantCapabilityResult.Success(report?.Report);
        }

        private static string CreateAccountContext(DCAccount account)
        {
            if (account == null)
                return string.Empty;

            return JsonConvert.SerializeObject(
                new
                {
                    account._Account,
                    account._Name,
                    Address = string.Join(
                        Environment.NewLine,
                        new[]
                        {
                        account._Address1,
                        account._Address2,
                        account._Address3
                        }.Where(x => !string.IsNullOrWhiteSpace(x))),
                    account._ZipCode,
                    account._City,
                    Country = account._Country,
                    Website = account._Www,
                    account._Phone
                },
                Formatting.Indented,
                new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                });
        }

        private static PromptDefinition NewPromptDefinition(string system, string user, bool expectsExplanationInUserLanguage = false)
        {
            return PromptDefinition.FromMessages(
                new[]
                {
                new PromptRoleNode
                {
                    Role = MessageRole.System,
                    Content = system
                },
                new PromptRoleNode
                {
                    Role = MessageRole.User,
                    Content = user
                }
                }, expectsExplanationInUserLanguage);
        }

        [PromptDefinition(
            "Capability.DeterminePublicProfileCapability",
            "Prompt used to research an account and create a public commercial company profile.")]
        [PromptDefinitionReplacement(
            "DebtorContext",
            "The selected account's number, name, address, website, phone number, and other available identifying information.")]
        public static PromptDefinition PublicProfilePrompt =>
            NewPromptDefinition(
                "You are a commercial company research assistant. " +
                "Research companies using publicly available online sources. " +
                "Identify the exact legal or commercial entity before creating the report. " +
                "Prefer the supplied website, registration number, exact address, and other strong identifiers. " +
                "Never identify a company from its name alone when multiple companies could match. " +
                "Do not invent facts. " +
                "Clearly distinguish confirmed information from estimates or uncertain information. " +
                "Only include URLs that were actually used as evidence. " +
                "First determine whether the company can be identified reliably. " +
                "Find the official company website and relevant supporting public sources. " +
                "Describe what the company sells or does. " +
                "Identify its industry, business model, products, services, customer segments, and markets served. " +
                "Estimate company size or revenue only when public evidence reasonably supports it. " +
                "Include the verified location and coordinates when these can be determined reliably. " +
                "Generate useful commercial characteristics and search keywords. " +
                "Avoid personal information unless essential to identifying the company. " +
                "Add clear warnings when identification is ambiguous, the company appears inactive, " +
                "information may be outdated, or important information could not be verified. " +
                "Return a polished, readable commercial company report in Markdown. " +
                "Use meaningful headings, short paragraphs, bullet lists, and Markdown links. " +
                "Include an identity-confidence section and finish with a Sources section. " +
                "Do not return JSON, XML, a schema, or explanatory text outside the report.",
                "Research the following account and produce a commercial company profile report.\n\n" +
                "Account context:\n{{DebtorContext}}", expectsExplanationInUserLanguage: true);

        // marked with Web search tool
        public sealed class PublicProfileReport : IGPTToolWebSearch
        {
            public string Report { get; set; }
        }  

    }
}
