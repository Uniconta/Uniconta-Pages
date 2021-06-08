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
            string purchaseHeader;
            if (selectedItem != null)
                purchaseHeader = string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Order"), selectedItem._OrderNumber);
            else
                purchaseHeader = string.Empty;

            switch (ActionType)
            {
                case "EditRow":
                    if (selectedItem == null)
                        return;
                    object[] EditParam = new object[2];
                    EditParam[0] = selectedItem as CreditorInvoiceClient;
                    EditParam[1] = true;
                    AddDockItem(TabControls.CreditorInvoicePage2, EditParam, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("CreditorPackNote"), selectedItem.Name));
                    break;
                case "DeliveryNoteLine":
                    if (selectedItem != null)
                        AddDockItem(TabControls.CreditorInvoiceLine, dgCreditorDeliveryNoteGrid.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("PackNoteNumber"), selectedItem.InvoiceNum));
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
                        AddDockItem(TabControls.UserDocsPage, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Documents"), selectedItem.InvoiceNum));
                    break;
                case "RefreshGrid":
                    InitQuery();
                    break;
#if !SILVERLIGHT
                case "SendAsOutlook":
                    if (selectedItem != null)
                        OpenOutLook(selectedItem);
                    break;
#endif
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

        DebtorMessagesClient[] messagesLookup;
        bool hasLookups;
        async private void ShowDeliveryNote(IEnumerable<CreditorDeliveryNoteLocal> packNoteItems)
        {
            busyIndicator.IsBusy = true;
            busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("GeneratingPage");

            try
            {
                var packNotelist = packNoteItems.ToList();
#if !SILVERLIGHT
                var failedPrints = new List<long>();
                var count = packNotelist.Count;
                string dockName = null, reportName = null;
                bool exportAsPdf = false;
                System.Windows.Forms.FolderBrowserDialog folderDialogSaveInvoice = null;
                hasLookups = false;
                if (count > 1)
                {
                    hasLookups = true;
                    if (count > StandardPrintReportPage.MAX_PREVIEW_REPORT_LIMIT)
                    {
                        var confirmMsg = string.Format(Uniconta.ClientTools.Localization.lookup("PreivewRecordsExportMsg"), count);
                        if (UnicontaMessageBox.Show(confirmMsg, Uniconta.ClientTools.Localization.lookup("Confirmation"), MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                            exportAsPdf = true;
                    }
                    else
                    {
                        dockName = string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Preview"), Uniconta.ClientTools.Localization.lookup("CreditorPackNote"));
                        reportName = Uniconta.ClientTools.Localization.lookup("CreditorPackNote");
                    }
                }
#elif SILVERLIGHT
                int top = 200, left = 300;
                int count = packNotelist.Count();

                if (count > 1)
                {
#endif
                foreach (var pckNote in packNotelist)
                {
#if !SILVERLIGHT
                    IsGeneratingPacknote = true;
                    IPrintReport printreport = await PrintPacknote(pckNote);

                    if (printreport?.Report != null)
                    {
                        if (count > 1 && IsGeneratingPacknote)
                        {
                            ribbonControl.DisableButtons(new string[] { "ShowDeliveryNote" });
                            if (exportAsPdf)
                            {
                                string docName = Uniconta.ClientTools.Localization.lookup("CreditorPackNote");
                                var docNumber = pckNote.InvoiceNum;
                                string directoryPath = string.Empty;
                                if (folderDialogSaveInvoice == null)
                                {
                                    folderDialogSaveInvoice = UtilDisplay.LoadFolderBrowserDialog;
                                    var dialogResult = folderDialogSaveInvoice.ShowDialog();
                                    if (dialogResult == System.Windows.Forms.DialogResult.OK || dialogResult == System.Windows.Forms.DialogResult.Yes)
                                        directoryPath = folderDialogSaveInvoice.SelectedPath;
                                }
                                else
                                    directoryPath = folderDialogSaveInvoice.SelectedPath;

                                Utilities.Utility.ExportReportAsPdf(printreport.Report, directoryPath, docName, docNumber);
                            }
                            else
                            {
                                if (standardPrintPreviewPage == null)
                                    standardPrintPreviewPage = dockCtrl.AddDockItem(api?.CompanyEntity, TabControls.StandardPrintReportPage, ParentControl, new object[] { printreport, Uniconta.ClientTools.Localization.lookup("CreditorPackNote") }
                                    , dockName) as StandardPrintReportPage;
                                else
                                    standardPrintPreviewPage.InsertToMasterReport(printreport.Report);
                            }
                        }
                        else
                        {
                            var pckNumber = pckNote.InvoiceNumber;
                            reportName = await Utilities.Utility.GetLocalizedReportName(api, pckNote, Uniconta.DataModel.CompanyLayoutType.PurchasePacknote);
                            dockName = string.Format("{0} {1}", Uniconta.ClientTools.Localization.lookup("Preview"), string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("CreditorPackNote"), pckNumber));

                            AddDockItem(TabControls.StandardPrintReportPage, new object[] { new List<IPrintReport> { printreport }, reportName }, dockName);
                            break;
                        }
                    }
                    else
                        failedPrints.Add(pckNote.InvoiceNumber);
                }

                IsGeneratingPacknote = false;
                ribbonControl.EnableButtons(new string[] { "ShowDeliveryNote" });

                if (failedPrints.Count > 0)
                {
                    var failedList = string.Join(",", failedPrints);
                    UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("FailedPrintmsg") + failedList, Uniconta.ClientTools.Localization.lookup("Error"), MessageBoxButton.OK);
                }
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
                api.ReportException(ex, string.Format("CreditorDeliveryNotes.ShowPackNote(), CompanyId={0}", api.CompanyId));
            }
            finally { busyIndicator.IsBusy = false; }
        }

#if !SILVERLIGHT
        async private void OpenOutLook(CreditorDeliveryNoteLocal invClient)
        {
            try
            {
                busyIndicator.IsBusy = true;
                busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("LaunchingWaitMsg");
                var creditor = invClient.Creditor;
                var invoiceReport = await PrintPacknote(invClient);
                InvoicePostingPrintGenerator.OpenReportInOutlook(api, invoiceReport, invClient, creditor, Uniconta.DataModel.CompanyLayoutType.PurchasePacknote);
            }
            catch (Exception ex)
            {
                UnicontaMessageBox.Show(ex.Message, Uniconta.ClientTools.Localization.lookup("Exception"));
            }
            finally { busyIndicator.IsBusy = false; }
        }

        bool IsGeneratingPacknote;
        StandardPrintReportPage standardPrintPreviewPage;
        private async Task<IPrintReport> PrintPacknote(CreditorDeliveryNoteLocal creditorInvoice)
        {
            var creditorPrint = new UnicontaClient.Pages.CreditorPrintReport(creditorInvoice, api, Uniconta.DataModel.CompanyLayoutType.PurchasePacknote);

            if (hasLookups)
                await FillLookUps(creditorPrint);

            var isInitializedSuccess = await creditorPrint.InstantiateFields();
            if (isInitializedSuccess)
            {
                var standardCreditorInvoice = new CreditorStandardReportClient(creditorPrint.Company, creditorPrint.Creditor, creditorPrint.CreditorInvoice, creditorPrint.InvTransInvoiceLines, creditorPrint.CreditorOrder,
                    creditorPrint.CompanyLogo, creditorPrint.ReportName, (byte)Uniconta.ClientTools.Controls.Reporting.StandardReports.PurchasePackNote, creditorPrint.CreditorMessage);

                var standardReports = new[] { standardCreditorInvoice };
                IPrintReport iprintReport = new StandardPrintReport(api, standardReports, (byte)Uniconta.ClientTools.Controls.Reporting.StandardReports.PurchasePackNote);
                await iprintReport.InitializePrint();
                if (iprintReport?.Report != null)
                    return iprintReport;

                //Call LayoutInvoice
                var layoutReport = new LayoutPrintReport(api, creditorInvoice, Uniconta.DataModel.CompanyLayoutType.PurchasePacknote);
                layoutReport.SetupLayoutPrintFields(creditorPrint);
                if (hasLookups)
                    layoutReport.SetLookUpForDebtorMessageClients(messagesLookup);

                await layoutReport.InitializePrint();
                return layoutReport;
            }
            return null;
        }

        /// <summary>
        /// Asycn FillLookup for MultiInvoice
        /// </summary>
        /// <param name="debtorInvoicePrint">DebtorInvoicePrint instance</param>
        async private Task FillLookUps(CreditorPrintReport creditorPrintReport)
        {
            if (messagesLookup == null)
                messagesLookup = await api.Query<DebtorMessagesClient>();

            creditorPrintReport.SetLookUpForMessageClient(messagesLookup);
        }

        public override bool IsDataChaged => IsGeneratingPacknote;

#elif SILVERLIGHT

        private void DefaultPrint(CreditorDeliveryNoteLocal creditorInvoice)
        {
            object[] ob = new object[2];
            ob[0] = creditorInvoice;
            ob[1] = Uniconta.DataModel.CompanyLayoutType.PurchasePacknote;
            string headerName = "CreditorPackNote";
            AddDockItem(TabControls.ProformaInvoice, ob, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup(headerName), creditorInvoice.InvoiceNum));
        }

        private void DefaultPrint(CreditorDeliveryNoteLocal creditorInvoice, bool isFloat, Point position)
        {
            object[] ob = new object[2];
            ob[0] = creditorInvoice;
            ob[1] = Uniconta.DataModel.CompanyLayoutType.PurchasePacknote;
            string headerName = "CreditorPackNote";
            AddDockItem(TabControls.ProformaInvoice, ob, isFloat, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup(headerName), creditorInvoice.InvoiceNum), floatingLoc: position);
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
            Credcache = api.GetCache(typeof(Uniconta.DataModel.Creditor)) ?? await api.LoadCache(typeof(Uniconta.DataModel.Creditor));
        }

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.StandardPrintReportPage)
            {
#if !SILVERLIGHT
                IsGeneratingPacknote = false;
                standardPrintPreviewPage = null;
#endif
            }
            if (screenName == TabControls.InvoicePage2)
                dgCreditorDeliveryNoteGrid.UpdateItemSource(argument);
        }
    }
}
