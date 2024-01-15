using System;
using System.Windows;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls.Reporting;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Util;
using UnicontaClient.Pages;

namespace UnicontaClient.Controls.Dialogs
{
    /// <summary>
    /// Interaction logic for CWCopyReport.xaml
    /// </summary>
    public partial class CWCopyReport : ChildWindow
    {
        public UnicontaReports SelectedReport { get; set; }
        public string ReportName { get; set; }

        public CWCopyReport(StandardReports standardReport)
        {
            InitializeComponent();
            DataContext = this;
            Title = string.Format(Uniconta.ClientTools.Localization.lookup("CopyOBJ"), Uniconta.ClientTools.Localization.lookup("Report"));
            copyToTxt.Text = string.Format("{0} {1}", Uniconta.ClientTools.Localization.lookup("StandardReport"), Uniconta.ClientTools.Localization.lookup("Type"));
            cmbReports.ItemsSource = PrepareSource(standardReport);
            this.Loaded += CWCopyReport_Loaded;

        }

        public CWCopyReport()
        {
            InitializeComponent();
            DataContext = this;
            Title = string.Format(Uniconta.ClientTools.Localization.lookup("CopyOBJ"), Uniconta.ClientTools.Localization.lookup("Report"));
            var report = new UnicontaReports();
            cmbReports.ItemsSource = new UnicontaReports[] { report };
            cmbReports.IsEnabled = false;
            SelectedReport = report;
            this.Loaded += CWCopyReport_Loaded;
        }

        private void CWCopyReport_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (string.IsNullOrWhiteSpace(cmbReports.Text))
                    cmbReports.Focus();
                else
                    OKButton.Focus();
            }));
        }

        private UnicontaReports[] PrepareSource(StandardReports standardReport)
        {
            UnicontaReports[] unicontaReports = null;

            switch (standardReport)
            {
                case StandardReports.Invoice:
                case StandardReports.PackNote:
                case StandardReports.Quotation:
                case StandardReports.OrderConfirmation:
                case StandardReports.SalesPickingList:
                    unicontaReports = new UnicontaReports[]
                    {
                        new UnicontaReports((int)StandardReports.PackNote),
                        new UnicontaReports((int)StandardReports.Invoice),
                        new UnicontaReports((int)StandardReports.Quotation),
                        new UnicontaReports((int)StandardReports.OrderConfirmation),
                        new UnicontaReports((int)StandardReports.SalesPickingList)
                    };

                    break;

                case StandardReports.InterestNote:
                case StandardReports.CollectionLetter:
                    unicontaReports = new UnicontaReports[]
                    {
                        new UnicontaReports((int)StandardReports.CollectionLetter),
                        new UnicontaReports((int)StandardReports.InterestNote)
                    };

                    break;

                case StandardReports.Statement:
                case StandardReports.StatementCurrency:
                    unicontaReports = new UnicontaReports[]
                    {
                        new UnicontaReports((int)StandardReports.Statement),
                        new UnicontaReports((int)StandardReports.StatementCurrency)
                    };

                    break;
                case StandardReports.PurchaseOrder:
                case StandardReports.PurchaseInvoice:
                case StandardReports.PurchasePackNote:
                case StandardReports.PurchaseRequisition:
                    unicontaReports = new UnicontaReports[]
                    {
                        new UnicontaReports((int)StandardReports.PurchaseRequisition),
                        new UnicontaReports((int)StandardReports.PurchasePackNote),
                        new UnicontaReports((int)StandardReports.PurchaseInvoice),
                        new UnicontaReports((int)StandardReports.PurchaseOrder)
                    };
                    break;
                case StandardReports.CollectionLetterCurrency:
                case StandardReports.InterestNoteCurrency:
                    unicontaReports = new UnicontaReports[]
                    {
                        new UnicontaReports((int)StandardReports.CollectionLetterCurrency),
                        new UnicontaReports((int)StandardReports.InterestNoteCurrency)
                    };
                    break;
                case StandardReports.BalanceFrontPage:
                    unicontaReports = new UnicontaReports[] { new UnicontaReports((int)standardReport) };
                    break;
                case StandardReports.ProjectEstimate:
                    unicontaReports = new UnicontaReports[] { new UnicontaReports((int)standardReport) };
                    break;
            }
            return unicontaReports;
        }
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedReport != null && !string.IsNullOrEmpty(ReportName))
                SetDialogResult(true);
            else
                Uniconta.ClientTools.Controls.UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("CannotBeBlank"), string.Format("{0} & {1}", Uniconta.ClientTools.Localization.lookup("Name"), Uniconta.ClientTools.Localization.lookup("Type"))),
                    Uniconta.ClientTools.Localization.lookup("Warning"), MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
