using UnicontaClient.Models;
using UnicontaClient.Pages;
using UnicontaClient.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Uniconta.API.DebtorCreditor;
using Uniconta.API.Service;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{

    public class MultiInvoiceGrid : CorasauDataGridClient
    {
        public override bool SingleBufferUpdate { get { return false; } }
        public override Type TableType { get { return typeof(DebtorOrderClient); } }
        public override IComparer GridSorting { get { return new DCOrderSort(); } }

        public override bool Readonly { get { return false; } }
    }

    public partial class MultiInvoicePage : GridBasePage
    {
        public override string NameOfControl
        {
            get { return TabControls.MultiInvoicePage.ToString(); }
        }

        public override bool IsDataChaged { get { return false; } }

        SQLCache items, debtors;

        public MultiInvoicePage(BaseAPI API) : base(API, string.Empty)
        {
            InitializeComponent();
            localMenu.dataGrid = dgMultiInvGrid;
            SetRibbonControl(localMenu, dgMultiInvGrid);
            dgMultiInvGrid.api = api;
            dgMultiInvGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
            dgMultiInvGrid.ShowTotalSummary();

            //Same ribbon menu is used in Creditor MassUpdate page. Physical Voucher local menu is hided.
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            UtilDisplay.RemoveMenuCommand(rb, new string[] { "ViewVoucher", "RefVoucher", "ImportVoucher", "RemoveVoucher" });

            var Comp = api.CompanyEntity;
            this.debtors = Comp.GetCache(typeof(Debtor));
            this.items = Comp.GetCache(typeof(InvItem));
        }

        protected override LookUpTable HandleLookupOnLocalPage(LookUpTable lookup, CorasauDataGrid dg)
        {
            var order = dg.SelectedItem as DebtorOrderClient;
            if (order == null)
                return lookup;
            if (dg.CurrentColumn?.Name == "Account")
                lookup.TableType = typeof(Uniconta.DataModel.Debtor);
            else if (dg.CurrentColumn?.Name == "OrderNumber")
                lookup.TableType = typeof(Uniconta.DataModel.DebtorOrder);
            return lookup;
        }
        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
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

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgMultiInvGrid.SelectedItem as DebtorOrderClient;
            switch (ActionType)
            {
                case "EditRow":
                    if (selectedItem == null)
                        return;
                    string salesHeader = string.Format("{0}:{1}", Uniconta.ClientTools.Localization.lookup("Orders"), selectedItem._OrderNumber);
                    if (dgMultiInvGrid.masterRecords != null)
                    {
                        object[] arr = new object[2] { selectedItem, dgMultiInvGrid.masterRecord };
                        AddDockItem(TabControls.DebtorOrdersPage2, arr, salesHeader);
                    }
                    else
                    {
                        AddDockItem(TabControls.DebtorOrdersPage2, selectedItem, salesHeader);
                    }
                    break;
                case "DeleteRow":
                    dgMultiInvGrid.DeleteRow();
                    break;
                case "OrderLine":
                    if (selectedItem == null)
                        return;
                    var olheader = string.Format("{0}:{1},{2}", Uniconta.ClientTools.Localization.lookup("OrdersLine"), selectedItem._OrderNumber, selectedItem.Name);
                    AddDockItem(TabControls.DebtorOrderLines, dgMultiInvGrid.syncEntity, olheader);
                    break;
                case "GenerateInvoice":
                    if (Utility.HasControlRights("GenerateInvoice", api.CompanyEntity))
                        GenerateInvoice();
                    else
                        UtilDisplay.ShowControlAccessMsg("GenerateInvoice");
                    break;
                case "GenerateInvoice2":
                    if (selectedItem != null)
                    {
                        if (Utility.HasControlRights("GenerateInvoice", api.CompanyEntity))
                        {
                            var debtor = ClientHelper.GetRef(selectedItem.CompanyId, typeof(Debtor), selectedItem._DCAccount) as Debtor;
                            if (debtor != null)
                            {
                                var InvoiceAccount = selectedItem._InvoiceAccount ?? debtor._InvoiceAccount;
                                if (InvoiceAccount != null)
                                    debtor = ClientHelper.GetRef(selectedItem.CompanyId, typeof(Debtor), InvoiceAccount) as Debtor;
                                if (debtor._PricesInclVat != selectedItem._PricesInclVat)
                                {
                                    var confirmationMsgBox = UnicontaMessageBox.Show(string.Format("{0}.\n{1}", string.Format(Uniconta.ClientTools.Localization.lookup("DebtorAndOrderMix"), Uniconta.ClientTools.Localization.lookup("InclVat")),
                                    Uniconta.ClientTools.Localization.lookup("ProceedConfirmation")), Uniconta.ClientTools.Localization.lookup("Confirmation"), MessageBoxButton.OKCancel);
                                    if (confirmationMsgBox != MessageBoxResult.OK)
                                        return;
                                }
                            }
                            GenerateRecordInvoice(selectedItem);
                        }
                        else
                            UtilDisplay.ShowControlAccessMsg("GenerateInvoice");
                    }
                    break;
                case "OrderConfirmation":
                    OrderConfirmation(CompanyLayoutType.OrderConfirmation);
                    break;
                case "PackNote":
                    OrderConfirmation(CompanyLayoutType.Packnote);
                    break;
                case "PickList":
                    PickList();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
#if SILVERLIGHT
        List<InvoicePostingResult> packListPosted;
#else
        Dictionary<InvoicePostingResult, DebtorOrderClient> packListPosted;
#endif
        private void PickList()
        {
#if SILVERLIGHT
            packListPosted = new List<InvoicePostingResult>();
#else
            packListPosted = new Dictionary<InvoicePostingResult, DebtorOrderClient>();
#endif
            var cwPickingList = new CWGeneratePickingList(false, false);
#if !SILVERLIGHT
            cwPickingList.DialogTableId = 2000000024;
#endif
            cwPickingList.Closed += async delegate
            {
                if (cwPickingList.DialogResult == true)
                {
                    var selectedDate = cwPickingList.SelectedDate;
                    busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("LoadingMsg");
                    busyIndicator.IsBusy = true;
                    InvoiceAPI Invapi = new InvoiceAPI(api);
                    int cnt = 0;
                    var errorList = new List<string>();
                    var dbVisibleOrders = dgMultiInvGrid.GetVisibleRows() as IEnumerable<DebtorOrderClient>;
                    foreach (var dbOrder in dbVisibleOrders)
                    {
                        var result = await Invapi.PostInvoice(dbOrder, null, selectedDate, 0, false, new DebtorInvoiceClient(), new DebtorInvoiceLines(), false, cwPickingList.ShowDocument || cwPickingList.PrintDocument,
                            CompanyLayoutType.PickingList);

                        if (result.Err == ErrorCodes.Succes)
                        {
#if SILVERLIGHT
                            packListPosted.Add(result);
#else
                            packListPosted.Add(result, dbOrder);
#endif
                            cnt++;
                        }
                        else
                        {
                            string error = string.Format("{0}:{1}", dbOrder.OrderNumber, Uniconta.ClientTools.Localization.lookup(result.Err.ToString()));
                            errorList.Add(error);
                        }
                    }
                    busyIndicator.IsBusy = false;

                    int picklistPreviewCount = packListPosted.Count;
                    string updatedMsg = string.Format(Uniconta.ClientTools.Localization.lookup("MulitDocPrintConfirmationMsg"), picklistPreviewCount,
                        string.Format("{0} {1}", Uniconta.ClientTools.Localization.lookup(CompanyLayoutType.PickingList.ToString()), Uniconta.ClientTools.Localization.lookup("Documents")));

                    if (errorList.Count == 0)
                        InitMultiplePreviewDocument(updatedMsg, CompanyLayoutType.PickingList, cwPickingList.PrintDocument);
                    else
                    {
                        CWErrorBox errorDialog = new CWErrorBox(errorList.ToArray(), true);
                        errorDialog.Closed += delegate { InitMultiplePreviewDocument(updatedMsg, CompanyLayoutType.PickingList, cwPickingList.PrintDocument); };
                        errorDialog.Show();
                    }
                }
            };
            cwPickingList.Show();
        }

#if SILVERLIGHT
        List<InvoicePostingResult> invoicePosted;
#else
        Dictionary<InvoicePostingResult, DebtorOrderClient> invoicePosted;
#endif
        private void GenerateInvoice()
        {
#if SILVERLIGHT
            invoicePosted = new List<InvoicePostingResult>();
#else
            invoicePosted = new Dictionary<InvoicePostingResult, DebtorOrderClient>();
#endif
            UnicontaClient.Pages.CWGenerateInvoice GenrateInvoiceDialog = new UnicontaClient.Pages.CWGenerateInvoice(true, string.Empty, false, true, true, isOrderOrQuickInv: true, isQuickPrintVisible: true, isPageCountVisible: false, isDebtorOrder: true);
#if !SILVERLIGHT
            GenrateInvoiceDialog.DialogTableId = 2000000011;
#endif
            GenrateInvoiceDialog.SetInvPrintPreview(printPreview);
            GenrateInvoiceDialog.Closed += async delegate
            {
                if (GenrateInvoiceDialog.DialogResult == true)
                {
                    printPreview = GenrateInvoiceDialog.ShowInvoice;
                    busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("SendingWait");
                    busyIndicator.IsBusy = true;
                    var InvoiceDate = GenrateInvoiceDialog.GenrateDate;
                    InvoiceAPI Invapi = new InvoiceAPI(api);
                    int cnt = 0;
                    List<string> errorList = new List<string>();
                    var dbVisibleOrders = dgMultiInvGrid.GetVisibleRows() as IEnumerable<DebtorOrderClient>;
                    foreach (var dbOrder in dbVisibleOrders)
                    {
                        if (dbOrder._SubscriptionEnded != DateTime.MinValue && dbOrder._SubscriptionEnded < InvoiceDate)
                            continue;

                        var result = await Invapi.PostInvoice(dbOrder, null, InvoiceDate,
                            0, GenrateInvoiceDialog.IsSimulation, new DebtorInvoiceClient(),
                            new DebtorInvoiceLines(), GenrateInvoiceDialog.SendByEmail, (GenrateInvoiceDialog.ShowInvoice || GenrateInvoiceDialog.InvoiceQuickPrint || GenrateInvoiceDialog.GenerateOIOUBLClicked), 0,
                            GenrateInvoiceDialog.Emails, GenrateInvoiceDialog.sendOnlyToThisEmail, null, null, GenrateInvoiceDialog.PostOnlyDelivered, false);
                        if (result != null && result.Err == 0)
                        {
#if SILVERLIGHT
                            invoicePosted.Add(result);
#else
                            invoicePosted.Add(result, dbOrder);
#endif
                            cnt++;

                            var dc = dbOrder.Debtor;
                            if (dc == null)
                            {
                                await api.LoadCache(typeof(Debtor), true);
                                dc = dbOrder.Debtor;
                            }

                            DebtorOrders.SetDeliveryAdress(result.Header, dc, api);
                        }
                        else
                        {
                            string error = string.Format("{0}:{1}", dbOrder._OrderNumber, Uniconta.ClientTools.Localization.lookup(result.Err.ToString()));
                            errorList.Add(error);
                        }
                    }

                    busyIndicator.IsBusy = false;
                    string updatedMsg = Uniconta.ClientTools.Localization.lookup("Succes");
                    if (!GenrateInvoiceDialog.IsSimulation)
                        updatedMsg = string.Format(Uniconta.ClientTools.Localization.lookup("RecordsUpdated"), cnt, Uniconta.ClientTools.Localization.lookup("DebtorOrders"));

#if !SILVERLIGHT
                    if (GenrateInvoiceDialog.GenerateOIOUBLClicked && !GenrateInvoiceDialog.IsSimulation)
                        GenerateOIOXmlForAll(errorList, !GenrateInvoiceDialog.SendByEmail);
#endif
                    int previewInvoiceCount = invoicePosted.Count;
                    updatedMsg = updatedMsg + "\n" + string.Format(Uniconta.ClientTools.Localization.lookup("MulitDocPrintConfirmationMsg"), previewInvoiceCount, Uniconta.ClientTools.Localization.lookup("Invoices"));

                    if (errorList.Count == 0)
                        PreInitMulitplePreviewDocument(updatedMsg, CompanyLayoutType.Invoice, GenrateInvoiceDialog.ShowInvoice, GenrateInvoiceDialog.InvoiceQuickPrint, previewInvoiceCount, GenrateInvoiceDialog.SendByEmail,
                            GenrateInvoiceDialog.sendOnlyToThisEmail);
                    else
                    {
                        CWErrorBox errorDialog = new CWErrorBox(errorList.ToArray(), true);
                        errorDialog.Closed += delegate
                        {
                            PreInitMulitplePreviewDocument(updatedMsg, CompanyLayoutType.Invoice, GenrateInvoiceDialog.ShowInvoice, GenrateInvoiceDialog.InvoiceQuickPrint, previewInvoiceCount, GenrateInvoiceDialog.SendByEmail,
                                GenrateInvoiceDialog.sendOnlyToThisEmail);
                        };
                        errorDialog.Show();
                    }
                }
            };
            GenrateInvoiceDialog.Show();
        }

        private void PreInitMulitplePreviewDocument(string message, CompanyLayoutType documentType, bool showInvoice, bool quickPrintInvoice, int docCount, bool sendBymail, bool sendOnlyToMail)
        {
            if (docCount > 0)
            {
                if (showInvoice || quickPrintInvoice)
                    InitMultiplePreviewDocument(message, documentType, quickPrintInvoice);
                else if (sendBymail || sendOnlyToMail)
                    UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("SendEmailMsgOBJ"), string.Format("{0} {1}", docCount, Uniconta.ClientTools.Localization.lookup(documentType.ToString()))),
                        Uniconta.ClientTools.Localization.lookup("Message"));
            }
        }

        private void InitMultiplePreviewDocument(string updatedMsg, CompanyLayoutType docType, bool isQuickPrint)
        {
#if !SILVERLIGHT
            if (UnicontaMessageBox.Show(updatedMsg, Uniconta.ClientTools.Localization.lookup("Message"), MessageBoxButton.YesNo) == MessageBoxResult.Yes)
#else
            if (UnicontaMessageBox.Show(updatedMsg, Uniconta.ClientTools.Localization.lookup("Message"), MessageBoxButton.OKCancel) == MessageBoxResult.OK)
#endif
                switch (docType)
                {
                    case CompanyLayoutType.Invoice:
                        ShowMultipleInvoicePreview(isQuickPrint);
                        break;
                    case CompanyLayoutType.Packnote:
                    case CompanyLayoutType.OrderConfirmation:
                        ShowMultipleOrderToPrint(docType, isQuickPrint);
                        break;
                    case CompanyLayoutType.PickingList:
                        ShowMultiplePackListPreview(isQuickPrint);
                        break;
                }
        }

#if SILVERLIGHT
        List<InvoicePostingResult> confirmOrder;
#else
        Dictionary<InvoicePostingResult, DebtorOrderClient> confirmOrder;
#endif
        static bool printPreview = true;
        private void OrderConfirmation(CompanyLayoutType docType)
        {
#if SILVERLIGHT
            confirmOrder = new List<InvoicePostingResult>();
#else
            confirmOrder = new Dictionary<InvoicePostingResult, DebtorOrderClient>();
#endif
            UnicontaClient.Pages.CWGenerateInvoice GenrateInvoiceDialog = new UnicontaClient.Pages.CWGenerateInvoice(false, docType.ToString(),isShowInvoiceVisible: true, askForEmail: true, showInputforInvNumber: false, 
                showInvoice: true, isShowUpdateInv: true, isQuickPrintVisible: true, isDebtorOrder: true, isPageCountVisible: false);
#if !SILVERLIGHT
            GenrateInvoiceDialog.DialogTableId = 2000000012;
#endif
            GenrateInvoiceDialog.SetInvPrintPreview(printPreview);
            GenrateInvoiceDialog.Closed += async delegate
            {
                if (GenrateInvoiceDialog.DialogResult == true)
                {
                    printPreview = GenrateInvoiceDialog.ShowInvoice;
                    busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("SendingWait");
                    busyIndicator.IsBusy = true;
                    var InvoiceDate = GenrateInvoiceDialog.GenrateDate;
                    var updateStatus = GenrateInvoiceDialog.UpdateInventory;
                    InvoiceAPI Invapi = new InvoiceAPI(api);
                    int cnt = 0;
                    List<string> errorList = new List<string>();
                    var dgOrderVisible = dgMultiInvGrid.GetVisibleRows() as IEnumerable<DebtorOrderClient>;
                    foreach (var dbOrder in dgOrderVisible)
                    {
                        var result = await Invapi.PostInvoice(dbOrder, null, GenrateInvoiceDialog.GenrateDate, 0,
                          !updateStatus, new DebtorInvoiceClient(),
                          new DebtorInvoiceLines(), GenrateInvoiceDialog.SendByEmail, GenrateInvoiceDialog.ShowInvoice || GenrateInvoiceDialog.InvoiceQuickPrint, docType,
                          GenrateInvoiceDialog.Emails, GenrateInvoiceDialog.sendOnlyToThisEmail, null, null, GenrateInvoiceDialog.PostOnlyDelivered, false);

                        if (result != null && result.Err == 0 && (GenrateInvoiceDialog.ShowInvoice || GenrateInvoiceDialog.InvoiceQuickPrint))
                        {
                            DebtorOrders.Updatedata(dbOrder, docType);

                            var dc = dbOrder.Debtor;
                            if (dc == null)
                            {
                                await api.LoadCache(typeof(Debtor), true);
                                dc = dbOrder.Debtor;
                            }
                            DebtorOrders.SetDeliveryAdress(result.Header, dc, api);
#if SILVERLIGHT
                            confirmOrder.Add(result);
#else
                            confirmOrder.Add(result, dbOrder);
#endif
                            cnt++;
                        }
                        else
                        {
                            string error = string.Format("{0}:{1}", dbOrder._OrderNumber, Uniconta.ClientTools.Localization.lookup(result.Err.ToString()));
                            errorList.Add(error);
                        }
                    }
                    busyIndicator.IsBusy = false;
                    string updatedMsg = Uniconta.ClientTools.Localization.lookup("Succes");


                    if (!GenrateInvoiceDialog.IsSimulation)
                        updatedMsg = string.Format(Uniconta.ClientTools.Localization.lookup("RecordsUpdated"), cnt, Uniconta.ClientTools.Localization.lookup("DebtorOrders"));

                    int documentsPreviewPrint = confirmOrder.Count;
                    updatedMsg = updatedMsg + "\n" + string.Format(Uniconta.ClientTools.Localization.lookup("MulitDocPrintConfirmationMsg"), documentsPreviewPrint,
                        string.Format("{0} {1}", Uniconta.ClientTools.Localization.lookup(docType.ToString()), Uniconta.ClientTools.Localization.lookup("Documents")));

                    if (errorList.Count == 0)
                        PreInitMulitplePreviewDocument(updatedMsg, docType, GenrateInvoiceDialog.ShowInvoice, GenrateInvoiceDialog.InvoiceQuickPrint, documentsPreviewPrint, GenrateInvoiceDialog.SendByEmail,
                            GenrateInvoiceDialog.sendOnlyToThisEmail);
                    else
                    {
                        CWErrorBox errorDialog = new CWErrorBox(errorList.ToArray(), true);
                        errorDialog.Closed += delegate
                        {
                            PreInitMulitplePreviewDocument(updatedMsg, docType, GenrateInvoiceDialog.ShowInvoice, GenrateInvoiceDialog.InvoiceQuickPrint, documentsPreviewPrint, GenrateInvoiceDialog.SendByEmail,
                                GenrateInvoiceDialog.sendOnlyToThisEmail);
                        };
                        errorDialog.Show();
                    }

                }
            };
            GenrateInvoiceDialog.Show();
        }

        void ShowMultipleOrderToPrint(CompanyLayoutType docType, bool isQuickPrint)
        {
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(1000);
            timer.Tick += async delegate
            {
                timer.Stop();
#if SILVERLIGHT
                int top = 200;
                int left = 300;
                int invoiceCount = 1;
                int itemcount = confirmOrder.Count();
#else
                List<IPrintReport> xtraReports = new List<IPrintReport>();
#endif
                busyIndicator.IsBusy = true;
                foreach (var item in confirmOrder)
                {
#if SILVERLIGHT
                    if (item.Header == null) continue;

                    var deb = (DebtorInvoiceClient)item.Header;
                    var printHeader = string.Format("{0}: {1}, {2}", Uniconta.ClientTools.Localization.lookup("ShowPrint") + "-" + invoiceCount.ToString(), deb._DCAccount, deb.Name);

                    object[] ob = new object[2] { item, docType };

                    AddDockItem(TabControls.ProformaInvoice, ob, true, printHeader, null, new Point(top, left));
                    left = left - left / itemcount;
                    top = top - top / itemcount;
                    invoiceCount++;
                }
                busyIndicator.IsBusy = false;
#else
                    if (item.Key.Header == null) continue;
                    var standardPrint = await ValidateStandardPrint(item.Key, item.Value, docType, isQuickPrint);

                    if (standardPrint?.Report != null)
                        xtraReports.Add(standardPrint);
                }
                busyIndicator.IsBusy = false;

                if (xtraReports.Count > 0)
                {
                    if (!isQuickPrint)
                    {
                        var reportName = Uniconta.ClientTools.Localization.lookup(docType.ToString());
                        AddDockItem(UnicontaTabs.StandardPrintReportPage, new object[] { xtraReports, reportName }, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("ShowPrint"),
                            reportName));
                    }
                    else
                        QuickPrintReports(xtraReports);
                }
#endif
            };
            timer.Start();
        }

        void ShowMultiplePackListPreview(bool isQuickPrint)
        {
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(1000);
            timer.Tick += async delegate
            {
                timer.Stop();
#if SILVERLIGHT
                int top = 200;
                int left = 300;
                int packListCount = 1;
                int itemCount = packListPosted.Count;
#else
                var xtraReports = new List<IPrintReport>();
#endif
                busyIndicator.IsBusy = true;
                foreach (var item in packListPosted)
                {
#if SILVERLIGHT
                    if(item.Header == null) continue;
                    
                    var deb = (DebtorInvoiceClient)item.Header;
                    var printHeader = string.Format("{0}: {1},{2}", Uniconta.ClientTools.Localization.lookup("ShowPrint") + "-" + packListCount.ToString(), deb.Account, deb.Name);

                    object[] ob = new object[2];
                    ob[0] = item;
                    ob[1] = CompanyLayoutType.PickingList;

                    AddDockItem(TabControls.ProformaInvoice, ob, true, printHeader, null, new Point(top, left));
                    left = left - left / packListCount;
                    top = top - top / packListCount;
                    packListCount++;
                }
                busyIndicator.IsBusy = false;
#else
                    if (item.Key.Header == null) continue;
                    var standardPrint = await ValidateStandardPrint(item.Key, item.Value, CompanyLayoutType.PickingList, isQuickPrint);

                    if (standardPrint?.Report != null)
                        xtraReports.Add(standardPrint);
                }
                busyIndicator.IsBusy = false;

                if (xtraReports.Count > 0)
                {
                    if (!isQuickPrint)
                    {
                        var dockname = string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("ShowPrint"), Uniconta.ClientTools.Localization.lookup("Packlist"));
                        AddDockItem(TabControls.StandardPrintReportPage, new object[] { xtraReports, Uniconta.ClientTools.Localization.lookup("Packlist") }, dockname);
                    }
                    else
                        QuickPrintReports(xtraReports);
                }
#endif
            };
            timer.Start();
        }

        void ShowMultipleInvoicePreview(bool isQuickPrint)
        {
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(1000);
            timer.Tick += async delegate
            {
                timer.Stop();
#if SILVERLIGHT
                int top = 200;
                int left = 300;
                int invoiceCount = 1;
                int itemcount = invoicePosted.Count();
#else
                List<IPrintReport> xtraReports = new List<IPrintReport>();
#endif
                busyIndicator.IsBusy = true;
                foreach (var item in invoicePosted)
                {
#if SILVERLIGHT
                    if (item.Header == null) continue;

                    var deb = (DebtorInvoiceClient)item.Header;
                    var printHeader = string.Format("{0}: {1}, {2}", Uniconta.ClientTools.Localization.lookup("ShowPrint") + "-" + invoiceCount.ToString(), deb.Account, deb.Name);

                    object[] ob = new object[2];
                    ob[0] = item;
                    ob[1] = CompanyLayoutType.Invoice;

                    AddDockItem(TabControls.ProformaInvoice, ob, true, printHeader, null, new Point(top, left));
                    left = left - left / itemcount;
                    top = top - top / itemcount;
                    invoiceCount++;
                }
                busyIndicator.IsBusy = false;
#else
                    if (item.Key.Header == null) continue;
                    var standardPrint = await ValidateStandardPrint(item.Key, item.Value, CompanyLayoutType.Invoice, isQuickPrint);

                    if (standardPrint?.Report != null)
                        xtraReports.Add(standardPrint);
                }
                busyIndicator.IsBusy = false;
                if (xtraReports.Count > 0)
                {
                    if (!isQuickPrint)
                    {
                        var dockname = string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("ShowPrint"), Uniconta.ClientTools.Localization.lookup("Invoices"));
                        AddDockItem(TabControls.StandardPrintReportPage, new object[] { xtraReports, Uniconta.ClientTools.Localization.lookup("Invoices") }, dockname);
                    }
                    else
                        QuickPrintReports(xtraReports);
                }
#endif
            };
            timer.Start();
        }

#if !SILVERLIGHT

        private void QuickPrintReports(List<IPrintReport> xtraReports)
        {
            var reports = new List<DevExpress.XtraReports.UI.XtraReport>();
            foreach (var rep in xtraReports)
                reports.Add(rep.Report);
            var unicontaAutoPrint = new UnicontaAutoPrint(reports);
            unicontaAutoPrint.ExecutePrintCommandAsync();
        }
#endif

        private void GenerateRecordInvoice(DebtorOrderClient dbOrder)
        {
            InvoiceAPI Invapi = new InvoiceAPI(api);
            var debtor = dbOrder.Debtor;
            bool showSendByMail = true;
            if (debtor != null)
                showSendByMail = !string.IsNullOrEmpty(debtor.InvoiceEmail);
            else
                api.LoadCache(typeof(Debtor), true);

            string debtorName = debtor?.Name ?? dbOrder._DCAccount;
            bool invoiceInXML = debtor?._InvoiceInXML ?? false;
            UnicontaClient.Pages.CWGenerateInvoice GenrateInvoiceDialog = new UnicontaClient.Pages.CWGenerateInvoice(true, string.Empty, false, true, true, showNoEmailMsg: !showSendByMail, debtorName: debtorName, isDebtorOrder: true, isOrderOrQuickInv: true, InvoiceInXML: invoiceInXML);
#if !SILVERLIGHT
            GenrateInvoiceDialog.DialogTableId = 2000000013;
#endif
            GenrateInvoiceDialog.Closed += async delegate
            {
                if (GenrateInvoiceDialog.DialogResult == true)
                {
                    busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("SendingWait");
                    busyIndicator.IsBusy = true;
                    var showOrPrint = GenrateInvoiceDialog.ShowInvoice || GenrateInvoiceDialog.InvoiceQuickPrint;
                    var result = await Invapi.PostInvoice(dbOrder, null, GenrateInvoiceDialog.GenrateDate,
                       0, GenrateInvoiceDialog.IsSimulation, new DebtorInvoiceClient(), new DebtorInvoiceLines(),
                       GenrateInvoiceDialog.SendByEmail, (showOrPrint || GenrateInvoiceDialog.GenerateOIOUBLClicked), 0, GenrateInvoiceDialog.Emails, GenrateInvoiceDialog.sendOnlyToThisEmail,
                       null, null, GenrateInvoiceDialog.PostOnlyDelivered, false);

                    busyIndicator.IsBusy = false;
                    busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("GeneratingPage");
                    if (result.ledgerRes.Err == ErrorCodes.Succes)
                    {
#if !SILVERLIGHT
                        if (GenrateInvoiceDialog.GenerateOIOUBLClicked && !GenrateInvoiceDialog.IsSimulation)
                            DebtorOrders.GenerateOIOXml(api, result);
#endif
                        if (showOrPrint)
                        {
#if SILVERLIGHT
                            object[] ob = new object[2];
                            ob[0] = result;
                            ob[1] = CompanyLayoutType.Invoice;
                            AddDockItem(TabControls.ProformaInvoice, ob, true, Uniconta.ClientTools.Localization.lookup("Invoice"), null, new Point(200, 300));
#else
                            busyIndicator.IsBusy = true;
                            var standardPrint = await ValidateStandardPrint(result, dbOrder, CompanyLayoutType.Invoice, GenrateInvoiceDialog.InvoiceQuickPrint);
                            busyIndicator.IsBusy = false;
                            if (standardPrint?.Report == null)
                            {
                                UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("LayoutDoesNotExist"), Uniconta.ClientTools.Localization.lookup("Invoice")),
                                    Uniconta.ClientTools.Localization.lookup("Error"), MessageBoxButton.OK);
                                return;
                            }
                            if (!GenrateInvoiceDialog.InvoiceQuickPrint)
                            {

                                var iReports = new IPrintReport[1] { standardPrint };
                                var invoiceNum = result.Header._InvoiceNumber;
                                var reportName = string.Format("{0}_{1}", Uniconta.ClientTools.Localization.lookup("Invoice"), invoiceNum);
                                var dockName = string.Format("{0} {1}", Uniconta.ClientTools.Localization.lookup("Preview"), string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Invoice"), invoiceNum));

                                AddDockItem(TabControls.StandardPrintReportPage, new object[] { iReports, reportName }, dockName);
                            }
                            else
                            {
                                var autoPrint = new UnicontaAutoPrint(standardPrint.Report, GenrateInvoiceDialog.NumberOfPages);
                                autoPrint.ExecutePrintCommand(!string.IsNullOrEmpty(standardPrint?.Report?.PrinterName) ? standardPrint.Report.PrinterName : session.User._Printer);
                            }
#endif
                        }
                    }
                    else
                        Utility.ShowJournalError(result.ledgerRes, dgMultiInvGrid);
                }
            };
            GenrateInvoiceDialog.Show();
        }

#if !SILVERLIGHT
        private async void GenerateOIOXmlForAll(List<string> errorlist, bool sendMail)
        {
            var api = this.api;
            var Comp = api.CompanyEntity;
            InvoiceAPI Invapi = new InvoiceAPI(api);
            var invoicePostedCount = invoicePosted.Count;
            Microsoft.Win32.SaveFileDialog saveDialog = null;
            System.Windows.Forms.FolderBrowserDialog folderBrowserDialog = null;
            var listOfXmlPath = new List<string>();
            int countErr = 0;
            bool hasUserFolder = false;
            var applFilePath = string.Empty;

            foreach (var item in invoicePosted)
            {
                var invClient = (DebtorInvoiceClient)item.Key.Header;
                var invoiceLines = (InvTransClient[])item.Key.Lines;

                var InvCache = api.GetCache(typeof(Uniconta.DataModel.InvItem)) ?? await api.LoadCache(typeof(Uniconta.DataModel.InvItem));
                var VatCache = api.GetCache(typeof(Uniconta.DataModel.GLVat)) ?? await api.LoadCache(typeof(Uniconta.DataModel.GLVat));
                var Debcache = api.GetCache(typeof(Debtor)) ?? await api.LoadCache(typeof(Debtor));
                var debtor = (Debtor)Debcache.Get(invClient._DCAccount);

                if (!debtor._InvoiceInXML || invClient.SendTime != DateTime.MinValue)
                    continue;

                Contact contactPerson = null;
                if (invClient._ContactRef != 0)
                {
                    var queryContact = await api.Query<Contact>(invClient);
                    foreach (var contact in queryContact)
                        if (contact.RowId == invClient._ContactRef)
                        {
                            contactPerson = contact;
                            break;
                        }
                }

                InvItemText[] invItemText = null;
                if (debtor._ItemNameGroup != null)
                    invItemText = await api.Query<InvItemText>(new UnicontaBaseEntity[] { debtor }, null);

                DebtorOrders.SetDeliveryAdress(invClient, debtor, api);

                Debtor deliveryAccount;
                if (invClient._DeliveryAccount != null)
                    deliveryAccount = (Debtor)Debcache.Get(invClient._DeliveryAccount);
                else
                    deliveryAccount = null;

                FromXSDFile.OIOUBL.ExportImport.CreationResult cResult;

                if (Comp._CountryId == CountryCode.Norway || Comp._CountryId == CountryCode.Netherlands)
                    cResult = ubl_norway_uniconta.EHF.GenerateEHFXML(Comp, debtor, deliveryAccount, invClient, invoiceLines, InvCache, VatCache, invItemText, contactPerson);
                else
                {
                    TableAddOnData[] attachments = await FromXSDFile.OIOUBL.ExportImport.Attachments.CollectInvoiceAttachments(invClient, api);
                    cResult = Uniconta.API.DebtorCreditor.OIOUBL.GenerateOioXML(Comp, debtor, deliveryAccount, invClient, invoiceLines, InvCache, VatCache, invItemText, contactPerson, attachments);
                }

                bool createXmlFile = true;

                var errorInfo = "";
                if (cResult.HasErrors)
                {
                    countErr++;
                    createXmlFile = false;
                    foreach (FromXSDFile.OIOUBL.ExportImport.PrecheckError error in cResult.PrecheckErrors)
                        errorInfo += error.ToString() + "\n";

                    errorlist.Add(string.Format("{0} {1}: \n{2}", Uniconta.ClientTools.Localization.lookup("Invoice"), invClient.InvoiceNumber, errorInfo));
                }

                if (cResult.Document != null && createXmlFile)
                {
                    string invoice = Uniconta.ClientTools.Localization.lookup("Invoice");
                    string filename = null;

                    if (session.User._AppDocPath != string.Empty && Directory.Exists(session.User._AppDocPath))
                    {
                        try
                        {
                            applFilePath = string.Format("{0}\\OIOUBL", session.User._AppDocPath);
                            Directory.CreateDirectory(applFilePath);

                            filename = string.Format("{0}\\{1}_{2}.xml", applFilePath, invoice, invClient.InvoiceNumber);
                            hasUserFolder = true;
                        }
                        catch
                        {

                        }
                    }
                    else
                    {
                        if (saveDialog == null && invoicePostedCount == 1)
                        {
                            saveDialog = UtilDisplay.LoadSaveFileDialog;
                            saveDialog.FileName = string.Format("{0}_{1}", invoice, invClient.InvoiceNumber);
                            saveDialog.Filter = "XML-File | *.xml";
                            bool? dialogResult = saveDialog.ShowDialog();
                            if (dialogResult != true)
                                break;
                        }
                        else if (folderBrowserDialog == null)
                        {
                            folderBrowserDialog = UtilDisplay.LoadFolderBrowserDialog;
                            var dialogResult = folderBrowserDialog.ShowDialog();
                            if (dialogResult != System.Windows.Forms.DialogResult.OK)
                                break;
                        }

                        if (invoicePostedCount > 1)
                        {
                            filename = folderBrowserDialog.SelectedPath;
                            filename = string.Format("{0}\\{1}_{2}.xml", filename, invoice, invClient.InvoiceNumber);
                        }
                        else
                        {
                            filename = saveDialog.FileName;
                        }
                    }

                    listOfXmlPath.Add(filename);
                    cResult.Document.Save(filename);

                    if (sendMail)
                        Invapi.MarkSendInvoice(invClient);

                    invClient.SendTime = BasePage.GetSystemDefaultDate();
                }
            }

            if (countErr != 0)
                UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("InvoiceFileCreationFailMsg"), countErr), Uniconta.ClientTools.Localization.lookup("Error"), MessageBoxButton.OK);
            else if (hasUserFolder)
                UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("SaveFileMsgOBJ"), listOfXmlPath.Count, listOfXmlPath.Count == 1 ?
                    Uniconta.ClientTools.Localization.lookup("Invoice") : Uniconta.ClientTools.Localization.lookup("Invoices"), applFilePath), Uniconta.ClientTools.Localization.lookup("Message"), MessageBoxButton.OK);
        }

        async private Task<IPrintReport> ValidateStandardPrint(InvoicePostingResult result, DebtorOrderClient dbOrder, CompanyLayoutType layoutType, bool isPrint)
        {
            busyIndicator.BusyContent = string.Format("{0} {1}: {2}", !isPrint ? Uniconta.ClientTools.Localization.lookup("LoadingMsg") : Uniconta.ClientTools.Localization.lookup("Printing"),
                Uniconta.ClientTools.Localization.lookup("OrderNumber"), dbOrder?.OrderNumber);

            try
            {
                var debtorInvoicePrint = new DebtorInvoicePrintReport(result, api, layoutType, dbOrder);
                var isInitializedSuccess = await debtorInvoicePrint.InstantiateFields();

                if (isInitializedSuccess)
                {
                    StandardPrintReport standardPrint;
                    DebtorInvoiceReportClient standardDebtorInvoice = null;
                    if (layoutType == CompanyLayoutType.OrderConfirmation || layoutType == CompanyLayoutType.Packnote)
                    {
                        var standardDocVersion = layoutType == CompanyLayoutType.OrderConfirmation ? Uniconta.ClientTools.Controls.Reporting.StandardReports.OrderConfirmation :
                            Uniconta.ClientTools.Controls.Reporting.StandardReports.PackNote;
                        standardDebtorInvoice = new DebtorQCPReportClient(debtorInvoicePrint.Company, debtorInvoicePrint.Debtor, debtorInvoicePrint.DebtorInvoice, debtorInvoicePrint.InvTransInvoiceLines, debtorInvoicePrint.DebtorOrder,
                            debtorInvoicePrint.CompanyLogo, debtorInvoicePrint.ReportName, (byte)standardDocVersion, messageClient: debtorInvoicePrint.MessageClient);
                        standardPrint = new StandardPrintReport(api, new [] { standardDebtorInvoice }, (byte)standardDocVersion);
                    }
                    else if (layoutType == CompanyLayoutType.PickingList)
                    {
                        standardDebtorInvoice = new DebtorSalesPickingListReportClient(debtorInvoicePrint.Company, debtorInvoicePrint.Debtor, debtorInvoicePrint.DebtorInvoice, debtorInvoicePrint.InvTransInvoiceLines, debtorInvoicePrint.DebtorOrder,
                            debtorInvoicePrint.CompanyLogo, debtorInvoicePrint.ReportName, messageClient: debtorInvoicePrint.MessageClient);
                        standardPrint = new StandardPrintReport(api, new [] { standardDebtorInvoice }, (byte)Uniconta.ClientTools.Controls.Reporting.StandardReports.SalesPickingList);
                    }
                    else // layout type for Invoice
                    {
                        standardDebtorInvoice = new DebtorInvoiceReportClient(debtorInvoicePrint.Company, debtorInvoicePrint.Debtor, debtorInvoicePrint.DebtorInvoice, debtorInvoicePrint.InvTransInvoiceLines, debtorInvoicePrint.DebtorOrder,
                        debtorInvoicePrint.CompanyLogo, debtorInvoicePrint.ReportName, isCreditNote: debtorInvoicePrint.IsCreditNote, messageClient: debtorInvoicePrint.MessageClient);
                        standardPrint = new StandardPrintReport(api, new [] { standardDebtorInvoice }, (byte)Uniconta.ClientTools.Controls.Reporting.StandardReports.Invoice);
                    }

                    await standardPrint.InitializePrint();

                    if (standardPrint.Report != null)
                        return standardPrint;

                    var layoutPrint = new LayoutPrintReport(api, result, layoutType);
                    await layoutPrint.InitializePrint();
                    return layoutPrint;
                }
            }
            catch (Exception ex)
            {
                busyIndicator.IsBusy = false;
                api.ReportException(ex, string.Format("MultiInvoicePage.ValidateStandardPrint(), CompanyId={0}", api.CompanyId));
            }

            return null;
        }
#endif

        void setDim()
        {
            UnicontaClient.Utilities.Utility.SetDimensionsGrid(api, cldim1, cldim2, cldim3, cldim4, cldim5);
        }


        protected override async void LoadCacheInBackGround()
        {
            var api = this.api;
            if (this.debtors == null)
                this.debtors = await api.LoadCache(typeof(Uniconta.DataModel.Debtor)).ConfigureAwait(false);
            if (this.items == null)
                this.items = await api.LoadCache(typeof(Uniconta.DataModel.InvItem)).ConfigureAwait(false);
            LoadType(new Type[] { typeof(Uniconta.DataModel.GLVat), typeof(Uniconta.DataModel.WorkInstallation), typeof(Uniconta.DataModel.InvItem) });
        }

        public override bool CheckIfBindWithUserfield(out bool isReadOnly, out bool useBinding)
        {
            isReadOnly = false;
            useBinding = true;
            return true;
        }
    }
}
