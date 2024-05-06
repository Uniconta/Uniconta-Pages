using Uniconta.API.DebtorCreditor;
using UnicontaClient.Models;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.DataModel;
using System;
using System.Collections;
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
using System.Windows.Threading;
using Uniconta.ClientTools.Util;
using Uniconta.API.GeneralLedger;
using System.Text;
using UnicontaClient.Utilities;
using DevExpress.Xpf.Grid;
using UnicontaClient.Controls.Dialogs;
using UnicontaClient.Pages;
using Uniconta.Common.Utility;
using Uniconta.API.Service;
using DevExpress.Mvvm.Native;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class ProjReservationLineGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(ProjectReservationLineClient); } }
        public override bool SingleBufferUpdate { get { return false; } }
        public override IComparer GridSorting { get { return new DCOrderLineSort(); } }
        public override string LineNumberProperty { get { return "_LineNumber"; } }
        public override bool AllowSort { get { return false; } }
        public override bool Readonly { get { return false; } }
        public override bool AddRowOnPageDown()
        {
            var selectedItem = (DCOrderLine)this.SelectedItem;
            return (selectedItem != null) && (selectedItem._Item != null || selectedItem._Text != null);
        }

        public override IEnumerable<UnicontaBaseEntity> ConvertPastedRows(IEnumerable<UnicontaBaseEntity> copyFromRows)
        {
            var row = copyFromRows.FirstOrDefault();
            var type = this.TableTypeUser;
            List<ProjectReservationLineClient> lst = null;
            if (row is InvTrans)
            {
                lst = new List<ProjectReservationLineClient>();
                foreach (var _it in copyFromRows)
                {
                    var it = (InvTrans)_it;
                    lst.Add(CreateNewReservationLine(it._Item, it.MovementTypeEnum == InvMovementType.Debtor ? -it._Qty : it._Qty, it._Text, it._Price, it.MovementTypeEnum == InvMovementType.Debtor ? -it._AmountEntered : it._AmountEntered,
                        it._DiscountPct, it._Discount, it._Variant1, it._Variant2, it._Variant3, it._Variant4, it._Variant5, it._Unit, it._Date, it._Week, it._Note));
                }
            }
            else if (row is DCOrderLine)
            {
                lst = new List<ProjectReservationLineClient>();
                foreach (var _it in copyFromRows)
                {
                    var it = (DCOrderLine)_it;
                    var line = CreateNewReservationLine(it._Item, it._Qty, it._Text, it._Price, it._AmountEntered, it._DiscountPct, it._Discount, it._Variant1, it._Variant2, it._Variant3, it._Variant4, it._Variant5, it._Unit,
                        it._Date, it._Week, it._Note);
                    TableField.SetUserFieldsFromRecord(it, line);
                    lst.Add(line);
                }
            }
            else if (row is InvItemClient)
            {
                lst = new List<ProjectReservationLineClient>();
                foreach (var _it in copyFromRows)
                {
                    double qty = (double)_it.GetType().GetProperty("Qty").GetValue(_it, null);
                    var it = (InvItemClient)_it;
                    lst.Add(CreateNewReservationLine(it._Item, qty, null, 0d, 0d, 0d, 0d, null, null, null, null, null, 0, DateTime.MinValue, 0, null));
                }
            }
            return lst;
        }

        private ProjectReservationLineClient CreateNewReservationLine(string item, double qty, string text, double price, double amountEntered, double discPct, double disc, string variant1, string variant2, string variant3, string variant4, string variant5,
           ItemUnit unit, DateTime date, byte week, string note)
        {
            var orderline = Activator.CreateInstance(this.TableTypeUser) as ProjectReservationLineClient;
            orderline._Qty = qty;
            orderline._Item = item;
            orderline._Text = text;
            orderline._Price = price;
            orderline._AmountEntered = amountEntered;
            orderline._DiscountPct = discPct;
            orderline._Discount = disc;
            orderline._Variant1 = variant1;
            orderline._Variant2 = variant2;
            orderline._Variant3 = variant3;
            orderline._Variant4 = variant4;
            orderline._Variant5 = variant5;
            orderline._Unit = unit;
            orderline._Date = date;
            orderline._Week = week;
            orderline._Note = note;
            orderline._Storage = StorageRegister.Register;
            return orderline;
        }

        public override bool ClearSelectedItemOnSave { get { return false; } }
        public bool allowSave = true;
        public override bool AllowSave { get { return allowSave; } }
    }

    public partial class ProjReservationLine : GridBasePage
    {
        SQLCache items, standardVariants, variants1, variants2, employees;
        ProjectReservation Order { get { return dgProjReservationLineGrid.masterRecord as ProjectReservation; } }
        FindPrices PriceLookup;
        Company company;
        double exchangeRate;

        public override void PageClosing()
        {
            globalEvents.OnRefresh(NameOfControl, Order);
            base.PageClosing();
        }

        public ProjReservationLine(UnicontaBaseEntity master)
           : base(master)
        {
            Init(master);
        }
        public ProjReservationLine(SynchronizeEntity syncEntity)
           : base(syncEntity, false)
        {
            Init(syncEntity.Row);
        }

        public void Init(UnicontaBaseEntity master)
        {
            InitializeComponent();
            company = api.CompanyEntity;
            ((TableView)dgProjReservationLineGrid.View).RowStyle = Application.Current.Resources["StyleRow"] as Style;
            localMenu.dataGrid = dgProjReservationLineGrid;
            SetRibbonControl(localMenu, dgProjReservationLineGrid);
            dgProjReservationLineGrid.api = api;
            SetupMaster(master);
            dgProjReservationLineGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgProjReservationLineGrid.SelectedItemChanged += dgProjReservationLineGrid_SelectedItemChanged;

            InitialLoad();
            dgProjReservationLineGrid.ShowTotalSummary();
            dgProjReservationLineGrid.CustomSummary += dgProjReservationLineGrid_CustomSummary;
            this.KeyDown += Page_KeyDown;
        }

        private void Page_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F8)
                ribbonControl.PerformRibbonAction("AddItems");
        }

        double sumCost, sumSales;
        private void dgProjReservationLineGrid_CustomSummary(object sender, DevExpress.Data.CustomSummaryEventArgs e)
        {
            var fieldName = ((GridSummaryItem)e.Item).FieldName;
            switch (e.SummaryProcess)
            {
                case DevExpress.Data.CustomSummaryProcess.Start:
                    sumCost = sumSales = 0d;
                    break;
                case DevExpress.Data.CustomSummaryProcess.Calculate:
                    var row = e.Row as ProjectReservationLineClient;
                    sumSales += row.SalesValue;
                    sumCost += row.CostValue;
                    break;
                case DevExpress.Data.CustomSummaryProcess.Finalize:
                    if (fieldName == "MarginRatio" && sumSales > 0)
                        e.TotalValue = Math.Round((sumSales - sumCost) * 100d / sumSales, 2);
                    break;
            }
        }

        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            SetupMaster(args);
            SetHeader();
            InitQuery();
        }

        void SetupMaster(UnicontaBaseEntity args)
        {
            PriceLookup = null;
            var OrderId = Order?.RowId;
            dgProjReservationLineGrid.UpdateMaster(args);
            if (Order?.RowId != OrderId)
                PriceLookup = new Uniconta.API.DebtorCreditor.FindPrices(Order, api);
        }

        void SetHeader()
        {
            var syncMaster = Order;
            string header = null;
            if (syncMaster != null)
                header = string.Format("{0}:{1}", Uniconta.ClientTools.Localization.lookup("ReservationLine"), syncMaster._OrderNumber);
            if (header != null)
                SetHeader(header);
        }
        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            if (!company.ProjectTask)
                Task.Visible = Task.ShowInColumnChooser = false;
            else
                Task.ShowInColumnChooser = true;
            SetVariantColumns();
            UnicontaClient.Utilities.Utility.SetDimensionsGrid(api, cldim1, cldim2, cldim3, cldim4, cldim5);
        }

        void SetVariantColumns()
        {
            if (!company.ItemVariants)
                colVariant.Visible = colVariant.ShowInColumnChooser = false;
            var n = company.NumberOfVariants;

            if (n >= 1)
                colVariant1.Header = company._Variant1;
            else
                colVariant1.Visible = colVariant1.ShowInColumnChooser = Variant1Name.ShowInColumnChooser = Variant1Name.Visible = false;

            if (n >= 2)
                colVariant2.Header = company._Variant2;
            else
                colVariant2.Visible = colVariant2.ShowInColumnChooser = Variant2Name.ShowInColumnChooser = Variant2Name.Visible = false;

            if (n >= 3)
                colVariant3.Header = company._Variant3;
            else
                colVariant3.Visible = colVariant3.ShowInColumnChooser = Variant3Name.ShowInColumnChooser = Variant3Name.Visible = false;

            if (n >= 4)
                colVariant4.Header = company._Variant4;
            else
                colVariant4.Visible = colVariant4.ShowInColumnChooser = Variant4Name.ShowInColumnChooser = Variant4Name.Visible = false;

            if (n >= 5)
                colVariant5.Header = company._Variant5;
            else
                colVariant5.Visible = colVariant5.ShowInColumnChooser = Variant5Name.ShowInColumnChooser = Variant5Name.Visible = false;
        }

        public override async void Utility_Refresh(string screenName, object argument)
        {
            var param = argument as object[];
            if (param != null)
            {
                if (screenName == TabControls.AddMultipleInventoryItem)
                {
                    var orderNumber = (int)NumberConvert.ToInt(Convert.ToString(param[1]));
                    if (orderNumber == Order._OrderNumber)
                    {
                        if (dgProjReservationLineGrid.isDefaultFirstRow)
                        {
                            dgProjReservationLineGrid.DeleteRow();
                            dgProjReservationLineGrid.isDefaultFirstRow = false;
                        }
                        dgProjReservationLineGrid.PasteRows(param[0] as IEnumerable<UnicontaBaseEntity>);
                    }
                }
                else if (screenName == TabControls.ItemVariantAddPage)
                {
                    var orderNumber = (int)NumberConvert.ToInt(Convert.ToString(param[1]));
                    if (orderNumber == Order._OrderNumber)
                        dgProjReservationLineGrid.PasteRows(param[0] as IEnumerable<UnicontaBaseEntity>);
                }
                else if (screenName == TabControls.SerialToOrderLinePage)
                {
                    var orderLine = param[0] as ProjectReservationLineClient;
                    if (IsDataChaged)
                    {
                        var t = saveGrid();
                        if (t != null && orderLine.RowId == 0)
                            await t;
                    }
                }
                else if (screenName == TabControls.CreateOrderFromQuickInvoice)
                {
                    var args = argument as object[];
                    bool checkIfCreditNote = (bool)args[1];
                    var orderlines = args[2] as IEnumerable<UnicontaBaseEntity>;
                    bool IsDeleteLines = (bool)args[3];
                    double sign = checkIfCreditNote ? -1d : 1d;
                    if (checkIfCreditNote)
                        orderlines.ForEach(u => (u as DCOrderLine)._Qty = (u as DCOrderLine)._Qty * sign);
                    if (IsDeleteLines)
                        dgProjReservationLineGrid.DeleteAllRows();
                    dgProjReservationLineGrid.PasteRows(orderlines);
                }
                else if (screenName == TabControls.CopyOfferLines)
                {
                    var args = argument as object[];
                    var IsDeleteLines = (bool)args[0];
                    if (IsDeleteLines)
                        dgProjReservationLineGrid.DeleteAllRows(); // It will remove the rows from the grid and on save it will remove it from sql
                    //Add lines to grid
                    dgProjReservationLineGrid.PasteRows(args[1] as IEnumerable<UnicontaBaseEntity>);
                }
                else if (screenName == TabControls.CopyBudgetLines)
                {
                    var args = argument as object[];
                    var IsDeleteLines = (bool)args[0];
                    if (IsDeleteLines)
                        dgProjReservationLineGrid.DeleteAllRows();
                    dgProjReservationLineGrid.PasteRows(args[1] as IEnumerable<UnicontaBaseEntity>);
                }
            }
            if (screenName == TabControls.RegenerateOrderFromProjectPage)
                InitQuery();
        }

        public bool DataChaged;

        private void dgProjReservationLineGrid_SelectedItemChanged(object sender, SelectedItemChangedEventArgs e)
        {
            var oldselectedItem = e.OldItem as ProjectReservationLineClient;
            if (oldselectedItem != null)
                oldselectedItem.PropertyChanged -= ProjInvProposedLineGrid_PropertyChanged;
            var selectedItem = e.NewItem as ProjectReservationLineClient;
            if (selectedItem != null)
            {
                selectedItem.PropertyChanged += ProjInvProposedLineGrid_PropertyChanged;
                if (selectedItem.Variant1Source == null)
                    setVariant(selectedItem, false);
                if (selectedItem.Variant2Source == null)
                    setVariant(selectedItem, true);
                if (addingRow && selectedItem._Item != null)
                    return;
                addingRow = false;
            }
        }

        private void ProjInvProposedLineGrid_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var rec = sender as ProjectReservationLineClient;
            switch (e.PropertyName)
            {
                case "Item":
                    var selectedItem = (InvItem)items?.Get(rec._Item);
                    if (selectedItem != null)
                    {
                        if (selectedItem._AlternativeItem != null && selectedItem._UseAlternative == UseAlternativeItem.Always)
                        {
                            var altItem = (InvItem)items.Get(selectedItem._AlternativeItem);
                            if (altItem != null && altItem._AlternativeItem == null)
                            {
                                rec.Item = selectedItem._AlternativeItem;
                                return;
                            }
                        }

                        var _priceLookup = this.PriceLookup;
                        this.PriceLookup = null; // avoid that we call priceupdated in property change on Qty
                        if (selectedItem._SalesQty != 0d)
                            rec.Qty = selectedItem._SalesQty;
                        else if (api.CompanyEntity._OrderLineOne)
                            rec.Qty = 1d;
                        rec.SetItemValues(selectedItem, api.CompanyEntity._OrderLineStorage);
                        this.PriceLookup = _priceLookup;
                        _priceLookup?.SetPriceFromItem(rec, selectedItem);

                        if (company._InvoiceUseQtyNow)
                            rec.QtyNow = rec._Qty;

                        if (selectedItem._StandardVariant != rec.standardVariant)
                        {
                            rec.Variant1 = null;
                            rec.Variant2 = null;
                            rec.variant2Source = null;
                            rec.NotifyPropertyChanged("Variant2Source");
                        }
                        setVariant(rec, false);
                        TableField.SetUserFieldsFromRecord(selectedItem, rec);
                        if (selectedItem._Blocked)
                            UtilDisplay.ShowErrorCode(ErrorCodes.ItemIsOnHold, null);

                        globalEvents.NotifyRefreshViewer(NameOfControl, rec);
                    }
                    break;
                case "Qty":
                    if (this.PriceLookup != null && this.PriceLookup.UseCustomerPrices)
                        this.PriceLookup.GetCustomerPrice(rec, false);
                    if (company._InvoiceUseQtyNow)
                        rec.QtyNow = rec._Qty;
                    break;
                case "Total":
                    Dispatcher.BeginInvoke(new Action(() => { RecalculateAmount(); }));
                    break;
                case "Employee":
                    if (rec._Employee != null)
                    {
                        var item = (InvItem)items?.Get(rec._Item);
                        if (item == null || item._ItemType == (byte)Uniconta.DataModel.ItemType.Service)
                        {
                            var emp = (Uniconta.DataModel.Employee)employees?.Get(rec._Employee);
                            if (emp != null && emp._CostPrice != 0d)
                                rec.CostPrice = emp._CostPrice;
                        }
                    }
                    break;
                case "EAN":
                    DebtorOfferLines.FindOnEAN(rec, this.items, api, this.PriceLookup);
                    break;
                case "Variant1":
                    if (rec._Variant1 != null)
                        setVariant(rec, true);
                    if (this.PriceLookup != null && this.PriceLookup.UseCustomerPrices)
                        this.PriceLookup.GetCustomerPrice(rec, false);
                    break;
                case "Variant2":
                case "Variant3":
                case "Variant4":
                case "Variant5":
                    if (this.PriceLookup != null && this.PriceLookup.UseCustomerPrices)
                        this.PriceLookup.GetCustomerPrice(rec, false);
                    break;
                case "Variant":
                    globalEvents.NotifyRefreshViewer(NameOfControl, rec);
                    break;
                case "CustomerItemNumber":
                    if (!string.IsNullOrEmpty(rec.CustomerItemNumber))
                        DebtorOfferLines.FindItemFromCustomerItem(rec, Order, api, rec.CustomerItemNumber);
                    break;
            }
        }

        async void setVariant(ProjectReservationLineClient rec, bool SetVariant2)
        {
            if (items == null || variants1 == null || variants2 == null)
                return;

            //Check for Variant2 Exist
            if (string.IsNullOrEmpty(api.CompanyEntity?._Variant2))
                SetVariant2 = false;

            var item = (InvItem)items.Get(rec._Item);
            if (item != null && item._StandardVariant != null)
            {
                rec.standardVariant = item._StandardVariant;
                var master = (InvStandardVariant)standardVariants?.Get(item._StandardVariant);
                if (master == null)
                    return;
                if (master._AllowAllCombinations)
                {
                    rec.Variant1Source = (IEnumerable<InvVariant1>)this.variants1?.GetKeyStrRecords;
                    rec.Variant2Source = (IEnumerable<InvVariant2>)this.variants2?.GetKeyStrRecords;
                }
                else
                {
                    var Combinations = master.Combinations ?? await master.LoadCombinations(api);
                    if (Combinations == null)
                        return;
                    List<InvVariant1> invs1 = null;
                    List<InvVariant2> invs2 = null;
                    string vr1 = null;
                    if (SetVariant2)
                    {
                        vr1 = rec._Variant1;
                        invs2 = new List<InvVariant2>();
                    }
                    else
                        invs1 = new List<InvVariant1>();

                    string LastVariant = null;
                    var var2Value = rec._Variant2;
                    bool hasVariantValue = (var2Value == null);
                    foreach (var cmb in Combinations)
                    {
                        if (SetVariant2)
                        {
                            if (cmb._Variant1 == vr1 && cmb._Variant2 != null)
                            {
                                var v2 = (InvVariant2)variants2.Get(cmb._Variant2);
                                if (v2 == null)
                                {
                                    variants2 = await api.LoadCache(typeof(Uniconta.DataModel.InvVariant2), true);
                                    v2 = (InvVariant2)variants2.Get(cmb._Variant2);
                                }
                                invs2.Add(v2);
                                if (var2Value == v2._Variant)
                                    hasVariantValue = true;
                            }
                        }
                        else if (LastVariant != cmb._Variant1)
                        {
                            LastVariant = cmb._Variant1;
                            var v1 = (InvVariant1)variants1.Get(cmb._Variant1);
                            if (v1 == null)
                            {
                                variants1 = await api.LoadCache(typeof(Uniconta.DataModel.InvVariant1), true);
                                v1 = (InvVariant1)variants1.Get(cmb._Variant1);
                            }
                            if (v1 != null)
                                invs1.Add(v1);
                        }
                    }
                    if (SetVariant2)
                    {
                        rec.variant2Source = invs2;
                        //if (!hasVariantValue)
                        //    rec.Variant2 = null;
                    }
                    else
                        rec.variant1Source = invs1;
                }
            }
            else
            {
                rec.variant1Source = null;
                rec.variant2Source = null;
            }
            if (SetVariant2)
                rec.NotifyPropertyChanged("Variant2Source");
            else
                rec.NotifyPropertyChanged("Variant1Source");
        }

        void RecalculateAmount()
        {
            var ord = this.Order;
            if (ord == null)
                return;
            var ret = DebtorOfferLines.RecalculateLineSum(ord, (IEnumerable<DCOrderLineClient>)dgProjReservationLineGrid.ItemsSource, this.exchangeRate);
            double Amountsum = ret.Item1;
            double Costsum = ret.Item2;
            double sales = ret.Item3;
            ord._OrderTotal = sales;
            if (ord._EndDiscountPct != 0)
                sales *= (100d - ord._EndDiscountPct) / 100d;

            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            var groups = UtilDisplay.GetMenuCommandsByStatus(rb, true);

            var strAmount = Uniconta.ClientTools.Localization.lookup("Amount");
            var strCost = Uniconta.ClientTools.Localization.lookup("CostValue");
            var strDB = Uniconta.ClientTools.Localization.lookup("DB");
            foreach (var grp in groups)
            {
                if (grp.Caption == strAmount)
                    grp.StatusValue = Amountsum.ToString("N2");
                else if (grp.Caption == strCost)
                    grp.StatusValue = Costsum.ToString("N2");
                else if (grp.Caption == strDB)
                {
                    var margin = (sales - Costsum);
                    var ratio = sales != 0d ? Math.Round(margin * 100d / sales) : 0d;
                    string str;
                    if (ratio != 0 && ratio != 100 && ratio > -100)
                        str = string.Format("{0}% {1:n2}", ratio, margin);
                    else
                        str = margin.ToString("N2");
                    grp.StatusValue = str;
                }
                else
                    grp.StatusValue = string.Empty;
            }
        }
        bool addingRow;

        private void localMenu_OnItemClicked(string ActionType)
        {
            ProjectReservationLineClient row;
            ProjectReservation ord = this.Order;
            var selectedItem = dgProjReservationLineGrid.SelectedItem as ProjectReservationLineClient;
            switch (ActionType)
            {
                case "AddRow":
                    addingRow = true;
                    row = dgProjReservationLineGrid.AddRow() as ProjectReservationLineClient;
                    row._ExchangeRate = this.exchangeRate;
                    break;
                case "CopyRow":
                    row = dgProjReservationLineGrid.CopyRow() as ProjectReservationLineClient;
                    if (row != null)
                    {
                        row._ExchangeRate = this.exchangeRate;
                        row._CostPriceLine = selectedItem._CostPriceLine;
                        row._QtyDelivered = 0;
                        row._QtyInvoiced = 0;
                    }
                    break;
                case "SaveGrid":
                    saveGridLocal();
                    break;
                case "DeleteRow":
                    dgProjReservationLineGrid.DeleteRow();
                    break;
                case "AddItems":
                    if (this.items != null)
                    {
                        object[] paramArray = new object[3] { new InvItemSalesCacheFilter(this.items), dgProjReservationLineGrid.TableTypeUser, Order };
                        AddDockItem(TabControls.AddMultipleInventoryItem, paramArray, true,
                            string.Format(Uniconta.ClientTools.Localization.lookup("AddOBJ"), Uniconta.ClientTools.Localization.lookup("InventoryItems")), null, floatingLoc: Utility.GetDefaultLocation());
                    }
                    break;
                case "AddVariants":
                    var itm = selectedItem?.InvItem;
                    if (itm?._StandardVariant != null)
                    {
                        var paramItem = new object[] { selectedItem, ord };
                        dgProjReservationLineGrid.SetLoadedRow(selectedItem);
                        AddDockItem(TabControls.ItemVariantAddPage, paramItem, true,
                        string.Format(Uniconta.ClientTools.Localization.lookup("AddOBJ"), Uniconta.ClientTools.Localization.lookup("Variants")), null, floatingLoc: Utility.GetDefaultLocation());
                    }
                    break;
                case "CreateFromInvoice":
                    try
                    {
                        AddDockItem(TabControls.CreateOrderFromQuickInvoice, new object[4] { api, ord._DCAccount, true, ord }, true, String.Format(Uniconta.ClientTools.Localization.lookup("CopyOBJ"), Uniconta.ClientTools.Localization.lookup("Invoice")), null, new Point(250, 200));
                    }
                    catch (Exception ex)
                    {
                        UnicontaMessageBox.Show(ex);
                    }
                    break;
                case "RefreshGrid":
                    RefreshGrid();
                    return;
                case "CopyOffer":
                    CopyLinesFromOffer();
                    break;
                case "CopyBudget":
                    CopyLinesFromBudget();
                    break;
                case "PostProjectOrder":
                    if (selectedItem != null)
                        PostProjectOrder(selectedItem);
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
            RecalculateAmount();
        }

        void PostProjectOrder(ProjectReservationLineClient order)
        {
            var dialog = new CwPostProjectOrder();
            dialog.DialogTableId = 2000000087;
            dialog.Closed += async delegate
            {
                if (dialog.DialogResult == true)
                {
                    busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("SendingWait");
                    busyIndicator.IsBusy = true;
                    var invApi = new Uniconta.API.DebtorCreditor.InvoiceAPI(api);
                    var postingResult = await invApi.PostProjectReservation(dgProjReservationLineGrid.masterRecord as Uniconta.DataModel.Project, null, dialog.Date, dialog.Simulation, new GLTransClientTotal(), null, dialog.PostOnlyDelivered);
                    busyIndicator.IsBusy = false;
                    busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("LoadingMsg");
                    var ledgerRes = postingResult.ledgerRes;
                    if (ledgerRes == null)
                        return;
                    if (ledgerRes.Err != ErrorCodes.Succes)
                        Utility.ShowJournalError(ledgerRes, dgProjReservationLineGrid, false);
                    else if (dialog.Simulation && ledgerRes.SimulatedTrans != null && ledgerRes.SimulatedTrans.Length > 0)
                        AddDockItem(TabControls.SimulatedTransactions, ledgerRes.SimulatedTrans, Uniconta.ClientTools.Localization.lookup("SimulatedTransactions"), null, true);
                    else
                    {
                        string msg;
                        if (ledgerRes.JournalPostedlId != 0)
                            msg = string.Format("{0} {1}={2}", Uniconta.ClientTools.Localization.lookup("JournalHasBeenPosted"), Uniconta.ClientTools.Localization.lookup("JournalPostedId"), ledgerRes.JournalPostedlId);
                        else
                            msg = Uniconta.ClientTools.Localization.lookup("JournalHasBeenPosted");
                        UnicontaMessageBox.Show(msg, Uniconta.ClientTools.Localization.lookup("Message"));
                    }
                }
            };
            dialog.Show();
        }

        void CopyLinesFromOffer()
        {
            try
            {
                AddDockItem(TabControls.CopyOfferLines, new object[1] { api }, true, String.Format(Uniconta.ClientTools.Localization.lookup("CopyOBJ"), Uniconta.ClientTools.Localization.lookup("OfferLine")), null, new Point(250, 200));
            }
            catch (Exception ex)
            {
                UnicontaMessageBox.Show(ex);
            }
        }
        void CopyLinesFromBudget()
        {
            AddDockItem(TabControls.CopyBudgetLines, new object[2] { api, Order }, true, String.Format(Uniconta.ClientTools.Localization.lookup("CopyOBJ"), Uniconta.ClientTools.Localization.lookup("ProjectEstimate")), null, new Point(250, 200));
        }
        async void RefreshGrid()
        {
            var savetask = saveGridLocal(); // we need to wait until it is saved, otherwise Storage is not updated
            if (savetask != null)
                await savetask;
            await dgProjReservationLineGrid.RefreshTask();
            RecalculateAmount();
        }

        public override bool IsDataChaged { get { return DataChaged || base.IsDataChaged; } }

        Task<ErrorCodes> saveGridLocal()
        {
            var orderLine = dgProjReservationLineGrid.SelectedItem;
            dgProjReservationLineGrid.SelectedItem = null;
            dgProjReservationLineGrid.SelectedItem = orderLine;
            if (IsDataChaged)
                return saveGrid();
            return null;
        }

        protected override async Task<ErrorCodes> saveGrid()
        {
            if (dgProjReservationLineGrid.HasUnsavedData)
            {
                ErrorCodes res = await dgProjReservationLineGrid.SaveData();
                if (res == ErrorCodes.Succes)
                {
                    DataChaged = false;
                    globalEvents.OnRefresh(NameOfControl, Order);
                }
                return res;
            }
            return ErrorCodes.Succes;
        }


        public override void RowsPastedDone() { RecalculateAmount(); }

        public override void RowPasted(UnicontaBaseEntity rec)
        {
            var Comp = api.CompanyEntity;
            var orderLine = rec as ProjectReservationLineClient;
            if (orderLine == null)
                return;
            if (Comp._InvoiceUseQtyNow)
                orderLine.QtyNow = orderLine._Qty;
            if (orderLine._Item != null)
            {
                var selectedItem = (InvItem)items.Get(orderLine._Item);
                if (selectedItem != null)
                {
                    PriceLookup?.SetPriceFromItem(orderLine, selectedItem);
                    orderLine.SetItemValues(selectedItem, Comp._OrderLineStorage, true);
                    TableField.SetUserFieldsFromRecord(selectedItem, orderLine);
                }
                else
                    orderLine._Item = null;
            }
        }

        private Task Filter(IEnumerable<PropValuePair> propValuePair)
        {
            return dgProjReservationLineGrid.Filter(propValuePair);
        }

        public async override Task InitQuery()
        {
            await Filter(null);
            var itemSource = (IList)dgProjReservationLineGrid.ItemsSource;
            if (itemSource == null || itemSource.Count == 0)
                dgProjReservationLineGrid.AddFirstRow();
            RecalculateAmount();
        }

        private void InitialLoad()
        {
            var Comp = api.CompanyEntity;
            this.items = Comp.GetCache(typeof(InvItem));
            this.variants1 = Comp.GetCache(typeof(InvVariant1));
            this.variants2 = Comp.GetCache(typeof(InvVariant2));
            this.standardVariants = Comp.GetCache(typeof(InvStandardVariant));
            if (Comp.UnitConversion)
                Unit.Visible = true;
            if (dgProjReservationLineGrid.IsLoadedFromLayoutSaved)
            {
                dgProjReservationLineGrid.ClearSorting();
                dgProjReservationLineGrid.IsLoadedFromLayoutSaved = false;
            }
        }

        protected override async System.Threading.Tasks.Task LoadCacheInBackGroundAsync()
        {
            var api = this.api;
            var Comp = api.CompanyEntity;

            if (this.items == null)
                this.items = api.GetCache(typeof(Uniconta.DataModel.InvItem)) ?? await api.LoadCache(typeof(Uniconta.DataModel.InvItem)).ConfigureAwait(false);
            if (Comp.ItemVariants)
            {
                if (this.standardVariants == null)
                    this.standardVariants = api.GetCache(typeof(Uniconta.DataModel.InvStandardVariant)) ?? await api.LoadCache(typeof(Uniconta.DataModel.InvStandardVariant)).ConfigureAwait(false);
                if (this.variants1 == null)
                    this.variants1 = api.GetCache(typeof(Uniconta.DataModel.InvVariant1)) ?? await api.LoadCache(typeof(Uniconta.DataModel.InvVariant1)).ConfigureAwait(false);
                if (this.variants2 == null)
                    this.variants2 = api.GetCache(typeof(Uniconta.DataModel.InvVariant2)) ?? await api.LoadCache(typeof(Uniconta.DataModel.InvVariant2)).ConfigureAwait(false);
            }

            if (this.PriceLookup == null && Order != null)
                PriceLookup = new Uniconta.API.DebtorCreditor.FindPrices(Order, api);
            var t = this.PriceLookup?.ExchangeTask;
            this.exchangeRate = this.PriceLookup != null ? this.PriceLookup.ExchangeRate : 0d;
            if (this.exchangeRate == 0d && t != null)
                this.exchangeRate = await t.ConfigureAwait(false);

            this.employees = api.GetCache(typeof(Uniconta.DataModel.Employee)) ?? await api.LoadCache(typeof(Uniconta.DataModel.Employee)).ConfigureAwait(false);

            Dispatcher.BeginInvoke(new Action(() =>
            {
                var selectedItem = dgProjReservationLineGrid.SelectedItem as ProjectReservationLineClient;
                if (selectedItem != null)
                {
                    if (selectedItem.Variant1Source == null)
                        setVariant(selectedItem, false);
                    if (selectedItem.Variant2Source == null)
                        setVariant(selectedItem, true);
                }
            }));
         }
    }
}
