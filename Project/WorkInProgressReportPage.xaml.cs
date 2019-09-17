using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using System.Windows.Shapes;
using Uniconta.API.Service;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;
using UnicontaClient.Models;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class WorkInProgressReportGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(ProjectTransLocalClient); } }
    }

    public partial class WorkInProgressReportPage : GridBasePage
    {
        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.Employee))]
        public string FromPerInCharge { get; set; }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.Employee))]
        public string ToPerInCharge { get; set; }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(Debtor))]
        public string FromDebtor { get; set; }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(Debtor))]
        public string ToDebtor { get; set; }
     
        static string fromPerInCharge, toPerInCharge, fromDebtor, toDebtor;
        static DateTime DefaultFromDate, DefaultToDate;
        public WorkInProgressReportPage(BaseAPI API) : base(API, string.Empty)
        {
            this.DataContext = this;
            InitializeComponent();
            //leFromDebtor.api = leToDebtor.api = leFromPerInChre.api = leToPerInChre.api =api;
            localMenu.dataGrid = dgWorkInProgressRpt;
            SetRibbonControl(localMenu, dgWorkInProgressRpt);
            dgWorkInProgressRpt.api = api;
            dgWorkInProgressRpt.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgWorkInProgressRpt.ShowTotalSummary();
            dgWorkInProgressRpt.tableView.AllowFixedColumnMenu = true;

            //this.lblFrmPerInChrg.Text = string.Format("{0} {1}", Uniconta.ClientTools.Localization.lookup("From"), Uniconta.ClientTools.Localization.lookup("PersonInCharge"));
            //this.lblToPerInChrg.Text = string.Format("{0} {1}", Uniconta.ClientTools.Localization.lookup("To"), Uniconta.ClientTools.Localization.lookup("PersonInCharge"));
            //this.lblFrmDebtor.Text = string.Format("{0} {1}", Uniconta.ClientTools.Localization.lookup("From"), Uniconta.ClientTools.Localization.lookup("Debtor"));
            //this.lblToDebtor.Text = string.Format("{0} {1}", Uniconta.ClientTools.Localization.lookup("To"), Uniconta.ClientTools.Localization.lookup("Debtor"));

            if (WorkInProgressReportPage.DefaultFromDate == DateTime.MinValue)
            {
                var now = BasePage.GetSystemDefaultDate();

                var fromDate = new DateTime(now.Year, now.Month, 1);
                fromDate = fromDate.AddMonths(-2);

                WorkInProgressReportPage.DefaultToDate = now;
                WorkInProgressReportPage.DefaultFromDate = fromDate;
            }

            //if (!string.IsNullOrEmpty(fromPerInCharge))
            //    leFromPerInChre.EditValue = fromPerInCharge;
            //if (!string.IsNullOrEmpty(toPerInCharge))
            //    leToPerInChre.EditValue = toPerInCharge;
            //if (!string.IsNullOrEmpty(fromDebtor))
            //    leFromDebtor.EditValue = fromDebtor;
            //if (!string.IsNullOrEmpty(toDebtor))
            //    leToDebtor.EditValue = toDebtor;

            txtDateTo.DateTime = WorkInProgressReportPage.DefaultToDate;
            txtDateFrm.DateTime = WorkInProgressReportPage.DefaultFromDate;
            StartLoadCache();
            WorkInProgressReportPage.SetDateTime(txtDateFrm, txtDateTo);
        }

        public static void SetDateTime(DateEditor frmDateeditor, DateEditor todateeditor)
        {
            var Now = BasePage.GetSystemDefaultDate();
            var previouslastDayOfTheYear = new DateTime(Now.Year - 1, 12, 31);
            if (frmDateeditor.Text == string.Empty)
                DefaultFromDate = Now;
            else
                DefaultFromDate = frmDateeditor.DateTime.Date;

            if (todateeditor.Text == string.Empty)
                DefaultToDate = previouslastDayOfTheYear;
            else
                DefaultToDate = todateeditor.DateTime.Date;
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            UnicontaClient.Utilities.Utility.SetDimensionsGrid(api, cldim1, cldim2, cldim3, cldim4, cldim5);
            SetDimensionLocalMenu();
        }

        public override Task InitQuery()
        {
            return null;
        }

        public void SetDimensionLocalMenu()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            var c = api.CompanyEntity;
            if (c == null)
                return;
            var ibase1 = UtilDisplay.GetMenuCommandByName(rb, "GroupByDimension1");
            var ibase2 = UtilDisplay.GetMenuCommandByName(rb, "GroupByDimension2");
            var ibase3 = UtilDisplay.GetMenuCommandByName(rb, "GroupByDimension3");
            var ibase4 = UtilDisplay.GetMenuCommandByName(rb, "GroupByDimension4");
            var ibase5 = UtilDisplay.GetMenuCommandByName(rb, "GroupByDimension5");
            ibase1.Caption = string.Format(Uniconta.ClientTools.Localization.lookup("GroupByOBJ"), (string)c._Dim1);
            ibase2.Caption = string.Format(Uniconta.ClientTools.Localization.lookup("GroupByOBJ"), (string)c._Dim2);
            ibase3.Caption = string.Format(Uniconta.ClientTools.Localization.lookup("GroupByOBJ"), (string)c._Dim3);
            ibase4.Caption = string.Format(Uniconta.ClientTools.Localization.lookup("GroupByOBJ"), (string)c._Dim4);
            ibase5.Caption = string.Format(Uniconta.ClientTools.Localization.lookup("GroupByOBJ"), (string)c._Dim5);
            var noofDimensions = c.NumberOfDimensions;
            if (noofDimensions < 5)
                UtilDisplay.RemoveMenuCommand(rb, "GroupByDimension5");
            if (noofDimensions < 4)
                UtilDisplay.RemoveMenuCommand(rb, "GroupByDimension4");
            if (noofDimensions < 3)
                UtilDisplay.RemoveMenuCommand(rb, "GroupByDimension3");
            if (noofDimensions < 2)
                UtilDisplay.RemoveMenuCommand(rb, "GroupByDimension2");
            if (noofDimensions < 1)
                UtilDisplay.RemoveMenuCommand(rb, "GroupByDimension1");
        }

        async void LoadGrid()
        {
            SetDateTime(txtDateFrm, txtDateTo);
            var transList = new List<ProjectTransLocalClient>();
            var secTransLst = new List<ProjectTransLocalClient>();
            var newLst = new List<ProjectTransLocalClient>();
            busyIndicator.IsBusy = true;
            var projTransLst = await api.Query<ProjectTransClient>(); 
            //var projBudgetTransLst = await api.Query<ProjectBudgetLineClient>();


            var projectTransEntity = projTransLst?.Where(x => x.Date >= DefaultFromDate && x.Date <= DefaultToDate).ToList();
            if (projectTransEntity != null)
            {
                var feeLst = projectTransEntity.Where(x => x.PrCategoryRef._CatType == CategoryType.Labour);
                var lst1 = GetProjectTransLines(feeLst, isFee: true);
                transList.AddRange(lst1);

                var xpensLst = projectTransEntity.
                    Where(x => x.PrCategoryRef._CatType == CategoryType.Materials ||
                          x.PrCategoryRef._CatType == CategoryType.Expenses ||
                          x.PrCategoryRef._CatType == CategoryType.ExternalWork ||
                          x.PrCategoryRef._CatType == CategoryType.Miscellaneous ||
                          x.PrCategoryRef._CatType == CategoryType.Other);
                var lst2 = GetProjectTransLines(xpensLst, isExpenses: true);
                transList.AddRange(lst2);

                var onAccLst = projectTransEntity.
                  Where(x => x.PrCategoryRef._CatType == CategoryType.OnAccountInvoicing);
                var lst3 = GetProjectTransLines(onAccLst, isOnAccount: true);
                transList.AddRange(lst3);

                var finalInvLst = projectTransEntity.
                    Where(x => x.PrCategoryRef._CatType == CategoryType.Revenue);
                var lst4 = GetProjectTransLines(finalInvLst, isFinalInvoice: true);
                transList.AddRange(lst4);

                var adjLst = projectTransEntity.
                   Where(x => x.PrCategoryRef._CatType == CategoryType.Adjustment);
                var lst5 = GetProjectTransLines(adjLst, isAdjustement: true);
                transList.AddRange(lst5);

                var newLst1 = transList?.GroupBy(x => x.Project).Select(y => new ProjectTransLocalClient
                {
                    CompanyId = api.CompanyId,
                    FromDate = DefaultFromDate.Date,
                    Date = y.First().Date,
                    _Project = y.First()._Project,
                    EmployeeFee = y.Sum(xs => xs.EmployeeFee),
                    Expenses = y.Sum(xs => xs.Expenses),
                    OnAccount = y.Sum(xs => xs.OnAccount),
                    Invoiced = y.Sum(xs => xs.Invoiced),
                    Adjustment = y.Sum(xs => xs.Adjustment),
                    //BudgetQty = y.Sum(xs => xs.BudgetQty),
                    //BudgetCost = y.Sum(xs => xs.BudgetCost),
                    //BudgetSales = y.Sum(xs => xs.BudgetSales),
                });

                newLst.AddRange(newLst1);
            }

            var transEntity = projTransLst?.Where(x => x.Date < DefaultFromDate).ToList();
            if (transEntity != null)
            {
                var feeLst = transEntity.Where(x => x.PrCategoryRef._CatType == CategoryType.Labour);
                var lst5 = GetProjectTransLines(feeLst, isFee: true);
                secTransLst.AddRange(lst5);

                var xpensLst = transEntity.
                    Where(x => x.PrCategoryRef._CatType == CategoryType.Materials ||
                          x.PrCategoryRef._CatType == CategoryType.Expenses ||
                          x.PrCategoryRef._CatType == CategoryType.ExternalWork ||
                          x.PrCategoryRef._CatType == CategoryType.Miscellaneous ||
                          x.PrCategoryRef._CatType == CategoryType.Other);
                var lst6 = GetProjectTransLines(xpensLst, isExpenses: true);
                secTransLst.AddRange(lst6);

                var OnAccLst = transEntity.
                  Where(x => x.PrCategoryRef._CatType == CategoryType.OnAccountInvoicing);
                var lst7 = GetProjectTransLines(OnAccLst, isOnAccount: true);
                secTransLst.AddRange(lst7);

                var FinalInvLst = transEntity.
                  Where(x => x.PrCategoryRef._CatType == CategoryType.Revenue);
                var lst8 = GetProjectTransLines(FinalInvLst, isFinalInvoice: true);
                secTransLst.AddRange(lst8);

                var adjLst = transEntity.
                   Where(x => x.PrCategoryRef._CatType == CategoryType.Adjustment);
                var lst9 = GetProjectTransLines(adjLst, isAdjustement: true);
                secTransLst.AddRange(lst9);

                var newLst2 = secTransLst?.GroupBy(x => x.Project).Select(y => new ProjectTransLocalClient
                {
                    CompanyId = api.CompanyId,
                    FromDate = DefaultFromDate.Date,
                    Date = y.First().Date,
                    _Project = y.First()._Project,
                    OpeningBalance = Math.Round((y.Sum(xs => xs.EmployeeFee) + y.Sum(xs => xs.Expenses)) + y.Sum(xs => xs.OnAccount) + y.Sum(xs => xs.Invoiced) + y.Sum(xs => xs.Adjustment), 2)
                });

                newLst.AddRange(newLst2);
            }

            var finalLst = newLst?.GroupBy(x => x.Project).OrderBy(s => s.Key).Select(y => new ProjectTransLocalClient
            {
                CompanyId = api.CompanyId,
                FromDate = DefaultFromDate.Date,
                Date = y.First().Date,
                _Project = y.First()._Project,
                EmployeeFee = y.Sum(xs => xs.EmployeeFee),
                Expenses = y.Sum(xs => xs.Expenses),
                OnAccount = y.Sum(xs => xs.OnAccount),
                Invoiced = y.Sum(xs => xs.Invoiced),
                Adjustment = y.Sum(xs => xs.Adjustment),
                OpeningBalance= y.Sum(xs=>xs.OpeningBalance)
            });

            dgWorkInProgressRpt.ItemsSource = finalLst;
            dgWorkInProgressRpt.Visibility = Visibility.Visible;
            busyIndicator.IsBusy = false;

        }

        IEnumerable<ProjectTransLocalClient> GetProjectTransLines(IEnumerable<ProjectTransClient> projectTransLst, bool isFee = false, bool isExpenses = false, bool isOnAccount = false, bool isFinalInvoice = false, bool isAdjustement = false)
        {
            var lst = projectTransLst.GroupBy(x => x.Project).Select(y => new ProjectTransLocalClient
            {
                CompanyId = api.CompanyId,
                Date = y.First().Date,
                _Project = y.First()._Project,
                EmployeeFee = isFee == true ? y.Sum(xs => xs.SalesAmount) : 0d,
                Expenses = isExpenses == true ? y.Sum(xs => xs.SalesAmount) : 0d,
                OnAccount = isOnAccount == true ? y.Sum(xs => xs.SalesAmount) : 0d,
                Invoiced = isFinalInvoice == true ? y.Sum(xs => xs.SalesAmount) : 0d,
                Adjustment = isAdjustement == true ? y.Sum(xs => xs.SalesAmount) : 0d
            });
            return lst;
        }

        private void leFromPerInChre_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            var value = e.NewValue;
            if (value != null)
                fromPerInCharge = (string)value;
        }

        private void leToPerInChre_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            var value = e.NewValue;
            if (value != null)
                toPerInCharge = (string)value;
        }

        private void leFromDebtor_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            var value = e.NewValue;
            if (value != null)
                fromDebtor = (string)value;
        }

        private void leToDebtor_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            var value = e.NewValue;
            if (value != null)
                toDebtor = (string)value;
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgWorkInProgressRpt.SelectedItem as ProjectTransLocalClient;
            switch (ActionType)
            {
                case "GroupByDebtor":
                    if (dgWorkInProgressRpt.ItemsSource == null) return;
                    dgWorkInProgressRpt.GroupBy("Debtor");
                    dgWorkInProgressRpt.UngroupBy("PersonInCharge");
                    dgWorkInProgressRpt.UngroupBy("Dimension1");
                    dgWorkInProgressRpt.UngroupBy("Dimension2");
                    dgWorkInProgressRpt.UngroupBy("Dimension3");
                    dgWorkInProgressRpt.UngroupBy("Dimension4");
                    dgWorkInProgressRpt.UngroupBy("Dimension5");
                    break;
                case "GroupByPersonInCharge":
                    if (dgWorkInProgressRpt.ItemsSource == null) return;
                    dgWorkInProgressRpt.GroupBy("PersonInCharge");
                    dgWorkInProgressRpt.UngroupBy("Debtor");
                    dgWorkInProgressRpt.UngroupBy("Dimension1");
                    dgWorkInProgressRpt.UngroupBy("Dimension2");
                    dgWorkInProgressRpt.UngroupBy("Dimension3");
                    dgWorkInProgressRpt.UngroupBy("Dimension4");
                    dgWorkInProgressRpt.UngroupBy("Dimension5");
                    break;
                case "GroupByDimension1":
                    if (dgWorkInProgressRpt.ItemsSource == null) return;
                    dgWorkInProgressRpt.GroupBy("Dimension1");
                    dgWorkInProgressRpt.UngroupBy("Debtor");
                    dgWorkInProgressRpt.UngroupBy("PersonInCharge");
                    dgWorkInProgressRpt.UngroupBy("Period");
                    dgWorkInProgressRpt.UngroupBy("Dimension2");
                    dgWorkInProgressRpt.UngroupBy("Dimension3");
                    dgWorkInProgressRpt.UngroupBy("Dimension4");
                    dgWorkInProgressRpt.UngroupBy("Dimension5");
                    break;
                case "GroupByDimension2":
                    if (dgWorkInProgressRpt.ItemsSource == null) return;
                    dgWorkInProgressRpt.GroupBy("Dimension2");
                    dgWorkInProgressRpt.UngroupBy("Debtor");
                    dgWorkInProgressRpt.UngroupBy("Project");
                    dgWorkInProgressRpt.UngroupBy("PersonInCharge");
                    dgWorkInProgressRpt.UngroupBy("Dimension1");
                    dgWorkInProgressRpt.UngroupBy("Dimension3");
                    dgWorkInProgressRpt.UngroupBy("Dimension4");
                    dgWorkInProgressRpt.UngroupBy("Dimension5");
                    break;
                case "GroupByDimension3":
                    if (dgWorkInProgressRpt.ItemsSource == null) return;
                    dgWorkInProgressRpt.GroupBy("Dimension3");
                    dgWorkInProgressRpt.UngroupBy("Debtor");
                    dgWorkInProgressRpt.UngroupBy("PersonInCharge");
                    dgWorkInProgressRpt.UngroupBy("Dimension1");
                    dgWorkInProgressRpt.UngroupBy("Dimension2");
                    dgWorkInProgressRpt.UngroupBy("Dimension4");
                    dgWorkInProgressRpt.UngroupBy("Dimension5");
                    break;
                case "GroupByDimension4":
                    if (dgWorkInProgressRpt.ItemsSource == null) return;
                    dgWorkInProgressRpt.GroupBy("Dimension4");
                    dgWorkInProgressRpt.UngroupBy("Debtor");
                    dgWorkInProgressRpt.UngroupBy("PersonInCharge");
                    dgWorkInProgressRpt.UngroupBy("Dimension1");
                    dgWorkInProgressRpt.UngroupBy("Dimension2");
                    dgWorkInProgressRpt.UngroupBy("Dimension3");
                    dgWorkInProgressRpt.UngroupBy("Dimension5");
                    break;
                case "GroupByDimension5":
                    if (dgWorkInProgressRpt.ItemsSource == null) return;
                    dgWorkInProgressRpt.GroupBy("Dimension5");
                    dgWorkInProgressRpt.UngroupBy("Debtor");
                    dgWorkInProgressRpt.UngroupBy("PersonInCharge");
                    dgWorkInProgressRpt.UngroupBy("Dimension1");
                    dgWorkInProgressRpt.UngroupBy("Dimension2");
                    dgWorkInProgressRpt.UngroupBy("Dimension3");
                    dgWorkInProgressRpt.UngroupBy("Dimension4");
                    break;
                case "UnGroupAll":
                    if (dgWorkInProgressRpt.ItemsSource == null) return;
                    dgWorkInProgressRpt.UngroupBy("Debtor");
                    dgWorkInProgressRpt.UngroupBy("PersonInCharge");
                    dgWorkInProgressRpt.UngroupBy("Dimension1");
                    dgWorkInProgressRpt.UngroupBy("Dimension2");
                    dgWorkInProgressRpt.UngroupBy("Dimension3");
                    dgWorkInProgressRpt.UngroupBy("Dimension4");
                    dgWorkInProgressRpt.UngroupBy("Dimension5");
                    break;
                case "Transactions":
                    if (selectedItem != null)
                        AddDockItem(TabControls.ProjectTransactionPage, selectedItem.ProjectRef, string.Format("{0} : {1}", Uniconta.ClientTools.Localization.lookup("Transactions"), selectedItem.Project));
                    break;
                case "Search":
                    LoadGrid();
                    break;
                case "CreateOrder":
                    if (selectedItem != null)
                        CreateOrder(selectedItem);
                    break;
                case "SalesOrder":
                    if (selectedItem != null)
                    {
                        var salesHeader = string.Format("{0} : {1}", Uniconta.ClientTools.Localization.lookup("SalesOrder"), selectedItem.Debtor);
                        AddDockItem(TabControls.DebtorOrders, selectedItem.ProjectRef, salesHeader);
                    }
                    break;
                case "DebtorAccount":
                    if (selectedItem != null)
                    {
                        var args = new object[2];
                        args[0] = api;
                        args[1] = selectedItem.DebtorRef.Account;
                        string header = string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("DebtorAccount"), selectedItem.DebtorRef?.Name);
                        this.AddDockItem(TabControls.DebtorAccount_lookup, args, header, null, false);
                    }
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        protected override void LoadCacheInBackGround()
        {
            var comp = api.CompanyEntity;
            LoadType(new Type[] { typeof(Uniconta.DataModel.ProjectTrans), typeof(Uniconta.DataModel.Debtor), typeof(Uniconta.DataModel.Employee), typeof(Uniconta.DataModel.Project) });
        }

        private void CreateOrder(ProjectTransLocalClient selectedItem)
        {
#if SILVERLIGHT
            var cwCreateOrder = new CWCreateOrderFromProject(api);
#else
            var cwCreateOrder = new UnicontaClient.Pages.CWCreateOrderFromProject(api);
            cwCreateOrder.DialogTableId = 2000000053;
#endif
            cwCreateOrder.Closed += async delegate
            {
                if (cwCreateOrder.DialogResult == true)
                {
                    busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("LoadingMsg");
                    busyIndicator.IsBusy = true;

                    var debtorOrderType = api.CompanyEntity.GetUserType(typeof(DebtorOrderClient)) ?? typeof(DebtorOrderClient);
                    var debtorOrderInstance = Activator.CreateInstance(debtorOrderType) as DebtorOrderClient;
                    var invoiceApi = new Uniconta.API.Project.InvoiceAPI(api);
                    var result = await invoiceApi.CreateOrderFromProject(debtorOrderInstance, selectedItem.Project, cwCreateOrder.InvoiceCategory, cwCreateOrder.GenrateDate,
                        cwCreateOrder.FromDate, cwCreateOrder.ToDate);
                    busyIndicator.IsBusy = false;
                    if (result != ErrorCodes.Succes)
                        UtilDisplay.ShowErrorCode(result);
                    else
                        ShowOrderLines(debtorOrderInstance);
                }
            };
            cwCreateOrder.Show();
        }

        private void ShowOrderLines(DebtorOrderClient order)
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

    }

    public class ProjectTransLocalClient : INotifyPropertyChanged
    {
        public int CompanyId;
        public DateTime FromDate { get; set; }

        [Display(Name = "Date", ResourceType = typeof(ProjectTransClientText)), Key]
        public DateTime Date { get; set; }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.Debtor))]
        [Display(Name = "DAccount", ResourceType = typeof(DCTransText)), Key]
        public string Debtor { get { return ProjectRef.Account; } }

        [Display(Name = "Name", ResourceType = typeof(GLTableText))]
        [NoSQL]
        public string DebtorName { get { return ClientHelper.GetName(CompanyId, typeof(Uniconta.DataModel.Debtor), Debtor); } }

        public string _Project;
        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.Project))]
        [Display(Name = "Project", ResourceType = typeof(ProjectTransClientText))]
        public string Project { get { return _Project; } }

        [Display(Name = "ProjectName", ResourceType = typeof(ProjectTransClientText))]
        [NoSQL]
        public string ProjectName { get { return ClientHelper.GetName(CompanyId, typeof(Uniconta.DataModel.Project), _Project); } }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.Employee))]
        [Display(Name = "PersonInCharge", ResourceType = typeof(ProjectTransClientText))]
        public string PersonInCharge { get { return ProjectRef.PersonInCharge; } }

        [Display(Name = "PersonInChargeName", ResourceType = typeof(ProjectTransClientText))]
        [NoSQL]
        public string PersonInChargeName { get { return ClientHelper.GetName(CompanyId, typeof(Uniconta.DataModel.Employee), PersonInCharge); } }

        [Display(Name = "EmployeeFee", ResourceType = typeof(ProjectTransClientText)), Key]
        public double EmployeeFee { get; set; }

        [Display(Name = "Expenses", ResourceType = typeof(ProjectTransClientText)), Key]
        public double Expenses { get; set; }

        [Display(Name = "Revenue", ResourceType = typeof(ProjectTransClientText)), Key]
        public double Revenue { get { return (Math.Round(EmployeeFee + Expenses, 2)); } }

        [Display(Name = "OnAccount", ResourceType = typeof(ProjectTransClientText)), Key]
        public double OnAccount { get; set; }

        [Display(Name = "Invoiced", ResourceType = typeof(ProjectTransClientText)), Key] 
        public double Invoiced { get; set; }

        [Display(Name = "TotalInvoiced", ResourceType = typeof(ProjectTransClientText)), Key]
        public double TotalInvoiced { get { return (Math.Round(OnAccount + Invoiced, 2)); } }

        [Display(Name = "Adjustment", ResourceType = typeof(ProjectTransClientText)), Key]
        public double Adjustment { get; set; }

        //[Display(Name = "BudgetQty", ResourceType = typeof(ProjectTransClientText)), Key]
        //public double BudgetQty { get; set; }

        //[Display(Name = "BudgetCost", ResourceType = typeof(ProjectTransClientText)), Key]
        //public double BudgetCost { get; set; }

        //[Display(Name = "BudgetSales", ResourceType = typeof(ProjectTransClientText)), Key]
        //public double BudgetSales { get; set; }

        [Display(Name = "OpeningBalance", ResourceType = typeof(ProjectTransClientText)), Key]
        public double OpeningBalance { get; set; }

        [Display(Name = "ClosingBalance", ResourceType = typeof(ProjectTransClientText)), Key]
        public double ClosingBalance { get { return (Math.Round(OpeningBalance + EmployeeFee + Expenses + Adjustment + OnAccount + Invoiced, 2)); } }

        public string Dimension1 { get {return ProjectRef.Dimension1; } }
        public string Dimension2 { get { return ProjectRef.Dimension2; } }
        public string Dimension3 { get { return ProjectRef.Dimension3; } }
        public string Dimension4 { get { return ProjectRef.Dimension4; } }
        public string Dimension5 { get { return ProjectRef.Dimension5; } }
        

        [ReportingAttribute]
        public ProjectClient ProjectRef
        {
            get
            {
                return ClientHelper.GetRefClient<ProjectClient>(CompanyId, typeof(Uniconta.DataModel.Project), _Project);
            }
        }

        [ReportingAttribute]
        public DebtorClient DebtorRef
        {
            get
            {
                return ClientHelper.GetRefClient<Uniconta.ClientTools.DataModel.DebtorClient>(CompanyId, typeof(Uniconta.DataModel.Debtor), ProjectRef.Account);
            }
        }

        [ReportingAttribute]
        public EmployeeClient EmployeeRef
        {
            get
            {
                return ClientHelper.GetRefClient<EmployeeClient>(CompanyId, typeof(Uniconta.DataModel.Employee), PersonInCharge);
            }
        }

        [ReportingAttribute]
        public CompanyClient CompanyRef { get { return Global.CompanyRef(CompanyId); } }

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
