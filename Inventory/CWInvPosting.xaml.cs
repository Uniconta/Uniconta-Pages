using Uniconta.API.System;
using Uniconta.Common;
using Uniconta.DataModel;
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
using Uniconta.ClientTools;
using System.Windows;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.DataModel;
using System.ComponentModel.DataAnnotations;
using UnicontaClient.Controls;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class CWInvPosting : ChildWindow
    {
        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.NumberSerie))]
        [InputFieldData]
        [Display(Name = "NumberSerie", ResourceType = typeof(InputFieldDataText))]
        public string NumberSeries { get; set; }
        [InputFieldData]
        [Display(Name = "Text", ResourceType = typeof(InputFieldDataText))]
        public string Text { get; set; }
        [InputFieldData]
        [Display(Name = "Comment", ResourceType = typeof(InputFieldDataText))]
        public string Comment { get; set; }
        [ForeignKeyAttribute(ForeignKeyTable = typeof(GLTransType))]
        [InputFieldData]
        [Display(Name = "TransType", ResourceType = typeof(InputFieldDataText))]
        public string TransType { get; set; }
        [InputFieldData]
        [Display(Name = "PostingDate", ResourceType = typeof(InputFieldDataText))]
        public DateTime Date { get; set; }
        [InputFieldData]
        [Display(Name = "LedgerVoucher", ResourceType = typeof(InputFieldDataText))]
        public int FixedVoucher { get; set; }

        [InputFieldData]
        [Display(Name = "Simulation", ResourceType = typeof(InputFieldDataText))]
        public bool Simulation { get; set; }

        [InputFieldData]
        [Display(Name = "PartlyReportAsFinished", ResourceType = typeof(InputFieldDataText))]
        public bool IsPartlyFinished { get; set; }

        [InputFieldData]
        [DisplayFormat(DataFormatString = "{0:#,##,##0.00###}", ApplyFormatInEditMode = true)]
        [Display(Name = "Qty", ResourceType = typeof(InputFieldDataText))]
        public double Quantity { get; set; }


        static DateTime postDte;
        string header { get; set; }
        public bool showCompanyName = false;
#if !SILVERLIGHT
        protected override int DialogId { get { return DialogTableId; } }
        public int DialogTableId { get; set; }
        protected override bool ShowTableValueButton { get { return true; } }
#endif
        public CWInvPosting(CrudAPI api, string headerName = null, bool showNumberSeries = false)
        {
            InitializeComponent();
            if (headerName == null)
                header = "PostJournal";
            else
                header = headerName;
            this.Title = Uniconta.ClientTools.Localization.lookup(header);
            OKButton.Content = Uniconta.ClientTools.Localization.lookup(header);
#if SILVERLIGHT
            UnicontaClient.Utilities.Utility.SetThemeBehaviorOnChildWindow(this);
#endif
            lookupTransType.api = api;
            Simulation = true;
            Date = (postDte != DateTime.MinValue) ? postDte : BasePage.GetSystemDefaultDate();
            if (!showNumberSeries)
                lookupNumberSeries.Visibility = txtNumberSerie.Visibility = Visibility.Collapsed;
            else
                lookupNumberSeries.api = api;
            this.DataContext = this;
            txtCompName.Text = api.CompanyEntity.Name;
            this.Loaded += CW_Loaded;
            if (header == "ReportAsFinished")
            {
                txtReportPartiallyFinished.Visibility = Visibility.Visible;
                chkReportPartiallyFinished.Visibility = Visibility.Visible;
            }
            else
            {
                txtReportPartiallyFinished.Visibility = Visibility.Collapsed;
                chkReportPartiallyFinished.Visibility = Visibility.Collapsed;
                chkReportPartiallyFinished.IsChecked = false;
            }
        }

        void CW_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (string.IsNullOrWhiteSpace(txtComment.Text))
                    txtComment.Focus();
                else
                    OKButton.Focus();
                if (!showCompanyName)
                {
                    rowComp.Height = new GridLength(0);
                    double h = this.Height - 30;
                    this.Height = h;
                    tbCompName.Visibility = txtCompName.Visibility = Visibility.Collapsed;
                }
            }));
        }

        private void ChildWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                SetDialogResult(false);
            }
            else if (e.Key == Key.Enter)
            {
                if (CancelButton.IsFocused)
                {
                    SetDialogResult(false);
                    return;
                }
                OKButton_Click(null, null);
            }
        }
        private void OKButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            int fxdVoucher;
            int.TryParse(txtFixedVoucher.Text, out fxdVoucher);
            FixedVoucher = fxdVoucher;
            SetDialogResult(true);
            if (Date != BasePage.GetSystemDefaultDate())
                postDte = Date;
        }

        private void CancelButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            SetDialogResult(false);
        }
    }
}

