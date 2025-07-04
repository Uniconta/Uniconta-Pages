using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Navigation;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.DataModel;
using UnicontaClient.Models;
using Uniconta.Common;
using System.Threading.Tasks;
using Uniconta.ClientTools.Controls;
using Uniconta.DataModel;
using System.Collections;
using Uniconta.ClientTools;
using UnicontaClient.Controls.Dialogs;
using DevExpress.Xpf.Grid;
using Uniconta.ClientTools.Util;
using UnicontaClient.Utilities;
using DevExpress.Data;
using Uniconta.API.Service;
using Uniconta.Client.Pages;
using Uniconta.Common.Utility;
using System.Windows;
using Localization = Uniconta.ClientTools.Localization;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class InvTransInvoiceSort : IComparer
    {
        public int Compare(object x, object y)
        {
            return ((InvTrans)x)._LineNumber - ((InvTrans)y)._LineNumber;
        }
    }

    public class InvTransClientGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(DebtorInvoiceLines); } }
        public override IComparer GridSorting { get { return new InvTransInvoiceSort(); } }

    }

    public partial class DebtorInvoiceLinesPage : GridBasePage
    {
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

        public DebtorInvoiceLinesPage(BaseAPI API) : base(API, string.Empty)
        {
            InitPage(null);
        }
        public DebtorInvoiceLinesPage(UnicontaBaseEntity master)
            : base(master)
        {
            InitPage(master);
        }
        public DebtorInvoiceLinesPage(SynchronizeEntity syncEntity)
            : base(syncEntity, true)
        {
            this.syncEntity = syncEntity;
            InitPage(syncEntity.Row);
        }
        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            dgInvLines.UpdateMaster(args);
            SetHeader();
            InitQuery();
        }
        void SetHeader()
        {
            string header;
            if (dgInvLines.masterRecord is DCInvoice dCInvoice)
            {
                header = dCInvoice.__DCType() != 6 ? string.Format("{0}: {1}", Localization.lookup("InvoiceNumber"), dCInvoice._InvoiceNumber) : string.Format("{0}: {1}", Localization.lookup("ProjectOrder"), dCInvoice._OrderNumber);
            }
            else if (dgInvLines.masterRecord is DCAccount dcAccount)
            {
                header = string.Format("{0}: {1}", Localization.lookup("InvTransactions"), dcAccount._Account);
            }
            else
                return;
            SetHeader(header);
        }
        UnicontaBaseEntity master;
        private void InitPage(UnicontaBaseEntity master)
        {
            InitializeComponent();
            this.master = master;
            dgInvLines.UpdateMaster(master);
            AddFilterAndSort = (master == null);
            filterDate = BasePage.GetFilterDate(api.CompanyEntity, master != null);
            if (filterDate == DateTime.MinValue)
                AddFilterAndSort = false;
            SetRibbonControl(localMenu, dgInvLines);
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgInvLines.api = api;
            dgInvLines.BusyIndicator = busyIndicator;
            LoadNow(typeof(Uniconta.DataModel.InvItem));
            dgInvLines.CustomSummary += DgInvLines_CustomSummary;
            dgInvLines.ShowTotalSummary();
        }

        double sumCost, sumSales;
        private void DgInvLines_CustomSummary(object sender, DevExpress.Data.CustomSummaryEventArgs e)
        {
            var fieldName = ((GridSummaryItem)e.Item).FieldName;
            switch (e.SummaryProcess)
            {
                case CustomSummaryProcess.Start:
                    sumCost = sumSales = 0d;
                    break;
                case CustomSummaryProcess.Calculate:
                    var row = e.Row as DebtorInvoiceLines;
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
            isReadOnly = true;
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
                Project.Visible = Project.ShowInColumnChooser = WorkSpace.ShowInColumnChooser = WorkSpace.Visible = PrCategory.Visible = PrCategory.ShowInColumnChooser = false;
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
            if (company.HideCostPrice)
            {
                Margin.Visible = Margin.ShowInColumnChooser = MarginRatio.Visible = MarginRatio.ShowInColumnChooser =
           CostPrice.Visible = CostPrice.ShowInColumnChooser = CostValue.Visible = CostValue.ShowInColumnChooser = false;
            }
            if (!company.UnitConversion)
                UnitGroup.Visible = UnitGroup.ShowInColumnChooser = false;
        }


        void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgInvLines.SelectedItem as InvTransInvoice;
            switch (ActionType)
            {
                case "ChangeVariant":
                    if (selectedItem == null)
                        return;
                    var cwChangeVaraints = new CWModifyVariants(api, selectedItem);
                    cwChangeVaraints.Closing += delegate
                    {
                        if (cwChangeVaraints.DialogResult == true)
                        {
                            gridRibbon_BaseActions("RefreshGrid");
                        }
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
                                var visibleItems = dgInvLines.VisibleItems.Cast<InvTransInvoice>();
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
                case "AddEditNote":
                    if (selectedItem == null) return;
                    CWAddEditNote cwAddEditNote = new CWAddEditNote(api, selectedItem, null);
                    cwAddEditNote.Closed += delegate
                    {
                        if (cwAddEditNote.DialogResult == true)
                        {
                            if (cwAddEditNote.result == ErrorCodes.Succes)
                            {
                                selectedItem._Note = cwAddEditNote.invTransClient._Note;
                                selectedItem.HasNote = !string.IsNullOrEmpty(cwAddEditNote.invTransClient._Note);
                                dgInvLines.UpdateItemSource(2, selectedItem);
                            }
                        }
                    };
                    cwAddEditNote.Show();
                    break;
                case "PostedBy":
                    if (selectedItem != null)
                        JournalPosted(selectedItem);
                    break;
                case "Invoice":
                    if (selectedItem == null) return;
                    var debtHeader = Util.ConcatParenthesis(Localization.lookup("DebtorInvoice"), selectedItem._Item);
                    AddDockItem(TabControls.Invoices, selectedItem, debtHeader);
                    break;
                case "ViewDownloadRow":
                    if (selectedItem != null)
                        DebtorTransactions.ShowVoucher(dgInvLines.syncEntity, api, busyIndicator);
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
                    AddDockItem(TabControls.AccountsTransaction, dgInvLines.syncEntity, vheader);
                    break;
                case "ChangeCostValue":
                    ChangeCostValue(selectedItem, dgInvLines);
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
        public static void ChangeCostValue(InvTransClient selectedItem, CorasauDataGrid dataGrid)
        {
            if (selectedItem == null)
                return;
            if (selectedItem._Qty < 0) // Invoice.
            {
                UnicontaMessageBox.Show(Localization.lookup("OnlyCreditnotes"), Localization.lookup("Information"), MessageBoxButton.OK);
                return;
            }
            var cwchangeCost = new CWChangeCostValue(selectedItem);
            cwchangeCost.Closing += async delegate
            {
                if (cwchangeCost.DialogResult == true)
                {
                    var tranApi = new Uniconta.API.Inventory.TransactionsAPI(dataGrid.api);
                    var result = await tranApi.ChangeCostOnInvtrans(selectedItem, cwchangeCost.CostValue, cwchangeCost.PostingDate);
                    if (result == ErrorCodes.Succes)
                        dataGrid.Refresh();
                    else
                        UtilDisplay.ShowErrorCode(result);
                }
            };
            cwchangeCost.Show();
        }
        async private void JournalPosted(InvTransInvoice selectedItem)
        {
            var result = await api.Query(new GLDailyJournalPostedClient(), new UnicontaBaseEntity[] { selectedItem }, null);
            if (result != null && result.Length == 1)
            {
                CWGLPostedClientFormView cwPostedClient = new CWGLPostedClientFormView(result[0]);
                cwPostedClient.Show();
            }
        }

        public override Task InitQuery()
        {
            if (master != null)
            {
                PropValuePair[] propValuePair = null;
                // we do this to select all lines. also the hidden ones.
                propValuePair = new PropValuePair[] { PropValuePair.GenereteParameter("ShowAllTrans", typeof(int), "1") };
                return dgInvLines.Filter(propValuePair);
            }
            else
                return base.InitQuery();
        }

        protected override void LoadCacheInBackGround()
        {
            var Comp = api.CompanyEntity;
            var lst = new List<Type>(20);
            if (Comp.Warehouse)
                lst.Add(typeof(Uniconta.DataModel.InvWarehouse));
            if (Comp.NumberOfDimensions >= 1)
                lst.Add(typeof(Uniconta.DataModel.GLDimType1));
            if (Comp.NumberOfDimensions >= 2)
                lst.Add(typeof(Uniconta.DataModel.GLDimType2));
            if (Comp.NumberOfDimensions >= 3)
                lst.Add(typeof(Uniconta.DataModel.GLDimType3));
            if (Comp.NumberOfDimensions >= 4)
                lst.Add(typeof(Uniconta.DataModel.GLDimType4));
            if (Comp.NumberOfDimensions >= 5)
                lst.Add(typeof(Uniconta.DataModel.GLDimType5));
            lst.Add(typeof(Uniconta.DataModel.GLVat));
            lst.Add(typeof(Uniconta.DataModel.Employee));
            lst.Add(typeof(Uniconta.DataModel.Debtor));
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
                lst.Add(typeof(Uniconta.DataModel.InvStandardVariant));
            }
            lst.Add(typeof(Uniconta.DataModel.InvItem));
            LoadType(lst);
        }

        public override string NameOfControl { get { return TabControls.DebtorInvoiceLines.ToString(); } }

        protected override LookUpTable HandleLookupOnLocalPage(LookUpTable lookup, CorasauDataGrid dg)
        {
            var inv = dg.SelectedItem as InvTransInvoice;
            if (inv == null)
                return lookup;
            var currentCol = dg.CurrentColumn;
            if (currentCol != null)
            {
                if (currentCol.FieldName == "Item")
                    lookup.TableType = typeof(Uniconta.DataModel.InvItem);
                else if (currentCol.FieldName == "DCAccount")
                    lookup.TableType = typeof(Uniconta.DataModel.Debtor);
            }
            return lookup;
        }
    }
}
