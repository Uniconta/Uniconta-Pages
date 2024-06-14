using UnicontaClient.Models;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.Common.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Uniconta.DataModel;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools;
using System.Windows.Data;
using DevExpress.Xpf.Grid;
using UnicontaClient.Controls.Dialogs;
using Uniconta.ClientTools.Util;
using Uniconta.API.DebtorCreditor;
using DevExpress.Data;
using Uniconta.API.Service;
using System.Windows.Input;
using Uniconta.Client.Pages;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class InvTransactionGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return IsProject ? typeof(InvTransProject) : typeof(InvTransClient); } }
        public bool IsProject;
    }

    public class SubTotalRowStyleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool val = (bool)value;
            if (val)
                return FontWeights.Bold;
            else
                return FontWeights.Normal;
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value;
        }
    }
    public partial class InventoryTransactions : GridBasePage
    {
        private SynchronizeEntity syncEntity;
        public override string NameOfControl { get { return TabControls.InventoryTransactions; } }
        bool AddFilterAndSort;
        DateTime filterDate;

        protected override Filter[] DefaultFilters()
        {
            if (AddFilterAndSort)
            {
                Filter dateFilter = new Filter() { name = "Date", value = String.Format("{0:d}..", filterDate) };
                return new Filter[] { dateFilter };
            }
            return base.DefaultFilters();
        }

        protected override SortingProperties[] DefaultSort()
        {
            if (AddFilterAndSort)
            {
                SortingProperties dateSort = new SortingProperties("Date") { Ascending = false };
                SortingProperties lineSort = new SortingProperties("LineNumber") { Ascending = false };
                return new SortingProperties[] { dateSort, lineSort };
            }
            return base.DefaultSort();
        }

        public InventoryTransactions(BaseAPI API) : base(API, string.Empty)
        {
            InitializePage(null);
        }
        public InventoryTransactions(UnicontaBaseEntity master)
            : base(null)
        {
            InitializePage(master);
        }
        public InventoryTransactions(SynchronizeEntity syncEntity)
            : base(syncEntity, true)
        {
            this.syncEntity = syncEntity;
            InitializePage(syncEntity.Row);
            SetHeader();
        }
        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            var invTrans = args as InvTrans;
            if (invTrans != null && invTrans._MovementType != (byte)InvMovementType.ReportAsFinished)
                return;

            dgInvTransGrid.UpdateMaster(args);
            SetHeader();
            InitQuery();
        }
        void SetHeader()
        {
            string id;
            var syncMaster = dgInvTransGrid.masterRecord as DCAccount;
            if (syncMaster != null)
                id = syncMaster._Account;
            else
            {
                var syncMaster2 = dgInvTransGrid.masterRecord as InvItem;
                if (syncMaster2 != null)
                    id = syncMaster2._Item;
                else
                {
                    var syncMaster3 = dgInvTransGrid.masterRecord as DCOrder;
                    if (syncMaster3 != null)
                        id = Convert.ToString(syncMaster3._OrderNumber);
                    else
                    {
                        var syncMaster4 = dgInvTransGrid.masterRecord as InvTrans;
                        if (syncMaster4 != null)
                            id = string.Format("{0}/{1}", syncMaster4._DCAccount, syncMaster4._Item);
                        else
                            return;
                    }
                }
            }
            string header = string.Format("{0}/{1}", Uniconta.ClientTools.Localization.lookup("InvTransactions"), id);
            SetHeader(header);
        }
        private void InitializePage(UnicontaBaseEntity master)
        {
            this.DataContext = this;
            AddFilterAndSort = !(master is InvTrans);
            InitializeComponent();
            dgInvTransGrid.UpdateMaster(master);
            ((TableView)dgInvTransGrid.View).RowStyle = this.Resources["SubTotalRowStyle"] as Style;

            var Comp = api.CompanyEntity;

            filterDate = BasePage.GetFilterDate(Comp, master != null);
            if (filterDate == DateTime.MinValue)
                AddFilterAndSort = false;

            localMenu.dataGrid = dgInvTransGrid;
            SetRibbonControl(localMenu, dgInvTransGrid);
            dgInvTransGrid.api = api;
            dgInvTransGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            if (Comp.InvBOM)
            {
                ribbonControl.DisableButtons("TransInBOM");
                ribbonControl.DisableButtons("SeriesBatchInBom");
            }
            dgInvTransGrid.SelectedItemChanged += DgInvTransGrid_SelectedItemChanged;
            RemoveMenuItem();
            dgInvTransGrid.CustomSummary += DgInvTransGrid_CustomSummary;
            dgInvTransGrid.ShowTotalSummary();
            LoadNow(typeof(Uniconta.DataModel.InvItem));
            if (Comp.RoundTo100)
                CostValue.HasDecimals = NetAmount.HasDecimals = Total.HasDecimals = Margin.HasDecimals = false;
        }

        public override void SetParameter(IEnumerable<ValuePair> Parameters)
        {
            foreach (var rec in Parameters)
            {
                if (rec.Name == null || rec.Name == "Master")
                {
                    if (rec.Value == "Project")
                    {
                        dgInvTransGrid.IsProject = true;
                    }
                }
            }
            base.SetParameter(Parameters);
        }

        double sumMargin, sumSales, sumMarginRatio;
        private void DgInvTransGrid_CustomSummary(object sender, DevExpress.Data.CustomSummaryEventArgs e)
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

        private void DgInvTransGrid_SelectedItemChanged(object sender, SelectedItemChangedEventArgs e)
        {
            var selectedItem = dgInvTransGrid.SelectedItem as InvTransClient;
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

        void RemoveMenuItem()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            var Comp = api.CompanyEntity;
            if (!Comp.InvBOM)
                UtilDisplay.RemoveMenuCommand(rb, "SeriesBatchInBom");
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            bool ShowDC = true, ShowItem = true, ShowSalesPrice = true, ShowOrderNumber = true;
            var master = dgInvTransGrid.masterRecords?.First();
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
                        AddFilterAndSort = false;
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
            var selectedItem = dgInvTransGrid.SelectedItem as InvTransClient;
            switch (ActionType)
            {
                case "SeriesBatch":
                    if (selectedItem != null)
                        AddDockItem(TabControls.InvSeriesBatch, dgInvTransGrid.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("SerialBatchNumbers"), selectedItem._Item));
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
                                var visibleItems = dgInvTransGrid.VisibleItems.Cast<InvTransClient>();
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
                    AddDockItem(TabControls.AccountsTransaction, dgInvTransGrid.syncEntity, vheader);
                    break;
                case "TransInBOM":
                    if (selectedItem != null && selectedItem._MovementType == (byte)InvMovementType.ReportAsFinished)
                    {
                        string header = Util.ConcatParenthesis(Uniconta.ClientTools.Localization.lookup("TransInBOM"), selectedItem._Item);
                        AddDockItem(TabControls.InventoryTransactions, dgInvTransGrid.syncEntity, header);
                    }
                    break;
                case "SeriesBatchInBom":
                    if (selectedItem != null && selectedItem._MovementType == (byte)InvMovementType.ReportAsFinished)
                    {
                        string header = Util.ConcatParenthesis(Uniconta.ClientTools.Localization.lookup("SeriesBatchInBom"), selectedItem._Item);
                        AddDockItem(TabControls.InventoryTransactionStatement, selectedItem, header);
                    }
                    break;
                case "MarkInvTrans":
                    if (selectedItem != null)
                        MarkInvTrans(selectedItem);
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
                        DebtorTransactions.ShowVoucher(dgInvTransGrid.syncEntity, api, busyIndicator);
                    break;
                case "ProductionPosted":
                    if (selectedItem != null)
                        AddDockItem(TabControls.ProductionPostedGridPage, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("ProductionPosted"), selectedItem._Item));
                    break;
                case "PostedBy":
                    if (selectedItem != null)
                        JournalPosted(selectedItem);
                    break;
                case "MarkOrderLineAgnstInvTrans":
                    if (selectedItem?._Item == null) return;
                    object[] param = new object[] { selectedItem };
                    AddDockItem(TabControls.InventoryTransactionsMarkedPage, param, true);
                    break;
                case "ChangeCostValue":
                    DebtorInvoiceLinesPage.ChangeCostValue(selectedItem, dgInvTransGrid);
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
                CWGLPostedClientFormView cwPostedClient = new CWGLPostedClientFormView(result[0]);
                cwPostedClient.Show();
            }
        }

        async void MarkInvTrans(InvTransClient selectedItem)
        {
            OrderAPI orderApi = new OrderAPI(api);
            busyIndicator.IsBusy = true;
            var invTrans = new InvTransClient();
            var res = await orderApi.GetMarkedInvTrans(selectedItem, invTrans);
            busyIndicator.IsBusy = false;
            if (res == ErrorCodes.Succes)
            {
                object[] paramArr = new object[] { api, invTrans };
                AddDockItem(TabControls.InvTransMarkedPage, paramArr, string.Format("{0}:{1}", Uniconta.ClientTools.Localization.lookup("InvTransactions"), invTrans._OrderNumber));
            }
            else
                UtilDisplay.ShowErrorCode(res);
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
