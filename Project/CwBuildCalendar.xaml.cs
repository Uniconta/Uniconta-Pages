using System;
using System.Collections.Generic;
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
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    /// <summary>
    /// Interaction logic for CwBuildCalendar.xaml
    /// </summary>
    public partial class CwBuildCalendar : ChildWindow
    {
        public List<CalendarData> calendarDataLst;
        public CwBuildCalendar()
        {
            this.DataContext = this;
            InitializeComponent();
            this.Title = string.Format(Uniconta.ClientTools.Localization.lookup("CreateOBJ"), Uniconta.ClientTools.Localization.lookup("Calendar"));
        }

        private void btnBuild_Click(object sender, RoutedEventArgs e)
        {
            if (fromDate.DateTime.Date > toDate.DateTime.Date)
            {
                var message = string.Format(Uniconta.ClientTools.Localization.lookup("ValueMayNoBeGreater"), Uniconta.ClientTools.Localization.lookup("FromDate"), Uniconta.ClientTools.Localization.lookup("ToDate"));
                UnicontaMessageBox.Show(message, Uniconta.ClientTools.Localization.lookup("Error"));
                return;
            }
            else
            {
                CreateData();
                this.DialogResult = true;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        DateTime fromDte, toDte;
        void CreateData()
        {
            fromDte = fromDate.DateTime.Date;
            toDte = toDate.DateTime.Date;
            calendarDataLst = new List<CalendarData>();
            int i = 0;
            while (fromDte < toDte)
            {
                fromDte = fromDte.AddDays(i);
                var day = fromDte.DayOfWeek;
                calendarDataLst.Add(new CalendarData { CalenderDate = fromDte, Hours = GetHours(day) });
                i=1;
            }

            if(fromDate.DateTime.Date == toDate.DateTime.Date)
            {
                var day = fromDate.DateTime.DayOfWeek;
                calendarDataLst.Add(new CalendarData { CalenderDate = fromDate.DateTime, Hours = GetHours(day) });
            }
        }

        double GetHours(DayOfWeek day)
        {
            double hours = 0;
            if (day == DayOfWeek.Monday)
                hours = Convert.ToDouble(txtMonHrs.EditValue);
            if (day == DayOfWeek.Tuesday)
                hours = Convert.ToDouble(txtTueHrs.EditValue);
            if (day == DayOfWeek.Wednesday)
                hours = Convert.ToDouble(txtWedHrs.EditValue);
            if (day == DayOfWeek.Thursday)
                hours = Convert.ToDouble(txtThuHrs.EditValue);
            if (day == DayOfWeek.Friday)
                hours = Convert.ToDouble(txtFriHrs.EditValue);
            if (day == DayOfWeek.Saturday)
                hours = Convert.ToDouble(txtSatHrs.EditValue);
            if (day == DayOfWeek.Sunday)
                hours = Convert.ToDouble(txtSunHrs.EditValue);
            return hours;
        }
    }

    public class CalendarData
    {
        public DateTime CalenderDate { get; set; }
        public double Hours { get; set; }
    }
}
