using System;
using System.ComponentModel.DataAnnotations;
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.Common;
using Uniconta.DataModel;

namespace UnicontaClient.Controls.Dialogs
{
    public partial class CWAddGLAccountInterest : ChildWindow
    {
        public DateTime FromDate { get { return _startDate; } set { _startDate = value; NotifyPropertyChanged(nameof(FromDate));} }
        public DateTime ToDate { get { return _endDate; } set { _endDate = value; NotifyPropertyChanged(nameof(ToDate)); } }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.GLDailyJournal))]
        public string Journal { get { return _Journal; } set { _Journal = value; } }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.GLTransType))]
        public string TextType { get { return _TextType; } set { _TextType = value; } }

        protected override int DialogId { get { return DialogTableId; } }
        public int DialogTableId { get; set; } = 2000000109;
        protected override bool ShowTableValueButton { get { return false; } }

        static DateTime _startDate, _endDate;
        static string _Journal, _TextType;
        public CWAddGLAccountInterest(CrudAPI api)
        {
            this.DataContext = this;
            if (_startDate == DateTime.MinValue)
            {
                var d = DateTime.Now.AddMonths(-1);
                _startDate = d.AddDays(1 - d.Day);
                _endDate = _startDate.AddDays(DateTime.DaysInMonth(d.Year, d.Month) - 1);
            }
            InitializeComponent();
            lookupJournal.api = api;
            lookupText.api = api;
            Title = Uniconta.ClientTools.Localization.lookup("InterestGroup");
            api.LoadCache(typeof(Uniconta.DataModel.GLDailyJournal));
            api.LoadCache(typeof(Uniconta.DataModel.GLAccount));
            api.LoadCache(typeof(Uniconta.DataModel.GLInterestGroup));
            api.LoadCache(typeof(Uniconta.DataModel.GLTransType));
        }

        private void CancelButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void OKButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(Journal))
            {
                UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("MandatoryField"), (Uniconta.ClientTools.Localization.lookup("Journal"))), Uniconta.ClientTools.Localization.lookup("Warning"));
                return;
            }
            DialogResult = true;
        }
    }
}
