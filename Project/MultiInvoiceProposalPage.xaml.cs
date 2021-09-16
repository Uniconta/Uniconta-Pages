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

    public class MultiInvoicePropGrid : CorasauDataGridClient
    {
        public override bool SingleBufferUpdate { get { return false; } }
        public override Type TableType { get { return typeof(ProjectInvoiceProposalClient); } }
        public override IComparer GridSorting { get { return new DCOrderSort(); } }

        public override bool Readonly { get { return false; } }
    }

    public partial class MultiInvoiceProposalPage : GridBasePage
    {
        public override string NameOfControl
        {
            get { return TabControls.MultiInvoiceProposalPage.ToString(); }
        }

        public override bool IsDataChaged { get { return false; } }

        SQLCache items, debtors;

        public MultiInvoiceProposalPage(BaseAPI API) : base(API, string.Empty)
        {
            InitializeComponent();
            localMenu.dataGrid = dgMultiInvGrid;
            SetRibbonControl(localMenu, dgMultiInvGrid);
            dgMultiInvGrid.api = api;
            dgMultiInvGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
            dgMultiInvGrid.ShowTotalSummary();
            var Comp = api.CompanyEntity;
            this.debtors = Comp.GetCache(typeof(Debtor));
            this.items = Comp.GetCache(typeof(InvItem));
        }

        protected override LookUpTable HandleLookupOnLocalPage(LookUpTable lookup, CorasauDataGrid dg)
        {
            var order = dg.SelectedItem as ProjectInvoiceProposalClient;
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
            var selectedItem = dgMultiInvGrid.SelectedItem as ProjectInvoiceProposalClient;
            switch (ActionType)
            {
                case "EditRow":
                    if (selectedItem == null)
                        return;
                    string salesHeader = string.Format("{0}:{1}", Uniconta.ClientTools.Localization.lookup("InvoiceProposal"), selectedItem._OrderNumber);
                    if (dgMultiInvGrid.masterRecords != null)
                    {
                        object[] arr = new object[2] { selectedItem, dgMultiInvGrid.masterRecord };
                        AddDockItem(TabControls.ProjInvProposalPage2, arr, salesHeader);
                    }
                    else
                        AddDockItem(TabControls.ProjInvProposalPage2, selectedItem, salesHeader);
                    break;
                case "DeleteRow":
                    dgMultiInvGrid.DeleteRow();
                    break;
                case "OrderLine":
                    if (selectedItem == null)
                        return;
                    var olheader = string.Format("{0}:{1},{2}", Uniconta.ClientTools.Localization.lookup("InvoiceProposalLine"), selectedItem._OrderNumber, selectedItem._DCAccount);
                    AddDockItem(TabControls.ProjInvoiceProposalLine, dgMultiInvGrid.syncEntity, olheader);
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
                case "RegenerateOrderFromProject":
                    if (selectedItem != null)
                        AddDockItem(TabControls.RegenerateOrderFromProjectPage, dgMultiInvGrid.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("RegenerateOrder"), selectedItem._OrderNumber));
                    break;
                case "ApproveOrder":
                    if (selectedItem != null && api.CompanyEntity.ApproveSalesOrders)
                        Utility.ApproveOrder(api, selectedItem);
                    break;
                case "ProjectTransaction":
                    if (selectedItem?._Project != null)
                        AddDockItem(TabControls.ProjectInvoiceProjectLinePage, dgMultiInvGrid.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("ProjectAdjustments"), selectedItem._OrderNumber));
                    break;

                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        private void GenerateInvoice()
        {
            UnicontaClient.Pages.CWGenerateInvoice GenrateInvoiceDialog = new UnicontaClient.Pages.CWGenerateInvoice(true, string.Empty, false, true, true, isOrderOrQuickInv: true, isQuickPrintVisible: true, isPageCountVisible: false, isDebtorOrder: true);
            GenrateInvoiceDialog.DialogTableId = 2000000011;
            GenrateInvoiceDialog.HideOutlookOption(true);
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
                    var dbVisibleOrders = dgMultiInvGrid.GetVisibleRows() as IEnumerable<ProjectInvoiceProposalClient>;

                    var invoicePostingPrintGenerator = new InvoicePostingPrintGenerator(api, this);
                    invoicePostingPrintGenerator.SetUpInvoicePosting(dbVisibleOrders, InvoiceDate, GenrateInvoiceDialog.IsSimulation, CompanyLayoutType.Invoice, GenrateInvoiceDialog.ShowInvoice, GenrateInvoiceDialog.PostOnlyDelivered, GenrateInvoiceDialog.InvoiceQuickPrint,
                        GenrateInvoiceDialog.SendByEmail, GenrateInvoiceDialog.sendOnlyToThisEmail, GenrateInvoiceDialog.Emails, GenrateInvoiceDialog.GenerateOIOUBLClicked);

                    await invoicePostingPrintGenerator.Execute();
                    busyIndicator.IsBusy = false;
                }
            };
            GenrateInvoiceDialog.Show();
        }

        static bool printPreview = true;
        private void GenerateRecordInvoice(ProjectInvoiceProposalClient dbOrder)
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
            GenrateInvoiceDialog.DialogTableId = 2000000013;
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
    }
}
