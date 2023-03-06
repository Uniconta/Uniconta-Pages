using Corasau.Admin.API;
using UnicontaClient.Models;
using UnicontaClient.Pages;
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
using Uniconta.API.Service;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;
using UnicontaClient.Utilities;
using UnicontaClient.Controls;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class PartnerInvoiceGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(PartnerInvoiceClient); } }
    }
    /// <summary>
    /// Interaction logic for PartnerInvoicePage.xaml
    /// </summary>
    public partial class PartnerInvoicePage : GridBasePage
    {
        UnicontaBaseEntity masterSub;
        Partner invoicePartner;

        public PartnerInvoicePage(UnicontaBaseEntity sourcedata, ResellerClient reseller) : base(null)
        {
            invoicePartner = reseller;
            InitPage(sourcedata);
        }

        public PartnerInvoicePage(UnicontaBaseEntity sourcedata) : base(null)
        {
            InitPage(sourcedata);
        }

        public PartnerInvoicePage(BaseAPI API)
            : base(API, string.Empty)
        {
            InitPage(null);
        }

        private void InitPage(UnicontaBaseEntity sourcedata)
        {
            AddFilterAndSort = true;
            InitializeComponent();
            masterSub = sourcedata;
            if (sourcedata != null)
                dgPartnerInvoice.UpdateMaster(sourcedata);
            localMenu.dataGrid = dgPartnerInvoice;
            SetRibbonControl(localMenu, dgPartnerInvoice);
            dgPartnerInvoice.api = api;
            dgPartnerInvoice.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgPartnerInvoice.ShowTotalSummary();
        }

        protected override void LoadCacheInBackGround()
        {
            LoadType(typeof(Uniconta.DataModel.Subscription));
        }

        bool AddFilterAndSort;

        protected override Filter[] DefaultFilters()
        {
            if (AddFilterAndSort)
            {
                Filter dateFilter = new Filter();
                dateFilter.name = "Date";
                dateFilter.value = String.Format("{0:d}..", BasePage.GetSystemDefaultDate().AddYears(-1).Date);
                return new Filter[] { dateFilter };
            }
            return base.DefaultFilters();
        }

        protected override SortingProperties[] DefaultSort()
        {
            if (AddFilterAndSort)
            {
                SortingProperties dateSort = new SortingProperties("Date");
                dateSort.Ascending = false;
                return new SortingProperties[] { dateSort };
            }
            return base.DefaultSort();
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgPartnerInvoice.SelectedItem as PartnerInvoiceClient;
            switch (ActionType)
            {
                case "SubscriptionInvoice":
                    if (selectedItem == null)
                        return;
                    AddDockItem(TabControls.SubscriptionInvoicePage, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("InvoiceNumber"), selectedItem._Invoice));
                    break;
                case "ShowInvoice":
                    if (selectedItem == null)
                        return;
                    ShowInvoice(selectedItem);
                    break;
                case "SendAsEmail":
                    if (dgPartnerInvoice.ItemsSource == null)
                    {
                        UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("zeroRecords"), Uniconta.ClientTools.Localization.lookup("Warning"), MessageBoxButton.OK);
                        return;
                    }

                    CWPartnerInvoice cwPartnerSendEmail = new CWPartnerInvoice();
                    cwPartnerSendEmail.Closing += delegate
                     {
                         if (cwPartnerSendEmail.DialogResult == true)
                             SendMail(selectedItem, cwPartnerSendEmail.IsChecked);
                     };
                    cwPartnerSendEmail.Show();
                    break;

                case "PostInvoice":
                    if (dgPartnerInvoice.ItemsSource == null)
                    {
                        UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("zeroRecords"), Uniconta.ClientTools.Localization.lookup("Warning"), MessageBoxButton.OK);
                        return;
                    }
                    CWPartnerInvoice cwPartnerPostInvoice = new CWPartnerInvoice(api);
                    cwPartnerPostInvoice.Closing += delegate
                    {
                        if (cwPartnerPostInvoice.DialogResult == true)
                            PostInvoice(selectedItem, cwPartnerPostInvoice.IsChecked, cwPartnerPostInvoice.Journal);

                    };
                    cwPartnerPostInvoice.Show();
                    break;

                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        async void PostInvoice(PartnerInvoiceClient partnerInv, bool sendAll, string journal)
        {
            var subAPi = new SubscriptionAPI(api);
            busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("BusyMessage");
            busyIndicator.IsBusy = true;
            ErrorCodes result = await subAPi.PostPartnerInvoice(partnerInv, sendAll, journal);
            busyIndicator.IsBusy = false;

            UtilDisplay.ShowErrorCode(result);
        }

        async void SendMail(PartnerInvoiceClient partnerInv, bool sendAll)
        {
            var subsApi = new SubscriptionAPI(api);
            busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("SendingWait");
            busyIndicator.IsBusy = true;
            ErrorCodes result = await subsApi.EmailPartnerInvoice(partnerInv, sendAll);
            busyIndicator.IsBusy = false;

            UtilDisplay.ShowErrorCode(result);
        }

        async private void ShowInvoice(PartnerInvoiceClient selectedItem)
        {
            var partnerDetails = await GetInvData(selectedItem);
            if (partnerDetails == null) return;

            object[] obj = new object[1];
            obj[0] = partnerDetails;

            AddDockItem(TabControls.InvoiceSubscriptionPage, obj, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("InvoiceNumber"), selectedItem._Invoice));
        }

        async Task<PartnerInvDetails> GetInvData(PartnerInvoiceClient selecteditem)
        {
            PartnerInvDetails partnerInvDetails = new PartnerInvDetails();
            partnerInvDetails.InvoiceHeader = selecteditem;

            var subscriptionLines = await api.Query<SubscriptionInvoiceClient>(selecteditem);
            partnerInvDetails.InvoiceLines = subscriptionLines;

            var reseller = selecteditem.Reseller;
            var rSellerClient = await api.Query<ResellerClient>();
            var partner = rSellerClient.Where(p => p.Pid == reseller).SingleOrDefault();

            partnerInvDetails.Reseller = partner;
            partnerInvDetails.CompanyLogo = await UtilCommon.GetLogo(api);
            partnerInvDetails.Language = session.User._Language;

            return partnerInvDetails;
        }
    }
}
