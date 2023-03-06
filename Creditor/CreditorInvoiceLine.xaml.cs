using UnicontaClient.Models;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using UnicontaClient.Controls.Dialogs;
using Uniconta.ClientTools.Util;
using System.Collections;
using UnicontaClient.Utilities;
using DevExpress.Xpf.Grid;
using DevExpress.Data;
using Uniconta.API.Service;
using Uniconta.Client.Pages;
using Uniconta.Common.Utility;
using System.Windows;
using Localization = Uniconta.ClientTools.Localization;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class CreditorInvTransClientGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(CreditorInvoiceLines); } }
        public override IComparer GridSorting { get { return new InvTransInvoiceSort(); } }
    }
    public partial class CreditorInvoiceLine : GridBasePage
    {
        private SynchronizeEntity syncEntity;

        bool AddFilterAndSort;
        DateTime filterDate;
        protected override Filter[] DefaultFilters()
        {
            if (AddFilterAndSort)
            {
                Filter dateFilter = new Filter() { name = "Date", value = String.Format("{0:d}..", filterDate) };
                return new Filter[] { dateFilter };
            }
            return base.DefaultFilters();
        }
        public CreditorInvoiceLine(BaseAPI API)
            : base(API, string.Empty)
        {
            InitPage(null);
        }

        public CreditorInvoiceLine(UnicontaBaseEntity master)
            : base(master)
        {
            InitPage(master);
        }
        public CreditorInvoiceLine(SynchronizeEntity syncEntity)
            : base(syncEntity, true)
        {
            this.syncEntity = syncEntity;
            InitPage(syncEntity.Row);
        }
        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            dgCrdInvLines.UpdateMaster(args);
            SetHeader();
            InitQuery();
        }
        void SetHeader()
        {
            string header;
            if (dgCrdInvLines.masterRecord is CreditorInvoiceClient dCInvoice)
            {
                header = string.Format("{0}: {1}", Localization.lookup("InvoiceNumber"), dCInvoice._InvoiceNumber);
            }
            else if (dgCrdInvLines.masterRecord is DCAccount dcAccount)
            {
                header = string.Format("{0}: {1}", Localization.lookup("InvTransactions"), dcAccount._Account);
            }
            else
                return;
            SetHeader(header);
        }

        UnicontaBaseEntity master = null;
        private void InitPage(UnicontaBaseEntity master)
        {
            InitializeComponent();
            this.master = master;
            AddFilterAndSort = (master == null);
            dgCrdInvLines.UpdateMaster(master);
            SetRibbonControl(localMenu, dgCrdInvLines);
            filterDate = BasePage.GetFilterDate(api.CompanyEntity, master != null);
            if (filterDate == DateTime.MinValue)
                AddFilterAndSort = false;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgCrdInvLines.api = api;
            dgCrdInvLines.BusyIndicator = busyIndicator;
            LoadNow(typeof(Uniconta.DataModel.InvItem));
            dgCrdInvLines.CustomSummary += DgCrdInvLines_CustomSummary;
            dgCrdInvLines.ShowTotalSummary();
        }

        double sumCost, sumSales;
        private void DgCrdInvLines_CustomSummary(object sender, DevExpress.Data.CustomSummaryEventArgs e)
        {
            var fieldName = ((GridSummaryItem)e.Item).FieldName;
            switch (e.SummaryProcess)
            {
                case CustomSummaryProcess.Start:
                    sumCost = sumSales = 0d;
                    break;
                case CustomSummaryProcess.Calculate:
                    var row = e.Row as CreditorInvoiceLines;
                    sumSales += row._NetAmount();
                    sumCost += row.CostValue;
                    break;
                case CustomSummaryProcess.Finalize:
                    if (fieldName == "MarginRatio" && sumSales > 0)
                        e.TotalValue = Math.Round((sumSales - sumCost) * 100d / sumSales, 2);
                    break;
            }
        }

        public override bool CheckIfBindWithUserfield(out bool isReadOnly, out bool useBinding)
        {
            isReadOnly = false;
            useBinding = true;
            return true;
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            var company = api.CompanyEntity;
            if (!company.Location || !company.Warehouse)
                Location.Visible = Location.ShowInColumnChooser = false;
            else
                Location.ShowInColumnChooser = true;
            if (!company.Warehouse)
                Warehouse.Visible = Warehouse.ShowInColumnChooser = false;
            else
                Warehouse.ShowInColumnChooser = true;
            if (!company.Project)
                Project.Visible = Project.ShowInColumnChooser = WorkSpace.ShowInColumnChooser = WorkSpace.Visible =
                    PrCategory.Visible = PrCategory.ShowInColumnChooser = false;
            else
                Project.ShowInColumnChooser = true;
            if (!company.ProjectTask)
                Task.Visible = Task.ShowInColumnChooser = false;
            else
                Task.ShowInColumnChooser = true;
            if (this.master != null)
                colAccount.Visible = AccountName.Visible = false;

            Utility.SetupVariants(api, null, colVariant1, colVariant2, colVariant3, colVariant4, colVariant5, Variant1Name, Variant2Name, Variant3Name, Variant4Name, Variant5Name);
            Utility.SetDimensionsGrid(api, cldim1, cldim2, cldim3, cldim4, cldim5);
        }


        void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgCrdInvLines.SelectedItem as InvTransClient;
            switch (ActionType)
            {
                case "ChangeVariant":
                    if (selectedItem == null)
                        return;
                    var cwChangeVaraints = new CWModifyVariants(api, selectedItem);
                    cwChangeVaraints.Closing += delegate
                    {
                        if (cwChangeVaraints.DialogResult == true)
                            gridRibbon_BaseActions("RefreshGrid");
                    };
                    cwChangeVaraints.Show();
                    break;
                case "ChangeStorage":
                    if (selectedItem == null)
                        return;
                    var cwchangeStorage = new CWModiyStorage(api);
                    cwchangeStorage.Closing += async delegate
                    {
                        if (cwchangeStorage.DialogResult == true)
                        {
                            var newWarehouse = cwchangeStorage.Warehouse;
                            var newLocation = cwchangeStorage.Location;
                            var tranApi = new Uniconta.API.Inventory.TransactionsAPI(api);
                            ErrorCodes result;
                            if (cwchangeStorage.AllLines)
                            {
                                var visibleItems = dgCrdInvLines.VisibleItems.Cast<InvTransClient>();
                                result = await tranApi.ChangeStorage(visibleItems, newWarehouse, newLocation);
                            }
                            else
                                result = await tranApi.ChangeStorage(selectedItem, newWarehouse, newLocation);

                            if (result == ErrorCodes.Succes)
                                gridRibbon_BaseActions("RefreshGrid");
                            else
                                UtilDisplay.ShowErrorCode(result);
                        }
                    };
                    cwchangeStorage.Show();
                    break;
                case "SeriesBatch":
                    if (selectedItem == null)
                        return;
                    AddDockItem(TabControls.InvSeriesBatch, selectedItem, string.Format("{0}: {1}", Localization.lookup("SerialBatchNumbers"), selectedItem._Item));
                    break;
                case "PostedBy":
                    if (selectedItem != null)
                        JournalPosted(selectedItem);
                    break;
                case "Invoice":
                    if (selectedItem == null) return;
                    var credHeader = Util.ConcatParenthesis(Localization.lookup("CreditorInvoice"), selectedItem._Item);
                    AddDockItem(TabControls.CreditorInvoice, selectedItem, credHeader);
                    break;
                case "ViewDownloadRow":
                    if (selectedItem != null)
                        DebtorTransactions.ShowVoucher(dgCrdInvLines.syncEntity, api, busyIndicator);
                    break;
                case "VoucherTransactions":
                    if (selectedItem == null)
                        return;
                    string arg;
                    if (selectedItem._JournalPostedId != 0)
                        arg = string.Format("{0}={1}", Localization.lookup("JournalPostedId"), selectedItem._JournalPostedId);
                    else if (selectedItem._InvoiceNumber != 0)
                        arg = string.Format("{0}={1}", Localization.lookup("Invoice"), selectedItem._InvoiceNumber);
                    else if (selectedItem._InvJournalPostedId != 0)
                        arg = string.Format("{0} ({1})={2}", Localization.lookup("JournalPostedId"), Localization.lookup("Inventory"), selectedItem._InvJournalPostedId);
                    else
                        arg = string.Format("{0}={1}", Localization.lookup("Account"), selectedItem.AccountName);
                    string vheader = Util.ConcatParenthesis(Localization.lookup("VoucherTransactions"), arg);
                    AddDockItem(TabControls.AccountsTransaction, dgCrdInvLines.syncEntity, vheader);
                    break;
                case "ChangeCostValue":
                    DebtorInvoiceLinesPage.ChangeCostValue(selectedItem, dgCrdInvLines);
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        async private void JournalPosted(InvTransClient selectedItem)
        {
            var result = await api.Query(new GLDailyJournalPostedClient(), new UnicontaBaseEntity[] { selectedItem }, null);
            if (result != null && result.Length == 1)
            {
                CWGLPostedClientFormView cwPostedClient = new CWGLPostedClientFormView(result[0]);
                cwPostedClient.Show();
            }
        }

        public override string NameOfControl { get { return TabControls.CreditorInvoiceLine.ToString(); } }

        protected override LookUpTable HandleLookupOnLocalPage(LookUpTable lookup, CorasauDataGrid dg)
        {
            var inv = dg.SelectedItem as InvTransClient;
            if (inv == null)
                return lookup;
            var currentCol = dg.CurrentColumn;
            if (currentCol != null)
            {
                if (currentCol.FieldName == "Item")
                    lookup.TableType = typeof(Uniconta.DataModel.InvItem);
                else if (currentCol.FieldName == "DCAccount")
                    lookup.TableType = typeof(Uniconta.DataModel.Creditor);
            }
            return lookup;
        }
    }
}
