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

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class CWProjectGenerateInvoice : ChildWindow
    {
        [InputFieldData]
        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.PrCategory))]
        [Display(Name = "Category", ResourceType = typeof(InputFieldDataText))]
        public string InvoiceCategory { get; set; }

        [InputFieldData]
        [Display(Name = "Simulation", ResourceType = typeof(InputFieldDataText))]
        public bool IsSimulation { get; set; }

        [InputFieldData]
        [Display(Name = "SendInvoiceByEmail", ResourceType = typeof(InputFieldDataText))]
        public bool SendByEmail { get; set; }

        [InputFieldData]
        [Display(Name = "GenerateInvoiceOIOUBL", ResourceType = typeof(InputFieldDataText))]
        public bool GenerateOIOUBLClicked { get; set; }

        [InputFieldData]
        [Display(Name = "Date", ResourceType = typeof(InputFieldDataText))]
        public DateTime GenrateDate { get; set; }

        [InputFieldData]
        [Display(Name = "FromDate", ResourceType = typeof(InputFieldDataText))]
        public DateTime FromDate { get; set; }

        [InputFieldData]
        [Display(Name = "ToDate", ResourceType = typeof(InputFieldDataText))]
        public DateTime ToDate { get; set; }

        [InputFieldData]
        [Display(Name = "SendOnlyToThisEmail", ResourceType = typeof(InputFieldDataText))]
        public bool SendOnlyEmail { get; set; }

        [InputFieldData]
        [Display(Name = "Email", ResourceType = typeof(InputFieldDataText))]
        public string EmailList { get; set; }

        [InputFieldData]
        [Display(Name = "Preview", ResourceType = typeof(InputFieldDataText))]
        public bool ShowInvoice { get; set; }

        [InputFieldData]
        [Display(Name = "PrintImmediately", ResourceType = typeof(InputFieldDataText))]
        public bool InvoiceQuickPrint { get; set; }

        [InputFieldData]
        [Display(Name = "NumberOfCopies", ResourceType = typeof(InputFieldDataText))]
        public short NumberOfPages { get; set; } = 1;

        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.PrCategory))]
        public string PrCategory { get; set; }

        CrudAPI api;

#if !SILVERLIGHT
        public int DialogTableId;
        protected override int DialogId { get { return DialogTableId; } }
        protected override bool ShowTableValueButton { get { return true; } }
#endif
        public CWProjectGenerateInvoice(CrudAPI crudapi, DateTime documentGenrateteDate, bool isSimulate = true, bool showInvoice = true, bool isQuickPrintVisible = true, bool askSendMail = true, bool generateOIOUBL = true,
            bool showToFromDate = false, bool showEmailList = false, bool showSendOnlyEmailCheck = false)
        {
            GenrateDate = documentGenrateteDate;
            this.DataContext = this;
            InitializeComponent();
            this.Title = Uniconta.ClientTools.Localization.lookup("GenerateInvoice");
            cmbCategory.api = crudapi;
#if !SILVERLIGHT
            tbOIOUBL.Text = string.Format(Uniconta.ClientTools.Localization.lookup("CreateOBJ"), Uniconta.ClientTools.Localization.lookup("OIOUBL"));
            if (!generateOIOUBL)
                RowOIOUBL.Height = new GridLength(0);

            chkPrintInvoice.Visibility = isQuickPrintVisible ? Visibility.Visible : Visibility.Collapsed;
            stkPageNumberCount.Visibility = isQuickPrintVisible ? Visibility.Visible : Visibility.Collapsed;
#endif
            if (!isSimulate)
                RowChk.Height = new GridLength(0);

            if (!askSendMail)
                RowSendByEmail.Height = new GridLength(0);

            if (!showToFromDate)
            {
                RowFromDate.Height = new GridLength(0);
                RowToDate.Height = new GridLength(0);
            }

            if (!showSendOnlyEmailCheck)
                RowOnlySendToEmail.Height = new GridLength(0);

            if (!showEmailList)
                RowEmailList.Height = new GridLength(0);

            chkShowInvoice.IsChecked = showInvoice;
            dpDate.DateTime = documentGenrateteDate;
            api = crudapi;
            SetItemSource(crudapi);
#if SILVERLIGHT
            Utility.SetThemeBehaviorOnChildWindow(this);
#endif
            this.Loaded += CW_Loaded;
        }


        async void SetItemSource(QueryAPI api)
        {
            var prCache = api.CompanyEntity.GetCache(typeof(Uniconta.DataModel.PrCategory)) ?? await api.CompanyEntity.LoadCache(typeof(Uniconta.DataModel.PrCategory), api);
            cmbCategory.ItemsSource = new PrCategoryRevenueFilter(prCache);
        }

        void CW_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => { OKButton.Focus(); }));
        }

        private void ChildWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.DialogResult = false;
            }
            else
                if (e.Key == Key.Enter)
            {
                if (CancelButton.IsFocused)
                {
                    this.DialogResult = false;
                    return;
                }
                OKButton_Click(null, null);
            }
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            InvoiceCategory = PrCategory;
            GenrateDate = dpDate.DateTime;
            FromDate = fromDate.DateTime;
            ToDate = toDate.DateTime;
            this.DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

#if !SILVERLIGHT
        private void chkShowInvoice_Checked(object sender, RoutedEventArgs e)
        {
            chkPrintInvoice.IsChecked = false;
        }

        private void chkPrintInvoice_Checked(object sender, RoutedEventArgs e)
        {
            chkShowInvoice.IsChecked = false;
        }
#endif
    }
}
