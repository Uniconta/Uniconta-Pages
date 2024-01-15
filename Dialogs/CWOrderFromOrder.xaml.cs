using UnicontaClient.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.DataModel;

namespace UnicontaClient.Controls.Dialogs
{
    public partial class CWOrderFromOrder : ChildWindow
    {
        [InputFieldData]
        [Display(Name = "InverSign", ResourceType = typeof(InputFieldDataText))]
        public bool InverSign { get; set; }

        [InputFieldData]
        [Display(Name = "IsDebtorOrder", ResourceType = typeof(InputFieldDataText))]
        public bool isDebtorOrder { get; set; }
        [InputFieldData]
        [Display(Name = "CopyAttachments", ResourceType = typeof(InputFieldDataText))]
        public bool copyAttachment { get; set; }
        [InputFieldData]
        [Display(Name = "CopyDeliveryAddress", ResourceType = typeof(InputFieldDataText))]
        public bool copyDeliveryAddress { get; set; }
        [InputFieldData]
        [Display(Name = "RecalculatePrices", ResourceType = typeof(InputFieldDataText))]
        public bool reCalculatePrice { get; set; }
        [InputFieldData]
        [Display(Name = "PerSupplier", ResourceType = typeof(InputFieldDataText))]
        public bool orderPerPurchaseAccount { get; set; }
        [InputFieldData]
        [Display(Name = "OnlyItemsWithSupplier", ResourceType = typeof(InputFieldDataText))]
        public bool onlyItemsWithSupplier { get; set; }
        [InputFieldData]
        [Display(Name = "DeliveryDate", ResourceType = typeof(InputFieldDataText))]
        public DateTime DeliveryDate { get; set; }
        [InputFieldData]
        [Display(Name = "CreateNewOrder", ResourceType = typeof(InputFieldDataText))]
        public bool CreateNewOrder { get; set; }

        public string Account;
    
        DCOrder _dcOrder;
        public string RelatedOrder { get; set; }
        CrudAPI _api;

#if !SILVERLIGHT
        protected override int DialogId { get { return DialogTableId; } }
        public int DialogTableId { get; set; }
        protected override bool ShowTableValueButton { get { return true; } }
#endif

        public DCOrder dcOrder
        {
            get
            {
                DCOrder order = null;
                if (CreateNewOrder)
                {
                    Type t;
                    var idx = cmbOrderTypes.SelectedIndex;
                    if (idx == 2)
                        t = typeof(DebtorOfferClient);
                    else if (idx == 1)
                        t = typeof(CreditorOrderClient);
                    else
                        t = typeof(DebtorOrderClient);
                    order = (DCOrder)Activator.CreateInstance(Global.GetTableWithUserFields(_api.CompanyEntity, t, true));
                }
                else
                    order = _dcOrder;
                return order;
            }
        }

        public CWOrderFromOrder(CrudAPI api)
        {
            InitializeComponent();
            _api = api;
            leDebtorAccount.api = leCreditorAccount.api = leDebtorOrder.api= leCreditorOrder.api = leDebtorOffer.api= api;
            CreateNewOrder= true;
            var orderTypes = new string[] { Uniconta.ClientTools.Localization.lookup("SalesOrder") , Uniconta.ClientTools.Localization.lookup("PurchaseOrder"), Uniconta.ClientTools.Localization.lookup("Offer") };
            cmbOrderTypes.ItemsSource = orderTypes;
            cmbOrderTypes.SelectedIndex = 0;
            isDebtorOrder = true;
            this.DataContext = this;
            lblCopyDelAdd.Text = string.Format(Uniconta.ClientTools.Localization.lookup("CopyOBJ"), Uniconta.ClientTools.Localization.lookup("DeliveryAddr"));

            this.Title = string.Format(Uniconta.ClientTools.Localization.lookup("CreateOBJ"),
                                       Uniconta.ClientTools.Localization.lookup("PurchaseSalesOrder"));

            lblPerSuuplier.Visibility = Visibility.Collapsed;
            chkPerSupplier.Visibility = Visibility.Collapsed;
            chkReCalPrice.IsChecked = true;
            this.Loaded += CWOrderFromOrder_Loaded;
        }

        private void CWOrderFromOrder_Loaded(object sender, RoutedEventArgs e)
        {
            SetAccountSource();
            Dispatcher.BeginInvoke(new Action(() => { cmbOrderTypes.Focus(); }));
        }

        async private void SetAccountSource()
        {
            var api = this._api;
            var Comp = api.CompanyEntity;

            var debtorCache = Comp.GetCache(typeof(Uniconta.DataModel.Debtor)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.Debtor), api);
            leDebtorAccount.ItemsSource = debtorCache;
            var creditorCache = Comp.GetCache(typeof(Uniconta.DataModel.Creditor)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.Creditor), api);
            leCreditorAccount.ItemsSource = creditorCache;

            var debtorOrderCache = Comp.GetCache(typeof(Uniconta.DataModel.DebtorOrder)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.DebtorOrder), api);
            leDebtorOrder.ItemsSource = debtorOrderCache;
            if (!string.IsNullOrEmpty(RelatedOrder))
                leDebtorOrder.SelectedItem = debtorOrderCache?.Get(RelatedOrder);

            var debtorOfferCache = Comp.GetCache(typeof(Uniconta.DataModel.DebtorOffer)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.DebtorOffer), api);
            leDebtorOffer.ItemsSource = debtorOfferCache;
            var creditorOrderCache = Comp.GetCache(typeof(Uniconta.DataModel.CreditorOrder)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.CreditorOrder), api);
            leCreditorOrder.ItemsSource = creditorOrderCache;
        }

        private void cmbOrderTypes_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            if (cmbOrderTypes.SelectedIndex == 1)
            {
                leDebtorAccount.Visibility = Visibility.Collapsed;
                leCreditorAccount.Visibility = Visibility.Visible;
                isDebtorOrder = false;
                lblPerSuuplier.Visibility = Visibility.Visible;
                chkPerSupplier.Visibility = Visibility.Visible;
                if (!CreateNewOrder)
                {
                    leDebtorOrder.Visibility= Visibility.Collapsed;
                    leDebtorOffer.Visibility = Visibility.Collapsed;
                    leCreditorOrder.Visibility = Visibility.Visible;
                    lbOrder.Text= Uniconta.ClientTools.Localization.lookup("PurchaseOrder");
                }
            }
            else if (cmbOrderTypes.SelectedIndex == 2)
            {
                chkPerSupplier.IsChecked = false;
                leDebtorAccount.Visibility = Visibility.Visible;
                leCreditorAccount.Visibility = Visibility.Collapsed;
                isDebtorOrder = true;
                lblPerSuuplier.Visibility = Visibility.Collapsed;
                chkPerSupplier.Visibility = Visibility.Collapsed;

                if (!CreateNewOrder)
                {
                    leCreditorOrder.Visibility = Visibility.Collapsed;
                    leDebtorOrder.Visibility= Visibility.Collapsed;
                    leDebtorOffer.Visibility = Visibility.Visible;
                    lbOrder.Text= Uniconta.ClientTools.Localization.lookup("Offer");
                }
            }
            else
            {
                chkPerSupplier.IsChecked = false;
                leDebtorAccount.Visibility = Visibility.Visible;
                leCreditorAccount.Visibility = Visibility.Collapsed;
                isDebtorOrder = true;
                lblPerSuuplier.Visibility = Visibility.Collapsed;
                chkPerSupplier.Visibility = Visibility.Collapsed;

                if (!CreateNewOrder)
                {
                    leCreditorOrder.Visibility = Visibility.Collapsed;
                    leDebtorOrder.Visibility= Visibility.Visible;
                    leDebtorOffer.Visibility = Visibility.Collapsed;
                    lbOrder.Text= Uniconta.ClientTools.Localization.lookup("SalesOrder");
                }
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            InverSign = (bool)chkInvertSign.IsChecked;
            copyAttachment = (bool)chkCopyAttach.IsChecked;
            copyDeliveryAddress = (bool)chkCopyDelAdd.IsChecked;
            reCalculatePrice = (bool)chkReCalPrice.IsChecked;
            onlyItemsWithSupplier = (bool)chkOnlyItemsWithSupplier.IsChecked;
            DCAccount dcAccount = null;
            if (isDebtorOrder)
            {
                orderPerPurchaseAccount = false;
                if (CreateNewOrder)
                {
                    Account = leDebtorAccount.Text;
                    dcAccount = ClientHelper.GetRef(_api.CompanyId, typeof(Debtor), Account) as DCAccount;
                }
            }
            else
            {
                orderPerPurchaseAccount = (bool)chkPerSupplier.IsChecked;
                if (CreateNewOrder)
                {
                    Account = leCreditorAccount.Text;
                    dcAccount = ClientHelper.GetRef(_api.CompanyId, typeof(Uniconta.DataModel.Creditor), Account) as DCAccount;
                }
            }

            if (!CreateNewOrder)
            {
                if (cmbOrderTypes.SelectedIndex == 0)
                    _dcOrder = leDebtorOrder.SelectedItem as DCOrder;
                else if (cmbOrderTypes.SelectedIndex == 1)
                    _dcOrder = leCreditorOrder.SelectedItem as DCOrder;
                else
                    _dcOrder = leDebtorOffer.SelectedItem as DCOrder;
            }

            if ((CreateNewOrder && !orderPerPurchaseAccount && dcAccount == null) || (!CreateNewOrder && _dcOrder == null))
            {
                UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("FieldCannotBeEmpty"), Uniconta.ClientTools.Localization.lookup("Warning"), MessageBoxButton.OK);
                return;
            }

            if (dcAccount != null)
            {
                if (!Utility.IsExecuteWithBlockedAccount(dcAccount))
                    return;
            }

            SetDialogResult(true);
        }

        private void chkCreateNewOrder_Checked(object sender, RoutedEventArgs e)
        {
            HideControls();
        }

        private void chkCreateNewOrder_Unchecked(object sender, RoutedEventArgs e)
        {
            HideControls();
        }

        void HideControls()
        {
            if (!(bool)chkCreateNewOrder.IsChecked)
            {
                Account= String.Empty;
                leDebtorAccount.SelectedItem= null;
                leCreditorAccount.SelectedItem= null;
                lbOrder.Visibility= Visibility.Visible;
                if (cmbOrderTypes.SelectedIndex == 1)
                {
                    leDebtorOrder.Visibility= Visibility.Collapsed;
                    leDebtorOffer.Visibility= Visibility.Collapsed;
                    leCreditorOrder.Visibility = Visibility.Visible;
                    lbOrder.Text= Uniconta.ClientTools.Localization.lookup("PurchaseOrder");
                }
                else if (cmbOrderTypes.SelectedIndex == 2)
                {
                    leCreditorOrder.Visibility = Visibility.Collapsed;
                    leDebtorOffer.Visibility= Visibility.Visible;
                    leDebtorOrder.Visibility= Visibility.Collapsed;
                    lbOrder.Text= Uniconta.ClientTools.Localization.lookup("Offer");
                }
                else
                {
                    leCreditorOrder.Visibility = Visibility.Collapsed;
                    leDebtorOffer.Visibility= Visibility.Collapsed;
                    leDebtorOrder.Visibility= Visibility.Visible;
                    lbOrder.Text= Uniconta.ClientTools.Localization.lookup("SalesOrder");
                }

            }
            else
            {
                lbOrder.Visibility= Visibility.Collapsed;
                leDebtorOrder.Visibility= Visibility.Collapsed;
                leDebtorOffer.Visibility= Visibility.Collapsed;
                leCreditorOrder.Visibility=Visibility.Collapsed;
                _dcOrder= null;
            }
        }
    }
}
