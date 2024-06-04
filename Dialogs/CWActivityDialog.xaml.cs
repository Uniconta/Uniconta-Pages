using System;
using System.ComponentModel.DataAnnotations;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.Common;

namespace UnicontaClient.Controls.Dialogs
{
    /// <summary>
    /// Interaction logic for CWActivityDialog.xaml
    /// </summary>
    public partial class CWActivityDialog : ChildWindow
    {
        [InputFieldData]
        [Display(Name = "Category", ResourceType = typeof(InputFieldDataText))]
        public string Category { get; set; }

        [InputFieldData]
        [Display(Name = "StartDate", ResourceType = typeof(InputFieldDataText))]
        public DateTime StartDate { get { return _startDate; } set { _startDate = value; NotifyPropertyChanged(nameof(StartDate)); NotifyPropertyChanged(nameof(TotalTime)); } }

        [InputFieldData]
        [Display(Name = "ToDate", ResourceType = typeof(InputFieldDataText))]
        public DateTime ToDate { get { return _endDate; } set { _endDate = value; NotifyPropertyChanged(nameof(ToDate)); NotifyPropertyChanged(nameof(TotalTime)); } }

        [InputFieldData]
        [Display(Name = "StartTime", ResourceType = typeof(InputFieldDataText))]
        public TimeSpan StartTime { get { return _startTime; } set { _startTime = value; NotifyPropertyChanged(nameof(StartTime)); NotifyPropertyChanged(nameof(TotalTime)); } }

        [InputFieldData]
        [Display(Name = "ToTime", ResourceType = typeof(InputFieldDataText))]
        public TimeSpan EndTime { get { return _endTime; } set { _endTime = value; NotifyPropertyChanged(nameof(EndTime)); NotifyPropertyChanged(nameof(TotalTime)); } }

        [InputFieldData]
        [Display(Name = "Comment", ResourceType = typeof(InputFieldDataText))]
        public string Comment { get; set; }

        public TimeSpan TotalTime { get { return EmployeeActivityRegistrationHelper.CalculateTotalTime(_startDate, _endDate, _startTime, _endTime); } }

        protected override int DialogId { get { return DialogTableId; } }
        public int DialogTableId { get; set; } = 2000000108;
        protected override bool ShowTableValueButton { get { return true; } }

        private DateTime _startDate, _endDate;
        private TimeSpan _endTime, _startTime;
        private EmployeeRegistrationLineClient employeeRegistrationLineClient;

        public CWActivityDialog()
        {
            this.DataContext = this;
            InitializeComponent();
            Title = string.Format(Uniconta.ClientTools.Localization.lookup("AddOBJ"), Uniconta.ClientTools.Localization.lookup("Activity"));
        }

        public CWActivityDialog(UnicontaBaseEntity entity) : this()
        {
            if (entity is EmployeeRegistrationLineClient employeeRegistrationLineClient)
            {
                Category = employeeRegistrationLineClient.Activity;
                StartDate = employeeRegistrationLineClient.FromTime.Date;
                ToDate = employeeRegistrationLineClient.ToTime.Date;
                StartTime = new TimeSpan(employeeRegistrationLineClient.FromTime.Ticks);
                EndTime = new TimeSpan(employeeRegistrationLineClient.ToTime.Ticks);
                Comment = employeeRegistrationLineClient.Text;
            }

            Title = string.Format(Uniconta.ClientTools.Localization.lookup("EditOBJ"), Uniconta.ClientTools.Localization.lookup("Activity"));
            NotifyPropertyChanged(nameof(Category));
            NotifyPropertyChanged(nameof(Comment));
        }

        private void CancelButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void OKButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
