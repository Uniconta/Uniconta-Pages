using DevExpress.Xpf.Grid;
using System.Linq;
using System;
using System.Windows;
using Uniconta.API.Service;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using UnicontaClient.Controls.Dialogs;
using UnicontaClient.Models;
using Uniconta.DataModel;
using Uniconta.Common.Utility;
using Uniconta.Client.Pages;
using System.Collections.Generic;
using Uniconta.ClientTools.Controls;
using DevExpress.Data;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class InvTransClientNoBatch : InvTransClient
    {
        [Display(Name = "QtyMarked", ResourceType = typeof(InvTransText))]
        public double? QtyMarked { get { return _QtyOrdered; } }
    }

    public class InventoryTransactionNoBatchGrid : CorasauDataGridClient
    {
        public override Type TableType => typeof(InvTransClientNoBatch);
    }

    /// <summary>
    /// Interaction logic for InventoryTransactionsWithoutBatch.xaml
    /// </summary>
    public partial class InventoryTransactionsWithoutBatch : GridBasePage
    {
        static DateTime fromDate, toDate;
        public InventoryTransactionsWithoutBatch(BaseAPI API) : base(API, string.Empty)
        {
            InitializePage();
        }

        public override string NameOfControl => TabControls.InventoryTransactionsWithoutBatch;

        private void InitializePage()
        {
            this.DataContext = this;
            InitializeComponent();
            ((TableView)dgInvTransNoBatchGrid.View).RowStyle = this.Resources["SubTotalRowStyle"] as Style;
            var Comp = api.CompanyEntity;

            if (fromDate == DateTime.MinValue)
            {
                var threeMontheOld = DateTime.Now.AddMonths(-3);
                fromDate = new DateTime(threeMontheOld.Year, threeMontheOld.Month, 1);
            }

            if (toDate == DateTime.MinValue)
                toDate = BasePage.GetSystemDefaultDate();

            txtDateFrm.DateTime = fromDate;
            txtDateTo.DateTime = toDate;

            localMenu.dataGrid = dgInvTransNoBatchGrid;
            SetRibbonControl(localMenu, dgInvTransNoBatchGrid);
            dgInvTransNoBatchGrid.api = api;
            dgInvTransNoBatchGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            if (Comp.InvBOM)
            {
                ribbonControl.DisableButtons("TransInBOM");
                ribbonControl.DisableButtons("SeriesBatchInBom");
            }
            dgInvTransNoBatchGrid.SelectedItemChanged += DgInvTransNoBatchGrid_SelectedItemChanged;
            dgInvTransNoBatchGrid.CustomSummary += DgInvTransNoBatchGrid_CustomSummary; ;
            dgInvTransNoBatchGrid.ShowTotalSummary();
            LoadNow(typeof(Uniconta.DataModel.InvItem));
            if (Comp.RoundTo100)
                CostValue.HasDecimals = NetAmount.HasDecimals = Total.HasDecimals = Margin.HasDecimals = false;
        }

        async public override Task InitQuery()
        {
            try
            {
                var reportApi = new Uniconta.API.Inventory.ReportAPI(api);
                busyIndicator.IsBusy = true;
                var invTrans = (InvTransClientNoBatch[])await reportApi.TransWithMissingBatch(new InvTransClientNoBatch(), null, txtDateFrm.DateTime, txtDateTo.DateTime);
                busyIndicator.IsBusy = false;
                dgInvTransNoBatchGrid.ItemsSource = invTrans;
                dgInvTransNoBatchGrid.Visibility = Visibility.Visible;
            }
            catch { busyIndicator.IsBusy = false; }
        }

        double sumMargin, sumSales, sumMarginRatio;
        private void DgInvTransNoBatchGrid_CustomSummary(object sender, DevExpress.Data.CustomSummaryEventArgs e)
        {
            var fieldName = ((GridSummaryItem)e.Item).FieldName;
            switch (e.SummaryProcess)
            {
                case CustomSummaryProcess.Start:
                    sumMargin = sumSales = 0d;
                    break;
                case CustomSummaryProcess.Calculate:
                    var row = e.Row as InvTransClient;
                    sumSales += row.SalesPrice;
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

        private void DgInvTransNoBatchGrid_SelectedItemChanged(object sender, SelectedItemChangedEventArgs e)
        {
            var selectedItem = dgInvTransNoBatchGrid.SelectedItem as InvTransClient;
            if (selectedItem == null)
                return;
            if (selectedItem._MovementType == (byte)InvMovementType.ReportAsFinished)
            {
                ribbonControl.EnableButtons("TransInBOM");
                ribbonControl.EnableButtons("SeriesBatchInBom");
            }
            else
            {
                ribbonControl.DisableButtons("TransInBOM");
                ribbonControl.DisableButtons("SeriesBatchInBom");
            }

            if (selectedItem._MovementType == (byte)InvMovementType.ReportAsFinished || selectedItem._MovementType == (byte)InvMovementType.IncludedInBOM)
                ribbonControl.EnableButtons("ProductionPosted");
            else
                ribbonControl.DisableButtons("ProductionPosted");

            if (selectedItem._InvoiceRowId != 0)
                ribbonControl.EnableButtons("Invoice");
            else
                ribbonControl.DisableButtons("Invoice");
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            bool ShowDC = true, ShowItem = true, ShowSalesPrice = true, ShowOrderNumber = true;
            var master = dgInvTransNoBatchGrid.masterRecords?.First();
            if (master != null)
            {
                var dcacc = master as DCAccount;
                if (dcacc != null)
                {
                    if (dcacc._Account != null)
                        ShowDC = false;
                    if (dcacc.__DCType() == 2)
                        ShowSalesPrice = false;
                }
                else if (master is InvItem)
                {
                    ShowItem = false;
                }
                else
                {
                    var dord = master as DCOrder;
                    if (dord != null)
                    {
                        ShowOrderNumber = false;
                        if (dord.__DCType() == 2)
                            ShowSalesPrice = false;
                    }
                }
            }
            DCAccount.Visible = AccountName.Visible = ShowDC;
            Item.Visible = ShowItem;
            SalesPrice.Visible = ShowSalesPrice;
            OrderNumber.Visible = ShowOrderNumber;

            var company = api.CompanyEntity;
            if (!company.Location || !company.Warehouse)
                Location.Visible = Location.ShowInColumnChooser = false;
            else
                Location.ShowInColumnChooser = true;
            if (!company.Warehouse)
                Warehouse.Visible = Warehouse.ShowInColumnChooser = false;
            else
                Warehouse.ShowInColumnChooser = true;
            if (!company.Project)
            {
                Project.Visible = Project.ShowInColumnChooser = false;
                ProjectName.Visible = ProjectName.ShowInColumnChooser = false;
                WorkSpace.Visible = WorkSpace.ShowInColumnChooser = false;
                PrCategory.Visible = PrCategory.ShowInColumnChooser = false;
            }

            if (!company.ProjectTask)
                Task.Visible = Task.ShowInColumnChooser = false;

            Utilities.Utility.SetupVariants(api, colVariant, VariantName, colVariant1, colVariant2, colVariant3, colVariant4, colVariant5, Variant1Name, Variant2Name, Variant3Name, Variant4Name, Variant5Name);
            Utilities.Utility.SetDimensionsGrid(api, cldim1, cldim2, cldim3, cldim4, cldim5);
            if (company.HideCostPrice)
            {
                Margin.Visible = Margin.ShowInColumnChooser = MarginRatio.Visible = MarginRatio.ShowInColumnChooser =
           CostPrice.Visible = CostPrice.ShowInColumnChooser = CostValue.Visible = CostValue.ShowInColumnChooser = false;
            }
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgInvTransNoBatchGrid.SelectedItem as InvTransClient;
            switch (ActionType)
            {
                case "SeriesBatch":
                    if (selectedItem != null)
                        AddDockItem(TabControls.InvSeriesBatch, dgInvTransNoBatchGrid.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("SerialBatchNumbers"), selectedItem._Item));
                    break;
                case "AttachSerialBatch":
                    if (selectedItem != null)
                        AddDockItem(TabControls.AttachInvSeriesBatch, selectedItem, Uniconta.ClientTools.Localization.lookup("SerialBatchNumbers"));
                    break;
                case "ChangeVariant":
                    if (selectedItem == null)
                        return;
                    var cwChangeVaraints = new CWModifyVariants(api, selectedItem);
                    cwChangeVaraints.Closing += delegate
                    {
                        if (cwChangeVaraints.DialogResult == true)
                        {
                            gridRibbon_BaseActions("RefreshGrid");
                        }
                    };
                    cwChangeVaraints.Show();
                    break;
                case "ChangeStorage":
                    if (selectedItem == null)
                        return;
                    var cwchangeStorage = new CWModiyStorage(api);
                    cwchangeStorage.Closing += async delegate
                    {
                        if (cwchangeStorage.DialogResult == true)
                        {
                            var newWarehouse = cwchangeStorage.Warehouse;
                            var newLocation = cwchangeStorage.Location;
                            var tranApi = new Uniconta.API.Inventory.TransactionsAPI(api);
                            ErrorCodes result;
                            if (cwchangeStorage.AllLines)
                            {
                                var visibleItems = dgInvTransNoBatchGrid.VisibleItems.Cast<InvTransClient>();
                                result = await tranApi.ChangeStorage(visibleItems, newWarehouse, newLocation);
                            }
                            else
                                result = await tranApi.ChangeStorage(selectedItem, newWarehouse, newLocation);

                            if (result == ErrorCodes.Succes)
                                gridRibbon_BaseActions("RefreshGrid");
                            else
                                UtilDisplay.ShowErrorCode(result);
                        }

                    };
                    cwchangeStorage.Show();
                    break;
                case "VoucherTransactions":
                    if (selectedItem == null)
                        return;
                    string arg;
                    if (selectedItem._JournalPostedId != 0)
                        arg = string.Format("{0}={1}", Uniconta.ClientTools.Localization.lookup("JournalPostedId"), selectedItem._JournalPostedId);
                    else if (selectedItem._InvoiceNumber != 0)
                        arg = string.Format("{0}={1}", Uniconta.ClientTools.Localization.lookup("Invoice"), selectedItem._InvoiceNumber);
                    else if (selectedItem._InvJournalPostedId != 0)
                        arg = string.Format("{0} ({1})={2}", Uniconta.ClientTools.Localization.lookup("JournalPostedId"), Uniconta.ClientTools.Localization.lookup("Inventory"), selectedItem._InvJournalPostedId);
                    else
                        arg = string.Format("{0}={1}", Uniconta.ClientTools.Localization.lookup("Account"), selectedItem.AccountName);
                    string vheader = Util.ConcatParenthesis(Uniconta.ClientTools.Localization.lookup("VoucherTransactions"), arg);
                    AddDockItem(TabControls.AccountsTransaction, dgInvTransNoBatchGrid.syncEntity, vheader);
                    break;
                case "Invoice":
                    if (selectedItem == null) return;
                    if (selectedItem._MovementType == (byte)InvMovementType.Debtor)
                    {
                        var debtHeader = Util.ConcatParenthesis(Uniconta.ClientTools.Localization.lookup("DebtorInvoice"), selectedItem._Item);
                        AddDockItem(TabControls.Invoices, selectedItem, debtHeader);
                    }
                    else if (selectedItem._MovementType == (byte)InvMovementType.Creditor)
                    {
                        var credHeader = Util.ConcatParenthesis(Uniconta.ClientTools.Localization.lookup("CreditorInvoice"), selectedItem._Item);
                        AddDockItem(TabControls.CreditorInvoice, selectedItem, credHeader);
                    }
                    break;
                case "ViewDownloadRow":
                    if (selectedItem != null)
                        DebtorTransactions.ShowVoucher(dgInvTransNoBatchGrid.syncEntity, api, busyIndicator);
                    break;
                case "PostedBy":
                    if (selectedItem != null)
                        JournalPosted(selectedItem);
                    break;
                case "Search":
                    InitQuery();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        async private void JournalPosted(InvTransClient selectedItem)
        {
            var result = await api.Query(new GLDailyJournalPostedClient(), new UnicontaBaseEntity[] { selectedItem }, null);
            if (result != null && result.Length == 1)
            {
                var cwPostedClient = new CWGLPostedClientFormView(result[0]);
                cwPostedClient.Show();
            }
        }

        protected override void LoadCacheInBackGround()
        {
            var Comp = api.CompanyEntity;
            var lst = new List<Type>(20);
            if (Comp.Warehouse)
                lst.Add(typeof(Uniconta.DataModel.InvWarehouse));
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
            lst.Add(typeof(Uniconta.DataModel.GLVat));
            lst.Add(typeof(Uniconta.DataModel.Employee));
            lst.Add(typeof(Uniconta.DataModel.Debtor));
            lst.Add(typeof(Uniconta.DataModel.Creditor));
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
            lst.Add(typeof(Uniconta.DataModel.InvItem));
            LoadType(lst);
        }

        protected override LookUpTable HandleLookupOnLocalPage(LookUpTable lookup, CorasauDataGrid dg)
        {
            var trans = dg.SelectedItem as InvTransClient;
            if (trans == null)
                return lookup;
            if (dg.CurrentColumn?.Name == "DCAccount")
            {
                switch ((int)trans._MovementType)
                {
                    case 1:
                        lookup.TableType = typeof(Uniconta.DataModel.Debtor);
                        break;
                    case 2:
                        lookup.TableType = typeof(Uniconta.DataModel.Creditor);
                        break;
                }
            }
            else if (dg.CurrentColumn?.Name == "Item")
            {
                lookup.TableType = typeof(Uniconta.DataModel.InvItem);
            }
            return lookup;
        }
    }

}
