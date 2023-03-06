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
    public class ProjInvProposedLineGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(ProjectInvoiceProposalLineClient); } }
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
            List<ProjectInvoiceProposalLineClient> lst = null;
            if (row is InvTrans)
            {
                lst = new List<ProjectInvoiceProposalLineClient>();
                foreach (var _it in copyFromRows)
                {
                    var it = (InvTrans)_it;
                    lst.Add(CreateNewProposalLine(it._Item, it.MovementTypeEnum == InvMovementType.Debtor ? -it._Qty : it._Qty, it._Text, it._Price, it.MovementTypeEnum == InvMovementType.Debtor ? -it._AmountEntered : it._AmountEntered,
                        it._DiscountPct, it._Discount, it._Variant1, it._Variant2, it._Variant3, it._Variant4, it._Variant5, it._Unit, it._Date, it._Week, it._Note));
                }
            }
            else if (row is DCOrderLine)
            {
                lst = new List<ProjectInvoiceProposalLineClient>();
                foreach (var _it in copyFromRows)
                {
                    var it = (DCOrderLine)_it;
                    var line = CreateNewProposalLine(it._Item, it._Qty, it._Text, it._Price, it._AmountEntered, it._DiscountPct, it._Discount, it._Variant1, it._Variant2, it._Variant3, it._Variant4, it._Variant5, it._Unit,
                        it._Date, it._Week, it._Note);
                    TableField.SetUserFieldsFromRecord(it, line);
                    lst.Add(line);
                }
            }
            else if (row is InvItemClient)
            {
                lst = new List<ProjectInvoiceProposalLineClient>();
                foreach (var _it in copyFromRows)
                {
                    double qty = (double)_it.GetType().GetProperty("Qty").GetValue(_it, null);
                    var it = (InvItemClient)_it;
                    lst.Add(CreateNewProposalLine(it._Item, qty, null, 0d, 0d, 0d, 0d, null, null, null, null, null, 0, DateTime.MinValue, 0, null));
                }
            }
            return lst;
        }

        private ProjectInvoiceProposalLineClient CreateNewProposalLine(string item, double qty, string text, double price, double amountEntered, double discPct, double disc, string variant1, string variant2, string variant3, string variant4, string variant5,
           ItemUnit unit, DateTime date, byte week, string note)
        {
            var type = this.TableTypeUser;
            var orderline = Activator.CreateInstance(type) as ProjectInvoiceProposalLineClient;
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
            return orderline;
        }

        public override bool ClearSelectedItemOnSave { get { return false; } }
        public bool allowSave = true;
        public override bool AllowSave { get { return allowSave; } }
    }

    public partial class ProjInvProposalLine : GridBasePage
    {
        SQLCache items, standardVariants, variants1, variants2, employees;
        ProjectInvoiceProposalClient Order { get { return dgProjInvProposedLineGrid.masterRecord as ProjectInvoiceProposalClient; } }
        FindPrices PriceLookup;
        Company company;
        double exchangeRate;

        public override void PageClosing()
        {
            globalEvents.OnRefresh(NameOfControl, Order);
            base.PageClosing();
        }

        public ProjInvProposalLine(UnicontaBaseEntity master)
           : base(master)
        {
            Init(master);
        }
        public ProjInvProposalLine(SynchronizeEntity syncEntity)
           : base(syncEntity, false)
        {
            Init(syncEntity.Row);
        }

        public void Init(UnicontaBaseEntity master)
        {
            InitializeComponent();
            company = api.CompanyEntity;
            ((TableView)dgProjInvProposedLineGrid.View).RowStyle = Application.Current.Resources["StyleRow"] as Style;
            localMenu.dataGrid = dgProjInvProposedLineGrid;
            SetRibbonControl(localMenu, dgProjInvProposedLineGrid);
            dgProjInvProposedLineGrid.api = api;
            SetupMaster(master);
            dgProjInvProposedLineGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgProjInvProposedLineGrid.SelectedItemChanged += DgProjInvProposedLineGrid_SelectedItemChanged;
            OnHandScreenInOrder = api.CompanyEntity._OnHandScreenInOrder;
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            if (!company.Production)
                UtilDisplay.RemoveMenuCommand(rb, "CreateProduction");
            if (IsOnAccountInvoicing(master))
                UtilDisplay.RemoveMenuCommand(rb, new string[] { "RegenerateOrderFromProject", "ProjectTransaction" });

            InitialLoad();
            dgProjInvProposedLineGrid.ShowTotalSummary();
            dgProjInvProposedLineGrid.CustomSummary += dgProjInvProposedLineGrid_CustomSummary;
            this.KeyDown += Page_KeyDown;
        }

        private bool IsOnAccountInvoicing(UnicontaBaseEntity master)
        {
            if (master is ProjectInvoiceProposalClient projInvProposalClient && projInvProposalClient.PrCategoryRef._CatType == CategoryType.OnAccountInvoicing)
                return true;

            return false;
        }
        private void Page_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F8)
                localMenu_OnItemClicked("AddItems");
        }

        double sumMargin, sumSales, sumMarginRatio;
        private void dgProjInvProposedLineGrid_CustomSummary(object sender, DevExpress.Data.CustomSummaryEventArgs e)
        {
            var fieldName = ((GridSummaryItem)e.Item).FieldName;
            switch (e.SummaryProcess)
            {
                case DevExpress.Data.CustomSummaryProcess.Start:
                    sumMargin = sumSales = 0d;
                    break;
                case DevExpress.Data.CustomSummaryProcess.Calculate:
                    var row = e.Row as ProjectInvoiceProposalLineClient;
                    sumSales += row.SalesValue;
                    sumMargin += row.Margin;
                    break;
                case DevExpress.Data.CustomSummaryProcess.Finalize:
                    if (fieldName == "MarginRatio" && sumSales > 0)
                    {
                        sumMarginRatio = 100 * sumMargin / sumSales;
                        e.TotalValue = sumMarginRatio;
                    }
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
            dgProjInvProposedLineGrid.UpdateMaster(args);
            if (Order?.RowId != OrderId)
                PriceLookup = new Uniconta.API.DebtorCreditor.FindPrices(Order, api);
        }

        void SetHeader()
        {
            var syncMaster = Order;
            string header = null;
            if (syncMaster != null)
                header = string.Format("{0}:{1},{2}", Uniconta.ClientTools.Localization.lookup("InvoiceProposalLine"), syncMaster._OrderNumber, syncMaster.Name);
            if (header != null)
                SetHeader(header);
        }
        bool OnHandScreenInOrder;
        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            var company = this.company;
            if (!company.Project)
            {
                PrCategory.Visible = PrCategory.ShowInColumnChooser = false;
                Project.Visible = Project.ShowInColumnChooser = false;
            }
            else
                PrCategory.ShowInColumnChooser = Project.ShowInColumnChooser = true;
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
            var n = company.ItemVariants ? company.NumberOfVariants : 0;

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
                        if (dgProjInvProposedLineGrid.isDefaultFirstRow)
                        {
                            dgProjInvProposedLineGrid.DeleteRow();
                            dgProjInvProposedLineGrid.isDefaultFirstRow = false;
                        }
                        var invItems = param[0] as List<UnicontaBaseEntity>;
                        dgProjInvProposedLineGrid.PasteRows(invItems);
                    }
                }
                else if (screenName == TabControls.ItemVariantAddPage)
                {
                    var orderNumber = (int)NumberConvert.ToInt(Convert.ToString(param[1]));
                    if (orderNumber == Order._OrderNumber)
                    {
                        var invItems = param[0] as List<UnicontaBaseEntity>;
                        dgProjInvProposedLineGrid.PasteRows(invItems);
                    }
                }
                else if (screenName == TabControls.SerialToOrderLinePage)
                {
                    var orderLine = param[0] as ProjectInvoiceProposalLineClient;
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
                        dgProjInvProposedLineGrid.DeleteAllRows();
                    dgProjInvProposedLineGrid.PasteRows(orderlines);
                }
                else if (screenName == TabControls.CopyOfferLines)
                {
                    var args = argument as object[];
                    var IsDeleteLines = (bool)args[0];
                    var offerLines = args[1] as DebtorOfferLineClient[];
                    if (IsDeleteLines)
                        dgProjInvProposedLineGrid.DeleteAllRows(); // It will remove the rows from the grid and on save it will remove it from sql
                    //Add lines to grid
                    dgProjInvProposedLineGrid.PasteRows(offerLines);
                }
                else if (screenName == TabControls.CopyBudgetLines)
                {
                    var args = argument as object[];
                    var IsDeleteLines = (bool)args[0];
                    var ProposalLines = args[1] as List<ProjectInvoiceProposalLineClient>;
                    if (IsDeleteLines)
                        dgProjInvProposedLineGrid.DeleteAllRows();
                    dgProjInvProposedLineGrid.PasteRows(ProposalLines);
                }
            }
            if (screenName == TabControls.RegenerateOrderFromProjectPage)
                InitQuery();
        }

        public bool DataChaged;


        private void DgProjInvProposedLineGrid_SelectedItemChanged(object sender, SelectedItemChangedEventArgs e)
        {
            var oldselectedItem = e.OldItem as ProjectInvoiceProposalLineClient;
            if (oldselectedItem != null)
                oldselectedItem.PropertyChanged -= ProjInvProposedLineGrid_PropertyChanged;
            var selectedItem = e.NewItem as ProjectInvoiceProposalLineClient;
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
            var rec = sender as ProjectInvoiceProposalLineClient;
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
                case "Subtotal":
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

        async void setVariant(ProjectInvoiceProposalLineClient rec, bool SetVariant2)
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
            var ret = DebtorOfferLines.RecalculateLineSum(ord, (IEnumerable<DCOrderLineClient>)dgProjInvProposedLineGrid.ItemsSource, this.exchangeRate);
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
            var strProjTotal = Uniconta.ClientTools.Localization.lookup("ProjectTotal");
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
                else if (grp.Caption == strProjTotal)
                    grp.StatusValue = ord._ProjectTotal.ToString("N2");
                else
                    grp.StatusValue = string.Empty;
            }
        }
        bool addingRow;

        private void localMenu_OnItemClicked(string ActionType)
        {
            ProjectInvoiceProposalLineClient row;
            ProjectInvoiceProposalClient ord = this.Order;
            var selectedItem = dgProjInvProposedLineGrid.SelectedItem as ProjectInvoiceProposalLineClient;
            switch (ActionType)
            {
                case "AddRow":
                    addingRow = true;
                    row = dgProjInvProposedLineGrid.AddRow() as ProjectInvoiceProposalLineClient;
                    row._ExchangeRate = this.exchangeRate;
                    break;
                case "CopyRow":
                    if (selectedItem != null)
                    {
                        row = dgProjInvProposedLineGrid.CopyRow() as ProjectInvoiceProposalLineClient;
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
                    dgProjInvProposedLineGrid.DeleteRow();
                    break;
                case "ShowInvoice":
                case "CreateInvoice":
                    if (ord != null)
                    {
                        if (Utility.HasControlRights("GenerateInvoice", api.CompanyEntity))
                            GenerateInvoice(ord, ActionType == "ShowInvoice" ? true : false);
                        else
                            UtilDisplay.ShowControlAccessMsg("GenerateInvoice");
                    }
                    break;
                case "InsertSubTotal":
                    row = dgProjInvProposedLineGrid.AddRow() as ProjectInvoiceProposalLineClient;
                    if (row != null)
                        row.Subtotal = true;
                    break;
                case "AddItems":
                    if (this.items != null)
                    {
                        object[] paramArray = new object[3] { new InvItemSalesCacheFilter(this.items), dgProjInvProposedLineGrid.TableTypeUser, Order };
                        AddDockItem(TabControls.AddMultipleInventoryItem, paramArray, true,
                            string.Format(Uniconta.ClientTools.Localization.lookup("AddOBJ"), Uniconta.ClientTools.Localization.lookup("InventoryItems")), null, floatingLoc: Utility.GetDefaultLocation());
                    }
                    break;
                case "EditOrder":
                    if (ord != null)
                        AddDockItem(TabControls.ProjInvProposalPage2, ord, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("InvoiceProposal"), ord._OrderNumber));
                    break;
                case "ProjectTransaction":
                    if (ord != null)
                    {
                        saveGridLocal();
                        AddDockItem(TabControls.ProjectInvoiceProjectLinePage, ord, string.Format("{0}: {1} ({2})", Uniconta.ClientTools.Localization.lookup("ProjectAdjustments"), ord._OrderNumber, ord._Project));
                    }
                    break;
                case "RegenerateOrderFromProject":
                    if (ord != null)
                        AddDockItem(TabControls.RegenerateOrderFromProjectPage, ord, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("RegenerateOrder"), ord._OrderNumber));
                    break;
                case "AddVariants":
                    var itm = selectedItem?.InvItem;
                    if (itm?._StandardVariant != null)
                    {
                        var paramItem = new object[] { selectedItem, ord };
                        dgProjInvProposedLineGrid.SetLoadedRow(selectedItem);
                        AddDockItem(TabControls.ItemVariantAddPage, paramItem, true,
                        string.Format(Uniconta.ClientTools.Localization.lookup("AddOBJ"), Uniconta.ClientTools.Localization.lookup("Variants")), null, floatingLoc: Utility.GetDefaultLocation());
                    }
                    break;
                case "CreateFromInvoice":
                    try
                    {
                        var par = new object[4];
                        par[0] = api;
                        par[1] = ord.Account;
                        par[2] = true;
                        par[3] = ord;
                        AddDockItem(TabControls.CreateOrderFromQuickInvoice, par, true, String.Format(Uniconta.ClientTools.Localization.lookup("CopyOBJ"), Uniconta.ClientTools.Localization.lookup("Invoice")), null, new Point(250, 200));
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
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
            RecalculateAmount();
        }

        void CopyLinesFromOffer()
        {
            try
            {
                var par = new object[1] { api };
                AddDockItem(TabControls.CopyOfferLines, par, true, String.Format(Uniconta.ClientTools.Localization.lookup("CopyOBJ"), Uniconta.ClientTools.Localization.lookup("OfferLine")), null, new Point(250, 200));
            }
            catch (Exception ex)
            {
                UnicontaMessageBox.Show(ex);
            }
        }
        void CopyLinesFromBudget()
        {
                var par = new object[2] { api, Order };
                AddDockItem(TabControls.CopyBudgetLines, par, true, String.Format(Uniconta.ClientTools.Localization.lookup("CopyOBJ"), Uniconta.ClientTools.Localization.lookup("ProjectEstimate")), null, new Point(250, 200));
        }
        async void RefreshGrid()
        {
            var savetask = saveGridLocal(); // we need to wait until it is saved, otherwise Storage is not updated
            if (savetask != null)
                await savetask;
            await dgProjInvProposedLineGrid.RefreshTask();
            RecalculateAmount();
        }

        public override bool IsDataChaged
        {
            get
            {
                if (DataChaged)
                    return true;
                return base.IsDataChaged;
            }
        }
        private void GenerateInvoice(ProjectInvoiceProposalClient dbOrder, bool showProformaInvoice)
        {
            var savetask = saveGridLocal();
            var curpanel = dockCtrl.Activpanel;

            bool showSendByMail = false;
            var debtor = ClientHelper.GetRef(dbOrder.CompanyId, typeof(Debtor), dbOrder._DCAccount) as Debtor;
            if (debtor != null)
            {
                var InvoiceAccount = dbOrder._InvoiceAccount ?? debtor._InvoiceAccount;
                if (InvoiceAccount != null)
                    debtor = ClientHelper.GetRef(dbOrder.CompanyId, typeof(Debtor), InvoiceAccount) as Debtor;
                if (debtor != null)
                {
                    if (debtor._PricesInclVat != dbOrder._PricesInclVat)
                    {
                        var confirmationMsgBox = UnicontaMessageBox.Show(string.Format("{0}.\n{1}", string.Format(Uniconta.ClientTools.Localization.lookup("DebtorAndOrderMix"), Uniconta.ClientTools.Localization.lookup("InclVat")),
                        Uniconta.ClientTools.Localization.lookup("ProceedConfirmation")), Uniconta.ClientTools.Localization.lookup("Warning"), MessageBoxButton.OKCancel);
                        if (confirmationMsgBox != MessageBoxResult.OK)
                            return;
                    }
                    if (!api.CompanyEntity.SameCurrency(dbOrder._Currency, debtor._Currency))
                    {
                        var confirmationMsgBox = UnicontaMessageBox.Show(string.Format("{0}.\n{1}", string.Format(Uniconta.ClientTools.Localization.lookup("CurrencyMismatch"), AppEnums.Currencies.ToString((int)debtor._Currency), dbOrder.Currency),
                        Uniconta.ClientTools.Localization.lookup("ProceedConfirmation")), Uniconta.ClientTools.Localization.lookup("Warning"), MessageBoxButton.OKCancel);
                        if (confirmationMsgBox != MessageBoxResult.OK)
                            return;
                    }
                    showSendByMail = (!string.IsNullOrEmpty(debtor._InvoiceEmail) || debtor._EmailDocuments);
                }
            }

            if (showProformaInvoice)
            {
                ShowProformaInvoice(dbOrder);
                return;
            }

            string debtorName = debtor?._Name ?? dbOrder._DCAccount;
            bool invoiceInXML = debtor != null && debtor.IsPeppolSupported && debtor._einvoice;

            var accountName = Util.ConcatParenthesis(dbOrder._DCAccount, dbOrder.Name);
            CWGenerateInvoice GenrateInvoiceDialog = new CWGenerateInvoice(true, string.Empty, false, true, true, showNoEmailMsg: !showSendByMail, debtorName: debtorName, isOrderOrQuickInv: true, isDebtorOrder: true, InvoiceInXML: invoiceInXML, AccountName: accountName);
            GenrateInvoiceDialog.DialogTableId = 2000000086;
            if (dbOrder._InvoiceDate != DateTime.MinValue)
                GenrateInvoiceDialog.SetInvoiceDate(dbOrder._InvoiceDate);
            var additionalOrdersList = Utility.GetAdditionalOrders(api, dbOrder);
            if (additionalOrdersList != null)
                GenrateInvoiceDialog.SetAdditionalOrders(additionalOrdersList);
            GenrateInvoiceDialog.SetOIOUBLLabelText(api.CompanyEntity._OIOUBLSendOnServer);

            GenrateInvoiceDialog.Closed += async delegate
            {
                if (GenrateInvoiceDialog.DialogResult == true)
                {
                    if (savetask != null)
                    {
                        var err = await savetask;
                        if (err != ErrorCodes.Succes)
                            return;
                    }
                    busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("SendingWait");
                    busyIndicator.IsBusy = true;
                    InvoiceAPI Invapi = new InvoiceAPI(api);
                    var isSimulated = GenrateInvoiceDialog.IsSimulation;
                    var invoicePostingResult = SetupInvoicePostingPrintGenerator(dbOrder, GenrateInvoiceDialog.GenrateDate, isSimulated, GenrateInvoiceDialog.ShowInvoice, GenrateInvoiceDialog.PostOnlyDelivered, GenrateInvoiceDialog.InvoiceQuickPrint,
                        GenrateInvoiceDialog.NumberOfPages, GenrateInvoiceDialog.SendByEmail, !isSimulated && GenrateInvoiceDialog.SendByOutlook, GenrateInvoiceDialog.sendOnlyToThisEmail, GenrateInvoiceDialog.Emails,
                        GenrateInvoiceDialog.GenerateOIOUBLClicked);
                    invoicePostingResult.SetAdditionalOrders(GenrateInvoiceDialog.AdditionalOrders?.Cast<DCOrder>().ToList());

                    busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("GeneratingPage");
                    busyIndicator.IsBusy = true;
                    var result = await invoicePostingResult.Execute();
                    busyIndicator.IsBusy = false;

                    if (!result)
                        Utility.ShowJournalError(invoicePostingResult.PostingResult.ledgerRes, dgProjInvProposedLineGrid);
                    else
                    {
                        Task reloadTask = null;
                        if (!GenrateInvoiceDialog.IsSimulation && dbOrder._DeleteLines)
                            reloadTask = Filter(null);

                        if (reloadTask != null)
                            CloseOrderLineScreen(reloadTask, curpanel);
                    }
                }
            };
            GenrateInvoiceDialog.Show();
        }

        async private void ShowProformaInvoice(ProjectInvoiceProposalClient order)
        {
            var invoicePostingResult = SetupInvoicePostingPrintGenerator(order, DateTime.Now, true, true, false, false, 0, false, false, false, null, false);

            busyIndicator.IsBusy = true;
            busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("GeneratingPage");
            var result = await invoicePostingResult.Execute();
            busyIndicator.IsBusy = false;

            if (!result)
                Utility.ShowJournalError(invoicePostingResult.PostingResult.ledgerRes, dgProjInvProposedLineGrid);
        }

        private InvoicePostingPrintGenerator SetupInvoicePostingPrintGenerator(ProjectInvoiceProposalClient dbOrder, DateTime generateDate, bool isSimulation, bool showInvoice, bool postOnlyDelivered,
           bool isQuickPrint, int pagePrintCount, bool invoiceSendByEmail, bool invoiceSendByOutlook, bool sendOnlyToEmail, string sendOnlyToEmailList, bool OIOUBLgenerate)
        {
            var invoicePostingResult = new InvoicePostingPrintGenerator(api, this);
            invoicePostingResult.SetUpInvoicePosting(dbOrder, null, CompanyLayoutType.Invoice, generateDate, null, isSimulation, showInvoice, postOnlyDelivered, isQuickPrint, pagePrintCount,
                invoiceSendByEmail, !isSimulation && invoiceSendByOutlook, sendOnlyToEmail, sendOnlyToEmailList, OIOUBLgenerate, null, false);
            return invoicePostingResult;
        }

        async void CloseOrderLineScreen(Task reloadTask, DevExpress.Xpf.Docking.DocumentPanel panel)
        {
            await reloadTask;
            if (((IList)dgProjInvProposedLineGrid.ItemsSource).Count == 0)
            {
                globalEvents.OnRefresh(this.NameOfControl, Order);
                dockCtrl?.JustClosePanel(panel);
            }
            else
                RecalculateAmount();
        }

        Task<ErrorCodes> saveGridLocal()
        {
            var orderLine = dgProjInvProposedLineGrid.SelectedItem as DCOrderLine;
            dgProjInvProposedLineGrid.SelectedItem = null;
            dgProjInvProposedLineGrid.SelectedItem = orderLine;
            if (IsDataChaged)
                return saveGrid();
            return null;
        }

        protected override async Task<ErrorCodes> saveGrid()
        {
            var orderLine = dgProjInvProposedLineGrid.SelectedItem as ProjectInvoiceProposalLineClient;
            if (dgProjInvProposedLineGrid.HasUnsavedData)
            {
                ErrorCodes res = await dgProjInvProposedLineGrid.SaveData();
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
            var orderLine = rec as ProjectInvoiceProposalLineClient;
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
            return dgProjInvProposedLineGrid.Filter(propValuePair);
        }

        public async override Task InitQuery()
        {
            await Filter(null);
            var itemSource = (IList)dgProjInvProposedLineGrid.ItemsSource;
            if (itemSource == null || itemSource.Count == 0)
                dgProjInvProposedLineGrid.AddFirstRow();
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
            if (dgProjInvProposedLineGrid.IsLoadedFromLayoutSaved)
            {
                dgProjInvProposedLineGrid.ClearSorting();
                dgProjInvProposedLineGrid.IsLoadedFromLayoutSaved = false;
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
                var selectedItem = dgProjInvProposedLineGrid.SelectedItem as ProjectInvoiceProposalLineClient;
                if (selectedItem != null)
                {
                    if (selectedItem.Variant1Source == null)
                        setVariant(selectedItem, false);
                    if (selectedItem.Variant2Source == null)
                        setVariant(selectedItem, true);
                }
            }));
            if (Comp.DeliveryAddress)
                LoadType(typeof(Uniconta.DataModel.WorkInstallation));
        }
    }
}
