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
namespace UnicontaClient.Pages.CustomPage
{
    public partial class CWTimePosting : ChildWindow
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

        protected override int DialogId { get { return DialogTableId; } }
        public int DialogTableId { get; set; }
        protected override bool ShowTableValueButton { get { return true; } }
        string companyName;
        public CWTimePosting(DateTime approveDate, string comment, string compName)
        {
            PostedDate = approveDate;
            this.comments = comment;
            companyName = compName;
            InitPage();
        }
        void InitPage()
        {
            IsSimulation = true;
            this.DataContext = this;
            InitializeComponent();

            this.Title = Uniconta.ClientTools.Localization.lookup("PostJournal");
            if (string.IsNullOrWhiteSpace(txtComments.Text))
                FocusManager.SetFocusedElement(txtComments, txtComments);
            dpApprovePrdPer.DateTime = PostedDate;  
            txtCompName.Text = companyName;
            this.Loaded += CW_Loaded;
        }

        public CWTimePosting()
        {
            InitPage();
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

        private void ChildWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                SetDialogResult(false);
            }
            else if (e.Key == Key.Enter)
            {
                if (OKButton.IsFocused)
                    OKButton_Click(null, null);
                else if (CancelButton.IsFocused)
                    SetDialogResult(false);
            }
        }
        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            IsSimulation = chkSimulation.IsChecked.GetValueOrDefault();
            PostedDate = dpApprovePrdPer.DateTime;
            comments = txtComments.Text;
            SetDialogResult(true);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }
    }
}

