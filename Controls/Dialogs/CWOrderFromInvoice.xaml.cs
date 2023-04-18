using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.DataModel;
using System.ComponentModel.DataAnnotations;
using UnicontaClient.Controls;

namespace UnicontaClient.Controls.Dialogs
{
    public partial class CWOrderFromInvoice : ChildWindow
    {
        [InputFieldData]
        [Display(Name = "Offer", ResourceType = typeof(InputFieldDataText))]
        public bool Offer { get; set; }
        [InputFieldData]
        [Display(Name = "InvertSign", ResourceType = typeof(InputFieldDataText))]
        public bool InverSign { get; set; }
        [InputFieldData]
        [Display(Name = "CopyDeliveryAddress", ResourceType = typeof(InputFieldDataText))]
        public bool copyDeliveryAddress { get; set; }
        [InputFieldData]
        [Display(Name = "RecalculatePrices", ResourceType = typeof(InputFieldDataText))]
        public bool reCalculatePrices { get; set; }
        [InputFieldData]
        [Display(Name = "DeliveryDate", ResourceType = typeof(InputFieldDataText))]
        public DateTime DeliveryDate { get; set; }

        public string Account;
        bool Iscreditor = false;
        CrudAPI _api;

#if !SILVERLIGHT
        protected override int DialogId { get { return DialogTableId; } }
        public int DialogTableId { get; set; }
        protected override bool ShowTableValueButton { get { return true; } }
#endif

        public CWOrderFromInvoice(CrudAPI api,bool isCreditor=false)
        {
            this.DataContext = this;
            InitializeComponent();
            _api = api;
            leAccount.api = api;
            if(isCreditor)
            {
                ofrRow.Height = new GridLength(0);
                Iscreditor = true;
                lbloffer.Visibility = chkOffer.Visibility = Visibility.Collapsed;
                lblDC.Text= Uniconta.ClientTools.Localization.lookup("Creditor");
            }
            else
                lblDC.Text = Uniconta.ClientTools.Localization.lookup("Debtor");

            lblCopyDelAdd.Text = string.Format(Uniconta.ClientTools.Localization.lookup("CopyOBJ"), Uniconta.ClientTools.Localization.lookup("DeliveryAddr"));

            this.Title = Uniconta.ClientTools.Localization.lookup("OrderFromInvoice");
            chkReCalPrices.IsChecked = true;
#if SILVERLIGHT
            Utilities.Utility.SetThemeBehaviorOnChildWindow(this);
#endif
            this.Loaded += CWOrderFromInvoice_Loaded;
        }

        private void CWOrderFromInvoice_Loaded(object sender, RoutedEventArgs e)
        {
            SetAccountSource();
#if SILVERLIGHT
            Dispatcher.BeginInvoke(new Action(() => { OKButton.Focus(); }));
#endif
        }

        async private void SetAccountSource()
        {
            Type t = Iscreditor ? typeof(Uniconta.DataModel.Creditor) : typeof(Debtor);
            var api = this._api;
            var Comp = api.CompanyEntity;
            var Cache = Comp.GetCache(t);
            if (Cache == null)
                Cache = await Comp.LoadCache(t, api);
            leAccount.ItemsSource = Cache;
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            Offer = (bool)chkOffer.IsChecked;
            InverSign = (bool)chkInvertSign.IsChecked;
            if (chkOtherCustomer.IsChecked == true)
                Account = leAccount.Text;
            else
                Account = string.Empty;
            copyDeliveryAddress = (bool)chkCopyDelAdd.IsChecked;
            reCalculatePrices = (bool)chkReCalPrices.IsChecked;
            SetDialogResult(true);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }

        private void chkOtherCustomer_Checked(object sender, RoutedEventArgs e)
        {
            leAccount.Visibility = Visibility.Visible;
        }

        private void chkOtherCustomer_Unchecked(object sender, RoutedEventArgs e)
        {
            leAccount.Visibility = Visibility.Collapsed;
        }

#if SILVERLIGHT

        private void ChildWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                SetDialogResult(false);
            }
            else
                if (e.Key == Key.Enter)
            {
                if (OKButton.IsFocused)
                    OKButton_Click(null, null);
                else if (CancelButton.IsFocused)
                    SetDialogResult(false);
            }
        }

        
#endif
    }
}

