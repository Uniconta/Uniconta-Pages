using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Uniconta.API.Service;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;
using UnicontaClient.Models;
using UnicontaClient.Utilities;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class TMJournalLineStartStopGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(ProjectJournalLineLocal); } }
        public override IComparer GridSorting { get { return new ProjectJournalLineSort(); } }
        public override bool Readonly { get { return true; } }
        public override bool IsAutoSave { get { return false; } }
        public bool IsTime { get; set; }
    }
    public partial class TMJournalLineStartStop : GridBasePage
    {
        SQLCache PayrollCache;
        Uniconta.DataModel.Employee Employee;
        CompanySettingsClient companySettings;
        Company company;
        PrJournal projectJournal;
        public TMJournalLineStartStop(BaseAPI API)
            : base(API, string.Empty)
        {
            InitPage();
        }
        void InitPage()
        {
            InitializeComponent();
            dgJournalLineStartStopPageGrid.IsTime = true;
            dgJournalLineStartStopPageGrid.api = api;
            company=api.CompanyEntity;
            dgJournalLineStartStopPageGrid.BusyIndicator = busyIndicator;
            SetRibbonControl(localMenu, dgJournalLineStartStopPageGrid);
            localMenu.OnItemClicked += localMenu_OnItemClicked;
        }

        public override bool CheckIfBindWithUserfield(out bool isReadOnly, out bool useBinding)
        {
            isReadOnly=false;
            useBinding=false;
            return false;
        }

        public override bool IsCalculatedFieldsToBeHandled()
        {
            return false;
        }

        public override void SetParameter(IEnumerable<ValuePair> Parameters)
        {
            string employee = null;
            string prJournal = null;
            foreach (var rec in Parameters)
            {
                if (string.Compare(rec.Name, "Employee", StringComparison.CurrentCultureIgnoreCase) == 0)
                    employee = rec.Value;
                if (string.Compare(rec.Name, "ProjectJournal", StringComparison.CurrentCultureIgnoreCase) == 0)
                    prJournal = rec.Value;
            }
            var hdrString = new StringBuilder();
            if (employee != null)
            {
                var cache = api.GetCache(typeof(Uniconta.DataModel.Employee)) ?? api.LoadCache(typeof(Uniconta.DataModel.Employee)).GetAwaiter().GetResult();
                Employee = (Uniconta.DataModel.Employee)cache.Get(employee);
                hdrString.Append(Uniconta.ClientTools.Localization.lookup("StartTimeRegistration")).Append(" : " + employee + "/");
            }
            if (prJournal != null)
            {
                var cache = api.GetCache(typeof(Uniconta.DataModel.PrJournal)) ?? api.LoadCache(typeof(Uniconta.DataModel.PrJournal)).GetAwaiter().GetResult();
                projectJournal = (Uniconta.DataModel.PrJournal)cache.Get(prJournal);
                hdrString.Append(prJournal);
                SetHeader(hdrString.ToString());
            }
            base.SetParameter(Parameters);
        }

        public override Task InitQuery()
        {
            LoadGrid();
            return null;
        }

        async void LoadGrid()
        {
            busyIndicator.IsBusy = true;
            var masters = new List<UnicontaBaseEntity> { projectJournal };
            var prJrnLineLst = await api.Query<ProjectJournalLineLocal>(masters, null);
            dgJournalLineStartStopPageGrid.ItemsSource = prJrnLineLst?.Where(x => x.Qty == 0 && x.TimeTo.TimeOfDay.ToString() == "00:00:00" && !string.IsNullOrEmpty(x.Text) && !x.Text.Contains("Text:")).ToList();
            dgJournalLineStartStopPageGrid.Visibility = Visibility.Visible;
            busyIndicator.IsBusy = false;
        }

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.TmJournalLineStartStopPage2)
                dgJournalLineStartStopPageGrid.UpdateItemSource(argument);
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgJournalLineStartStopPageGrid.SelectedItem as ProjectJournalLineLocal;
            switch (ActionType)
            {
                case "AddRow":
                    {
                        var newItem = new ProjectJournalLineLocal();
                        newItem.SetMaster(projectJournal);
                        AddDockItem(TabControls.TmJournalLineStartStopPage2, new object[] { newItem, false, true }, Uniconta.ClientTools.Localization.lookup("StartTimeRegistration"), "Add_16x16.png");
                    }
                    break;
                case "EditRow":
                    if (selectedItem != null)
                        AddDockItem(TabControls.TmJournalLineStartStopPage2, new object[] { selectedItem, true , true }, string.Format("{0}: {1}/{2}", Uniconta.ClientTools.Localization.lookup("StartTimeRegistration"), Employee?._Number, projectJournal?.KeyStr));
                    break;
                case "Stop":
                    if (selectedItem!= null)
                        Stop(selectedItem);
                    break;
                case "RefreshGrid":
                    InitQuery();
                    return;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        async void Stop(ProjectJournalLineLocal selectedItem)
        {
            TimeToRounding(selectedItem);
            selectedItem.Qty = (selectedItem.TimeTo - selectedItem.TimeFrom).TotalMinutes;
            if (company.TimeManagement)
            {
                TMJournalLineClient tmJournalLine = new TMJournalLineClient
                {
                    Text = selectedItem.Text,
                    Project = selectedItem.Project,
                    PayrollCategory = selectedItem.PayrollCategory,
                    Task = selectedItem.Task,
                    WorkSpace = selectedItem.WorkSpace
                };

                tmJournalLine.SetMaster(Employee);

                if(dgJournalLineStartStopPageGrid.IsTime)
                    tmJournalLine._RegistrationType = RegistrationType.Hours;

                if (tmJournalLine.PayrollCategoryRef != null)
                    tmJournalLine.PayrollCategoryRef.PrCategory = selectedItem.PrCategory;

                tmJournalLine.Date = DateTime.Today;
                GetHours(selectedItem, tmJournalLine);

                var result = await api.Insert(tmJournalLine);

                if (result == ErrorCodes.Succes)
                {
                    var res = await api.Delete(selectedItem);
                    if (res == ErrorCodes.Succes)
                        InitQuery();
                    else
                        UtilDisplay.ShowErrorCode(res);
                }
                else
                    UtilDisplay.ShowErrorCode(result);
            }
            else
            {
                var res = await api.Update(selectedItem);
                if (res== ErrorCodes.Succes)
                    InitQuery();
                else
                    UtilDisplay.ShowErrorCode(res);
            }

        }

        void GetHours(ProjectJournalLineLocal PrJrLine, TMJournalLineClient tmJrLine)
        {
            TimeSpan ts = PrJrLine.TimeTo - PrJrLine.TimeFrom;
            var weekDay = (int)DateTime.Now.DayOfWeek;
            switch (weekDay)
            {
                case 0:
                    tmJrLine._Day7 =  ts.TotalHours;
                    break;
                case 1:
                    tmJrLine._Day1 =  ts.TotalHours;
                    break;
                case 2:
                    tmJrLine._Day2 =  ts.TotalHours;
                    break;
                case 3:
                    tmJrLine._Day3 =  ts.TotalHours;
                    break;
                case 4:
                    tmJrLine._Day4 =  ts.TotalHours;
                    break;
                case 5:
                    tmJrLine._Day5 =  ts.TotalHours;
                    break;
                case 6:
                    tmJrLine._Day6 =  ts.TotalHours;
                    break;

            }
        }

         void TimeToRounding(ProjectJournalLineLocal lineClient)
        {
            var roundingEnum = AppEnums.TMRounding.IndexOf(companySettings.RoundingStop);
            switch (roundingEnum)
            {
                case 0:
                case 1:
                case 2:
                case 3:
                    lineClient.TimeTo = Utility.RoundUp(roundingEnum, DateTime.Now);
                    break;
                case 4:
                case 5:
                case 6:
                case 7:
                    lineClient.TimeTo = Utility.RoundDown(roundingEnum, DateTime.Now);
                    break;
            }
        }

        protected override async void LoadCacheInBackGround()
        {
            PayrollCache = company.GetCache(typeof(Uniconta.DataModel.EmpPayrollCategory)) ?? await company.LoadCache(typeof(Uniconta.DataModel.EmpPayrollCategory), api).ConfigureAwait(false);
            var settings = await api.Query<CompanySettingsClient>();
            companySettings = settings.FirstOrDefault();
        }
        
    }
}
