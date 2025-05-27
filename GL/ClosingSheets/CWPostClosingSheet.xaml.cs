using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
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
using System.Windows;
using System.ComponentModel.DataAnnotations;
using UnicontaClient.Controls;
using Uniconta.Common.Utility;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{

    public partial class CWPostClosingSheet : ChildWindow
    {
        [InputFieldData]
        [Display(Name = "Simulation", ResourceType = typeof(InputFieldDataText))]
        public bool IsSimulation { get; set; }
        [InputFieldData]
        [Display(Name = "PostingDate", ResourceType = typeof(InputFieldDataText))]
        public DateTime PostedDate { get; set; }
        [InputFieldData]
        [Display(Name = "Comment", ResourceType = typeof(InputFieldDataText))]
        public string comments { get; set; }
        [InputFieldData]
        [Display(Name = "Code", ResourceType = typeof(InputFieldDataText))]
        public int CodeNo { get; set; }

        public byte? Code;

        protected override int DialogId { get { return DialogTableId; } }
        public int DialogTableId { get; set; }
        protected override bool ShowTableValueButton { get { return true; } }

        public CWPostClosingSheet(DateTime date, bool isDeletelines = false)
        {
            IsSimulation = true;
            this.DataContext = this;
            InitializeComponent();
            this.Title = Uniconta.ClientTools.Localization.lookup("PostClosingSheet");
            if (isDeletelines)
            {
                rwComment.Height = rwDate.Height = rwSimulation.Height = new GridLength(0);
                this.Title = string.Format(Uniconta.ClientTools.Localization.lookup("DeleteOBJ"), Uniconta.ClientTools.Localization.lookup("ClosingSheetLines"));
            }
            dpPostingDate.DateTime = date;
            this.Loaded += CW_Loaded;
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
            else
                if (e.Key == Key.Enter)
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
            PostedDate = dpPostingDate.DateTime;
            comments = txtComments.Text;
            Code = (byte)NumberConvert.ToInt(txtCode.Text);
            SetDialogResult(true);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }
    }
}

