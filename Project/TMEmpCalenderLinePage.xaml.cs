using UnicontaClient.Models;
using UnicontaClient.Pages;
using DevExpress.Xpf.Grid;
using DevExpress.XtraGrid;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
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
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class TMEmpCalendarLinePageGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(TMEmpCalendarLineClientLocal); } }
        public override bool Readonly { get { return false; } }
    }

    public partial class TMEmpCalendarLinePage : GridBasePage
    {
        UnicontaBaseEntity master;
        public override string NameOfControl { get { return TabControls.TMEmpCalendarLinePage; } }
        public TMEmpCalendarLinePage(UnicontaBaseEntity master)
            : base(master)
        {
            InitializeComponent();
            this.master = master;
            dgTMEmpCalendarLineGrid.UpdateMaster(master);
            localMenu.dataGrid = dgTMEmpCalendarLineGrid;
            SetRibbonControl(localMenu, dgTMEmpCalendarLineGrid);
            dgTMEmpCalendarLineGrid.api = api;
            dgTMEmpCalendarLineGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgTMEmpCalendarLineGrid.ShowTotalSummary();
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgTMEmpCalendarLineGrid.SelectedItem as TMEmpCalendarLineClientLocal;
            switch (ActionType)
            {
                case "AddRow":
                    dgTMEmpCalendarLineGrid.AddRow();
                    break;
                case "CopyRow":
                    dgTMEmpCalendarLineGrid.CopyRow();
                    break;
                case "SaveGrid":
                    saveGrid();
                    break;
                case "DeleteRow":
                    if (selectedItem != null)
                        dgTMEmpCalendarLineGrid.DeleteRow();
                    break;
                case "BuildCalendar":
                    BuildCalender();
                    break;
                case "DayView":
                    {
                        dgTMEmpCalendarLineGrid.UngroupBy("Month");
                        dgTMEmpCalendarLineGrid.UngroupBy("Week");
                    }
                    break;
                case "WeekView":
                    {
                        dgTMEmpCalendarLineGrid.UngroupBy("Month");
                        dgTMEmpCalendarLineGrid.GroupBy("Week");
                    }
                    break;
                case "MonthView":
                    {
                        dgTMEmpCalendarLineGrid.UngroupBy("Week");
                        dgTMEmpCalendarLineGrid.GroupBy("Month");
                    }
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
        bool DataChaged;
        public override bool IsDataChaged { get { return DataChaged || base.IsDataChaged; } }

        protected override Task<ErrorCodes> saveGrid()
        {
            var t = base.saveGrid();
           
            if (master != null)
            {
                var calendar = master as Uniconta.DataModel.TMEmpCalendar;
                if (calendar != null)
                    calendar.CalendarLines = null;
            }

            return t;
        }


        void BuildCalender()
        {
            CwBuildCalendar cw = new CwBuildCalendar();
            cw.Closing += delegate
            {
                if (cw.DialogResult == true)
                {
                    var tempCalenderLst = dgTMEmpCalendarLineGrid.GetVisibleRows();
                    if (tempCalenderLst?.Count == 0)
                    {
                        foreach (var data in cw.calendarDataLst)
                        {
                            string bankHolidayName = null;
#if !SILVERLIGHT
                            bankHolidayName = Uniconta.DirectDebitPayment.Common.BankHolidayName(api.CompanyEntity._CountryId, data.CalenderDate);
#endif
                            var line = new TMEmpCalendarLineClientLocal();
                            line._Date = data.CalenderDate;
                            line._Hours = bankHolidayName == null ? data.Hours : 0;
                            line._Description = bankHolidayName;
                            dgTMEmpCalendarLineGrid.AddRow(line, -1, false);
                        }
                    }
                    else
                    {
                        foreach (var data in cw.calendarDataLst)
                        {
                            foreach (TMEmpCalendarLineClientLocal row in tempCalenderLst)
                            {
                                if (row.Date.Equals(data.CalenderDate))
                                {
                                    string bankHolidayName = null;
#if !SILVERLIGHT
                                    bankHolidayName = Uniconta.DirectDebitPayment.Common.BankHolidayName(api.CompanyEntity._CountryId, data.CalenderDate);
#endif
                                    row.Hours = bankHolidayName == null ? data.Hours : 0;
                                    row.NotifyPropertyChanged("Hours");
                                    row.Description = bankHolidayName;
                                    row.NotifyPropertyChanged("Description");

                                    dgTMEmpCalendarLineGrid.SetLoadedRow(row);
                                    DataChaged = true;
                                }
                            }

                            var lst= tempCalenderLst as IEnumerable<TMEmpCalendarLineClientLocal>;
                            var itemNotExist = lst?.Where(x => x._Date == data.CalenderDate).FirstOrDefault();
                            if (itemNotExist == null)
                            {
                                string bankHolidayName = null;
#if !SILVERLIGHT
                                    bankHolidayName = Uniconta.DirectDebitPayment.Common.BankHolidayName(api.CompanyEntity._CountryId, data.CalenderDate);
#endif
                                var line = new TMEmpCalendarLineClientLocal();
                                line._Date = data.CalenderDate;
                                line._Hours = bankHolidayName == null ? data.Hours : 0;
                                line._Description = bankHolidayName;
                                dgTMEmpCalendarLineGrid.AddRow(line, -1, false);
                            }
                        }
                    }
                }
            };
            cw.Show();
        }
    }

    public class TMEmpCalendarLineClientLocal : TMEmpCalendarLineClient
    {
        [Display(Name = "Week", ResourceType = typeof(EmployeeText))]
        public int Week
        {
            get
            {
                var cul = CultureInfo.CurrentCulture;
                var week = cul.Calendar.GetWeekOfYear(_Date, CalendarWeekRule.FirstDay, DayOfWeek.Monday);
                return week;
            }
        }

        [Display(Name = "Month", ResourceType = typeof(EmployeeText))]
        public int Month
        {
            get
            {
                return _Date.Month;
            }
        }

        [Display(Name = "Day", ResourceType = typeof(EmployeeText))]
        public string Day
        {
            get
            {
                if (_Date != null)
                    return Uniconta.ClientTools.Localization.lookup(_Date.DayOfWeek.ToString());
                else
                    return null;
            }
        }
    }
}
