using Uniconta.API.System;
using Uniconta.ClientTools.DataModel;
using Uniconta.Common;
using Uniconta.DataModel;
using Uniconta.API.GeneralLedger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Uniconta.ClientTools.Controls;
using UnicontaClient.Models;
using UnicontaClient.Utilities;
using Uniconta.ClientTools;
using System.Windows;
using Uniconta.ClientTools.Page;
using System.ComponentModel.DataAnnotations;
using UnicontaClient.Controls;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.ChartOfAccount.Reports
{
    public partial class CWPosting : ChildWindow
    {
        [InputFieldData]
        [Display(Name = "Simulation", ResourceType = typeof(InputFieldDataText))]
        public bool IsSimulation { get; set; }
        [InputFieldData]
        [Display(Name = "PostedDate", ResourceType = typeof(InputFieldDataText))]
        public DateTime PostedDate { get; set; }
        [InputFieldData]
        [Display(Name = "Comment", ResourceType = typeof(InputFieldDataText))]
        public string comments { get; set; }

#if !SILVERLIGHT
        protected override int DialogId { get { return DialogTableId; } }
        public int DialogTableId { get; set; }
        protected override bool ShowTableValueButton { get { return true; } }
#endif
        string companyName;
        public CWPosting(Uniconta.DataModel.GLDailyJournal journal, string compName)
        {
            companyName = compName;
            InitPage(journal);
        }
        void InitPage(object objJournal)
        {
            IsSimulation = true;
            this.DataContext = this;
            InitializeComponent();

#if !SILVERLIGHT
            this.Title = Uniconta.ClientTools.Localization.lookup("PostJournal");
            if (string.IsNullOrWhiteSpace(txtComments.Text))
                FocusManager.SetFocusedElement(txtComments, txtComments);
#else
            Utility.SetThemeBehaviorOnChildWindow(this);
#endif
            dpPostingDate.DateTime = BasePage.GetSystemDefaultDate().Date;  // just need the date and it should NOT be convert to univesal time
            if (objJournal is Uniconta.DataModel.GLDailyJournal)
            {
                if (((Uniconta.DataModel.GLDailyJournal)objJournal)._DateFunction == GLJournalDate.Fixed)
                    tbPostingDate.Visibility = dpPostingDate.Visibility = Visibility.Visible;
            }
            else if (objJournal is Uniconta.DataModel.PrJournal)
            {
                if (((Uniconta.DataModel.PrJournal)objJournal)._DateFunction == GLJournalDate.Fixed)
                    tbPostingDate.Visibility = dpPostingDate.Visibility = Visibility.Visible;

            }
            txtCompName.Text = companyName;
            this.Loaded += CW_Loaded;
        }

        public CWPosting(Uniconta.DataModel.PrJournal journal)
        {
            InitPage(journal);
        }

        void CW_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => 
            {
                if (string.IsNullOrWhiteSpace(txtComments.Text))
                    txtComments.Focus();
                else
                    OKButton.Focus();
            }));
        }

        private void ChildWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.DialogResult = false;
            }
            else if (e.Key == Key.Enter)
            {
                if (OKButton.IsFocused)
                    OKButton_Click(null, null);
                else if (CancelButton.IsFocused)
                    this.DialogResult = false;
            }
        }
        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            IsSimulation = chkSimulation.IsChecked.Value;
            PostedDate = dpPostingDate.DateTime;
            comments = txtComments.Text;
            this.DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}

