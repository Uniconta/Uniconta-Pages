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
#if !SILVERLIGHT
using UBL.Iceland;
#endif

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
                    var olheader = string.Format("{0}:{1},{2}", Uniconta.ClientTools.Localization.lookup("OrdersLine"), selectedItem._OrderNumber, selectedItem._DCAccount);
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
                case "RecalculateOrderPrices":
                    RecalculateOrderPrices();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

#if SILVERLIGHT
        List<InvoicePostingResult> packListPosted;
#endif
        private void PickList()
        {
#if SILVERLIGHT
            packListPosted = new List<InvoicePostingResult>();
#endif
            var cwPickingList = new CWGeneratePickingList(true);
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

                    var dbVisibleOrders = dgMultiInvGrid.GetVisibleRows() as IEnumerable<DebtorOrderClient>;
#if SILVERLIGHT
                    InvoiceAPI Invapi = new InvoiceAPI(api);
                    int cnt = 0;
                    var errorList = new List<string>();
                    foreach (var dbOrder in dbVisibleOrders)
                    {
                        var result = await Invapi.PostInvoice(dbOrder, null, selectedDate, null, false, new DebtorInvoiceClient(), new DebtorInvoiceLines(), false, cwPickingList.ShowDocument || cwPickingList.PrintDocument,
                            CompanyLayoutType.PickingList);

                        if (result.Err == ErrorCodes.Succes)
                        {
                            packListPosted.Add(result);
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
#else
                    var invoicePostringPrintGenerator = new InvoicePostingPrintGenerator(api, this);
                    invoicePostringPrintGenerator.SetUpInvoicePosting(dbVisibleOrders, selectedDate, false, CompanyLayoutType.PickingList, cwPickingList.ShowDocument, false, cwPickingList.PrintDocument,
                        cwPickingList.SendByEmail, cwPickingList.sendOnlyToThisEmail, cwPickingList.EmailList, false);

                    await invoicePostringPrintGenerator.Execute();
                    busyIndicator.IsBusy = false;
#endif
                }
            };
            cwPickingList.Show();
        }

#if SILVERLIGHT
        List<InvoicePostingResult> invoicePosted;
#endif
        private void GenerateInvoice()
        {
#if SILVERLIGHT
            invoicePosted = new List<InvoicePostingResult>();
#endif
            UnicontaClient.Pages.CWGenerateInvoice GenrateInvoiceDialog = new UnicontaClient.Pages.CWGenerateInvoice(true, string.Empty, false, true, true, isOrderOrQuickInv: true, isQuickPrintVisible: true, isPageCountVisible: false, isDebtorOrder: true);
#if !SILVERLIGHT
            GenrateInvoiceDialog.DialogTableId = 2000000011;
            GenrateInvoiceDialog.HideOutlookOption(true);
#endif
            GenrateInvoiceDialog.SetInvPrintPreview(printPreview);
            GenrateInvoiceDialog.SetOIOUBLLabelText(api.CompanyEntity._OIOUBLSendOnServer);
            GenrateInvoiceDialog.Closed += async delegate
            {
                if (GenrateInvoiceDialog.DialogResult == true)
                {
                    printPreview = GenrateInvoiceDialog.ShowInvoice;
                    busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("SendingWait");
                    busyIndicator.IsBusy = true;
                    var InvoiceDate = GenrateInvoiceDialog.GenrateDate;
                    var dbVisibleOrders = dgMultiInvGrid.GetVisibleRows() as IEnumerable<DebtorOrderClient>;
#if SILVERLIGHT
                    InvoiceAPI Invapi = new InvoiceAPI(api);
                    int cnt = 0;
                    List<string> errorList = new List<string>();
                    foreach (var dbOrder in dbVisibleOrders)
                    {
                        if (dbOrder._SubscriptionEnded != DateTime.MinValue && dbOrder._SubscriptionEnded < InvoiceDate)
                            continue;

                        var result = await Invapi.PostInvoice(dbOrder, null, InvoiceDate,
                            null, GenrateInvoiceDialog.IsSimulation, new DebtorInvoiceClient(),
                            new DebtorInvoiceLines(), GenrateInvoiceDialog.SendByEmail, (GenrateInvoiceDialog.ShowInvoice || GenrateInvoiceDialog.InvoiceQuickPrint || GenrateInvoiceDialog.GenerateOIOUBLClicked), 0,
                            GenrateInvoiceDialog.Emails, GenrateInvoiceDialog.sendOnlyToThisEmail, null, null, GenrateInvoiceDialog.PostOnlyDelivered, false);
                        if (result != null && result.Err == 0)
                        {
                            invoicePosted.Add(result);
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
#else
                    var invoicePostingPrintGenerator = new InvoicePostingPrintGenerator(api, this);
                    invoicePostingPrintGenerator.SetUpInvoicePosting(dbVisibleOrders, InvoiceDate, GenrateInvoiceDialog.IsSimulation, CompanyLayoutType.Invoice, GenrateInvoiceDialog.ShowInvoice, GenrateInvoiceDialog.PostOnlyDelivered, GenrateInvoiceDialog.InvoiceQuickPrint,
                        GenrateInvoiceDialog.SendByEmail, GenrateInvoiceDialog.sendOnlyToThisEmail, GenrateInvoiceDialog.Emails, GenrateInvoiceDialog.GenerateOIOUBLClicked);

                    await invoicePostingPrintGenerator.Execute();
                    busyIndicator.IsBusy = false;
#endif
                }
            };
            GenrateInvoiceDialog.Show();
        }



#if SILVERLIGHT
        List<InvoicePostingResult> confirmOrder;
#endif
        static bool printPreview = true;
        private void OrderConfirmation(CompanyLayoutType docType)
        {
#if SILVERLIGHT
            confirmOrder = new List<InvoicePostingResult>();
#endif
            UnicontaClient.Pages.CWGenerateInvoice GenrateInvoiceDialog = new UnicontaClient.Pages.CWGenerateInvoice(false, docType.ToString(), isShowInvoiceVisible: true, askForEmail: true, showInputforInvNumber: false,
                showInvoice: true, isShowUpdateInv: true, isQuickPrintVisible: true, isDebtorOrder: true, isPageCountVisible: false);
#if !SILVERLIGHT
            GenrateInvoiceDialog.DialogTableId = 2000000012;
            GenrateInvoiceDialog.HideOutlookOption(true);
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
                    var dgOrderVisible = dgMultiInvGrid.GetVisibleRows() as IEnumerable<DebtorOrderClient>;
#if SILVERLIGHT
                    InvoiceAPI Invapi = new InvoiceAPI(api);
                    int cnt = 0;
                    List<string> errorList = new List<string>();
                    foreach (var dbOrder in dgOrderVisible)
                    {
                        var result = await Invapi.PostInvoice(dbOrder, null, GenrateInvoiceDialog.GenrateDate, null,
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
                            confirmOrder.Add(result);
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
#else
                    var invoicePostingPrintGenerator = new InvoicePostingPrintGenerator(api, this);
                    invoicePostingPrintGenerator.SetUpInvoicePosting(dgOrderVisible, InvoiceDate, !updateStatus, docType, GenrateInvoiceDialog.ShowInvoice, GenrateInvoiceDialog.PostOnlyDelivered,
                        GenrateInvoiceDialog.InvoiceQuickPrint, GenrateInvoiceDialog.SendByEmail, GenrateInvoiceDialog.sendOnlyToThisEmail, GenrateInvoiceDialog.Emails, false);
                    await invoicePostingPrintGenerator.Execute();
                    busyIndicator.IsBusy = false;
#endif
                }
            };
            GenrateInvoiceDialog.Show();
        }

#if SILVERLIGHT

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
            if (UnicontaMessageBox.Show(updatedMsg, Uniconta.ClientTools.Localization.lookup("Message"), MessageBoxButton.OKCancel) == MessageBoxResult.OK)
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

        void ShowMultipleOrderToPrint(CompanyLayoutType docType, bool isQuickPrint)
        {
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(1000);
            timer.Tick +=  delegate
            {
                timer.Stop();
                int top = 200;
                int left = 300;
                int invoiceCount = 1;
                int itemcount = confirmOrder.Count();
                busyIndicator.IsBusy = true;
                foreach (var item in confirmOrder)
                {
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
            };
            timer.Start();
        }

        void ShowMultiplePackListPreview(bool isQuickPrint)
        {
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(1000);
            timer.Tick +=  delegate
            {
                timer.Stop();
                int top = 200;
                int left = 300;
                int packListCount = 1;
                int itemCount = packListPosted.Count;

                busyIndicator.IsBusy = true;
                foreach (var item in packListPosted)
                {
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
            };
            timer.Start();
        }

        void ShowMultipleInvoicePreview(bool isQuickPrint)
        {
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(1000);
            timer.Tick +=  delegate
            {
                timer.Stop();
                int top = 200;
                int left = 300;
                int invoiceCount = 1;
                int itemcount = invoicePosted.Count();
                busyIndicator.IsBusy = true;
                foreach (var item in invoicePosted)
                {
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
            };
            timer.Start();
        }
#endif

        private void GenerateRecordInvoice(DebtorOrderClient dbOrder)
        {
            InvoiceAPI Invapi = new InvoiceAPI(api);
            var debtor = dbOrder.Debtor;
            bool showSendByMail = true;
            if (debtor != null)
                showSendByMail = (!string.IsNullOrEmpty(debtor.InvoiceEmail) || debtor.EmailDocuments);
            else
                api.LoadCache(typeof(Debtor), true);

            string debtorName = debtor?.Name ?? dbOrder._DCAccount;
            bool invoiceInXML = debtor?._InvoiceInXML ?? false;
            UnicontaClient.Pages.CWGenerateInvoice GenrateInvoiceDialog = new UnicontaClient.Pages.CWGenerateInvoice(true, string.Empty, false, true, true, showNoEmailMsg: !showSendByMail, debtorName: debtorName, isDebtorOrder: true, isOrderOrQuickInv: true, InvoiceInXML: invoiceInXML);
#if !SILVERLIGHT
            GenrateInvoiceDialog.DialogTableId = 2000000013;
#endif
            GenrateInvoiceDialog.SetOIOUBLLabelText(api.CompanyEntity._OIOUBLSendOnServer);
            GenrateInvoiceDialog.Closed += async delegate
            {
                if (GenrateInvoiceDialog.DialogResult == true)
                {
                    busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("SendingWait");
                    busyIndicator.IsBusy = true;
                    var isSimulated = GenrateInvoiceDialog.IsSimulation;
                    var invoicePostingResult = new InvoicePostingPrintGenerator(api, this);
                    invoicePostingResult.SetUpInvoicePosting(dbOrder, null, CompanyLayoutType.Invoice, GenrateInvoiceDialog.GenrateDate, null, isSimulated, GenrateInvoiceDialog.ShowInvoice, GenrateInvoiceDialog.PostOnlyDelivered,
                        GenrateInvoiceDialog.InvoiceQuickPrint, GenrateInvoiceDialog.NumberOfPages, GenrateInvoiceDialog.SendByEmail, !isSimulated && GenrateInvoiceDialog.SendByOutlook, GenrateInvoiceDialog.sendOnlyToThisEmail,
                        GenrateInvoiceDialog.Emails, GenrateInvoiceDialog.GenerateOIOUBLClicked, null, false);
                    busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("GeneratingPage");
                    var result = await invoicePostingResult.Execute();
                    busyIndicator.IsBusy = true;
                    busyIndicator.IsBusy = false;

                    if (!result)
                        Utility.ShowJournalError(invoicePostingResult.PostingResult.ledgerRes, dgMultiInvGrid);
                }
            };
            GenrateInvoiceDialog.Show();
        }

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

        private void RecalculateOrderPrices()
        {
            var orderLst = dgMultiInvGrid.GetVisibleRows() as IEnumerable<DebtorOrderClient>;
            if (orderLst == null || orderLst.Count() == 0)
                return;
            CWConfirmationBox dialog = new CWConfirmationBox(Uniconta.ClientTools.Localization.lookup("AreYouSureToContinue"), Uniconta.ClientTools.Localization.lookup("Confirmation"), false);
            dialog.Closing += async delegate
            {
                if (dialog.ConfirmationResult == CWConfirmationBox.ConfirmationResultEnum.Yes)
                {
                    busyIndicator.IsBusy = true;
                    OrderAPI orderApi = new OrderAPI(this.api);
                    var err = await orderApi.RecalcOrderPrices(orderLst);
                    busyIndicator.IsBusy = false;
                    UtilDisplay.ShowErrorCode(err);
                }
            };
            dialog.Show();
        }
    }
}
