using UnicontaClient.Models;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using System;
using DevExpress.Xpf.Grid;
using Uniconta.ClientTools.Util;
using Uniconta.ClientTools.Controls;
using UnicontaClient.Controls.Dialogs;
using Uniconta.ClientTools;
using DevExpress.Data.Filtering;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class EmployeeRegistrationLinePageGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(EmployeeRegistrationLineClient); } }
        public override bool Readonly { get { return false; } }
    }

    public partial class EmployeeRegistrationLinePage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.EmployeeRegistrationLinePage; } }
        EmployeeClient _master;
        int currentWeekIndex = 0;
        DateTime currentWeekStartDate = DateTime.MinValue;
        List<ActivityModel> chartWeekData;
        RibbonBase currentRb;
        ItemBase selectedActionMenu;
        public EmployeeRegistrationLinePage(UnicontaBaseEntity master)
            : base(master)
        {
            InitializeComponent();
            InitPage(master);
        }

        void InitPage(UnicontaBaseEntity master)
        {
            localMenu.dataGrid = dgEmployeeRegistrationLinePageGrid;
            dgEmployeeRegistrationLinePageGrid.api = api;
            _master = master as EmployeeClient;
            chartWeekData = new List<ActivityModel>(7);
            dgEmployeeRegistrationLinePageGrid.UpdateMaster(master);
            SetRibbonControl(localMenu, dgEmployeeRegistrationLinePageGrid);
            dgEmployeeRegistrationLinePageGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            currentRb = (RibbonBase)localMenu.DataContext;
            dgEmployeeRegistrationLinePageGrid.CustomUnboundColumnData += DgEmployeeRegistrationLinePageGrid_CustomUnboundColumnData;
            dgEmployeeRegistrationLinePageGrid.ItemsSourceChanged += delegate { UpdateUI(); };
            dgEmployeeRegistrationLinePageGrid.FilterChanged += delegate { UpdateUI(); };
            dgEmployeeRegistrationLinePageGrid.View.ShowFilterPanelMode = ShowFilterPanelMode.Never;
            dgEmployeeRegistrationLinePageGrid.View.AllowColumnFiltering = false;
            dgEmployeeRegistrationLinePageGrid.View.AllowFilterEditor = DevExpress.Utils.DefaultBoolean.False;
            dgEmployeeRegistrationLinePageGrid.ShowTotalSummary();
            ShowHideMenu();
            ResetChartData(DateTime.Now);
            FilterString(currentWeekIndex);
        }

        private void ShowHideMenu()
        {
            if (currentRb == null)
                return;

            if (session.Uid == _master._Uid)
            {
                UtilDisplay.RemoveMenuCommand(currentRb, "ApproveAdmin");
                selectedActionMenu = UtilDisplay.GetMenuCommandByName(currentRb, "ApproveEmployee");

            }
            else
            {
                UtilDisplay.RemoveMenuCommand(currentRb, "ApproveEmployee");
                selectedActionMenu = UtilDisplay.GetMenuCommandByName(currentRb, "ApproveAdmin");
            }
        }

        private void ResetChartData(DateTime startDate)
        {
            chartWeekData.Clear();
            for (int i = 0; i < 7; i++)
                chartWeekData.Add(new ActivityModel() { Date = startDate.AddDays(i), AmountOfWork = 0.0d });

            chartControl.DataSource = chartWeekData;
            chartControl.UpdateData();
        }

        private void DgEmployeeRegistrationLinePageGrid_CustomUnboundColumnData(object sender, GridColumnDataEventArgs e)
        {
            if (e.IsGetData)
            {
                var startTime = Convert.ToDateTime(e.GetListSourceFieldValue("FromTime"));
                var endTime = Convert.ToDateTime(e.GetListSourceFieldValue("ToTime"));
                e.Value = endTime - startTime;
            }
        }

        private void FilterString(int increaseWeek)
        {
            var stDate = GetCurrentWeekStartDate(DateTime.Now);
            stDate = stDate.AddDays(increaseWeek * 7);
            var enDate = stDate.AddDays(7);
            currentWeekStartDate = stDate;

            ResetChartData(stDate);
            dgEmployeeRegistrationLinePageGrid.FilterCriteria = (new BinaryOperator("FromTime", stDate, BinaryOperatorType.GreaterOrEqual) &
                 new BinaryOperator("ToTime", enDate, BinaryOperatorType.LessOrEqual));
        }

        private void UpdateUI()
        {
            try
            {
                busyIndicator.IsBusy = true;
                //Update Counts
                var records = dgEmployeeRegistrationLinePageGrid.GetVisibleRows() as IEnumerable<EmployeeRegistrationLineClient>;
                var isReopenAdm = records.Any(p => p.ApprovedAdm == true);
                var employeeRegActivities = EmployeeActivityRegistrationHelper.GetDistinctListWithSumHours(records.ToList());
                var totalHrs = employeeRegActivities.Sum(p => p.AmountOfWork);

                txtDuration.Text = $"{(int)(totalHrs / 60)}{Uniconta.ClientTools.Localization.lookup("HR")} {totalHrs % 60}{Uniconta.ClientTools.Localization.lookup("Min")}";
                var weekNum = UtilFunctions.GetIso8601WeekOfYear(currentWeekStartDate); 
                txtWeekNum.Text = string.Concat(weekNum, " ", Uniconta.ClientTools.Localization.lookup("Week"));
                txtWeekPeriod.Text = string.Format("{0} - {1}", currentWeekStartDate.ToString("dd MMM"), currentWeekStartDate.AddDays(6).ToString("dd MMM"));

                //Update Charts
                var employeeRegistrationLines = EmployeeActivityRegistrationHelper.GetActivitiesByDateRange(currentWeekStartDate, currentWeekStartDate.AddDays(6),
                    records.ToList());
                var distinctListWithSumHours = EmployeeActivityRegistrationHelper.GetDistinctListWithSumHours(employeeRegistrationLines);
                var distinctActivities = new ObservableCollection<ActivityModel>(distinctListWithSumHours.Select(x => new ActivityModel() { Date = x.Date, AmountOfWork = x.AmountOfWork }));

                for (int i = 0; i < chartWeekData.Count; i++)
                {
                    var defActivity = chartWeekData[i];
                    var distActivity = distinctActivities.Where(p => p.Date == defActivity.Date).FirstOrDefault();

                    if (distActivity.IsDefault())
                        defActivity.AmountOfWork = 0.0d;
                    else
                        defActivity.AmountOfWork = distActivity.AmountOfWork;

                    chartWeekData[i] = defActivity;
                }

                if (session.Uid == _master._Uid)
                {
                    selectedActionMenu.Caption = Uniconta.ClientTools.Localization.lookup(records.Any(p => p.ApprovedEmp == true) ? "ReopenWeek" : "ApproveWeek");
                    if (records.Any(p => p.ApprovedAdm == true))
                        ribbonControl.DisableButtons("ApproveEmployee");
                    else
                        ribbonControl.EnableButtons("ApproveEmployee");
                }
                else
                    selectedActionMenu.Caption = Uniconta.ClientTools.Localization.lookup(records.Any(p => p.ApprovedAdm == true) ? "ReopenWeek" : "ApproveWeek");

                chartControl.DataSource = chartWeekData;
                chartControl.UpdateData();
                busyIndicator.IsBusy = false;
            }
            catch
            {
                busyIndicator.IsBusy = false;
            }
        }

        private DateTime GetCurrentWeekStartDate(DateTime dateTime)
        {
            var currentWeekOfYear = UtilFunctions.GetIso8601WeekOfYear(dateTime);

            DateTime jan1 = new DateTime(dateTime.Year, 1, 1);
            int daysOffset = DayOfWeek.Monday - jan1.DayOfWeek;
            DateTime firstMonday = jan1.AddDays(daysOffset);
            var firstWeek = UtilFunctions.GetIso8601WeekOfYear(jan1);
            if (firstWeek <= 1)
                currentWeekOfYear -= 1;

            return firstMonday.AddDays(currentWeekOfYear * 7);
        }

        protected override void OnLayoutLoaded()
        {
            if (!api.CompanyEntity.Project)
                UtilDisplay.RemoveMenuCommand((RibbonBase)localMenu.DataContext, new string[] { "PrTransaction" });
            base.OnLayoutLoaded();
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgEmployeeRegistrationLinePageGrid.SelectedItem as EmployeeRegistrationLineClient;
            switch (ActionType)
            {
                case "AddRow":
                    AddActivity();
                    break;
                case "EditRow":
                    EditActivity();
                    break;
                case "SaveGrid":
                    saveGrid();
                    break;
                case "DeleteRow":
                    dgEmployeeRegistrationLinePageGrid.DeleteRow();
                    UpdateUI();
                    break;
                case "PrTransaction":
                    if (selectedItem != null && selectedItem._LineNumber != 0)
                        AddDockItem(TabControls.ProjectTransactionPage, dgEmployeeRegistrationLinePageGrid.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Transactions"), selectedItem._Employee));
                    break;
                case "ApproveEmployee":
                    Approve(true);
                    break;
                case "ApproveAdmin":
                    Approve(false);
                    break;
                case "TransactionsReport":
                    var Parameters = new List<BasePage.ValuePair> { new BasePage.ValuePair("Dashboard", "work_db_report user") };
                    AddDockItem(TabControls.DashBoardViewerPage, null, string.Concat(Uniconta.ClientTools.Localization.lookup("Dashboard"), ": ", "work_db_report user"), null, true, null, Parameters);
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        private void Approve(bool isEmployee)
        {
            var visibleRows = dgEmployeeRegistrationLinePageGrid.GetVisibleRows();
            var isReopen = selectedActionMenu.Caption != Uniconta.ClientTools.Localization.lookup("ApproveWeek");
            List<UnicontaBaseEntity> updates = new List<UnicontaBaseEntity>(visibleRows.Count);
            foreach (EmployeeRegistrationLineClient row in visibleRows)
            {
                if (isEmployee)
                    row.ApprovedEmp = isReopen ? false : true;
                else
                    row.ApprovedEmp = row.ApprovedAdm = isReopen ? false : true;

                updates.Add(row);
            }

            if (isReopen)
                selectedActionMenu.Caption = Uniconta.ClientTools.Localization.lookup("ApproveWeek");
            else
                selectedActionMenu.Caption = Uniconta.ClientTools.Localization.lookup("ReopenWeek");

            api.UpdateNoResponse(updates);
        }

        private void AddActivity()
        {
            var cwAddActivity = new CWActivityDialog(currentWeekStartDate);
            cwAddActivity.Closed += async delegate
            {
                if (cwAddActivity.DialogResult == false)
                    return;

                var activityrecordBreaker = EmployeeActivityRegistrationHelper.BreakActivityLineForNew(cwAddActivity.StartDate, cwAddActivity.ToDate, cwAddActivity.StartTime,
                    cwAddActivity.EndTime, cwAddActivity.Category, cwAddActivity.Comment, _master as Uniconta.DataModel.Employee);

                if (activityrecordBreaker?.InsertList == null || activityrecordBreaker.InsertList.Count == 0)
                    return;

                var result = await api.Insert(activityrecordBreaker.InsertList);

                if (result != ErrorCodes.Succes)
                {
                    UtilDisplay.ShowErrorCode(result);
                    return;
                }

                foreach (var rec in activityrecordBreaker.InsertList)
                {
                    var empRegLineClientUser = api.CompanyEntity.CreateUserType<EmployeeRegistrationLineClient>();
                    StreamingManager.Copy(rec, empRegLineClientUser);
                    dgEmployeeRegistrationLinePageGrid.UpdateItemSource(1, empRegLineClientUser);
                }

                UpdateUI();
            };
            cwAddActivity.ShowDialog();
        }

        private void EditActivity()
        {
            var selectedItem = dgEmployeeRegistrationLinePageGrid.SelectedItem as EmployeeRegistrationLineClient;
            var cwAddActivity = new CWActivityDialog(selectedItem);
            cwAddActivity.Closed += async delegate
            {
                if (cwAddActivity.DialogResult == false)
                    return;

                var activityrecordBreaker = EmployeeActivityRegistrationHelper.BreakActivityLineForModify(selectedItem, cwAddActivity.StartDate, cwAddActivity.ToDate, cwAddActivity.StartTime,
                    cwAddActivity.EndTime, cwAddActivity.Category, cwAddActivity.Comment, selectedItem.EmployeeRef);

                ErrorCodes result = ErrorCodes.NoSucces;
                if (activityrecordBreaker?.UpdateList != null && activityrecordBreaker.UpdateList.Count != 0)
                    result = await api.Update(activityrecordBreaker.UpdateList);

                if (result != ErrorCodes.Succes)
                {
                    UtilDisplay.ShowErrorCode(result);
                    return;
                }
                foreach (var rec in activityrecordBreaker.UpdateList)
                {
                    StreamingManager.Copy(rec, selectedItem);
                    dgEmployeeRegistrationLinePageGrid.UpdateItemSource(2, selectedItem);
                }

                if (activityrecordBreaker?.InsertList != null && activityrecordBreaker.InsertList.Count != 0)
                    result = await api.Insert(activityrecordBreaker.InsertList);

                if (result != ErrorCodes.Succes)
                {
                    UtilDisplay.ShowErrorCode(result);
                    return;
                }

                foreach (var rec in activityrecordBreaker.InsertList)
                {
                    var empRegLineClientUser = api.CompanyEntity.CreateUserType<EmployeeRegistrationLineClient>();
                    StreamingManager.Copy(rec, empRegLineClientUser);
                    dgEmployeeRegistrationLinePageGrid.UpdateItemSource(1, empRegLineClientUser);
                }
                UpdateUI();
            };
            cwAddActivity.ShowDialog();
        }

        private void prevBtn_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            --currentWeekIndex;
            FilterString(currentWeekIndex);
        }

        private void nxtBtn_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ++currentWeekIndex;
            FilterString(currentWeekIndex);
        }

        //private void dgEmployeeRegistrationLinePageGrid_CustomSummary(object sender, DevExpress.Data.CustomSummaryEventArgs e)
        //{
        //    if (e.Item is GridSummaryItem summaryItem && summaryItem.FieldName == "TotalTime")
        //    {
        //        if (e.SummaryProcess == CustomSummaryProcess.Start)
        //        {
        //            e.TotalValue = TimeSpan.Zero;
        //        }
        //        else if (e.SummaryProcess == CustomSummaryProcess.Calculate)
        //        {
        //            if (e.Row is EmployeeRegistrationLineClient empReg && empReg._Activity != Uniconta.DataModel.InternalType.Pause)
        //                e.TotalValue = ((TimeSpan)e.TotalValue) + (TimeSpan)e.FieldValue;
        //        }
        //    }
        //}
    }
}
