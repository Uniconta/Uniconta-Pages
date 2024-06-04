using Uniconta.API.DebtorCreditor;
using Uniconta.API.Service;
using UnicontaClient.Models;
using UnicontaClient.Utilities;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
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
using UnicontaClient.Controls.Dialogs;
using Uniconta.ClientTools.Controls;
using DevExpress.XtraReports.UI;
using System.ComponentModel.DataAnnotations;
using System.IO;
using Uniconta.Client.Pages;
using DevExpress.Xpf.Grid;
using DevExpress.Data;
using Uniconta.Common.Utility;
using FromXSDFile.OIOUBL.ExportImport;
using Microsoft.Win32;
using UnicontaClient.Pages;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class ProjectOrdersGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(ProjectOrderClient); } }
        public override IComparer GridSorting { get { return new DCInvoiceSort(); } }
        public override bool Readonly { get { return true; } }

        protected override void DataLoaded(UnicontaBaseEntity[] Arr)
        {
            if (api.CompanyEntity.DeliveryAddress && Arr != null)
            {
                var cache = api.GetCache(typeof(Debtor));
                if (cache != null)
                {
                    foreach (var rec in (IEnumerable<DCInvoice>)Arr)
                        UtilCommon.SetDeliveryAdress(rec, (DCAccount)cache.Get(rec._DCAccount), api);
                }
            }
        }
    }
    public partial class ProjectOrders : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.ProjectOrders.ToString(); } }

        DateTime filterDate;

        protected override Filter[] DefaultFilters()
        {
            if (filterDate != DateTime.MinValue)
            {
                Filter dateFilter = new Filter() { name = "Date", value = String.Format("{0:d}..", filterDate) };
                return new Filter[] { dateFilter };
            }
            return base.DefaultFilters();
        }

        public ProjectOrders(BaseAPI API) : base(API, string.Empty)
        {
            InitPage();
        }
        public ProjectOrders(SynchronizeEntity syncEntity)
            : base(syncEntity, true)
        {
            this.syncEntity = syncEntity;
            InitPage(syncEntity.Row);
            SetHeader();
        }
        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            dgProjectOrderGrid.UpdateMaster(args);
            SetHeader();
            InitQuery();
        }
        void SetHeader()
        {
            string header;
            var masterClient1 = dgProjectOrderGrid.masterRecord as Debtor;
            if (masterClient1 != null)
                header = string.Format("{0}/{1}", Uniconta.ClientTools.Localization.lookup("DebtorInvoice"), masterClient1._Account);
            else
            {
                var masterClient2 = dgProjectOrderGrid.masterRecord as Uniconta.DataModel.Project;
                if (masterClient2 != null)
                    header = string.Format("{0}/{1}", Uniconta.ClientTools.Localization.lookup("ProjectInvoice"), masterClient2._DCAccount);
                else
                {
                    var masterClient3 = dgProjectOrderGrid.masterRecord as Uniconta.DataModel.DebtorTrans;
                    if (masterClient3 != null)
                        header = string.Format("{0}/{1}", Uniconta.ClientTools.Localization.lookup("DebtorInvoice"), masterClient3._Account);
                    else
                    {
                        var masterClient4 = dgProjectOrderGrid.masterRecord as DebtorTransOpenClient;
                        if (masterClient4 != null)
                            header = string.Format("{0}/{1}", Uniconta.ClientTools.Localization.lookup("DebtorInvoice"), masterClient4.Account);
                        else
                            return;
                    }
                }
            }
            SetHeader(header);
        }

        public ProjectOrders(UnicontaBaseEntity master)
            : base(master)
        {
            InitPage(master);
        }

        private void InitPage(UnicontaBaseEntity master = null)
        {
            InitializeComponent();
            dgProjectOrderGrid.UpdateMaster(master);
            SetRibbonControl(localMenu, dgProjectOrderGrid);
            dgProjectOrderGrid.BusyIndicator = busyIndicator;
            dgProjectOrderGrid.api = api;
            var Comp = api.CompanyEntity;
            if (master == null)
                filterDate = BasePage.GetFilterDate(Comp, false, 4);
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgProjectOrderGrid.ShowTotalSummary();
            if (Comp.RoundTo100)
                CostValue.HasDecimals = NetAmount.HasDecimals = TotalAmount.HasDecimals = Margin.HasDecimals = SalesValue.HasDecimals = false;

            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            if (!Comp.Order && !Comp.Purchase)
                UtilDisplay.RemoveMenuCommand(rb, "CreateOrder");
            dgProjectOrderGrid.CustomSummary += dgInvoicesGrid_CustomSummary;
        }

        double sumMargin, sumSales, sumMarginRatio;
        private void dgInvoicesGrid_CustomSummary(object sender, DevExpress.Data.CustomSummaryEventArgs e)
        {
            var fieldName = ((GridSummaryItem)e.Item).FieldName;
            switch (e.SummaryProcess)
            {
                case CustomSummaryProcess.Start:
                    sumMargin = sumSales = 0d;
                    break;
                case CustomSummaryProcess.Calculate:
                    var row = e.Row as ProjectOrderClient;
                    sumSales += row.SalesValue;
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

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            bool showFields = (dgProjectOrderGrid.masterRecords == null);
            Account.Visible = showFields;
            Name.Visible = showFields;
            setDim();
            if (!api.CompanyEntity.DeliveryAddress)
            {
                DeliveryName.Visible = false;
                DeliveryAddress1.Visible = false;
                DeliveryAddress2.Visible = false;
                DeliveryAddress3.Visible = false;
                DeliveryZipCode.Visible = false;
                DeliveryCity.Visible = false;
                DeliveryCountry.Visible = false;
            }
            Margin.Visible = Margin.ShowInColumnChooser = MarginRatio.Visible = MarginRatio.ShowInColumnChooser =
            CostValue.Visible = CostValue.ShowInColumnChooser = !api.CompanyEntity.HideCostPrice;
        }

        protected override void LoadCacheInBackGround()
        {
            var Comp = api.CompanyEntity;

            var lst = new List<Type>(12) { typeof(Uniconta.DataModel.InvItem), typeof(Uniconta.DataModel.PaymentTerm), typeof(Uniconta.DataModel.GLVat), typeof(Uniconta.DataModel.Employee) };
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
            }
            if (Comp.Warehouse)
                lst.Add(typeof(Uniconta.DataModel.InvWarehouse));
            if (Comp.Shipments)
            {
                lst.Add(typeof(Uniconta.DataModel.ShipmentType));
                lst.Add(typeof(Uniconta.DataModel.DeliveryTerm));
            }
            if (Comp.DeliveryAddress)
                lst.Add(typeof(Uniconta.DataModel.WorkInstallation));

            LoadType(lst);
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgProjectOrderGrid.SelectedItem as ProjectOrderClient;
            switch (ActionType)
            {
                case "InvTransactions":
                    if (selectedItem != null)
                        AddDockItem(TabControls.DebtorInvoiceLines, dgProjectOrderGrid.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("ProjectOrder"), selectedItem._OrderNumber));
                    break;
                 case "Trans":
                    if (selectedItem != null)
                        AddDockItem(TabControls.PostedTransactions, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Transactions"), selectedItem._Voucher));
                    break;
                case "AddDoc":
                    if (selectedItem != null)
                        AddDockItem(TabControls.UserDocsPage, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Documents"), selectedItem._Voucher));
                    break;
                case "CreateOrder":
                    if (selectedItem != null)
                    {
                        CWOrderFromInvoice cwOrderInvoice = new CWOrderFromInvoice(api);
                        cwOrderInvoice.DialogTableId = 2000000032;
                        cwOrderInvoice.Closed += async delegate
                        {
                            if (cwOrderInvoice.DialogResult == true)
                            {
                                var orderApi = new OrderAPI(api);
                                var inversign = cwOrderInvoice.InverSign;
                                var account = cwOrderInvoice.Account;
                                var copyDelAddress = cwOrderInvoice.copyDeliveryAddress;
                                var reCalPrices = cwOrderInvoice.reCalculatePrices;
                                Type t;
                                if (cwOrderInvoice.Offer == true)
                                    t = typeof(DebtorOfferClient);
                                else
                                    t = typeof(DebtorOrderClient);
                                var dcOrder = this.CreateGridObject(t) as DCOrder;
                                var result = await orderApi.CreateOrderFromInvoice(selectedItem, dcOrder, account, inversign, CopyDeliveryAddress: copyDelAddress, RecalculatePrices: reCalPrices);
                                if (result != ErrorCodes.Succes)
                                    UtilDisplay.ShowErrorCode(result);
                                else
                                    ShowOrderLines(dcOrder);
                            }
                        };
                        cwOrderInvoice.Show();
                    }
                    break;
                case "PostedBy":
                    if (selectedItem != null)
                        JournalPosted(selectedItem);
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        async private void JournalPosted(ProjectOrderClient selectedItem)
        {
            var result = await api.Query(new GLDailyJournalPostedClient(), new UnicontaBaseEntity[] { selectedItem }, null);
            if (result != null && result.Length == 1)
            {
                CWGLPostedClientFormView cwPostedClient = new CWGLPostedClientFormView(result[0]);
                cwPostedClient.Show();
            }
        }

        private void ShowOrderLines(DCOrder order)
        {
            var confrimationText = string.Format(" {0}. {1}:{2},{3}:{4}\r\n{5}", Uniconta.ClientTools.Localization.lookup("SalesOrderCreated"), Uniconta.ClientTools.Localization.lookup("OrderNumber"), order._OrderNumber,
                Uniconta.ClientTools.Localization.lookup("Account"), order._DCAccount, string.Concat(string.Format(Uniconta.ClientTools.Localization.lookup("GoTo"), Uniconta.ClientTools.Localization.lookup("Orderline")), " ?"));

            var confirmationBox = new CWConfirmationBox(confrimationText, string.Empty, false);
            confirmationBox.Closing += delegate
            {
                if (confirmationBox.DialogResult == null)
                    return;

                switch (confirmationBox.ConfirmationResult)
                {
                    case CWConfirmationBox.ConfirmationResultEnum.Yes:
                        AddDockItem(order.__DCType() == 1 ? TabControls.DebtorOrderLines : TabControls.DebtorOfferLines, order, string.Format("{0}:{1},{2}", Uniconta.ClientTools.Localization.lookup("OrdersLine"), order._OrderNumber, order._DCAccount));
                        break;

                    case CWConfirmationBox.ConfirmationResultEnum.No:
                        break;
                }
            };
            confirmationBox.Show();
        }

        void setDim()
        {
            UnicontaClient.Utilities.Utility.SetDimensionsGrid(api, cldim1, cldim2, cldim3, cldim4, cldim5);
        }

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.InvoicePage2)
                dgProjectOrderGrid.UpdateItemSource(argument);
        }
    }
}
