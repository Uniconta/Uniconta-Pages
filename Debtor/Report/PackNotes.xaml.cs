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
using UnicontaClient.Pages;

#if !SILVERLIGHT
using ubl_norway_uniconta;
using Microsoft.Win32;
using UnicontaClient.Pages;
#endif

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class DebtorDeliveryNoteLocal : DebtorDeliveryNoteClient
    {
        [Display(Name = "System Info")]
        public string SystemInfo { get { return _SystemInfo; } }
        public string _SystemInfo;

        internal void NotifySystemInfoSet()
        {
            NotifyPropertyChanged("SystemInfo");
        }
    }

    public class PackNotesGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(DebtorDeliveryNoteLocal); } }
        public override IComparer GridSorting { get { return new DCInvoiceSort(); } }
    }

    public partial class PackNotes : GridBasePage
    {
        public PackNotes(BaseAPI API) : base(API, string.Empty)
        {
            InitPage();
        }
        public PackNotes(SynchronizeEntity syncEntity)
            : base(syncEntity, true)
        {
            this.syncEntity = syncEntity;
            InitPage(syncEntity.Row);
            SetHeader();
        }
        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            dgPackNotesGrid.UpdateMaster(args);
            SetHeader();
            InitQuery();
        }
        void SetHeader()
        {
            var masterClient = dgPackNotesGrid.masterRecord as Debtor;
            if (masterClient == null)
                return;
            string header = string.Format("{0}/{1}", Uniconta.ClientTools.Localization.lookup("Packnote"), masterClient._Account);
            SetHeader(header);
        }
        public PackNotes(UnicontaBaseEntity master)
            : base(master)
        {
            InitPage(master);
        }

        private void InitPage(UnicontaBaseEntity master = null)
        {
            InitializeComponent();
            dgPackNotesGrid.UpdateMaster(master);
            SetRibbonControl(localMenu, dgPackNotesGrid);
            dgPackNotesGrid.api = api;
            dgPackNotesGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
#if SILVERLIGHT
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            UtilDisplay.RemoveMenuCommand(rb, "GenerateOioXml");
#endif
            dgPackNotesGrid.ShowTotalSummary();
            var Comp = api.CompanyEntity;
            if (Comp.RoundTo100)
                CostValue.HasDecimals = NetAmount.HasDecimals = TotalAmount.HasDecimals = Margin.HasDecimals = SalesValue.HasDecimals = false;
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            bool showFields = (dgPackNotesGrid.masterRecords == null);
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
        }

        public async override Task InitQuery()
        {
            await dgPackNotesGrid.Filter(null);

            var api = this.api;
            if (api.CompanyEntity.DeliveryAddress)
            {
                var lst = dgPackNotesGrid.ItemsSource as IEnumerable<DebtorDeliveryNoteLocal>;
                if (lst != null)
                {
                    foreach (var rec in lst)
                    {
                        DebtorOrders.SetDeliveryAdress(rec, rec.Debtor, api);
                    }
                }
            }
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgPackNotesGrid.SelectedItem as DebtorDeliveryNoteLocal;
            string salesHeader = string.Empty;
            if (selectedItem != null)
                salesHeader = string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Orders"), selectedItem._OrderNumber);
            switch (ActionType)
            {
                case "EditRow":
                    if (selectedItem == null)
                        return;
                    object[] EditParam = new object[2];
                    EditParam[0] = selectedItem as DebtorInvoiceClient;
                    EditParam[1] = true;
                    AddDockItem(TabControls.InvoicePage2, EditParam, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("PackNote"), selectedItem.Name));
                    break;
                case "DeliveryNoteLine":
                    if (selectedItem != null)
                        AddDockItem(TabControls.DebtorInvoiceLines, dgPackNotesGrid.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("PackNoteNumber"), selectedItem.InvoiceNum));
                    break;
                case "ShowDeliveryNote":
                    if (selectedItem == null || dgPackNotesGrid.SelectedItems == null)
                        return;
                    var selectedItems = dgPackNotesGrid.SelectedItems.Cast<DebtorDeliveryNoteLocal>();
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

        DCPreviousAddressClient[] previousAddressLookup;
        DebtorMessagesClient[] messagesLookup;
        bool hasLookups;
        async private void ShowDeliveryNote(IEnumerable<DebtorDeliveryNoteLocal> debtorInvoices)
        {
            busyIndicator.IsBusy = true;
            busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("GeneratingPage");

            try
            {
                ribbonControl.DisableButtons("ShowDeliveryNote");

                var invoicesList = debtorInvoices.ToList();
#if !SILVERLIGHT
                var failedPrints = new List<long>();
                var count = invoicesList.Count;
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
                        dockName = string.Concat(Uniconta.ClientTools.Localization.lookup("ShowPrint"), ":", Uniconta.ClientTools.Localization.lookup("Packnote"));
                        reportName = Uniconta.ClientTools.Localization.lookup("Packnote");
                    }
                }
#elif SILVERLIGHT
                int top = 200, left = 300;
                int count = invoicesList.Count();

                if (count > 1)
                {
#endif
                foreach (var debtInvoice in invoicesList)
                {
#if !SILVERLIGHT
                    isGeneratingPacknote = true;
                    IPrintReport printReport = await PrintPacknote(debtInvoice);
                    if (printReport?.Report != null)
                    {
                        if (count > 1 && isGeneratingPacknote)
                        {
                            if (exportAsPdf)
                            {
                                string docName = Uniconta.ClientTools.Localization.lookup("Packnote");
                                var docNumber = debtInvoice._PackNote;
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

                                Utility.ExportReportAsPdf(printReport.Report, directoryPath, docName, docNumber.ToString());
                            }
                            else
                            {
                                if (standardViewerPrintPage == null)
                                    standardViewerPrintPage = dockCtrl.AddDockItem(api?.CompanyEntity, TabControls.StandardPrintReportPage, ParentControl, new object[] { printReport, reportName }, dockName) as StandardPrintReportPage;
                                else
                                    standardViewerPrintPage.InsertToMasterReport(printReport.Report);
                            }
                        }
                        else
                        {
                            var pckNumber = debtInvoice.InvoiceNumber;
                            reportName = await Utilities.Utility.GetLocalizedReportName(api, debtInvoice, CompanyLayoutType.Packnote);
                            dockName = string.Format("{0} {1}", Uniconta.ClientTools.Localization.lookup("Preview"), string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("PackNote"), pckNumber));

                            AddDockItem(TabControls.StandardPrintReportPage, new object[] { new List<IPrintReport> { printReport }, reportName }, dockName);
                        }
                    }
                    else
                        failedPrints.Add(debtInvoice.InvoiceNumber);

                }

                isGeneratingPacknote = false;

                if (failedPrints.Count > 0)
                {
                    var failedList = string.Join(",", failedPrints);
                    UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("FailedPrintmsg") + failedList, Uniconta.ClientTools.Localization.lookup("Error"), MessageBoxButton.OK);
                }
#elif SILVERLIGHT
                        DefaultPrint(debtInvoice, true, new Point(top, left));
                        left = left - left / count;
                        top = top - top / count;
                    }
                }
                else
                    DefaultPrint(debtorInvoices.Single());
#endif
            }
            catch (Exception ex)
            {
                api.ReportException(ex, string.Format("PackNotes.ShowInvoice(), CompanyId={0}", api.CompanyId));
            }
            finally
            {
                ribbonControl?.EnableButtons("ShowDeliveryNote");
                busyIndicator.IsBusy = false;
            }
        }
#if !SILVERLIGHT
        /// <summary>
        /// Asycn FillLookup for MultiInvoice
        /// </summary>
        /// <param name="debtorInvoicePrint">DebtorInvoicePrint instance</param>
        async private Task FillLookUps(DebtorInvoicePrintReport debtorInvoicePrint)
        {
            if (previousAddressLookup == null)
                previousAddressLookup = await api.Query<DCPreviousAddressClient>();

            debtorInvoicePrint.SetLookUpForPreviousAddressClients(previousAddressLookup);

            if (messagesLookup == null)
                messagesLookup = await api.Query<DebtorMessagesClient>();

            debtorInvoicePrint.SetLookUpForDebtorMessageClients(messagesLookup);
        }

        /// <summary>
        /// Fill look up for MultiInvoice
        /// </summary>
        /// <param name="layoutPrint">LayoutPrint Instances</param>
        private void FillLookUps(LayoutPrintReport layoutPrint)
        {
            layoutPrint.SetLookUpForDebtorMessageClients(messagesLookup);
            layoutPrint.SetLookUpForPreviousAddressClients(previousAddressLookup);
        }



        async private void OpenOutLook(DebtorInvoiceClient invClient)
        {
            try
            {
                busyIndicator.IsBusy = true;
                busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("LaunchingWaitMsg");
                var debtor = invClient.Debtor;
                var invoiceReport = await PrintPacknote(invClient);
                InvoicePostingPrintGenerator.OpenReportInOutlook(api, invoiceReport, invClient, debtor, CompanyLayoutType.Packnote);
            }
            catch (Exception ex)
            {
                UnicontaMessageBox.Show(ex.Message, Uniconta.ClientTools.Localization.lookup("Exception"));
            }
            finally { busyIndicator.IsBusy = false; }
        }

        bool isGeneratingPacknote;
        StandardPrintReportPage standardViewerPrintPage;
        private async Task<IPrintReport> PrintPacknote(DebtorInvoiceClient debtorInvoice)
        {
            var debtorQcpPrint = new UnicontaClient.Pages.DebtorInvoicePrintReport(debtorInvoice, api, CompanyLayoutType.Packnote);

            //In case of Multple invoices we create a lookup for Previous Address Clients
            if (hasLookups)
                await FillLookUps(debtorQcpPrint);

            var isInitializedSuccess = await debtorQcpPrint.InstantiateFields();
            if (isInitializedSuccess)
            {
                var standardDebtorPackNote = new DebtorQCPReportClient(debtorQcpPrint.Company, debtorQcpPrint.Debtor, debtorQcpPrint.DebtorInvoice, debtorQcpPrint.InvTransInvoiceLines, debtorQcpPrint.DebtorOrder,
                    debtorQcpPrint.CompanyLogo, debtorQcpPrint.ReportName, (byte)Uniconta.ClientTools.Controls.Reporting.StandardReports.PackNote, messageClient: debtorQcpPrint.MessageClient);

                var standardReports = new[] { standardDebtorPackNote };
                IPrintReport iprintReport = new StandardPrintReport(api, standardReports, (byte)Uniconta.ClientTools.Controls.Reporting.StandardReports.PackNote);
                await iprintReport.InitializePrint();
                if (iprintReport.Report != null)
                    return iprintReport;

                //Call Invoice Layout
                var layoutPrint = new LayoutPrintReport(api, debtorInvoice, CompanyLayoutType.Packnote);
                layoutPrint.SetupLayoutPrintFields(debtorQcpPrint);
                
                if (hasLookups)
                    FillLookUps(layoutPrint);

                await layoutPrint.InitializePrint();
                return layoutPrint;
            }
            return null;
        }

        public override bool IsDataChaged => isGeneratingPacknote;

#elif SILVERLIGHT
        private void DefaultPrint(DebtorInvoiceClient debtorInvoice)
        {
            object[] ob = new object[2];
            ob[0] = debtorInvoice;
            ob[1] = CompanyLayoutType.Packnote;
            string headerName = "PackNote";
            AddDockItem(TabControls.ProformaInvoice, ob, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup(headerName), debtorInvoice.InvoiceNum));
        }

        private void DefaultPrint(DebtorInvoiceClient debtorInvoice, bool isFloat, Point position, bool isPackNote = false)
        {
            object[] ob = new object[2];
            ob[0] = debtorInvoice;
            ob[1] = CompanyLayoutType.Packnote;
            string headerName = "PackNote";
            AddDockItem(TabControls.ProformaInvoice, ob, isFloat, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup(headerName), debtorInvoice.InvoiceNum), floatingLoc: position);
        }
#endif

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
                        AddDockItem(TabControls.DebtorOrderLines, order, string.Format("{0}:{1},{2}", Uniconta.ClientTools.Localization.lookup("OrdersLine"), order._OrderNumber, order._DCAccount));
                        break;

                    case CWConfirmationBox.ConfirmationResultEnum.No:
                        break;
                }
            };
            confirmationBox.Show();

        }
        void SendDeliveryNote(DebtorInvoiceClient invClient)
        {
            UnicontaClient.Pages.CWSendInvoice cwSendInvoice = new UnicontaClient.Pages.CWSendInvoice();
#if !SILVERLIGHT
            cwSendInvoice.DialogTableId = 2000000029;
#endif
            cwSendInvoice.Closed += async delegate
            {
                if (cwSendInvoice.DialogResult == true)
                {
                    busyIndicator.IsBusy = true;
                    InvoiceAPI Invapi = new InvoiceAPI(api);
                    ErrorCodes res = await Invapi.SendInvoice(invClient, cwSendInvoice.Emails, cwSendInvoice.sendOnlyToThisEmail, CWSendInvoice.sendInBackgroundOnly);

                    busyIndicator.IsBusy = false;
                    if (res == ErrorCodes.Succes)
                        UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("SendEmailMsgOBJ"), Uniconta.ClientTools.Localization.lookup("PackNote")), Uniconta.ClientTools.Localization.lookup("Message"));
                    else
                        UtilDisplay.ShowErrorCode(res);
                }
            };
            cwSendInvoice.Show();
        }

        protected override void LoadCacheInBackGround()
        {
            var Comp = api.CompanyEntity;

            var lst = new List<Type>(12) { typeof(Uniconta.DataModel.InvItem), typeof(Uniconta.DataModel.PaymentTerm) };
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

        void setDim()
        {
            UnicontaClient.Utilities.Utility.SetDimensionsGrid(api, cldim1, cldim2, cldim3, cldim4, cldim5);
        }

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.StandardPrintReportPage)
            {
#if !SILVERLIGHT
                isGeneratingPacknote = false;
                standardViewerPrintPage = null;
#endif
            }
            if (screenName == TabControls.InvoicePage2)
                dgPackNotesGrid.UpdateItemSource(argument);
        }
    }
}
