using UnicontaClient.Models;
using UnicontaClient.Utilities;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using System;
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
using Uniconta.DataModel;
using Uniconta.ClientTools.Controls;
using System.Collections;
using Uniconta.API.DebtorCreditor;
using System.Windows.Threading;
using Uniconta.ClientTools.Util;
using Uniconta.API.Service;
using DevExpress.Xpf.Grid;
using DevExpress.Data;
using Uniconta.API.Project;
using UnicontaClient.Controls.Dialogs;
using DevExpress.CodeParser;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class ProjectTransDeletePage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.ProjectTransDelete; } }

        public ProjectTransDeletePage(BaseAPI API) : base(API, string.Empty)
        {
            Init();
        }
        
        private void Init(UnicontaBaseEntity master = null)
        {
            InitializeComponent();
            localMenu.dataGrid = dgProjectGrid;
            dgProjectGrid.api = api;
            dgProjectGrid.BusyIndicator = busyIndicator;
            SetRibbonControl(localMenu, dgProjectGrid);
            dgProjectGrid.ShowTotalSummary();
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgProjectGrid.CustomSummary += DgProjectGrid_CustomSummary;
            this.PreviewKeyDown += RootVisual_KeyDown;
            this.BeforeClose += Project_BeforeClose;
        }

        private void Project_BeforeClose()
        {
            this.PreviewKeyDown -= RootVisual_KeyDown;
        }

        private void RootVisual_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.F8 && Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                ribbonControl.PerformRibbonAction("PrTrans");
        }

        double sumMargin, sumSales, sumMarginRatio;
        private void DgProjectGrid_CustomSummary(object sender, DevExpress.Data.CustomSummaryEventArgs e)
        {
            var fieldName = ((GridSummaryItem)e.Item).FieldName;
            switch (e.SummaryProcess)
            {
                case CustomSummaryProcess.Start:
                    sumMargin = sumSales = 0d;
                    break;
                case CustomSummaryProcess.Calculate:
                    var row = e.Row as ProjectClient;
                    sumSales += row.SalesValue;
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

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            setDim();
            dgProjectGrid.Readonly = true;
        }
        protected override void LoadCacheInBackGround()
        {
            var projects = api.GetCache(typeof(Uniconta.DataModel.Project));
            TestDebtorReload(false, projects?.GetNotNullArray as IEnumerable<Uniconta.DataModel.Project>);

            var Comp = api.CompanyEntity;
            var lst = new List<Type>(15) { typeof(Uniconta.DataModel.PrType), typeof(Uniconta.DataModel.PaymentTerm), typeof(Uniconta.DataModel.ProjectGroup), typeof(Uniconta.DataModel.PrStandard),
                typeof(Uniconta.DataModel.PrCategory), typeof(Uniconta.DataModel.PrWorkSpace) };
            if (Comp.Contacts)
                lst.Add(typeof(Uniconta.DataModel.Contact));
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
            lst.Add(typeof(Uniconta.DataModel.Employee));
            lst.Add(typeof(Uniconta.DataModel.Debtor));
            if (Comp.DeliveryAddress)
                lst.Add(typeof(Uniconta.DataModel.WorkInstallation));
            lst.Add(typeof(Uniconta.DataModel.PrCategory));
            LoadType(lst);
        }

        async void TestDebtorReload(bool refresh, IEnumerable<Uniconta.DataModel.Project> lst)
        {
            if (lst != null && lst.Count() > 0)
            {
                var cache = api.GetCache(typeof(Uniconta.DataModel.Debtor));
                if (cache != null)
                {
                    bool reload = false;
                    var Contacts = api.GetCache(typeof(Uniconta.DataModel.Contact));
                    foreach (var rec in lst)
                    {
                        if (rec._DCAccount != null && cache.Get(rec._DCAccount) == null)
                        {
                            reload = true;
                            break;
                        }
                        if (rec._ContactRef != 0 && Contacts != null && Contacts.Get(rec._ContactRef) == null)
                        {
                            Contacts = null;
                            api.LoadCache(typeof(Uniconta.DataModel.Contact), true);
                        }
                    }
                    if (reload)
                        await api.LoadCache(typeof(Uniconta.DataModel.Debtor), true);
                }
            }
            if (refresh)
                gridRibbon_BaseActions("RefreshGrid");
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItems = dgProjectGrid.SelectedItems;
            var selectedItem = dgProjectGrid.SelectedItem as ProjectClient;
            string salesHeader = string.Empty;
            if (selectedItem != null)
                salesHeader = string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Project"), selectedItem._Number);
            switch (ActionType)
            {
                case "PrTrans":
                    if (selectedItem != null)
                        AddDockItem(TabControls.ProjectTransactionPage, dgProjectGrid.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Transactions"), selectedItem._Number));
                    break;
                case "DeleteCurrentProjectTransactions":
                    if (selectedItem != null)
                        DeleteOldTransactions(false);
                    break;
                case "DeleteAllProjectTransactions":
                    DeleteOldTransactions(true);
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

      

        void setDim()
        {
            UnicontaClient.Utilities.Utility.SetDimensionsGrid(api, cldim1, cldim2, cldim3, cldim4, cldim5);
        }

        private void HasDocImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var projectClient = (sender as Image).Tag as ProjectClient;
            if (projectClient != null)
                AddDockItem(TabControls.UserDocsPage, dgProjectGrid.syncEntity);
        }

        private void HasNoteImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var projectClient = (sender as Image).Tag as ProjectClient;
            if (projectClient != null)
                AddDockItem(TabControls.UserNotesPage, dgProjectGrid.syncEntity);
        }

        private void HasEmailImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var projectClient = (sender as TextBlock).Tag as ProjectClient;
            if (projectClient != null)
            {
                var mail = string.Concat("mailto:", projectClient._Email);
                System.Diagnostics.Process proc = new System.Diagnostics.Process();
                proc.StartInfo.FileName = mail;
                proc.Start();
            }
        }
        void DeleteOldTransactions(bool DeleteAll)
        {
            var papi = new UnicontaAPI.Project.API.PostingAPI(api);
            var cwDateSelector = new CWDateSelector(DateTime.Now.AddYears(-2));
            cwDateSelector.DialogTableId = 2000000042;
            cwDateSelector.Title = Uniconta.ClientTools.Localization.lookup("DeleteTrans");
            cwDateSelector.Closed += delegate
            {
                if (cwDateSelector.DialogResult == true)
                {
                    var messageBox = new CWConfirmationBox(Uniconta.ClientTools.Localization.lookup("AreYouSureToContinue"), Uniconta.ClientTools.Localization.lookup("Delete"), false, string.Format("{0}:",
                           Uniconta.ClientTools.Localization.lookup("Warning")));
                    messageBox.Closed += async delegate
                    {
                        if (messageBox.ConfirmationResult == CWConfirmationBox.ConfirmationResultEnum.Yes)
                        {
                            busyIndicator.IsBusy = true;
                            if (DeleteAll)
                            {
                                var tasks = new List<Task<ErrorCodes>>();
                                foreach (var pr in dgProjectGrid.VisibleItems)
                                {
                                    var t = papi.DeleteTransactions((pr as ProjectClient).KeyStr, cwDateSelector.SelectedDate);
                                    tasks.Add(t);
                                }
                                var result = await Task.WhenAll(tasks);
                                busyIndicator.IsBusy = false;
                                var error = result.Where(e => e != ErrorCodes.Succes).FirstOrDefault();
                                if (error != ErrorCodes.Succes)
                                    UtilDisplay.ShowErrorCode(error);
                            }
                            else
                            {
                                var result = await papi.DeleteTransactions((dgProjectGrid.SelectedItem as ProjectClient).KeyStr, cwDateSelector.SelectedDate);
                                busyIndicator.IsBusy = false;
                                if (result != ErrorCodes.Succes)
                                    UtilDisplay.ShowErrorCode(result);
                            }
                        }
                    };
                    messageBox.Show();

                }
            };
            cwDateSelector.Show();
        }
    }
}