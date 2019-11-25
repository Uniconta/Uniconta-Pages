using System;
using System.Collections.Generic;
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
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.Common.Utility;
using UnicontaClient.Utilities;
using Uniconta.ClientTools.Page;
using UnicontaClient.Controls;
using System.ComponentModel.DataAnnotations;
using Uniconta.Common;
using Uniconta.DataModel;
using static UnicontaClient.Pages.Project.TimeManagement.TMJournalLineHelper;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class CwSetPeriodPerDate : ChildWindow
    {
        public DateTime StartDate{get;set;}

        [InputFieldData]
        [ForeignKeyAttribute(ForeignKeyTable = typeof(PrJournal))]
        [Display(Name = "Journal", ResourceType = typeof(InputFieldDataText))]
        public string ProjectJournal { get; set; }

#if !SILVERLIGHT
        public int DialogTableId;
        protected override int DialogId { get { return DialogTableId; } }
        protected override bool ShowTableValueButton { get { return true; } }
#endif

        public CwSetPeriodPerDate(TMJournalActionType actionType, DateTime startDate, CrudAPI api)
        {
            InitializeComponent();
            if (actionType == TMJournalActionType.Approve || actionType == TMJournalActionType.Close)
                StartDate = startDate.AddDays(6);
            else
                StartDate = startDate;
            this.DataContext = this;
            if (actionType != TMJournalActionType.Approve)
            {
                showJournal.Height = new GridLength(0);
                double h = this.Height - 30;
                this.Height = h;
                lblJournal.Visibility = Visibility.Collapsed;
                lejournal.Visibility = Visibility.Collapsed;
            }
            lejournal.api = api;
            var lblStr = string.Empty;
            if (actionType == TMJournalActionType.Open)
                lblStr = Uniconta.ClientTools.Localization.lookup("OpenPeriodPer");
            else if (actionType == TMJournalActionType.Close)
                lblStr = Uniconta.ClientTools.Localization.lookup("ClosePeriodPer");
            else
                lblStr = Uniconta.ClientTools.Localization.lookup("ApprovePeriodPer");
            this.Title = lblDate.Text = lblStr;
            this.Loaded += CW_Loaded;
        }

        private void CW_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => { btnBuild.Focus(); }));
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}
