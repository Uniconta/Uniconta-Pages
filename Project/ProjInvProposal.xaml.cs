using DevExpress.Data;
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
using System.Windows.Shapes;
using Uniconta.API.DebtorCreditor;
using Uniconta.API.Service;
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.Common.Utility;
using Uniconta.DataModel;
using UnicontaClient.Controls.Dialogs;
using UnicontaClient.Models;
using UnicontaClient.Utilities;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class ProjInvPropProposalGrid : CorasauDataGridClient
    {
        public override bool SingleBufferUpdate { get { return false; } }
        public override Type TableType { get { return typeof(ProjectInvoiceProposalClient); } }
        public override IComparer GridSorting { get { return new DCOrderSort(); } }
    }

    public partial class ProjInvProposal : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.ProjInvProposal; } }

        private SynchronizeEntity syncEntity;
        public ProjInvProposal(BaseAPI api, string lookupKey)
            : base(api, lookupKey)
        {
            Init(null);
        }

        public ProjInvProposal(BaseAPI API) : base(API, string.Empty)
        {
            Init(null);
        }
        public ProjInvProposal(SynchronizeEntity syncEntity)
            : base(syncEntity, true)
        {
            this.syncEntity = syncEntity;
            if (syncEntity != null)
                Init(syncEntity.Row);
            SetHeader();
        }

        protected override SortingProperties[] DefaultSort()
        {
            SortingProperties orderNoSort = new SortingProperties("OrderNumber");
            orderNoSort.Ascending = false;
            return new SortingProperties[] { orderNoSort };
        }

        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            dgProjInvProposalGrid.UpdateMaster(args);
            SetHeader();
            BindGrid();
        }

        void SetHeader()
        {
            var syncMaster = dgProjInvProposalGrid.masterRecord as Uniconta.DataModel.Debtor;
            string header = null;
            if (syncMaster != null)
                header = string.Format("{0}/{1}", Uniconta.ClientTools.Localization.lookup("InvoiceProposal"), syncMaster._Account);
            else
            {
                var projectMaster = dgProjInvProposalGrid.masterRecord as Uniconta.DataModel.Project;
                if (projectMaster != null)
                    header = string.Format("{0}/{1}", Uniconta.ClientTools.Localization.lookup("InvoiceProposal"), projectMaster._DCAccount);
            }
            if (header != null)
                SetHeader(header);
        }

        public ProjInvProposal(UnicontaBaseEntity master)
            : base(master)
        {
            Init(master);
            if (syncEntity != null)
                dgProjInvProposalGrid.UpdateMaster(master);

        }

        private void Init(UnicontaBaseEntity master)
        {
            InitializeComponent();
            dgProjInvProposalGrid.UpdateMaster(master);
            dgProjInvProposalGrid.api = api;
            dgProjInvProposalGrid.BusyIndicator = busyIndicator;
            SetRibbonControl(localMenu, dgProjInvProposalGrid);
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgProjInvProposalGrid.ShowTotalSummary();
            ribbonControl.DisableButtons(new string[] { "UndoDelete", "DeleteRow", "SaveGrid" });
            dgProjInvProposalGrid.CustomSummary += dgProjInvProposalGrid_CustomSummary;
            dgProjInvProposalGrid.RowDoubleClick += dgProjInvProposalGrid_RowDoubleClick;
            dgProjInvProposalGrid.SelectedItemChanged += DgProjInvProposalGrid_SelectedItemChanged;
        }

        private void DgProjInvProposalGrid_SelectedItemChanged(object sender, SelectedItemChangedEventArgs e)
        {
            if (e.NewItem != null && e.NewItem is ProjectInvoiceProposalClient projectInvProposalClient)
            {
                if (projectInvProposalClient.PrCategoryRef._CatType == CategoryType.OnAccountInvoicing)
                    ribbonControl.DisableButtons(new string[] { "RegenerateOrderFromProject", "ProjectTransaction" });
                else
                    ribbonControl.EnableButtons(new string[] { "RegenerateOrderFromProject", "ProjectTransaction" });
            }
        }

        public override async void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.ProjInvProposalPage2)
                dgProjInvProposalGrid.UpdateItemSource(argument);
            else if (screenName == TabControls.ProjInvoiceProposalLine)
            {
                var InvoiveProposal = argument as ProjectInvoiceProposalClient;
                if (InvoiveProposal == null)
                    return;
                var err = await api.Read(InvoiveProposal);
                if (err == ErrorCodes.CouldNotFind)
                    dgProjInvProposalGrid.UpdateItemSource(3, InvoiveProposal);
                else if (err == ErrorCodes.Succes)
                    dgProjInvProposalGrid.UpdateItemSource(2, InvoiveProposal);
            }
        }

        double sumMargin, sumSales, sumMarginRatio;
        private void dgProjInvProposalGrid_CustomSummary(object sender, DevExpress.Data.CustomSummaryEventArgs e)
        {
            var fieldName = ((GridSummaryItem)e.Item).FieldName;
            switch (e.SummaryProcess)
            {
                case CustomSummaryProcess.Start:
                    sumMargin = sumSales = 0d;
                    break;
                case CustomSummaryProcess.Calculate:
                    var row = e.Row as ProjectInvoiceProposalClient;
                    sumSales += row.SalesValue;
                    sumMargin += row.Margin;
                    break;
                case CustomSummaryProcess.Finalize:
                    if (fieldName == "MarginRatio" && sumSales > 0)
                    {
                        sumMarginRatio = 100 * sumMargin / sumSales;
                        e.TotalValue = sumMarginRatio;
                    }
                    break;
            }
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            bool showFields = (dgProjInvProposalGrid.masterRecords == null);
            Account.Visible = showFields;
            Name.Visible = showFields;
            Utility.SetDimensionsGrid(api, cldim1, cldim2, cldim3, cldim4, cldim5);
            var Comp = api.CompanyEntity;
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
            dgProjInvProposalGrid.Readonly = true;
            if (!Comp.ApproveSalesOrders)
                Approver.ShowInColumnChooser = Approved.ShowInColumnChooser = ApprovedDate.ShowInColumnChooser = false;
            else
                Approver.ShowInColumnChooser = Approved.ShowInColumnChooser = ApprovedDate.ShowInColumnChooser = true;
            if (!Comp.ProjectTask)
                Task.ShowInColumnChooser = Task.Visible = false;
            else
                Task.ShowInColumnChooser = Task.Visible = true;
            if (Comp.HideCostPrice)
            {
                Margin.Visible = Margin.ShowInColumnChooser = MarginRatio.Visible = MarginRatio.ShowInColumnChooser =
            CostValue.Visible = CostValue.ShowInColumnChooser = false;
            }
            if (Comp.InvPackaging)
                Consumer.Visible = Consumer.ShowInColumnChooser = false;
        }
        void dgProjInvProposalGrid_RowDoubleClick()
        {
            ribbonControl.PerformRibbonAction("ProjInvProposalLine");
        }
        private void localMenu_OnItemClicked(string ActionType)
        {
            var dgProjInvProposalGrid = this.dgProjInvProposalGrid;
            var selectedItem = dgProjInvProposalGrid.SelectedItem as ProjectInvoiceProposalClient;
            string salesHeader = string.Empty;
            if (selectedItem != null)
                salesHeader = string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("InvoiceProposal"), selectedItem._OrderNumber);
            switch (ActionType)
            {
                case "AddRow":
                    if (dgProjInvProposalGrid.masterRecords != null)
                    {
                        AddDockItem(TabControls.ProjInvProposalPage2, new object[] { api, dgProjInvProposalGrid.masterRecord }, Uniconta.ClientTools.Localization.lookup("InvoiceProposal"), "Add_16x16");
                    }
                    else
                        AddDockItem(TabControls.ProjInvProposalPage2, api, Uniconta.ClientTools.Localization.lookup("InvoiceProposal"), "Add_16x16");
                    break;
                case "EditRow":
                    if (selectedItem == null)
                        return;
                    if (dgProjInvProposalGrid.masterRecords != null)
                    {
                        object[] arr = new object[2] { selectedItem, dgProjInvProposalGrid.masterRecord };
                        AddDockItem(TabControls.ProjInvProposalPage2, arr, salesHeader);
                    }
                    else
                    {
                        AddDockItem(TabControls.ProjInvProposalPage2, selectedItem, salesHeader);
                    }
                    break;
                case "ProjInvProposalLine":
                    if (selectedItem == null)
                        return;
                    var olheader = string.Format("{0}:{1},{2}", Uniconta.ClientTools.Localization.lookup("InvoiceProposalLine"), selectedItem._OrderNumber, selectedItem.Name);
                    AddDockItem(TabControls.ProjInvoiceProposalLine, dgProjInvProposalGrid.syncEntity, olheader);
                    break;
                case "Invoices":
                    AddDockItem(TabControls.Invoices, selectedItem, salesHeader);
                    break;
                case "AddNote":
                    if (selectedItem != null)
                    {
                        string header = string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("UserNotesInfo"), selectedItem._OrderNumber);
                        AddDockItem(TabControls.UserNotesPage, dgProjInvProposalGrid.syncEntity, header);
                    }
                    break;
                case "AddDoc":
                    if (selectedItem != null)
                    {
                        string header = string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Documents"), selectedItem._OrderNumber);
                        AddDockItem(TabControls.UserDocsPage, dgProjInvProposalGrid.syncEntity, header);
                    }
                    break;
                case "Contacts":
                    if (selectedItem == null)
                        return;
                    AddDockItem(TabControls.ContactPage, selectedItem);
                    break;
                case "EditDebtor":
                    if (selectedItem?._DCAccount != null)
                        jumpToDebtor(selectedItem);
                    break;
                case "EditAll":
                    if (dgProjInvProposalGrid.Visibility == Visibility.Visible)
                        EditAll();
                    break;
                case "AddLine":
                    dgProjInvProposalGrid.AddRow();
                    break;
                case "CopyRow":
                    dgProjInvProposalGrid.CopyRow();
                    break;
                case "DeleteRow":
                    dgProjInvProposalGrid.DeleteRow();
                    break;
                case "UndoDelete":
                    dgProjInvProposalGrid.UndoDeleteRow();
                    break;
                case "SaveGrid":
                    Save();
                    break;
                case "CreatePacknote":
                case "CreateInvoice":
                    if (selectedItem != null)
                    {
                        if (Utility.HasControlRights("GenerateInvoice", api.CompanyEntity))
                            GenerateInvoice(selectedItem, ActionType == "CreatePacknote" ? CompanyLayoutType.Packnote : CompanyLayoutType.Invoice);
                        else
                            UtilDisplay.ShowControlAccessMsg("GenerateInvoice");
                    }
                    break;
                case "ProjectTransaction":
                    if (selectedItem?._Project != null)
                        AddDockItem(TabControls.ProjectInvoiceProjectLinePage, dgProjInvProposalGrid.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("ProjectAdjustments"), selectedItem._OrderNumber));
                    break;
                case "RefreshGrid":
                    TestDebtorReload(true, dgProjInvProposalGrid.ItemsSource as IEnumerable<ProjectInvoiceProposal>);
                    break;
                case "RegenerateOrderFromProject":
                    if (selectedItem != null)
                        AddDockItem(TabControls.RegenerateOrderFromProjectPage, dgProjInvProposalGrid.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("RegenerateOrder"), selectedItem._OrderNumber));
                    break;
                case "ApproveOrder":
                    if (selectedItem != null && api.CompanyEntity.ApproveSalesOrders)
                        Utility.ApproveOrder(api, selectedItem);
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        async void TestDebtorReload(bool refresh, IEnumerable<ProjectInvoiceProposal> lst)
        {
            bool reload = false;
            if (lst != null && lst.Count() > 0)
            {
                var cache = api.GetCache(typeof(Uniconta.DataModel.Debtor));
                if (cache == null)
                    return;

                var Contacts = api.GetCache(typeof(Uniconta.DataModel.Contact));
                foreach (var rec in lst)
                {
                    if (rec._DCAccount != null && cache.Get(rec._DCAccount) == null)
                    {
                        reload = true;
                        break;
                    }
                    if (rec._ContactRef != 0 && Contacts != null && Contacts.Get(rec._ContactRef) == null)
                    {
                        Contacts = null;
                        api.LoadCache(typeof(Uniconta.DataModel.Contact), true);
                    }
                }
                if (reload)
                    await api.LoadCache(typeof(Uniconta.DataModel.Debtor), true);
            }
            if (refresh)
                gridRibbon_BaseActions("RefreshGrid");
        }

        async void jumpToDebtor(ProjectInvoiceProposalClient selectedItem)
        {
            var dc = selectedItem.Debtor;
            if (dc == null)
            {
                await api.CompanyEntity.LoadCache(typeof(Debtor), api, true);
                dc = selectedItem.Debtor;
                if (dc == null)
                    return;
            }
            var param = new object[2] { dc, true };
            AddDockItem(TabControls.DebtorAccountPage2, param, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("DebtorAccount"), dc._Account));
        }

        bool editAllChecked;
        private void EditAll()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            var iBase = UtilDisplay.GetMenuCommandByName(rb, "EditAll");
            if (iBase == null) return;

            if (dgProjInvProposalGrid.Readonly)
            {
                dgProjInvProposalGrid.MakeEditable();
                UserFieldControl.MakeEditable(dgProjInvProposalGrid);
                iBase.Caption = Uniconta.ClientTools.Localization.lookup("LeaveEditAll");
                ribbonControl.EnableButtons(new string[] { "UndoDelete", "DeleteRow", "SaveGrid" });
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
                                await dgProjInvProposalGrid.SaveData();
                                break;
                            case CWConfirmationBox.ConfirmationResultEnum.No:
                                dgProjInvProposalGrid.CancelChanges();
                                break; 
                        }
                        editAllChecked = true;
                        dgProjInvProposalGrid.Readonly = true;
                        dgProjInvProposalGrid.tableView.CloseEditor();
                        iBase.Caption = Uniconta.ClientTools.Localization.lookup("EditAll");
                        ribbonControl.DisableButtons(new string[] { "UndoDelete", "DeleteRow", "SaveGrid" });
                    };
                    confirmationDialog.Show();
                }
                else
                {
                    dgProjInvProposalGrid.Readonly = true;
                    dgProjInvProposalGrid.tableView.CloseEditor();
                    iBase.Caption = Uniconta.ClientTools.Localization.lookup("EditAll");
                    ribbonControl.DisableButtons(new string[] { "UndoDelete", "DeleteRow", "SaveGrid" });
                }
            }
        }

        public override bool IsDataChaged
        {
            get
            {
                return editAllChecked ? false : dgProjInvProposalGrid.HasUnsavedData;
            }
        }

        private void Save()
        {
            dgProjInvProposalGrid.SaveData();
        }

        private Task BindGrid()
        {
            return dgProjInvProposalGrid.Filter(null);
        }

        protected override void LoadCacheInBackGround()
        {
            var orders = api.GetCache(typeof(Uniconta.DataModel.ProjectInvoiceProposal));
            TestDebtorReload(false, orders?.GetNotNullArray as IEnumerable<ProjectInvoiceProposal>);
            var lst = new List<Type>(15) { typeof(Uniconta.DataModel.Debtor), typeof(Uniconta.DataModel.Employee), typeof(Uniconta.DataModel.GLVat) };
            var Comp = api.CompanyEntity;
            if (Comp.Contacts)
                lst.Add(typeof(Uniconta.DataModel.Contact));
            if (Comp.DeliveryAddress)
                lst.Add(typeof(Uniconta.DataModel.WorkInstallation));
            if (Comp.InvPrice)
                lst.Add(typeof(Uniconta.DataModel.DebtorPriceList));
            if (Comp.ItemVariants)
            {
                lst.Add(typeof(Uniconta.DataModel.InvVariant1));
                lst.Add(typeof(Uniconta.DataModel.InvVariant2));
                var n = Comp.NumberOfVariants;
                if (n >= 3)
                    lst.Add(typeof(Uniconta.DataModel.InvVariant3));
                if (n >= 4)
                    lst.Add(typeof(Uniconta.DataModel.InvVariant4));
                if (n >= 5)
                    lst.Add(typeof(Uniconta.DataModel.InvVariant5));
                lst.Add(typeof(Uniconta.DataModel.InvStandardVariant));
            }
            lst.Add(typeof(Uniconta.DataModel.InvGroup));
            lst.Add(typeof(Uniconta.DataModel.InvItem));
            lst.Add(typeof(Uniconta.DataModel.ProjectTask));

            LoadType(lst);
        }

        private void GenerateInvoice(ProjectInvoiceProposalClient projInvProp, CompanyLayoutType companyLayout)
        {
            InvoiceAPI Invapi = new InvoiceAPI(api);
            bool showSendByMail = false;

            var debtor = ClientHelper.GetRef(projInvProp.CompanyId, typeof(Debtor), projInvProp._DCAccount) as Debtor;
            if (debtor != null)
            {
                var InvoiceAccount = projInvProp._InvoiceAccount ?? debtor._InvoiceAccount;
                if (InvoiceAccount != null)
                    debtor = ClientHelper.GetRef(projInvProp.CompanyId, typeof(Debtor), InvoiceAccount) as Debtor;
                if (debtor != null)
                {
                    if (debtor._PricesInclVat != projInvProp._PricesInclVat)
                    {
                        var confirmationMsgBox = UnicontaMessageBox.Show(string.Format("{0}.\n{1}", string.Format(Uniconta.ClientTools.Localization.lookup("DebtorAndOrderMix"), Uniconta.ClientTools.Localization.lookup("InclVat")),
                        Uniconta.ClientTools.Localization.lookup("ProceedConfirmation")), Uniconta.ClientTools.Localization.lookup("Confirmation"), MessageBoxButton.OKCancel);
                        if (confirmationMsgBox != MessageBoxResult.OK)
                            return;
                    }
                    if (!api.CompanyEntity.SameCurrency(projInvProp._Currency, debtor._Currency))
                    {
                        var confirmationMsgBox = UnicontaMessageBox.Show(string.Format("{0}.\n{1}", string.Format(Uniconta.ClientTools.Localization.lookup("CurrencyMismatch"), AppEnums.Currencies.ToString((int)debtor._Currency), projInvProp.Currency),
                        Uniconta.ClientTools.Localization.lookup("ProceedConfirmation")), Uniconta.ClientTools.Localization.lookup("Confirmation"), MessageBoxButton.OKCancel);
                        if (confirmationMsgBox != MessageBoxResult.OK)
                            return;
                    }
                    showSendByMail = (!string.IsNullOrEmpty(debtor._InvoiceEmail) || debtor._EmailDocuments);
                }
            }
            else
                api.LoadCache(typeof(Debtor), true);

            bool invoiceInXml, showUpdateInv, isOrderOrQuickUpdate, showSimulation;
            string docType;
            int dialogId;
            if (companyLayout != CompanyLayoutType.Packnote)
            {
                docType = string.Empty;
                showSimulation = true;
                invoiceInXml = debtor != null && debtor.IsPeppolSupported && debtor._einvoice;
                showUpdateInv = false;
                isOrderOrQuickUpdate = true;
                dialogId = 2000000085;
            }
            else
            {
                docType = companyLayout.ToString();
                showSimulation = false;
                invoiceInXml = false;
                showUpdateInv = api.CompanyEntity.Storage || api.CompanyEntity.Packnote;
                isOrderOrQuickUpdate = false;
                dialogId = 2000000104;
            }

            string debtorName = debtor?._Name ?? projInvProp._DCAccount;
            var accountName = Util.ConcatParenthesis(projInvProp._DCAccount, projInvProp.Name);
            CWGenerateInvoice GenrateInvoiceDialog = new CWGenerateInvoice(showSimulation, docType, false, true, true, showNoEmailMsg: !showSendByMail, debtorName: debtorName,
                isOrderOrQuickInv: isOrderOrQuickUpdate, isShowUpdateInv: showUpdateInv, isDebtorOrder: true, InvoiceInXML: invoiceInXml, AccountName: accountName);
            GenrateInvoiceDialog.DialogTableId = dialogId;
            if (projInvProp._InvoiceDate != DateTime.MinValue)
                GenrateInvoiceDialog.SetInvoiceDate(projInvProp._InvoiceDate);
            var additionalOrdersList = Utility.GetAdditionalOrders(api, projInvProp);
            if (additionalOrdersList != null)
                GenrateInvoiceDialog.SetAdditionalOrders(additionalOrdersList);
            if (!api.CompanyEntity._DeactivateSendNemhandel)
            {
                GenrateInvoiceDialog.SetOIOUBLLabelText(true);
                GenrateInvoiceDialog.EnableSentEinvoice(api.CompanyEntity._OIOUBLSendOnServer);
            }

            GenrateInvoiceDialog.Closed += async delegate
            {
                if (GenrateInvoiceDialog.DialogResult == true)
                {
                    busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("SendingWait");
                    busyIndicator.IsBusy = true;
                    var isSimulated = GenrateInvoiceDialog.IsSimulation;
                    var invoicePostingResult = new InvoicePostingPrintGenerator(api, this);
                    invoicePostingResult.SetUpInvoicePosting(projInvProp, null, companyLayout, GenrateInvoiceDialog.GenrateDate, null, isSimulated, GenrateInvoiceDialog.ShowInvoice, GenrateInvoiceDialog.PostOnlyDelivered,
                        GenrateInvoiceDialog.InvoiceQuickPrint, GenrateInvoiceDialog.NumberOfPages, GenrateInvoiceDialog.SendByEmail, !isSimulated && GenrateInvoiceDialog.SendByOutlook, GenrateInvoiceDialog.sendOnlyToThisEmail,
                        GenrateInvoiceDialog.Emails, GenrateInvoiceDialog.GenerateOIOUBLClicked, null, false);
                    invoicePostingResult.SetAdditionalOrders(GenrateInvoiceDialog.AdditionalOrders?.Cast<DCOrder>().ToList());
                    busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("GeneratingPage");
                    busyIndicator.IsBusy = true;
                    var result = await invoicePostingResult.Execute();
                    busyIndicator.IsBusy = false;

                    if (result)
                    {
                        if (invoicePostingResult.PostingResult.OrderDeleted)
                            dgProjInvProposalGrid.UpdateItemSource(3, dgProjInvProposalGrid.SelectedItem as ProjectInvoiceProposalClient);
                    }
                    else
                        Utility.ShowJournalError(invoicePostingResult.PostingResult.ledgerRes, dgProjInvProposalGrid);
                }
            };
            GenrateInvoiceDialog.Show();
        }
        private void ProjectName_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            dgProjInvProposalGrid_RowDoubleClick();
        }

        private void HasDocImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var obj = (sender as Image).Tag as ProjectInvoiceProposalClient;
            if (obj != null)
                AddDockItem(TabControls.UserDocsPage, dgProjInvProposalGrid.syncEntity);
        }

        private void HasNoteImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var obj = (sender as Image).Tag as ProjectInvoiceProposalClient;
            if (obj != null)
                AddDockItem(TabControls.UserNotesPage, dgProjInvProposalGrid.syncEntity);
        }
    }
}
