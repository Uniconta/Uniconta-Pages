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
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.DataModel;
using UnicontaClient.Models;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class DCorderGrid : CorasauDataGridClient
    {
        public override Type TableType
        {
            get
            {
                return isOrder ? typeof(DebtorOrderLineClient) : typeof(DebtorOrderLineClient);
            }
        }

        public bool isOrder { get; set; }
    }

    public partial class CreateOrderFromQuickInvoice : GridBasePage
    {
        public string Account { get; set; }
        bool isOrder;
        DCOrder dcOrder;
        CrudAPI api;
        SQLCache items;
        public IEnumerable<UnicontaBaseEntity> DCOrderLines;
        bool disableOrderLineGrid = false;
        Company company;
        static double pageHeight = 650.0d, pageWidth = 700.0d;
        static Point position = new Point();
        public override string NameOfControl => TabControls.CreateOrderFromQuickInvoice;
        public bool IsDeleteLines { get; set; }
        public override void PageClosing()
        {
            if (dgCreateOrderGrid.SelectedItem != null && createbuttonclicked)
            {
                var args = new object[4];
                args[0] = dgCreateOrderGrid.SelectedItem;
                args[1] = chkIfCreditNote.IsChecked.HasValue ? chkIfCreditNote.IsChecked.Value : false;
                args[2] = DCOrderLines;
                args[3] = IsDeleteLines;
                globalEvents.OnRefresh(NameOfControl, args);
            }
            position = AttachVoucherGridPage.GetPosition(dockCtrl);
        }
        public CreateOrderFromQuickInvoice(CrudAPI _api, string account) : base(_api, string.Empty)
        {
            api = _api;
            this.Account = account;
            disableOrderLineGrid = true;
            InitPage();
        }
        public CreateOrderFromQuickInvoice(CrudAPI _api, string account, bool _isOrder, DCOrder _dcOrder) : base(_api, string.Empty)
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
            if (dcOrder is ProjectInvoiceProposalClient)
                chkDelete.Visibility = Visibility.Visible;
            dcOrderlineGrid.isOrder = isOrder;
            ((TableView)dgCreateOrderGrid.View).RowStyle = Application.Current.Resources["StyleRow"] as Style;
            ((TableView)dcOrderlineGrid.View).RowStyle = Application.Current.Resources["StyleRow"] as Style;
            company = api.CompanyEntity;
            this.items = company.GetCache(typeof(InvItem));
            this.SetHeader(String.Format(Uniconta.ClientTools.Localization.lookup("CopyOBJ"), Uniconta.ClientTools.Localization.lookup("Invoice")));
            this.Loaded += CW_Loaded;
            dgCreateOrderGrid.BusyIndicator = busyIndicator;
            dgCreateOrderGrid.api = api;
            SetRibbonControl(localMenu, dgCreateOrderGrid);
            dgCreateOrderGrid.SelectedItemChanged += DgCreateOrderGrid_SelectedItemChanged;
            if (disableOrderLineGrid)
                layOutOrderline.Visibility = Visibility.Collapsed;
            if (!company.ItemVariants)
                colVariant.Visible = false;
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
            
        }
        private void LocalMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        private void DgCreateOrderGrid_SelectedItemChanged(object sender, DevExpress.Xpf.Grid.SelectedItemChangedEventArgs e)
        {
            var selectedItem = e.NewItem as DebtorInvoiceClient;
            if (selectedItem != null && !disableOrderLineGrid)
                LoadOrderlines(selectedItem);
        }

        async void LoadOrderlines(DebtorInvoiceClient selectedItem)
        {
            dcOrderlineGrid.ItemsSource = null;
            busyIndicator.IsBusy = true;

            var lines = await CreateDCOrderLinesFromInvoice(dcOrder, selectedItem, (bool)chkIfCreditNote.IsChecked);
            if (lines != null)
                DCOrderLines = lines.Cast<UnicontaBaseEntity>();
            busyIndicator.IsBusy = false;
            dcOrderlineGrid.ItemsSource = lines;
            dcOrderlineGrid.Visibility = Visibility.Visible;
        }

        void CW_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => { CreateButton.Focus(); }));
        }

        private void ChildWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (CreateButton.IsFocused)
                    CreateButton_Click(null, null);
                else if (CancelButton.IsFocused)
                {
                    dgCreateOrderGrid.SelectedItem = null;
                    this.dockCtrl.JustClosePanel(dockCtrl.Activpanel);
                }
            }
        }
        bool createbuttonclicked;
        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            createbuttonclicked = true;
            if (dgCreateOrderGrid.SelectedItem == null)
            {
                UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("RecordNotSelected"), Uniconta.ClientTools.Localization.lookup("Warning"));
                return;
            }
            else
                this.dockCtrl.JustClosePanel(dockCtrl.Activpanel);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            dgCreateOrderGrid.SelectedItem = null;
            this.dockCtrl.JustClosePanel(dockCtrl.Activpanel);
        }
        protected override Filter[] DefaultFilters()
        {
            Filter fileExtensionFilter = new Filter();
            fileExtensionFilter.name = "Account";
            fileExtensionFilter.value = Account;
            return new Filter[] { fileExtensionFilter };

        }

        async private Task<IEnumerable<DCOrderLineClient>> CreateDCOrderLinesFromInvoice(DCOrder dcOrder, DCInvoiceClient dcInvoiceClient, bool checkIfCreditNote)
        {
            var lstOrderlines = new List<DCOrderLineClient>();

            var invoiceLines = await api.Query<DebtorInvoiceLines>(dcInvoiceClient);
            if (invoiceLines == null || invoiceLines.Length == 0)
                return null;

            Array.Sort(invoiceLines, new InvLineSort());
            lstOrderlines.Capacity = invoiceLines.Length;

            int lineNo = 0;
            double sign = checkIfCreditNote ? -1d : 1d;
            foreach (var invoiceline in invoiceLines)
            {
                DCOrderLineClient line;
                switch (dcOrder.__DCType())
                {
                    case 1: line = api.CompanyEntity.CreateUserType<DebtorOrderLineClient>(); break;
                    case 3: line = api.CompanyEntity.CreateUserType<DebtorOfferLineClient>(); break;
                    case 6: line = api.CompanyEntity.CreateUserType<ProjectInvoiceProposalLineClient>(); break;
                    default: line = api.CompanyEntity.CreateUserType<DebtorOrderLineClient>(); break;
                }
                line.SetMaster((UnicontaBaseEntity)dcOrder);
                line._LineNumber = ++lineNo;
                line._Item = invoiceline._Item;
                line._DiscountPct = invoiceline._DiscountPct;
                line._Discount = invoiceline._Discount;
                line._Qty = invoiceline.InvoiceQty * sign;
                line._Price = invoiceline.CurrencyEnum != null ? invoiceline._PriceCur : invoiceline._Price;
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

                lstOrderlines.Add(line);
            }
            return lstOrderlines;
        }
        private void chkIfCreditNote_Checked(object sender, RoutedEventArgs e)
        {
            var selectedItem = dgCreateOrderGrid.SelectedItem as DebtorInvoiceClient;
            if (selectedItem != null && !disableOrderLineGrid)
            {
                double sign = (bool)chkIfCreditNote.IsChecked ? -1d : 1d;
                var source = dcOrderlineGrid.ItemsSource as IEnumerable<DebtorCommonOrderLineClient>;
                if (source != null)
                {
                    foreach (var line in source)
                        line.Qty = line.Qty > 0d ? line.Qty * sign : line.Qty * -1d;
                }
            }
        }
        protected override void OnLayoutLoaded()
        {
            AttachVoucherGridPage.SetFloatingHeightAndWidth(dockCtrl, position, NameOfControl);
            base.OnLayoutLoaded();
        }

    }
}
