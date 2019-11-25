using DevExpress.Xpf.Grid;
using System;
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
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.Common;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
   public class DCorderGrid : CorasauDataGridClient
    {
        public override Type TableType
        {
            get
            {
                return isOrder ? typeof(DebtorOrderLineClient): typeof(DebtorOrderLineClient);
            }
        }

        public bool isOrder { get; set; }
    }

    public partial class CWCreateOrderFromQuickInvoice : ChildWindow
    {
        public string Account { get; set; }
        bool isOrder;
        DCOrder dcOrder;
        CrudAPI api;
        SQLCache items;
        public List<DebtorOrderLineClient> debtorOrderLines;
        public List<DebtorOfferLineClient> debtorOfferLines;
        bool disableOrderLineGrid = false;
        Company company;
        public CWCreateOrderFromQuickInvoice(CrudAPI _api, string account)
        {
            api = _api;
            this.Account = account;
            disableOrderLineGrid = true;
            InitPage();
        }
        public CWCreateOrderFromQuickInvoice(CrudAPI _api, string account, bool _isOrder, DCOrder _dcOrder)
        {
            api = _api;
            this.Account = account;
            dcOrder = _dcOrder;
            isOrder = _isOrder;
            InitPage();
        }

        void InitPage()
        {
            this.DataContext = this;
            InitializeComponent();
            dcOrderlineGrid.isOrder = isOrder ;
            ((TableView)dgCreateOrderGrid.View).RowStyle = Application.Current.Resources["StyleRow"] as Style;
            ((TableView)dcOrderlineGrid.View).RowStyle = Application.Current.Resources["StyleRow"] as Style;
            company = api.CompanyEntity;
            this.items = company.GetCache(typeof(InvItem));
            this.Title = String.Format(Uniconta.ClientTools.Localization.lookup("CopyOBJ"), Uniconta.ClientTools.Localization.lookup("Invoice"));
            this.Loaded += CW_Loaded;
            dgCreateOrderGrid.BusyIndicator = busyIndicator;
            dgCreateOrderGrid.api = api;
            localMenu.dataGrid = dgCreateOrderGrid;
            dgCreateOrderGrid.SelectedItemChanged += DgCreateOrderGrid_SelectedItemChanged;
            if (disableOrderLineGrid)
                layOutOrderline.Visibility = Visibility.Collapsed;
            if (!this.dgCreateOrderGrid.ReuseCache(typeof(Uniconta.DataModel.DCInvoice)))
                BindGrid();
            if (!company.ItemVariants)
                colVariant.Visible =  false;
        }

        private void DgCreateOrderGrid_SelectedItemChanged(object sender, DevExpress.Xpf.Grid.SelectedItemChangedEventArgs e)
        {
            var selectedItem = e.NewItem as DebtorInvoiceLocal;
            if (selectedItem != null && !disableOrderLineGrid)
                LoadOrderlines(selectedItem);
        }

        async void LoadOrderlines(DebtorInvoiceLocal selectedItem)
        {
            busyIndicator.IsBusy = true;
            if (isOrder)
                await CreateOrderLinesFromInvoice(dcOrder as DebtorOrderClient, selectedItem, (bool)chkIfCreditNote.IsChecked);
            else
                await CreateOfferLinesFromInvoice(dcOrder as DebtorOfferClient, selectedItem, (bool)chkIfCreditNote.IsChecked);
            busyIndicator.IsBusy = false;
        }

        void CW_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => { CreateButton.Focus(); }));
        }

        private void ChildWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.DialogResult = false;
            }
            else if (e.Key == Key.Enter)
            {
                if (CreateButton.IsFocused)
                    CreateButton_Click(null, null);
                else if (CancelButton.IsFocused)
                    this.DialogResult = false;
            }
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            if (dgCreateOrderGrid.SelectedItem == null)
            {
                UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("RecordNotSelected"), Uniconta.ClientTools.Localization.lookup("Warning"));
                return;
            }
            else
                this.DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private Task Filter(IEnumerable<PropValuePair> propValuePair)
        {
            if(!string.IsNullOrWhiteSpace(Account))
                propValuePair = new List<PropValuePair>(){ PropValuePair.GenereteWhereElements("Account", Account, CompareOperator.Equal) };
            
            return dgCreateOrderGrid.Filter(propValuePair);
        }

        private void BindGrid()
        {
            var t = Filter(null);
        }

        async Task CreateOfferLinesFromInvoice(DebtorOfferClient order, DebtorInvoiceLocal invoice, bool checkIfCreditNote)
        {
            var offerlines = new List<DebtorOfferLineClient>();
            order.OfferLines = offerlines;
            var invoiceLines = await api.Query<DebtorInvoiceLines>(invoice);
            if (invoiceLines == null || invoiceLines.Length == 0)
                return;

            offerlines.Capacity = invoiceLines.Length;
            int lineNo = 0;
            double sign = checkIfCreditNote ? -1d : 1d;
            foreach (var invoiceline in invoiceLines)
            {
                var line = new DebtorOfferLineClient();
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

                offerlines.Add(line);
            }
            dcOrderlineGrid.ItemsSource = debtorOfferLines=null;
            dcOrderlineGrid.ItemsSource = debtorOfferLines = offerlines;
            dcOrderlineGrid.Visibility = Visibility.Visible;
        }

        async Task CreateOrderLinesFromInvoice(DebtorOrderClient order, DebtorInvoiceLocal invoice, bool checkIfCreditNote)
        {
            var orderlines = new List<DebtorOrderLineClient>();
            order.Lines = orderlines;
            var invoiceLines = await api.Query<DebtorInvoiceLines>(invoice);
            if (invoiceLines == null || invoiceLines.Length == 0)
                return;

            orderlines.Capacity = invoiceLines.Length;
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
            dcOrderlineGrid.ItemsSource = debtorOrderLines =null;
            dcOrderlineGrid.ItemsSource = debtorOrderLines = orderlines;
            dcOrderlineGrid.Visibility = Visibility.Visible;
        }
       
        private void chkIfCreditNote_Checked(object sender, RoutedEventArgs e)
        {
            var selectedItem = dgCreateOrderGrid.SelectedItem as DebtorInvoiceLocal;
            if (selectedItem != null && !disableOrderLineGrid)
            {
                double sign = (bool)chkIfCreditNote.IsChecked ? -1d : 1d;
                if (isOrder)
                {
                    foreach (var line in dcOrderlineGrid.ItemsSource as List<DebtorOrderLineClient>)
                        line.Qty = line.Qty > 0d ? line.Qty * sign : line.Qty * -1d;
                }
                else
                {
                    foreach (var line in dcOrderlineGrid.ItemsSource as List<DebtorOfferLineClient>)
                        line.Qty = line.Qty > 0d ? line.Qty * sign : line.Qty * -1d;
                }
            }
        }
    }
}
