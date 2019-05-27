using UnicontaClient.Models;
using UnicontaClient.Utilities;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using DevExpress.Xpf.Editors.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Collections;
using Uniconta.DataModel;
using Uniconta.ClientTools.Controls;
using Uniconta.API.DebtorCreditor;
using System.Windows.Threading;
using DevExpress.Xpf.Grid;
using UnicontaClient.Controls.Dialogs;
using DevExpress.XtraReports.UI;
using System.Reflection;
using Uniconta.API.Service;
using DevExpress.Xpf.Editors;
#if !SILVERLIGHT
using ubl_norway_uniconta;
using UnicontaClient.Pages;
using FromXSDFile.OIOUBL.ExportImport;
using Microsoft.Win32;
#endif
using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class CreateInvoicePage : GridBasePage
    {
        public override string NameOfControl
        {
            get { return TabControls.CreateInvoicePage.ToString(); }
        }
        public CreateInvoicePage(BaseAPI API) : base(API, string.Empty)
        {
            InitPage(null);
        }
        public CreateInvoicePage(UnicontaBaseEntity master)
            : base(null)
        {
            InitPage(master);
        }
        public CreateInvoicePage(UnicontaBaseEntity master, DebtorOrderLineClient[] orderLines)
           : base(null)
        {
            InitPage(master, orderLines);
        }

        bool hasEmail;
        DebtorOrderClient Order;
        Uniconta.API.DebtorCreditor.FindPrices PriceLookup;
        SQLCache items, warehouse, debtors, standardVariants, variants1, variants2;
        DebtorOrderClient initialOrder;
        double exchangeRate;
        bool linesFromProjectInvoice = false;
        private void InitPage(UnicontaBaseEntity master, DebtorOrderLineClient[] orderLines = null)
        {
            InitializeComponent();
            var api = this.api;
            var Comp = api.CompanyEntity;
            ((TableView)dgDebtorOrderLineGrid.View).RowStyle = Application.Current.Resources["StyleRow"] as Style;
            ledim1.api = ledim2.api = ledim3.api = ledim4.api = ledim5.api = LeAccount.api = lePayment.api = leTransType.api = LePostingAccount.api = leShipment.api = leDeliveryTerm.api = Projectlookupeditor.api = PrCategorylookupeditor.api = leLayoutGroup.api = employeelookupeditor.api = leInvAccount.api = leDeliveryAddress.api = lePriceList.api = api;
            var orderType = Comp.GetUserType(typeof(DebtorOrder));
            initialOrder = Activator.CreateInstance(orderType ?? typeof(DebtorOrderClient)) as DebtorOrderClient;
            initialOrder._DeliveryCountry = Comp._CountryId;
            if (master != null)
            {
                if (master is DebtorOrder)
                    StreamingManager.Copy(master, initialOrder);
                else
                    initialOrder.SetMaster(master);
                LeAccount.IsEnabled = false;
                lePrCategory.api = api;
                InputWindowOrder1.ActiveGroup = navGroupOrders;
                navGroupOrders.IsExpanded = true;
            }
            else
            {
                initialOrder.SetMaster(Comp);
                txtPrCategory.Visibility = lePrCategory.Visibility = Visibility.Collapsed;
            }
            setDimensions();
            localMenu.dataGrid = dgDebtorOrderLineGrid;
            SetRibbonControl(localMenu, dgDebtorOrderLineGrid);
            dgDebtorOrderLineGrid.api = api;
            dgDebtorOrderLineGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            this.Loaded += CreateInvoicePage_Loaded;
            dgDebtorOrderLineGrid.View.DataControl.CurrentItemChanged += DataControl_CurrentItemChanged;
            cbDeliveryCountry.ItemsSource = Enum.GetValues(typeof(Uniconta.Common.CountryCode));
            txtName.IsEnabled = false;
            dgDebtorOrderLineGrid.Visibility = Visibility.Visible;

#if !SILVERLIGHT
            txtDelZipCode.EditValueChanged += TxtDelZipCode_EditValueChanged;
            btnAccount.ToolTip = string.Format(Uniconta.ClientTools.Localization.lookup("CreateOBJ"), Uniconta.ClientTools.Localization.lookup("Debtor"));
#endif
            ShowCustomCloseConfiramtion = true;

            // we setup first order
            ClearFields();

            if (dgDebtorOrderLineGrid.IsLoadedFromLayoutSaved)
            {
                dgDebtorOrderLineGrid.ClearSorting();
                dgDebtorOrderLineGrid.IsLoadedFromLayoutSaved = false;
            }

            this.debtors = Comp.GetCache(typeof(Debtor));
            this.items = Comp.GetCache(typeof(InvItem));
            this.warehouse = Comp.GetCache(typeof(InvWarehouse));
            this.variants1 = Comp.GetCache(typeof(InvVariant1));
            this.variants2 = Comp.GetCache(typeof(InvVariant2));
            this.standardVariants = Comp.GetCache(typeof(InvStandardVariant));
            if (orderLines != null)
            {
                linesFromProjectInvoice = true;
                dgDebtorOrderLineGrid.SetSource(orderLines);
            }

            dgDebtorOrderLineGrid.allowSave = false;
#if SILVERLIGHT
            Application.Current.RootVisual.KeyDown += Page_KeyDown;
#else
            this.KeyDown += Page_KeyDown;
#endif
        }
        
        private void Page_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F8)
                localMenu_OnItemClicked("AddItems");

            if (e.Key == Key.F9)
            {
                dgDebtorOrderLineGrid.tableView.FocusedRowHandle = 0;
                dgDebtorOrderLineGrid.tableView.ShowEditor();
            }
        }

        public override Task InitQuery()
        {
            return null;
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            var company = api.CompanyEntity;
            if (!company.Shipments)
            {
                tbShipment.Visibility = Visibility.Collapsed;
                leShipment.Visibility = Visibility.Collapsed;
            }
            if (!company.Location || !company.Warehouse)
                Location.Visible = Location.ShowInColumnChooser = false;
            if (!company.Warehouse)
                Warehouse.Visible = Warehouse.ShowInColumnChooser = false;
            if (!company.SerialBatchNumbers)
                SerieBatch.Visible = SerieBatch.ShowInColumnChooser = false;
            if (!company.DeliveryAddress)
                delAddNavBar.IsVisible = false;

            Utility.SetupVariants(api, colVariant, colVariant1, colVariant2, colVariant3, colVariant4, colVariant5, Variant1Name, Variant2Name, Variant3Name, Variant4Name, Variant5Name);
            Utility.SetDimensionsGrid(api, cldim1, cldim2, cldim3, cldim4, cldim5);
        }
        private async void TxtDelZipCode_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            var s = sender as TextEditor;
            if (s != null && s.IsLoaded)
            {
                var deliveryCountry = Order.DeliveryCountry ?? api.CompanyEntity._CountryId;
                var city = await UtilDisplay.GetCityName(s.Text, deliveryCountry);
                if (city != null)
                    Order.DeliveryCity = city;
            }
        }

        bool loadOrderTemplate = false;
        protected override bool LoadTemplateHandledLocally(IEnumerable<UnicontaBaseEntity> templateRows)
        {
            if (!loadOrderTemplate)
                return false;

            var orderClient = (DebtorOrderClient)templateRows.FirstOrDefault();
            if (orderClient != null)
            {
                Order.Account = orderClient.Account;
                Order.SetCurrency(orderClient._Currency);

                Order.DeliveryName = orderClient._DeliveryName;
                Order.DeliveryAddress1 = orderClient._DeliveryAddress1;
                Order.DeliveryAddress2 = orderClient._DeliveryAddress2;
                Order.DeliveryAddress3 = orderClient._DeliveryAddress3;
                Order.DeliveryZipCode = orderClient._DeliveryZipCode;
                Order.DeliveryCity = orderClient._DeliveryCity;
                if (orderClient._DeliveryCountry != 0)
                    Order.DeliveryCountry = orderClient._DeliveryCountry;

                Order.Remark = orderClient.Remark;
                Order.YourRef = orderClient.YourRef;
                Order.OurRef = orderClient.OurRef;
                Order.Project = orderClient.Project;
                Order.PrCategory = orderClient.PrCategory;

                Order.OrderNumber = orderClient._OrderNumber;
                Order.Payment = orderClient._Payment;
                Order.PricesInclVat = orderClient._PricesInclVat;
                Order.EndDiscountPct = orderClient._EndDiscountPct;
                Order.PostingAccount = orderClient._PostingAccount;
                Order.Shipment = orderClient._Shipment;
                Order.LayoutGroup = orderClient._LayoutGroup;
                Order.DeliveryTerm = orderClient._DeliveryTerm;
                Order.TransType = orderClient._TransType;
            }
            return true;
        }

        private void CreateInvoicePage_Loaded(object sender, RoutedEventArgs e)
        {
            //navGroupAccounts.IsExpanded = false;
            //navGroupOrders.IsExpanded = true;
        }

        public override void Utility_Refresh(string screenName, object argument)
        {
            if (screenName == TabControls.InvItemStoragePage && argument != null)
            {
                var storeloc = argument as InvItemStorageClient;
                if (storeloc == null) return;
                var selected = dgDebtorOrderLineGrid.SelectedItem as DebtorOrderLineClient;
                if (selected == null) return;
                dgDebtorOrderLineGrid.SetLoadedRow(selected);
                selected.Warehouse = storeloc.Warehouse;
                selected.Location = storeloc.Location;
                dgDebtorOrderLineGrid.SetModifiedRow(selected);
            }

            if (screenName == TabControls.AddMultipleInventoryItem)
            {
                var param = argument as object[];
                if (param != null)
                {
                    var invItems = param[0] as List<UnicontaBaseEntity>;
                    var iSource = dgDebtorOrderLineGrid.ItemsSource as IEnumerable<DebtorOrderLineClient>;
                    var nonemptyList = new List<UnicontaBaseEntity>();
                    if (iSource != null)
                    {
                        foreach (var row in iSource)
                        {
                            if (row._Item != null || row._Text != null || row._Note != null)
                                nonemptyList.Add(row);
                        }
                        dgDebtorOrderLineGrid.SetSource(nonemptyList.ToArray());
                    }
                    dgDebtorOrderLineGrid.PasteRows(invItems);
                }
            }

            if (screenName == TabControls.AttachVoucherGridPage && argument != null)
            {
                var voucherObj = argument as object[];
                var voucher = voucherObj[0] as VouchersClient;
                if (voucher != null)
                    Order.DocumentRef = voucher.RowId;
            }
        }
        public override bool IsDataChaged
        {
            get
            {
                var lst = dgDebtorOrderLineGrid.ItemsSource as IEnumerable<DCOrderLine>;
                if (lst == null || !lst.Any())
                    return false;
                if (lst.Count() > 1)
                    return true;
                var lin = lst.First();
                return lin._Item != null || lin._Qty != 0 || lin._Amount != 0;
            }
        }
        private void DataControl_CurrentItemChanged(object sender, DevExpress.Xpf.Grid.CurrentItemChangedEventArgs e)
        {
            var oldselectedItem = e.OldItem as DebtorOrderLineClient;
            if (oldselectedItem != null)
                oldselectedItem.PropertyChanged -= DebtorOrderLineGrid_PropertyChanged;
            var selectedItem = e.NewItem as DebtorOrderLineClient;
            if (selectedItem != null)
            {
                selectedItem.PropertyChanged += DebtorOrderLineGrid_PropertyChanged;
                if (selectedItem.Variant1Source == null)
                    setVariant(selectedItem, false);
                if (selectedItem.Variant2Source == null)
                    setVariant(selectedItem, true);
            }
        }
        private void DebtorOrderLineGrid_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var rec = sender as DebtorOrderLineClient;
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
                        if (selectedItem._SalesQty != 0d)
                            rec.Qty = selectedItem._SalesQty;
                        else if (api.CompanyEntity._OrderLineOne)
                            rec.Qty = 1d;
                        rec.SetItemValues(selectedItem, api.CompanyEntity._OrderLineStorage);
                        this.PriceLookup?.SetPriceFromItem(rec, selectedItem);
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
                    }
                    break;
                case "Qty":
                    if (this.PriceLookup != null && this.PriceLookup.UseCustomerPrices)
                        this.PriceLookup.GetCustomerPrice(rec, false);
                    break;
                case "Total":
                    RecalculateAmount();
                    break;
                case "Warehouse":
                    if (warehouse != null)
                    {
                        var selected = (InvWarehouse)warehouse.Get(rec._Warehouse);
                        setLocation(selected, (DebtorOrderLineClient)rec);
                    }
                    break;
                case "Location":
                    if (string.IsNullOrEmpty(rec._Warehouse))
                        rec._Location = null;
                    break;
                case "EAN":
                    DebtorOfferLines.FindOnEAN(rec, this.items, api);
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
               
            }
        }
        async void RecalculateAmount()
        {
            if (this.exchangeRate == 0d && this.PriceLookup != null)
            {
                var t = this.PriceLookup.ExchangeTask;
                this.exchangeRate = this.PriceLookup.ExchangeRate;
                if (this.exchangeRate == 0d && t != null)
                    this.exchangeRate = await t;
            }

            var ret = DebtorOfferLines.RecalculateLineSum((IList)dgDebtorOrderLineGrid.ItemsSource, this.exchangeRate);
            double Amountsum = ret.Item1;
            double Costsum = ret.Item2;
            double sales = ret.Item3;
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            var groups = UtilDisplay.GetMenuCommandsByStatus(rb, true);
            foreach (var grp in groups)
            {
                if (grp.Caption == Uniconta.ClientTools.Localization.lookup("Amount"))
                    grp.StatusValue = Amountsum.ToString("N2");
                else if (grp.Caption == Uniconta.ClientTools.Localization.lookup("CostValue"))
                    grp.StatusValue = Costsum.ToString("N2");
                else if (grp.Caption == Uniconta.ClientTools.Localization.lookup("DB"))
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
                else grp.StatusValue = string.Empty;
            }
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            DebtorOrderLineClient row;
            switch (ActionType)
            {
                case "AddRow":
                    row = dgDebtorOrderLineGrid.AddRow() as DebtorOrderLineClient;
                    row._ExchangeRate = this.exchangeRate;
                    break;
                case "CopyRow":
                    var org = dgDebtorOrderLineGrid.SelectedItem as DebtorOrderLineClient;
                    if (org != null)
                    {
                        row = dgDebtorOrderLineGrid.CopyRow() as DebtorOrderLineClient;
                        row._ExchangeRate = this.exchangeRate;
                        row._CostPriceLine = org._CostPriceLine;
                    }
                    break;
                case "DeleteRow":
                    dgDebtorOrderLineGrid.DeleteRow();
                    break;
                case "GenerateInvoice":
                    if (!string.IsNullOrEmpty(Order._DCAccount))
                    {
                        if (Utility.HasControlRights("GenerateInvoice", api.CompanyEntity))
                        {
                            var debtor = ClientHelper.GetRef(Order.CompanyId, typeof(Debtor), Order._DCAccount) as Debtor;
                            if (debtor != null)
                            {
                                var InvoiceAccount = Order._InvoiceAccount ?? debtor._InvoiceAccount;
                                if (InvoiceAccount != null)
                                    debtor = ClientHelper.GetRef(Order.CompanyId, typeof(Debtor), InvoiceAccount) as Debtor;
                                if (debtor._PricesInclVat != Order._PricesInclVat)
                                {
                                    var confirmationMsgBox = UnicontaMessageBox.Show(string.Format("{0}.\n{1}", string.Format(Uniconta.ClientTools.Localization.lookup("DebtorAndOrderMix"), Uniconta.ClientTools.Localization.lookup("InclVat")),
                                    Uniconta.ClientTools.Localization.lookup("ProceedConfirmation")), Uniconta.ClientTools.Localization.lookup("Confirmation"), MessageBoxButton.OKCancel);
                                    if (confirmationMsgBox != MessageBoxResult.OK)
                                        return;
                                }
                            }
                            GenerateInvoice(Order);
                        }
                        else
                            UtilDisplay.ShowControlAccessMsg("GenerateInvoice");
                    }
                    else
                    {
                        string strmg = string.Format(Uniconta.ClientTools.Localization.lookup("CannotBeBlank"), Uniconta.ClientTools.Localization.lookup("Account"));
                        UnicontaMessageBox.Show(strmg, Uniconta.ClientTools.Localization.lookup("Warning"), MessageBoxButton.OK);
                    }
                    break;
                case "Storage":
                    AddDockItem(TabControls.InvItemStoragePage, dgDebtorOrderLineGrid.syncEntity, true);
                    break;
                case "InsertSubTotal":
                    dgDebtorOrderLineGrid.AddRow(new DebtorOrderLineClient { Subtotal = true });
                    break;
                case "LoadOrderTemplate":
                    loadOrderTemplate = true;
                    LoadSavedTemplates(typeof(DebtorOrderClient), true);
                    break;
                case "LoadLinesTemplate":
                    loadOrderTemplate = false;
                    gridRibbon_BaseActions("LoadTemplate");
                    break;
                case "ViewVoucher":
                    busyIndicator.IsBusy = true;
                    ViewVoucher(TabControls.VouchersPage3, Order);
                    busyIndicator.IsBusy = false;
                    break;
                case "ImportVoucher":
                    Utility.ImportVoucher(Order, api);
                    break;
                case "RemoveVoucher":
                    RemoveVoucher(Order);
                    break;
                case "CreateFromInvoice":
                    try
                    {
                        CWCreateOrderFromQuickInvoice createOrderCW = new CWCreateOrderFromQuickInvoice(api, Order.Account);
                        createOrderCW.Closing += async delegate
                        {
                            if (createOrderCW.DialogResult == true)
                            {
                                var orderApi = new OrderAPI(api);
                                var checkIfCreditNote = createOrderCW.chkIfCreditNote.IsChecked.HasValue ? createOrderCW.chkIfCreditNote.IsChecked.Value : false;
                                var selectedItem = createOrderCW.dgCreateOrderGrid.SelectedItem as DebtorInvoiceLocal;

                                await CreateOrderFromInvoice(Order, selectedItem, checkIfCreditNote);
                                dgDebtorOrderLineGrid.ItemsSource = Order.Lines;
                                LeAccount.Focus();
                            }
                        };
                        createOrderCW.Show();
                    }
                    catch (Exception ex)
                    {
                        UnicontaMessageBox.Show(ex.Message, Uniconta.ClientTools.Localization.lookup("Exception"));
                    }
                    break;
                case "StockLines":
                    if (dgDebtorOrderLineGrid.SelectedItem == null) return;

                    var debtOrderLine = dgDebtorOrderLineGrid.SelectedItem as DebtorOrderLineClient;
                    if (!string.IsNullOrEmpty(debtOrderLine?._Item))
                        AddDockItem(TabControls.DebtorInvoiceLines, debtOrderLine, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("InvTransaction"), debtOrderLine._Item));
                    break;
                case "AttachDoc":
                    AttachDocuments();
                    break;
                case "AddItems":
                    if (this.items == null)
                        return;
                    object[] paramArray = new object[3] { new InvItemSalesCacheFilter(this.items), dgDebtorOrderLineGrid.TableTypeUser, Order };
                    AddDockItem(TabControls.AddMultipleInventoryItem, paramArray, true,
                        string.Format(Uniconta.ClientTools.Localization.lookup("AddOBJ"), Uniconta.ClientTools.Localization.lookup("InventoryItems")), null, floatingLoc: Utility.GetDefaultLocation());
                    break;
                case "RefVoucher":
                    var _refferedVouchers = new List<int>();
                    if (Order._DocumentRef != 0)
                        _refferedVouchers.Add(Order._DocumentRef);

                    AddDockItem(TabControls.AttachVoucherGridPage, new object[1] { _refferedVouchers }, true);
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
            RecalculateAmount();
        }

        public override void RowsPastedDone() { RecalculateAmount(); }

        public override void RowPasted(UnicontaBaseEntity rec)
        {
            var orderLine = (DebtorOrderLineClient)rec;
            if (orderLine._Item != null)
            {
                var selectedItem = (InvItem)items.Get(orderLine._Item);
                if (selectedItem != null)
                {
                    PriceLookup?.SetPriceFromItem(orderLine, selectedItem);
                    orderLine.SetItemValues(selectedItem, 0);
                    TableField.SetUserFieldsFromRecord(selectedItem, orderLine);
                }
                else
                    orderLine._Item = null;
            }
        }

        async Task CreateOrderFromInvoice(DebtorOrderClient order, DebtorInvoiceLocal invoice, bool checkIfCreditNote)
        {
            order.OrderNumber = invoice._OrderNumber;
            order.Account = invoice._DCAccount;
            order.LayoutGroup = invoice._LayoutGroup;
            order.DeliveryName = invoice._DeliveryName;
            order.DeliveryAddress1 = invoice._DeliveryAddress1;
            order.DeliveryAddress2 = invoice._DeliveryAddress2;
            order.DeliveryAddress3 = invoice._DeliveryAddress3;
            order.DeliveryZipCode = invoice._DeliveryZipCode;
            order.DeliveryCity = invoice._DeliveryCity;
            order.DeliveryCountry = invoice._DeliveryCountry;
            order.Installation = invoice._Installation;
            order.YourRef = invoice._YourRef;
            order.OurRef = invoice._OurRef;
            order.Requisition = invoice._Requisition;
            order.Remark = invoice._Remark;
            order.Project = invoice._Project;
            order.Employee = invoice._Employee;
            order.EndDiscountPct = invoice._EndDiscountPct;
            order.Payment = invoice._Payment;
            order.Currency = invoice.Currency;
            order.PricesInclVat = invoice._PricesInclVat;
            order.Shipment = invoice._Shipment;
            order.DeliveryTerm = invoice._DeliveryTerm;
            order.Dimension1 = invoice._Dim1;
            order.Dimension2 = invoice._Dim2;
            order.Dimension3 = invoice._Dim3;
            order.Dimension4 = invoice._Dim4;
            order.Dimension5 = invoice._Dim5;
            if (checkIfCreditNote)
                order.Settlements = invoice._InvoiceNumber;

            var orderlines = new List<DebtorOrderLineClient>();
            order.Lines = orderlines;

            var invoiceLines = await api.Query<DebtorInvoiceLines>(invoice);
            if (invoiceLines == null || invoiceLines.Length == 0)
                return;

            int lineNo = 0;
            double sign = checkIfCreditNote ? -1d : 1d;
            foreach (var invoiceline in invoiceLines)
            {
                var line = new DebtorOrderLineClient();
                line.SetMaster(order);

                line._LineNumber = ++lineNo;
                line._Item = invoiceline._Item;
                line._DiscountPct = invoiceline._DiscountPct;
                line._Discount = invoiceline._Discount;
                line._Qty = invoiceline.InvoiceQty * sign;
                line._Price = (invoiceline.CurrencyEnum != null ? invoiceline._PriceCur : invoiceline._Price);
                if (line._Price != 0)
                    line._Price += invoiceline._PriceVatPart;

                if (line._Qty * line._Price == 0)
                    line._AmountEntered = ((invoiceline.CurrencyEnum != null ? invoiceline._AmountCur : invoiceline._Amount) + invoiceline._PriceVatPart) * sign;

                line._Dim1 = invoiceline._Dim1;
                line._Dim2 = invoiceline._Dim2;
                line._Dim3 = invoiceline._Dim3;
                line._Dim4 = invoiceline._Dim4;
                line._Dim5 = invoiceline._Dim5;
                line._Employee = invoiceline._Employee;
                line._Note = invoiceline._Note;
                line._Text = invoiceline._Text;
                line._Unit = invoiceline._Unit;
                line._Variant1 = invoiceline._Variant1;
                line._Variant2 = invoiceline._Variant2;
                line._Variant3 = invoiceline._Variant3;
                line._Variant4 = invoiceline._Variant4;
                line._Variant5 = invoiceline._Variant5;
                line._Warehouse = invoiceline._Warehouse;
                line._Location = invoiceline._Location;

                var selectedItem = (InvItem)items?.Get(invoiceline._Item);
                if (selectedItem != null)
                {
                    line._Item = selectedItem._Item;
                    line._CostPriceLine = selectedItem._CostPrice;
                    if (selectedItem._Unit != 0)
                        line._Unit = selectedItem._Unit;
                }

                orderlines.Add(line);
            }
        }

        void ClearFields()
        {
            Order = StreamingManager.Clone(initialOrder) as DebtorOrderClient;
            this.DataContext = Order;
            dgDebtorOrderLineGrid.UpdateMaster(Order);
            dgDebtorOrderLineGrid.ItemsSource = null;
            dgDebtorOrderLineGrid.AddFirstRow();
            LeAccount.Focus();
        }

        static bool showInvPrintPrv = true;
        private void GenerateInvoice(DebtorOrderClient dbOrder)
        {
            IEnumerable<DCOrderLineClient> lines = (IEnumerable<DCOrderLineClient>)dgDebtorOrderLineGrid.ItemsSource;
            if (lines == null || lines.Count() == 0)
                return;
            var dc = dbOrder.Debtor;
            if (!Utility.IsExecuteWithBlockedAccount(dc)) return;

            if (!api.CompanyEntity.SameCurrency(dbOrder._Currency, dc._Currency))
            {
                var confirmationMsgBox = UnicontaMessageBox.Show(string.Format("{0}.\n{1}", string.Format(Uniconta.ClientTools.Localization.lookup("CurrencyMismatch"), dc.Currency, dbOrder.Currency),
                Uniconta.ClientTools.Localization.lookup("ProceedConfirmation")), Uniconta.ClientTools.Localization.lookup("Confirmation"), MessageBoxButton.OKCancel);
                if (confirmationMsgBox != MessageBoxResult.OK)
                    return;
            }

            if (api.CompanyEntity._InvoiceUseQtyNow)
            {
                foreach (var rec in lines)
                    rec._QtyNow = rec._Qty;
            }

            string debtorName = dbOrder.Debtor?.Name ?? dbOrder._DCAccount;
            bool invoiceInXML = dc?.InvoiceInXML ?? false;
            CWGenerateInvoice GenrateInvoiceDialog = new CWGenerateInvoice(true, string.Empty, false, askForEmail: true, showNoEmailMsg: !hasEmail, debtorName: debtorName, isOrderOrQuickInv: true, isDebtorOrder: true, InvoiceInXML: invoiceInXML);
#if !SILVERLIGHT
            GenrateInvoiceDialog.DialogTableId = 2000000005;
#endif
            GenrateInvoiceDialog.SetInvPrintPreview(showInvPrintPrv);
            GenrateInvoiceDialog.Closed += async delegate
            {
                if (GenrateInvoiceDialog.DialogResult == true)
                {
                    showInvPrintPrv = GenrateInvoiceDialog.ShowInvoice;
                    busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("SendingWait");
                    var showOrPrintInvoice = GenrateInvoiceDialog.InvoiceQuickPrint || GenrateInvoiceDialog.ShowInvoice;

                    var invoicePostingResult = new InvoicePostingPrintGenerator(api, this, dbOrder, lines, GenrateInvoiceDialog.GenrateDate, 0, GenrateInvoiceDialog.IsSimulation, CompanyLayoutType.Invoice, showOrPrintInvoice,
                        GenrateInvoiceDialog.InvoiceQuickPrint, GenrateInvoiceDialog.NumberOfPages, GenrateInvoiceDialog.SendByEmail, GenrateInvoiceDialog.Emails, GenrateInvoiceDialog.sendOnlyToThisEmail, GenrateInvoiceDialog.GenerateOIOUBLClicked,
                        GenrateInvoiceDialog.PostOnlyDelivered, documents);

                    busyIndicator.IsBusy = true;
                    busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("GeneratingPage");
                    var result = await invoicePostingResult.Execute();
                    busyIndicator.IsBusy = false;

                    if (result)
                    {
                        if (!GenrateInvoiceDialog.IsSimulation)
                        {
                            documents = null;
                            if(attachDocMenu!= null)
                                attachDocMenu.Caption = string.Format(Uniconta.ClientTools.Localization.lookup("AttachOBJ"), Uniconta.ClientTools.Localization.lookup("Documents"));
                        }

                        if (invoicePostingResult.PostingResult.Header._InvoiceNumber != 0)
                        {
                            var msg = string.Format(Uniconta.ClientTools.Localization.lookup("InvoiceHasBeenGenerated"), invoicePostingResult.PostingResult.Header._InvoiceNumber);
                            msg = string.Format("{0}{1}{2} {3}", msg, Environment.NewLine, Uniconta.ClientTools.Localization.lookup("LedgerVoucher"), invoicePostingResult.PostingResult.Header._Voucher);
                            var resultMessage = UnicontaMessageBox.Show(msg, Uniconta.ClientTools.Localization.lookup("Message"));
#if !SILVERLIGHT
                            if (GenrateInvoiceDialog.GenerateOIOUBLClicked)
                                DebtorOrders.GenerateOIOXml(api, invoicePostingResult.PostingResult);
#endif
                            ClearFields();
                        }
                    }
                    else
                        Utility.ShowJournalError(invoicePostingResult.PostingResult.ledgerRes, dgDebtorOrderLineGrid);
                }
            };
            GenrateInvoiceDialog.Show();
        }

        void leAccount_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            string id = Convert.ToString(e.NewValue);
            var Debtor = (Debtor)debtors?.Get(id);
            if (Debtor != null)
            {
                Order.Account = Debtor._Account;
                Order.SetCurrency(Debtor._Currency);
                Order.DeliveryName = Debtor._DeliveryName;
                Order.DeliveryAddress1 = Debtor._DeliveryAddress1;
                Order.DeliveryAddress2 = Debtor._DeliveryAddress2;
                Order.DeliveryAddress3 = Debtor._DeliveryAddress3;
                Order.DeliveryZipCode = Debtor._DeliveryZipCode;
                Order.DeliveryCity = Debtor._DeliveryCity;
                if (Debtor._DeliveryCountry != 0)
                    Order.DeliveryCountry = Debtor._DeliveryCountry;
                Order.Payment = Debtor._Payment;
                Order.PricesInclVat = Debtor._PricesInclVat;
                Order.EndDiscountPct = Debtor._EndDiscountPct;
                Order.PostingAccount = Debtor._PostingAccount;
                Order.Shipment = Debtor._Shipment;
                Order.LayoutGroup = Debtor._LayoutGroup;
                Order.Employee = Debtor._Employee;
                Order.DeliveryTerm = Debtor._DeliveryTerm;
                hasEmail = Debtor._InvoiceEmail != null;
                PriceLookup?.OrderChanged(Order);
                BindContact(Debtor);
                if (installationCache != null)
                {
                    leDeliveryAddress.cacheFilter = new AccountCacheFilter(installationCache, Debtor.__DCType(), Debtor._Account);
                    leDeliveryAddress.InvalidCache();
                }
                IEnumerable<DCOrderLineClient> lines = (IEnumerable<DCOrderLineClient>)dgDebtorOrderLineGrid.ItemsSource;
                if (!linesFromProjectInvoice)
                    lines?.FirstOrDefault()?.SetMaster(Order);
                Dispatcher.BeginInvoke(new Action(async () =>
                {
                    await api.Read(Debtor);
                    Order.RefreshBalance();
                }));
            }
        }

        async void setVariant(DebtorOrderLineClient rec, bool SetVariant2)
        {
            if (items == null || variants1 == null || variants2 == null)
                return;

            //Check for Variant2 Exist
            if (api.CompanyEntity.NumberOfVariants < 2)
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
                    rec.Variant1Source = this.variants1?.GetKeyStrRecords.Cast<InvVariant1>();
                    rec.Variant2Source = this.variants2?.GetKeyStrRecords.Cast<InvVariant2>();
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
                                invs2.Add(v2);
                                if (var2Value == v2._Variant)
                                    hasVariantValue = true;

                            }
                        }
                        else if (LastVariant != cmb._Variant1)
                        {
                            LastVariant = cmb._Variant1;
                            var v1 = (InvVariant1)variants1.Get(cmb._Variant1);
                            invs1.Add(v1);
                        }
                    }
                    if (SetVariant2)
                    {
                        rec.variant2Source = invs2;
                        if (!hasVariantValue)
                            rec.Variant2 = null;
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
        async void setLocation(InvWarehouse master, DebtorOrderLineClient rec)
        {
            if (api.CompanyEntity.Location)
            {
                if (master != null)
                {
                    if (master.Locations != null)
                        rec.locationSource = master.Locations;
                    else
                        rec.locationSource = await master.LoadLocations(api);
                }
                else
                {
                    rec.locationSource = null;
                    rec.Location = null;
                }
                rec.NotifyPropertyChanged("LocationSource");
            }
        }
        CorasauGridLookupEditorClient prevLocation;
        private void Location_GotFocus(object sender, RoutedEventArgs e)
        {
            DebtorOrderLineClient selectedItem = dgDebtorOrderLineGrid.SelectedItem as DebtorOrderLineClient;
            if (selectedItem != null && selectedItem._Warehouse != null && warehouse != null)
            {
                var selected = (InvWarehouse)warehouse.Get(selectedItem._Warehouse);
                setLocation(selected, selectedItem);
                if (prevLocation != null)
                    prevLocation.isValidate = false;
                var editor = (CorasauGridLookupEditorClient)sender;
                prevLocation = editor;
                editor.isValidate = true;
            }
        }
        CorasauGridLookupEditorClient prevVariant1;

        private void lePriceList_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            PriceLookup?.OrderChanged(Order);
        }

        private void variant1_GotFocus(object sender, RoutedEventArgs e)
        {
            if (prevVariant1 != null)
                prevVariant1.isValidate = false;
            var editor = (CorasauGridLookupEditorClient)sender;
            prevVariant1 = editor;
            editor.isValidate = true;
        }

        CorasauGridLookupEditorClient prevVariant2;
        private void variant2_GotFocus(object sender, RoutedEventArgs e)
        {
            if (prevVariant2 != null)
                prevVariant2.isValidate = false;
            var editor = (CorasauGridLookupEditorClient)sender;
            prevVariant2 = editor;
            editor.isValidate = true;
        }

        SQLCache installationCache;
        protected override async void LoadCacheInBackGround()
        {
            var api = this.api;
            var Comp = api.CompanyEntity;

            if (this.debtors == null)
                this.debtors = Comp.GetCache(typeof(Uniconta.DataModel.Debtor)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.Debtor), api).ConfigureAwait(false);

            if (this.items == null)
                this.items = Comp.GetCache(typeof(Uniconta.DataModel.InvItem)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.InvItem), api).ConfigureAwait(false);

            if (Comp.Warehouse && this.warehouse == null)
                this.warehouse = await Comp.LoadCache(typeof(Uniconta.DataModel.InvWarehouse), api).ConfigureAwait(false);

            if (Comp.ItemVariants)
            {
                if (this.standardVariants == null)
                    this.standardVariants = Comp.GetCache(typeof(Uniconta.DataModel.InvStandardVariant)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.InvStandardVariant), api).ConfigureAwait(false);
                if (this.variants1 == null)
                    this.variants1 = Comp.GetCache(typeof(Uniconta.DataModel.InvVariant1)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.InvVariant1), api).ConfigureAwait(false);
                if (this.variants2 == null)
                    this.variants2 = Comp.GetCache(typeof(Uniconta.DataModel.InvVariant2)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.InvVariant2), api).ConfigureAwait(false);
            }

            if (Comp.DeliveryAddress)
                installationCache = Comp.GetCache(typeof(Uniconta.DataModel.WorkInstallation)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.WorkInstallation), api).ConfigureAwait(false);

            PriceLookup = new Uniconta.API.DebtorCreditor.FindPrices(Order, api);
            if (this.PriceLookup != null)
            {
                var t = this.PriceLookup.ExchangeTask;
                this.exchangeRate = this.PriceLookup.ExchangeRate;
                if (this.exchangeRate == 0d && t != null)
                    this.exchangeRate = await t.ConfigureAwait(false);
            }
            Dispatcher.BeginInvoke(new Action(() =>
            {
                var selectedItem = dgDebtorOrderLineGrid.SelectedItem as DebtorOrderLineClient;
                if (selectedItem != null)
                {
                    if (selectedItem.Variant1Source == null)
                        setVariant(selectedItem, false);
                    if (selectedItem.Variant2Source == null)
                        setVariant(selectedItem, true);
                }
            }));
        }

        public void setDimensions()
        {
            var c = api.CompanyEntity;
            lbldim1.Text = c._Dim1;
            lbldim2.Text = c._Dim2;
            lbldim3.Text = c._Dim3;
            lbldim4.Text = c._Dim4;
            lbldim5.Text = c._Dim5;

            int noofDimensions = c.NumberOfDimensions;
            if (noofDimensions < 5)
                lbldim5.Visibility = ledim5.Visibility = Visibility.Collapsed;
            if (noofDimensions < 4)
                lbldim4.Visibility = ledim4.Visibility = Visibility.Collapsed;
            if (noofDimensions < 3)
                lbldim3.Visibility = ledim3.Visibility = Visibility.Collapsed;
            if (noofDimensions < 2)
                lbldim2.Visibility = ledim2.Visibility = Visibility.Collapsed;
            if (noofDimensions < 1)
                lbldim1.Visibility = ledim1.Visibility = Visibility.Collapsed;
        }

        List<TableAddOnData> documents;

        private void btnAccount_Click(object sender, RoutedEventArgs e)
        {
            object[] param = new object[2];
            param[0] = api;
            param[1] = null;
            AddDockItem(TabControls.DebtorAccountPage2, param, Uniconta.ClientTools.Localization.lookup("DebtorAccount"), ";component/Assets/img/Add_16x16.png");
        }

        FileBrowseControl fileBrowser;
        ItemBase attachDocMenu;
        void AttachDocuments()
        {
            if (fileBrowser == null)
                fileBrowser = new FileBrowseControl();
            else
                fileBrowser.SelectedFileInfos = null;
            fileBrowser.IsMultiSelect = true;
            fileBrowser.BrowseFile();
            var fileList = fileBrowser.SelectedFileInfos;
            if (fileList != null)
            {
                if (documents == null)
                    documents = new List<TableAddOnData>();
                foreach (var file in fileList)
                {
                    documents.Add(new TableAddOnData
                    {
                        _Text = file.FileName,
                        _DocumentType = DocumentConvert.GetDocumentType(file.FileExtension),
                        _Data = file.FileBytes
                    });
                }
            }
            if (documents?.Count > 0)
            {
                var rb = (RibbonBase)localMenu.DataContext;
                attachDocMenu = UtilDisplay.GetMenuCommandByName(rb, "AttachDoc");
                attachDocMenu.Caption = string.Format("{0} ({1})", string.Format(Uniconta.ClientTools.Localization.lookup("AttachOBJ"), Uniconta.ClientTools.Localization.lookup("Documents")), documents.Count);
            }
        }

        int contactRefId;
        async void BindContact(Debtor debtor)
        {
            if (debtor == null)
                return;

            contactRefId = Order._ContactRef;
            var Comp = api.CompanyEntity;
            var cache = Comp.GetCache(typeof(Uniconta.DataModel.Contact)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.Contact), api);
            var items = ((IEnumerable<Uniconta.DataModel.Contact>)cache?.GetNotNullArray)?.Where(x => x._DCType == 1 && x._DCAccount == debtor._Account);
            cmbContactName.ItemsSource = items;
            cmbContactName.DisplayMember = "KeyName";

            if (contactRefId != 0 && items != null)
            {
                var contact = items.Where(x => x.RowId == contactRefId).FirstOrDefault();
                cmbContactName.SelectedItem = contact;
                if (contact == null)
                {
                    Order._ContactRef = 0;
                    Order.ContactName = null;
                }
            }
        }

        private void cmbContactName_EditValueChanged(object sender, EditValueChangedEventArgs e)
        {
            var selectedItem = cmbContactName.SelectedItem as Contact;
            if (selectedItem != null)
            {
                Order._ContactRef = selectedItem.RowId;
                Order.ContactName = selectedItem._Name;
            }
            else
            {
                Order._ContactRef = 0;
                Order.ContactName = null;
            }
        }
    }
}

