using UnicontaClient.Models;
using UnicontaClient.Pages;
using UnicontaClient.Utilities;
using System;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using System.Threading.Tasks;
using System.Collections.Generic;
using Uniconta.API.DebtorCreditor;
using Uniconta.ClientTools.Util;
using System.Windows;
using Uniconta.ClientTools;
using Uniconta.DataModel;
using Uniconta.API.Inventory;
using System.Collections;
using System.Linq;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.Controls.Reporting;
using Uniconta.API.Service;
using System.Windows.Input;
using System.Windows.Controls;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class ProductionOrdersPageGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(ProductionOrderClient); } }
        public override IComparer GridSorting { get { return new DCOrderSort(); } }
    }

    public partial class ProductionOrdersPage : GridBasePage
    {
        public override string NameOfControl
        {
            get { return TabControls.ProductionOrders.ToString(); }
        }
        public ProductionOrdersPage(BaseAPI api, string lookupKey)
            : base(api, lookupKey)
        {
            Init(null);
        }
        public ProductionOrdersPage(BaseAPI API) : base(API, string.Empty)
        {
            Init(null);
        }
        public ProductionOrdersPage(UnicontaBaseEntity master)
            : base(master)
        {
            Init(master);
        }

        private void Init(UnicontaBaseEntity master)
        {
            InitializeComponent();
            dgProductionOrders.UpdateMaster(master);
            localMenu.dataGrid = dgProductionOrders;
            SetRibbonControl(localMenu, dgProductionOrders);
            dgProductionOrders.api = api;
            dgProductionOrders.BusyIndicator = busyIndicator;

            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgProductionOrders.RowDoubleClick += DgProductionOrders_RowDoubleClick;
            ribbonControl.DisableButtons(new string[] { "DeleteRow", "UndoDelete", "SaveGrid" });
#if SILVERLIGHT
            HideMenuItems();
#endif
        }

        private void HideMenuItems()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            if (rb != null)
                UtilDisplay.RemoveMenuCommand(rb, "ProductionReport");
        }
        private void DgProductionOrders_RowDoubleClick()
        {
            localMenu_OnItemClicked("ProductionLines");
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            UnicontaClient.Utilities.Utility.SetDimensionsGrid(api, cldim1, cldim2, cldim3, cldim4, cldim5);

            bool showFields = (dgProductionOrders.masterRecords == null);
            Account.Visible = showFields;
            AccountName.Visible = showFields;
            setDim();
            var Comp = api.CompanyEntity;
            if (!Comp.Location || !Comp.Warehouse)
                Location.Visible = Location.ShowInColumnChooser = false;
            if (!Comp.Warehouse)
                Warehouse.Visible = Warehouse.ShowInColumnChooser = false;
            if (!Comp.Project)
            {
                Project.Visible = Project.ShowInColumnChooser = false;
                PrCategory.Visible = PrCategory.ShowInColumnChooser = false;
            }
            Utility.SetupVariants(api, null, colVariant1, colVariant2, colVariant3, colVariant4, colVariant5, Variant1Name, Variant2Name, Variant3Name, Variant4Name, Variant5Name);
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgProductionOrders.SelectedItem as ProductionOrderClient;
            string salesHeader = string.Empty;
            if (selectedItem != null)
                salesHeader = string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Production"), selectedItem._OrderNumber);
            switch (ActionType)
            {
                case "AddRow":
                    if (dgProductionOrders.masterRecords != null)
                    {
                        object[] arr = new object[2] { api, dgProductionOrders.masterRecord };
                        AddDockItem(TabControls.ProductionOrdersPage2, arr, Uniconta.ClientTools.Localization.lookup("Production"), "Add_16x16.png", true);
                    }
                    else
                    {
                        AddDockItem(TabControls.ProductionOrdersPage2, api, Uniconta.ClientTools.Localization.lookup("Production"), "Add_16x16.png", true);
                    }
                    break;
                case "EditRow":
                    if (selectedItem == null)
                        return;
                    if (dgProductionOrders.masterRecords != null)
                    {
                        object[] arr = new object[2] { selectedItem, dgProductionOrders.masterRecord };
                        AddDockItem(TabControls.ProductionOrdersPage2, arr, salesHeader);
                    }
                    else
                    {
                        AddDockItem(TabControls.ProductionOrdersPage2, selectedItem, salesHeader);
                    }
                    break;
                case "AddNote":
                    if (selectedItem != null)
                        AddDockItem(TabControls.UserNotesPage, dgProductionOrders.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Notes"), selectedItem._OrderNumber));
                    break;
                case "AddDoc":
                    if (selectedItem != null)
                        AddDockItem(TabControls.UserDocsPage, dgProductionOrders.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Documents"), selectedItem._OrderNumber));
                    break;
                case "ProductionLines":
                    if (selectedItem != null)
                    {
                        var olheader = string.Format("{0}: {1}, {2}", Uniconta.ClientTools.Localization.lookup("ProductionLines"), selectedItem._OrderNumber, selectedItem._DCAccount);
                        AddDockItem(TabControls.ProductionOrderLines, dgProductionOrders.syncEntity, olheader);
                    }
                    break;
                case "CreateProductionLines":
                    if (selectedItem != null)
                        CreateOrderLines(selectedItem);
                    break;
                case "ReportAsFinished":
                    if (selectedItem != null)
                        PostProduction(selectedItem);
                    break;
                case "ProductionPosted":
                    if (selectedItem != null)
                        AddDockItem(TabControls.ProductionPostedGridPage, selectedItem);
                    break;
                case "MarkOrderLine":
                    if (selectedItem?._ProdItem != null)
                        MarkedOrderLine(selectedItem);
                    break;
#if !SILVERLIGHT
                case "ProductionReport":
                    if (selectedItem != null)
                        CreateProductionReport(selectedItem);
                    break;
#endif
                case "EditAll":
                    if (dgProductionOrders.Visibility == Visibility.Visible)
                        EditAll();
                    break;
                case "SaveGrid":
                    Save();
                    break;
                case "ViewPhoto":
                    if (selectedItem != null && selectedItem?.ProdItemRef != null)
                        AddDockItem(TabControls.UserDocsPage, selectedItem.ProdItemRef, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Documents"), selectedItem?.ProdItemRef?._Name));
                    break;
                case "DeleteRow":
                    dgProductionOrders.DeleteRow();
                    break;
                case "UndoDelete":
                    dgProductionOrders.UndoDeleteRow();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        private async void Save()
        {
            SetBusy();
            var err = await dgProductionOrders.SaveData();
            if (err != ErrorCodes.Succes)
                api.AllowBackgroundCrud = true;
            ClearBusy();
        }

        bool editAllChecked;
        private void EditAll()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            var iBase = UtilDisplay.GetMenuCommandByName(rb, "EditAll");
            if (iBase == null) return;

            if (dgProductionOrders.Readonly)
            {
                dgProductionOrders.MakeEditable();
                UserFieldControl.MakeEditable(dgProductionOrders);
                iBase.Caption = Uniconta.ClientTools.Localization.lookup("LeaveEditAll");
                ribbonControl.EnableButtons(new string[] { "DeleteRow", "UndoDelete", "SaveGrid" });
                editAllChecked = false;
            }
            else
            {
                if (IsDataChaged)
                {
                    string message = Uniconta.ClientTools.Localization.lookup("SaveChangesPrompt");
                    CWConfirmationBox confirmationDialog = new CWConfirmationBox(message);
                    confirmationDialog.Closing += async delegate
                    {
                        if (confirmationDialog.DialogResult == null)
                            return;

                        switch (confirmationDialog.ConfirmationResult)
                        {
                            case CWConfirmationBox.ConfirmationResultEnum.Yes:
                                await dgProductionOrders.SaveData();
                                break;
                            case CWConfirmationBox.ConfirmationResultEnum.No:
                                break;
                        }
                        editAllChecked = true;
                        dgProductionOrders.Readonly = true;
                        dgProductionOrders.tableView.CloseEditor();
                        iBase.Caption = Uniconta.ClientTools.Localization.lookup("EditAll");
                        ribbonControl.DisableButtons(new string[] { "DeleteRow", "UndoDelete", "SaveGrid" });
                    };
                    confirmationDialog.Show();
                }
                else
                {
                    dgProductionOrders.Readonly = true;
                    dgProductionOrders.tableView.CloseEditor();
                    iBase.Caption = Uniconta.ClientTools.Localization.lookup("EditAll");
                    ribbonControl.DisableButtons(new string[] { "DeleteRow", "UndoDelete", "SaveGrid" });
                }
            }
        }

        public override bool IsDataChaged
        {
            get
            {
                return editAllChecked ? false : dgProductionOrders.HasUnsavedData;
            }
        }

        async void MarkedOrderLine(ProductionOrderClient selectedItem)
        {
            busyIndicator.IsBusy = true;
            var orderLineMarked = new DebtorOrderLineClient();
            OrderAPI orderApi = new OrderAPI(api);
            var res = await orderApi.GetMarkedOrderLine(selectedItem, orderLineMarked);
            busyIndicator.IsBusy = false;
            if (res == ErrorCodes.Succes)
            {
                object[] paramArr = new object[] { api, orderLineMarked };
                AddDockItem(TabControls.OrderLineMarkedPage, paramArr, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("OrderLine"), orderLineMarked._OrderNumber));
            }
            else
                UtilDisplay.ShowErrorCode(res);
        }

#if !SILVERLIGHT
        async private void CreateProductionReport(ProductionOrderClient productionOrder)
        {
            var companyClient = Utility.GetCompanyClientUserInstance(api.CompanyEntity);
            var getLogo = await UtilDisplay.GetLogo(api);

            var productionOrderLineUserType = api.CompanyEntity.GetUserTypeNotNull(typeof(ProductionOrderLineClient));
            var productionOrderLineInstance = Activator.CreateInstance(productionOrderLineUserType) as ProductionOrderLineClient;
            var productionOrderLines = await api.Query(productionOrderLineInstance, new UnicontaBaseEntity[] { productionOrder }, null);

            if (productionOrderLines != null && productionOrderLines.Length > 0)
            {
                var productionReportSource = new ProductionStandardReportClient(companyClient, productionOrder, productionOrderLines, getLogo, Uniconta.ClientTools.Localization.lookup("ProductionOrder"));
                var standardReportSrc = new[] { productionReportSource };
                var standardPrint = new StandardPrintReport(api, standardReportSrc, (int)StandardReports.ProductionOrder);
                standardPrint.UseReportCache = true;
                await standardPrint.InitializePrint();

                if (standardPrint?.Report != null)
                    AddDockItem(TabControls.StandardPrintReportPage, new object[] { new DevExpress.XtraReports.UI.XtraReport[] { standardPrint.Report } },
                        string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("PrintPreview"), productionOrder.ProductionNumber));
            }
            else
                UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("NoLinesFound"), Uniconta.ClientTools.Localization.lookup("Warning"), MessageBoxButton.OK);
        }
#endif
        private void PostProduction(ProductionOrder dbOrder)
        {
            CWInvPosting invpostingDialog = new CWInvPosting(api, "ReportAsFinished", true);
#if !SILVERLIGHT
            invpostingDialog.DialogTableId = 2000000041;
#endif
            invpostingDialog.Closed += async delegate
            {
                if (invpostingDialog.DialogResult == true)
                {
                    busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("SendingWait");
                    busyIndicator.IsBusy = true;
                    var papi = new Uniconta.API.Inventory.ProductionAPI(api);
                    var postingResult = await papi.ReportAsFinished(dbOrder, invpostingDialog.Date, invpostingDialog.Text, invpostingDialog.TransType,
                        invpostingDialog.Comment, invpostingDialog.FixedVoucher, invpostingDialog.Simulation, new GLTransClientTotal(), 0, invpostingDialog.NumberSeries);
                    busyIndicator.IsBusy = false;
                    busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("LoadingMsg");
                    if (postingResult == null)
                        return;
                    if (postingResult.Err != ErrorCodes.Succes)
                        Utility.ShowJournalError(postingResult, dgProductionOrders, goToLinesMsg: false);
                    else if (invpostingDialog.Simulation)
                    {
                        if (postingResult.SimulatedTrans != null)
                            AddDockItem(TabControls.SimulatedTransactions, postingResult.SimulatedTrans, Uniconta.ClientTools.Localization.lookup("SimulatedTransactions"), null, true);
                        else
                        {
                            var msg = string.Format(Uniconta.ClientTools.Localization.lookup("OBJisEmpty"), Uniconta.ClientTools.Localization.lookup("LedgerTransList"));
                            msg = Uniconta.ClientTools.Localization.lookup("JournalOK") + Environment.NewLine + msg;
                            UnicontaMessageBox.Show(msg, Uniconta.ClientTools.Localization.lookup("Message"));
                        }
                    }
                    else
                    {
                        dgProductionOrders.UpdateItemSource(3, dbOrder);

                        string msg;
                        if (postingResult.JournalPostedlId != 0)
                            msg = string.Format("{0} {1}={2}", Uniconta.ClientTools.Localization.lookup("JournalHasBeenPosted"), Uniconta.ClientTools.Localization.lookup("JournalPostedId"), postingResult.JournalPostedlId);
                        else
                            msg = Uniconta.ClientTools.Localization.lookup("JournalHasBeenPosted");
                        UnicontaMessageBox.Show(msg, Uniconta.ClientTools.Localization.lookup("Message"));
                    }
                }
            };
            invpostingDialog.Show();
        }

        void CreateOrderLines(ProductionOrderClient productionOrder)
        {
            CWProductionOrderLine dialog = new CWProductionOrderLine(api);
#if !SILVERLIGHT
            dialog.DialogTableId = 2000000078;
#endif
            dialog.Closing += async delegate
            {
                if (dialog.DialogResult == true)
                {
                    var prodAPI = new ProductionAPI(api);
                    var result = await prodAPI.CreateProductionLines(productionOrder, (StorageRegister)dialog.storage, dialog.Force);
                    UtilDisplay.ShowErrorCode(result);
                    //else
                    //    CreditorOrders.ShowOrderLines(4, productionOrder, this, dgProductionOrders);
                }
            };
            dialog.Show();
        }

        protected override void LoadCacheInBackGround()
        {
            var Comp = api.CompanyEntity;
            var lst = new List<Type>(4) { typeof(Uniconta.DataModel.InvItem), typeof(Uniconta.DataModel.Debtor) };
            if (Comp.Warehouse)
                lst.Add(typeof(Uniconta.DataModel.InvWarehouse));
            if (Comp.Project)
                lst.Add(typeof(Uniconta.DataModel.Project));
            LoadType(lst);
        }

        public override async void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.ProductionOrdersPage2)
                dgProductionOrders.UpdateItemSource(argument);
            else if (screenName == TabControls.ProductionOrderLines)
            {
                var ProductionOrder = argument as ProductionOrderClient;
                if (ProductionOrder == null)
                    return;
                var err = await api.Read(ProductionOrder);
                if (err == ErrorCodes.CouldNotFind)
                    dgProductionOrders.UpdateItemSource(3, ProductionOrder);
                else if (err == ErrorCodes.Succes)
                    dgProductionOrders.UpdateItemSource(2, ProductionOrder);
            }
        }

        private Task Filter(IEnumerable<PropValuePair> propValuePair)
        {
            return dgProductionOrders.Filter(propValuePair);
        }

        void setDim()
        {
            UnicontaClient.Utilities.Utility.SetDimensionsGrid(api, cldim1, cldim2, cldim3, cldim4, cldim5);
        }

        private Task BindGrid()
        {
            return dgProductionOrders.Filter(null);
        }

        private void HasDocImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var order = (sender as Image).Tag as ProductionOrderClient;
            if (order != null)
                AddDockItem(TabControls.UserDocsPage, dgProductionOrders.syncEntity);
        }

        private void HasNoteImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var order = (sender as Image).Tag as ProductionOrderClient;
            if (order != null)
                AddDockItem(TabControls.UserNotesPage, dgProductionOrders.syncEntity);
        }
    }
}
