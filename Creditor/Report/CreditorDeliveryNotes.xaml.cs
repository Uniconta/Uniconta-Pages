using System;
using System.Collections;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Uniconta.API.DebtorCreditor;
using Uniconta.API.Service;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using UnicontaClient.Models;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class CreditorDeliveryNoteLocal : CreditorDeliveryNoteClient
    {
        [Display(Name = "System Info")]
        public string SystemInfo { get { return _SystemInfo; } }
        public string _SystemInfo;

        internal void NotifySystemInfoSet()
        {
            NotifyPropertyChanged("SystemInfo");
        }
    }

    public class CreditorDeliveryNotesGrid : CorasauDataGridClient
    {
        public override Type TableType => typeof(CreditorDeliveryNoteLocal);
        public override IComparer GridSorting => new DCInvoiceSort();
    }

    /// <summary>
    /// Interaction logic for CreditorPackNotesPage.xaml
    /// </summary>
    public partial class CreditorDeliveryNotes : GridBasePage
    {
        public override string NameOfControl => UnicontaTabs.AccountantClientPage;
        SQLCache Credcache;
        public CreditorDeliveryNotes(BaseAPI api) : base(api, string.Empty)
        {
            InitPage();
        }

        public CreditorDeliveryNotes(SynchronizeEntity syncEntity) : base(syncEntity, true)
        {
            this.syncEntity = syncEntity;
            InitPage(syncEntity.Row);
            SetHeader();

        }

        public CreditorDeliveryNotes(UnicontaBaseEntity master) : base(master)
        {
            InitPage(master);
        }
        private void InitPage(UnicontaBaseEntity master = null)
        {
            InitializeComponent();
            dgCreditorDeliveryNoteGrid.UpdateMaster(master);
            SetRibbonControl(localMenu, dgCreditorDeliveryNoteGrid);
            dgCreditorDeliveryNoteGrid.api = api;
            dgCreditorDeliveryNoteGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
#if SILVERLIGHT
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            UtilDisplay.RemoveMenuCommand(rb, "GenerateOioXml");
#endif
            InitialLoad();
            dgCreditorDeliveryNoteGrid.ShowTotalSummary();
            var comp = api.CompanyEntity;
            if (comp.RoundTo100)
                CostValue.HasDecimals = NetAmount.HasDecimals = TotalAmount.HasDecimals = Margin.HasDecimals = SalesValue.HasDecimals = false;
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            bool showFields = (dgCreditorDeliveryNoteGrid.masterRecords == null);
            Account.Visible = showFields;
            Name.Visible = showFields;
            SetDimension();
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
        }

        private void SetDimension()
        {
            UnicontaClient.Utilities.Utility.SetDimensionsGrid(api, cldim1, cldim2, cldim3, cldim4, cldim5);
        }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgCreditorDeliveryNoteGrid.SelectedItem as CreditorDeliveryNoteLocal;
            string purchaseHeader = string.Empty;
            if (selectedItem == null)
                purchaseHeader = string.Format("{0} :{1}", Uniconta.ClientTools.Localization.lookup("Orders"), selectedItem._OrderNumber);

            switch (ActionType)
            {
                case "DeliveryNoteLine":
                    if (selectedItem != null)
                        AddDockItem(TabControls.CreditorInvoiceLine, dgCreditorDeliveryNoteGrid.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("PackNoteNumber"), selectedItem._InvoiceNumber));
                    break;
                case "ShowDeliveryNote":
                    if (selectedItem == null || dgCreditorDeliveryNoteGrid.SelectedItems == null)
                        return;
                    var selectedItems = dgCreditorDeliveryNoteGrid.SelectedItems.Cast<CreditorDeliveryNoteLocal>();
                    ShowDeliveryNote(selectedItems);
                    break;
                case "SendDeliveryNote":
                    if (selectedItem != null)
                        SendDeliveryNote(selectedItem);
                    break;
                case "AddDoc":
                    if (selectedItem != null)
                        AddDockItem(TabControls.UserDocsPage, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Documents"), selectedItem._InvoiceNumber));
                    break;
                case "RefreshGrid":
                    InitQuery();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            dgCreditorDeliveryNoteGrid.UpdateMaster(args);
            SetHeader();
            InitQuery();
        }

        private void SendDeliveryNote(CreditorDeliveryNoteLocal credDeliverNoteClient)
        {
            CWSendInvoice cwsendInvoice = new CWSendInvoice();
#if !SILVERLIGHT
            cwsendInvoice.DialogTableId = 2000000066;
#endif
            cwsendInvoice.Closed += async delegate
             {
                 busyIndicator.IsBusy = true;
                 InvoiceAPI invApi = new InvoiceAPI(api);
                 ErrorCodes result = await invApi.SendInvoice(credDeliverNoteClient, cwsendInvoice.Emails, cwsendInvoice.sendOnlyToThisEmail, CWSendInvoice.sendInBackgroundOnly);

                 busyIndicator.IsBusy = false;
                 if (result == ErrorCodes.Succes)
                     UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("SendEmailMsgOBJ"), Uniconta.ClientTools.Localization.lookup("CreditorPackNote")), Uniconta.ClientTools.Localization.lookup("Message"));
                 else
                     UtilDisplay.ShowErrorCode(result);
             };
        }

        async private void ShowDeliveryNote(IEnumerable<CreditorDeliveryNoteLocal> packNoteItems)
        {
            busyIndicator.IsBusy = true;
            busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("GeneratingPage");

            try
            {
                var packNotelist = packNoteItems.ToList();
#if !SILVERLIGHT
                var failedPrints = new List<long>();
                var reports = new List<IPrintReport>();
                long invNumber = 0;
#elif SILVERLIGHT
                int top = 200, left = 300;
                int count = packNotelist.Count();

                if (count > 1)
                {
#endif
                    foreach (var pckNote in packNotelist)
                    {
#if !SILVERLIGHT
                    IPrintReport printreport = await PrintPacknote(pckNote);
                    invNumber = pckNote._InvoiceNumber;

                    if (printreport?.Report != null)
                        reports.Add(printreport);
                    else
                        failedPrints.Add(invNumber);
                    }

                if (reports.Count > 0)
                {
                    if (reports.Count == 1 && invNumber > 0)
                    {
                        var reportName = string.Format("{0}_{1}", Uniconta.ClientTools.Localization.lookup("CreditorPackNote"), invNumber);
                        var dockName = string.Format("{0} {1}", Uniconta.ClientTools.Localization.lookup("Preview"), string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("CreditorPackNote"), invNumber));
                        AddDockItem(TabControls.StandardPrintReportPage, new object[] { reports, reportName }, dockName);
                    }
                    else
                    {
                        var reportsName = string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("ShowPrint"), Uniconta.ClientTools.Localization.lookup("CreditorPackNote"));
                        AddDockItem(TabControls.StandardPrintReportPage, new object[] { reports, Uniconta.ClientTools.Localization.lookup("CreditorPackNote") }, reportsName);
                    }

                    if (failedPrints.Count > 0)
                    {
                        var failedList = string.Join(",", failedPrints);
                        UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("FailedPrintmsg") + failedList, Uniconta.ClientTools.Localization.lookup("Error"), MessageBoxButton.OK);
                    }
                }
                else
                    UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("LayoutDoesNotExist"), Uniconta.ClientTools.Localization.lookup("CreditorPackNote")),
                      Uniconta.ClientTools.Localization.lookup("Error"), MessageBoxButton.OK);
#elif SILVERLIGHT
                        DefaultPrint(pckNote, true, new Point(top, left));
                        left = left - left / count;
                        top = top - top / count;
                    }
                }
                else
                    DefaultPrint(packNotelist.Single());
#endif
            }

            catch (Exception ex)
            {
                busyIndicator.IsBusy = false;
                api.ReportException(ex, string.Format("PurchasePacknote.ShowInvoice(), CompanyId={0}", api.CompanyId));
            }
            finally { busyIndicator.IsBusy = false; }
        }

#if !SILVERLIGHT
        private async Task<IPrintReport> PrintPacknote(CreditorDeliveryNoteLocal creditorInvoice)
        {
            IPrintReport iprintReport = null;

            var creditorPrint = new UnicontaClient.Pages.CreditorPrintReport(creditorInvoice, api, Uniconta.DataModel.CompanyLayoutType.PurchasePacknote);
            var isInitializedSuccess = await creditorPrint.InstantiaeFields();
            if (isInitializedSuccess)
            {
                var standardCreditorInvoice = new CreditorStandardReportClient(creditorPrint.Company, creditorPrint.Creditor, creditorPrint.CreditorInvoice, creditorPrint.InvTransInvoiceLines, null,
                    creditorPrint.CompanyLogo, creditorPrint.ReportName, (byte)Uniconta.ClientTools.Controls.Reporting.StandardReports.PurchasePackNote);

                var standardReports = new ICreditorStandardReport[] { standardCreditorInvoice };
                iprintReport = new StandardPrintReport(api, standardReports, (byte)Uniconta.ClientTools.Controls.Reporting.StandardReports.PurchasePackNote);
                await iprintReport.InitializePrint();


                if (iprintReport?.Report == null)
                {
                    iprintReport = new LayoutPrintReport(api, standardCreditorInvoice, Uniconta.DataModel.CompanyLayoutType.PurchasePacknote);
                    await iprintReport.InitializePrint();
                }
            }
            return iprintReport;
        }

#elif SILVERLIGHT

        private void DefaultPrint(CreditorDeliveryNoteLocal creditorInvoice)
        {
            object[] ob = new object[2];
            ob[0] = creditorInvoice;
            ob[1] = Uniconta.DataModel.CompanyLayoutType.PurchasePacknote;
            string headerName = "CreditorPackNote";
            AddDockItem(TabControls.ProformaInvoice, ob, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup(headerName),
                creditorInvoice._InvoiceNumber));
        }

        private void DefaultPrint(CreditorDeliveryNoteLocal creditorInvoice, bool isFloat, Point position)
        {
            object[] ob = new object[2];
            ob[0] = creditorInvoice;
            ob[1] = Uniconta.DataModel.CompanyLayoutType.PurchasePacknote;
            string headerName = "CreditorPackNote";
            AddDockItem(TabControls.ProformaInvoice, ob, isFloat, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup(headerName),
                creditorInvoice._InvoiceNumber), floatingLoc: position);
        }
#endif
        private void SetHeader()
        {
            var masterClient = dgCreditorDeliveryNoteGrid.masterRecord as Uniconta.DataModel.Creditor;
            if (masterClient == null)
                return;

            string header = string.Format("{0}/{1}", Uniconta.ClientTools.Localization.lookup("CreditorPackNote"), masterClient._Account);
            SetHeader(header);
        }

        protected override void LoadCacheInBackGround()
        {
            var comp = api.CompanyEntity;

            var lst = new List<Type>() { typeof(Uniconta.DataModel.InvItem), typeof(Uniconta.DataModel.PaymentTerm) };
            if (comp.ItemVariants)
            {
                lst.Add(typeof(Uniconta.DataModel.InvVariant1));
                lst.Add(typeof(Uniconta.DataModel.InvVariant2));

                var variantcount = comp.NumberOfVariants;
                if (variantcount > 3)
                    lst.Add(typeof(Uniconta.DataModel.InvVariant3));
                if (variantcount > 4)
                    lst.Add(typeof(Uniconta.DataModel.InvVariant4));
                if (variantcount > 5)
                    lst.Add(typeof(Uniconta.DataModel.InvVariant5));
            }
            if (comp.Warehouse)
                lst.Add(typeof(Uniconta.DataModel.InvWarehouse));
            if (comp.Shipments)
            {
                lst.Add(typeof(Uniconta.DataModel.ShipmentType));
                lst.Add(typeof(Uniconta.DataModel.DeliveryTerm));
            }
            if (comp.DeliveryAddress)
                lst.Add(typeof(Uniconta.DataModel.WorkInstallation));

            LoadType(lst);
        }
        async void InitialLoad()
        {
            var comp = api.CompanyEntity;
            Credcache = comp.GetCache(typeof(Uniconta.DataModel.Creditor)) ?? await comp.LoadCache(typeof(Uniconta.DataModel.Creditor), api);
        }

    }
}
