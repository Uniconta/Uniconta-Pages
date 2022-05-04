using UnicontaClient.Controls.Dialogs;
using UnicontaClient.Models;
using UnicontaClient.Pages;
using UnicontaClient.Utilities;
using DevExpress.Xpf.Grid;
using System;
using System.Collections;
using Uniconta.API.Service;
using Uniconta.Client.Pages;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.Common.Utility;
using Uniconta.DataModel;
using DevExpress.Data;
using System.Linq;
using Uniconta.ClientTools.Util;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class ProductionPostedTransSort : IComparer
    {
        public int Compare(object x, object y)
        {
            return ((ProductionPostedTrans)x)._LineNumber - ((ProductionPostedTrans)y)._LineNumber;
        }
    }
    public class ProductionPostedTransGridClient : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(ProductionPostedTransClient); } }
        public override IComparer GridSorting { get { return new InvTransInvoiceSort(); } }
    }
    /// <summary>
    /// Interaction logic for ProductionPostedTransGridPage.xaml
    /// </summary>
    public partial class ProductionPostedTransGridPage : GridBasePage
    {
        public ProductionPostedTransGridPage(BaseAPI API) : base(API, string.Empty)
        {
            InitPage(null);
        }

        public ProductionPostedTransGridPage(UnicontaBaseEntity master):base(master)
        {
            InitPage(master);
        }
        private void InitPage(UnicontaBaseEntity master)
        {
            InitializeComponent();
            dgProductionPostedTrans.UpdateMaster(master);
            SetRibbonControl(localMenu, dgProductionPostedTrans);
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
            dgProductionPostedTrans.api = api;
            dgProductionPostedTrans.BusyIndicator = busyIndicator;
            LoadNow(typeof(Uniconta.DataModel.InvItem));
            dgProductionPostedTrans.CustomSummary += DgProductionPostedTrans_CustomSummary;
            dgProductionPostedTrans.ShowTotalSummary();
        }

        double sumMargin, sumSales, sumMarginRatio;
        private void DgProductionPostedTrans_CustomSummary(object sender, DevExpress.Data.CustomSummaryEventArgs e)
        {
            var fieldName = ((GridSummaryItem)e.Item).FieldName;
            switch (e.SummaryProcess)
            {
                case CustomSummaryProcess.Start:
                    sumMargin = sumSales = 0d;
                    break;
                case CustomSummaryProcess.Calculate:
                    var row = e.Row as ProductionPostedTransClient;
                    sumSales += row.SalesPrice;
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

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgProductionPostedTrans.SelectedItem as ProductionPostedTransClient;
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
                                var visibleItems = dgProductionPostedTrans.VisibleItems.Cast<ProductionPostedTransClient>();
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
                    AddDockItem(TabControls.InvSeriesBatch, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("SerialBatchNumbers"), selectedItem._Item));
                    break;
                case "AddEditNote":
                    if (selectedItem == null) return;
                    CWAddEditNote cwAddEditNote = new CWAddEditNote(api, selectedItem);
                    cwAddEditNote.Closed += delegate
                    {
                        if (cwAddEditNote.DialogResult == true)
                        {
                            if (cwAddEditNote.result == ErrorCodes.Succes)
                            {
                                selectedItem._Note = cwAddEditNote.invTransClient._Note;
                                selectedItem.HasNote = !string.IsNullOrEmpty(cwAddEditNote.invTransClient._Note);
                                dgProductionPostedTrans.UpdateItemSource(2, selectedItem);
                            }
                        }
                    };
                    cwAddEditNote.Show();
                    break;
                case "PostedBy":
                    if (selectedItem != null)
                        JournalPosted(selectedItem);
                    break;
                case "VoucherTransactions":
                    if (selectedItem == null)
                        return;
                    string arg;
                    if (selectedItem._JournalPostedId != 0)
                        arg = string.Format("{0}={1}", Uniconta.ClientTools.Localization.lookup("JournalPostedId"), selectedItem._JournalPostedId);
                    else if (selectedItem._InvoiceNumber != 0)
                        arg = string.Format("{0}={1}", Uniconta.ClientTools.Localization.lookup("Invoice"), selectedItem._InvoiceNumber);
                    else if (selectedItem._InvJournalPostedId != 0)
                        arg = string.Format("{0} ({1})={2}", Uniconta.ClientTools.Localization.lookup("JournalPostedId"), Uniconta.ClientTools.Localization.lookup("Inventory"), selectedItem._InvJournalPostedId);
                    else
                        arg = string.Format("{0}={1}", Uniconta.ClientTools.Localization.lookup("Account"), selectedItem.AccountName);
                    string vheader = Util.ConcatParenthesis(Uniconta.ClientTools.Localization.lookup("VoucherTransactions"), arg);
                    AddDockItem(TabControls.AccountsTransaction, dgProductionPostedTrans.syncEntity, vheader);
                    break;
                case "AttachSerialBatch":
                    if (selectedItem != null)
                        AddDockItem(TabControls.AttachInvSeriesBatch, selectedItem, Uniconta.ClientTools.Localization.lookup("SerialBatchNumbers"));
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            var comp = api.CompanyEntity;
            if (!comp.Location || !comp.Warehouse)
                Location.Visible = Location.ShowInColumnChooser = false;
            else
                Location.ShowInColumnChooser = true;
            if (!comp.Warehouse)
                Warehouse.Visible = Warehouse.ShowInColumnChooser = false;
            else
                Warehouse.ShowInColumnChooser = true;
            if (!comp.Project)
                PrCategory.Visible = PrCategory.ShowInColumnChooser = false;
            Utility.SetupVariants(api, colVariant, VariantName, colVariant1, colVariant2, colVariant3, colVariant4, colVariant5, Variant1Name, Variant2Name, Variant3Name, Variant4Name, Variant5Name);
            Utility.SetDimensionsGrid(api, cldim1, cldim2, cldim3, cldim4, cldim5);
        }

        public override bool CheckIfBindWithUserfield(out bool isReadOnly, out bool useBinding)
        {
            isReadOnly = true;
            useBinding = true;
            return true;
        }
        async private void JournalPosted(ProductionPostedTransClient selectedItem)
        {
            var result = await api.Query(new GLDailyJournalPostedClient(), new UnicontaBaseEntity[] { selectedItem }, null);
            if (result != null && result.Length == 1)
            {
                CWGLPostedClientFormView cwPostedClient = new CWGLPostedClientFormView(result[0]);
                cwPostedClient.Show();
            }
        }
        public override string NameOfControl { get { return TabControls.ProductionPostedTransGridPage; } }

        protected override LookUpTable HandleLookupOnLocalPage(LookUpTable lookup, CorasauDataGrid dg)
        {
            var inv = dg.SelectedItem as ProductionPostedTransClient;
            if (inv == null)
                return lookup;
            if (dg.CurrentColumn?.Name == "Item")
                lookup.TableType = typeof(Uniconta.DataModel.InvItem);
            return lookup;
        }
    }
}
