using UnicontaClient.Models;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Uniconta.DataModel;
using System.Windows;
using Corasau.Admin.API;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.Util;
using System.Collections;
using Uniconta.API.Service;
#if !SILVERLIGHT
using UnicontaClient.Controls;
#endif

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class SubscriptionInvoiceGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(SubscriptionInvoiceClient); } }
        public override IComparer GridSorting { get { return new SubscriptionInvoiceSort(); } }
    }
    public partial class SubscriptionInvoicePage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.SubscriptionInvoicePage; } }
        UnicontaBaseEntity masterSub;
        Partner invoicePartner;

        public SubscriptionInvoicePage(UnicontaBaseEntity sourcedata, ResellerClient reseller) : base(sourcedata)
        {
            invoicePartner = reseller;
            InitPage(sourcedata);
        }

        public SubscriptionInvoicePage(UnicontaBaseEntity soucedata) : base(null)
        {
            InitPage(soucedata);
        }

        public SubscriptionInvoicePage(BaseAPI API)
            : base(API, string.Empty)
        {
            InitPage(null);
        }

        bool showFields;

        private void InitPage(UnicontaBaseEntity soucedata)
        {
            AddFilterAndSort = true;
            InitializeComponent();
            if (soucedata != null)
            {
                masterSub = soucedata;
                dgSubInvoicesGrid.UpdateMaster(soucedata);
                if (invoicePartner == null)
                    invoicePartner = soucedata as Partner;
                RibbonBase rb = (RibbonBase)localMenu.DataContext;
                UtilDisplay.RemoveMenuCommand(rb, new string[] { "SendAsEmailOnDate", "PostInvoiceOnDate" });
            }
            showFields = (api.session.User._Role >= (byte)Uniconta.Common.User.UserRoles.Reseller);
            localMenu.dataGrid = dgSubInvoicesGrid;
            SetRibbonControl(localMenu, dgSubInvoicesGrid);
            dgSubInvoicesGrid.api = api;
            dgSubInvoicesGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
#if SILVERLIGHT
            ribbonControl.DisableButtons(new string[] { "ShowInvoice" });
#endif
            dgSubInvoicesGrid.ShowTotalSummary();
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            Subscriptionid.Visible = showFields;
            UserName.Visible = showFields;
            CompanyName.Visible = showFields;
        }

        protected override void LoadCacheInBackGround()
        {
            if (showFields)
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
            var selectedItem = dgSubInvoicesGrid.SelectedItem as SubscriptionInvoiceClient;
            switch (ActionType)
            {
                case "InvoiceLine":
                    if (selectedItem != null)
                        AddDockItem(TabControls.SubscriptionInvoiceLinePage, dgSubInvoicesGrid.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("InvoiceNumber"), selectedItem._Invoice));
                    break;
#if !SILVERLIGHT
                case "ShowInvoice":
                    if (selectedItem != null)
                        ShowInvoice(selectedItem);
                    break;
#endif
                case "SendAsEmail":
                    if (selectedItem == null)
                    {
                        UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("zeroRecords"), Uniconta.ClientTools.Localization.lookup("Warning"), MessageBoxButton.OK);
                        return;
                    }
                    if (invoicePartner != null)
                    {
                        CWDateSelector dateSelector = new CWDateSelector(true);
#if !SILVERLIGHT
                        dateSelector.DialogTableId = 2000000021;
#endif
                        dateSelector.Closing += delegate
                        {
                            if (dateSelector.DialogResult == true)
                            {
                                SendMail(selectedItem, dateSelector.SelectedDate, dateSelector.SendAll);
                            }
                        };
                        dateSelector.Show();
                    }
                    else
                    {
                        SendMail(selectedItem, selectedItem._Date);
                    }
                    break;

                case "SendAsEmailOnDate":
                    if (selectedItem == null)
                    {
                        UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("zeroRecords"), Uniconta.ClientTools.Localization.lookup("Warning"), MessageBoxButton.OK);
                        return;
                    }
                    CWDateSelector objdateSelector = new CWDateSelector(true);
#if !SILVERLIGHT
                    objdateSelector.DialogTableId = 2000000022;
#endif
                    objdateSelector.Closing += delegate
                        {
                            if (objdateSelector.DialogResult == true)
                            {
                                SendMail(selectedItem, objdateSelector.SelectedDate, objdateSelector.SendAll);
                            }
                        };
                    objdateSelector.Show();
                    break;

                case "PostInvoice":
                    if (selectedItem == null)
                        return;
                    CWPostInvoice CWPostInvoiceDailogue = new CWPostInvoice(api);
                    CWPostInvoiceDailogue.dpDate.Visibility = Visibility.Collapsed;
                    CWPostInvoiceDailogue.txtDate.Visibility = Visibility.Collapsed;
                    CWPostInvoiceDailogue.Closed += async delegate
                    {
                        if (CWPostInvoiceDailogue.DialogResult == true)
                        {
                            var sbsApi = new SubscriptionAPI(api);
                            var result = await sbsApi.PostInternalInvoice(invoicePartner, selectedItem, selectedItem._Date, CWPostInvoiceDailogue.Journal);
                            if (result != ErrorCodes.Succes)
                                UtilDisplay.ShowErrorCode(result);
                        }
                    };
                    CWPostInvoiceDailogue.Show();
                    break;

                case "PostInvoiceOnDate":
                    if (selectedItem == null)
                        return;
                    var objCWPostInvoiceDailogue = new CWPostInvoice(api);
                    objCWPostInvoiceDailogue.Closed += async delegate
                    {
                        if (objCWPostInvoiceDailogue.DialogResult == true)
                        {
                            var sbsApi = new SubscriptionAPI(api);
                            var result = await sbsApi.PostInternalInvoice(invoicePartner, null, objCWPostInvoiceDailogue.InvoiceDate, objCWPostInvoiceDailogue.Journal);
                            UtilDisplay.ShowErrorCode(result);
                        }
                    };
                    objCWPostInvoiceDailogue.Show();
                    break;

                case "DeleteRow":
                    {

                        CWConfirmationBox dialog = new CWConfirmationBox(Uniconta.ClientTools.Localization.lookup("AreYouSureToContinue"), Uniconta.ClientTools.Localization.lookup("Confirmation"), false, Uniconta.ClientTools.Localization.lookup("DeleteRow"));
                        dialog.Closing += async delegate
                        {
                            if (dialog.ConfirmationResult == CWConfirmationBox.ConfirmationResultEnum.Yes)
                            {

                                var res = await api.Delete(selectedItem);
                                if (res != ErrorCodes.Succes)
                                    UtilDisplay.ShowErrorCode(res);

                                else
                                {
                                    await InitQuery();
                                }
                            }
                        };
                        dialog.Show();


                    }
                    break;


                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }


        async void SendMail(SubscriptionInvoice Invoice, DateTime selectedDate, bool SendALL = false)
        {
            var subscriptionMsg = SendALL ? Uniconta.ClientTools.Localization.lookup("SendAllInvoiceMsg") : string.Format(Uniconta.ClientTools.Localization.lookup("SendInvoiceSubscriptionMsg"), Invoice._Sid);
            if (UnicontaMessageBox.Show(subscriptionMsg, Uniconta.ClientTools.Localization.lookup("Message"), MessageBoxButton.OKCancel
#if !SILVERLIGHT
                , MessageBoxImage.Question
#endif
                ) == MessageBoxResult.OK)
            {
                Task<ErrorCodes> task;
                var subsApi = new SubscriptionAPI(api);
                if (invoicePartner != null && SendALL && api.session.User._Role >= (byte)Uniconta.Common.User.UserRoles.Reseller)
                    task = subsApi.EmailSubscriptionInvoice(invoicePartner, masterSub as Subscription, selectedDate);
                else
                    task = subsApi.EmailSubscriptionInvoice(Invoice, SendALL && api.session.User._Role >= (byte)Uniconta.Common.User.UserRoles.Reseller);

                busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("SendingWait");
                busyIndicator.IsBusy = true;
                var result = await task;
                busyIndicator.IsBusy = false;
                UtilDisplay.ShowErrorCode(result);
            }
        }

#if !SILVERLIGHT
        async private void ShowInvoice(SubscriptionInvoiceClient selectedItem)
        {
            var subsApi = new SubscriptionAPI(api);
            var invoicePdf = await subsApi.SubscriptionInvoicePDF(selectedItem);
            if (invoicePdf != null)
            {
                var viewer = new CWDocumentViewer(invoicePdf, FileextensionsTypes.PDF);
                viewer.Show();
            }
        }

        async Task<SubscriptionInvDetails> getInvData(SubscriptionInvoiceClient selecteditem)
        {
            SubscriptionInvDetails subDetails = new SubscriptionInvDetails();
            if (masterSub != null && masterSub is SubscriptionClient)
                subDetails.clientSubscriptions = (SubscriptionClient)masterSub;
            else
            {
                subDetails.clientSubscriptions = new SubscriptionClient();
                subDetails.clientSubscriptions.SetMaster(selecteditem);
                await api.Read(subDetails.clientSubscriptions);
            }
            subDetails.DefaultDebtorAcc = invoicePartner?._DebitorAccountIfBlank;
            subDetails.invHeader = selecteditem;

            /* We only can load Logo, if we are in the right company
            if (api.CompanyId == PartnerCompanyId)
            {
                CompanyDocumentClient documentClient = new CompanyDocumentClient();
                documentClient.UseFor = CompanyDocumentUse.CompanyLogo;
                await api.Read(documentClient);
                subDetails.CompanyLogo = documentClient.DocumentData;
            }
            */

            List<UnicontaBaseEntity> masterlist = new List<UnicontaBaseEntity>();
            masterlist.Add(selecteditem);
            var lines = (SubscriptionInvoiceLineClient[])await api.Query(new SubscriptionInvoiceLineClient(), masterlist, null);
            subDetails.invLines = lines;
            subDetails.Language = session.User._Language;
            return subDetails;
        }
#endif
    }
}
