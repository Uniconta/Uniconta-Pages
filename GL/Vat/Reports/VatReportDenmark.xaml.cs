using UnicontaClient.Models;
using DevExpress.Xpf.Printing;
using DevExpress.XtraPrinting;
using System;
using System.Collections.Generic;
using System.Globalization;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
#if !SILVERLIGHT
using DevExpress.XtraReports.Design;
#endif
using Uniconta.API.System;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class VatDenmarkReportHeader
    {
        public string exportUri { get; set; }

    }
    public class VatPrintBaseModuleDenmark : BaseModulel
    {
        public VatDenmarkReportHeader ReportHeaderDataContext { get; set; }
        public ReportDataDenmark DetailData { get; set; }

        protected override TemplatedLink CreateLink()
        {
            SimpleLink link = new SimpleLink();
#if SILVERLIGHT
            link.ExportServiceUri = ReportHeaderDataContext.exportUri;
#endif
            link.PrintingSystem.ExportOptions.Html.EmbedImagesInHTML = true;
#if !SILVERLIGHT
            link.PrintingSystem.Graph.PageBackColor=System.Drawing.Color.Transparent;
            if (!string.IsNullOrEmpty(BasePage.session.User._Printer))
                link.PrintingSystem.PageSettings.PrinterName = BasePage.session.User._Printer;
#endif
            ExportOptions options = link.PrintingSystem.ExportOptions;
            ExportOptionKind[] OptionsKinds = new ExportOptionKind[]{
                ExportOptionKind.PdfConvertImagesToJpeg,
                ExportOptionKind.PdfACompatibility,
                ExportOptionKind.PdfDocumentAuthor,
                ExportOptionKind.PdfDocumentKeywords,
                ExportOptionKind.PdfDocumentSubject,
                ExportOptionKind.PdfDocumentKeywords,
            };
            options.SetOptionsVisibility(OptionsKinds, true);

            ((DevExpress.Xpf.Printing.LinkBase)(link)).Margins.Left = 40;
            ((DevExpress.Xpf.Printing.LinkBase)(link)).Margins.Right = 20;
            link.PageHeaderData = ReportHeaderDataContext;
            link.PageHeaderTemplate = PageHeaderTemplate;
            link.DetailTemplate = DetailTemplate;
#if !SILVERLIGHT
            link.PaperKind = System.Drawing.Printing.PaperKind.A4;
#endif
            link.DetailCount = 1;
            link.CreateDetail += link_CreateDetail;
            return link;
        }

        void link_CreateDetail(object sender, DevExpress.Xpf.Printing.CreateAreaEventArgs e)
        {
            e.Data = DetailData;
        }
    }

    public partial class VatReportDenmark : BasePage
    {

        public override string NameOfControl
        {
            get { return TabControls.VatReportDenmark; }
        }

        public VatReportDenmark(CrudAPI api, List<VatSumOperationReport> vatSumOperationLst, DateTime fromDate, DateTime toDate)
        {
            InitializeComponent();
            ReportDataDenmark data = new ReportDataDenmark();
            data.VatPeriode = string.Format("Periode: {0} - {1}", fromDate.ToShortDateString(), toDate.ToShortDateString());

            var vatArray = new double[100];
            data.VatArray = vatArray;
            var OtherTaxName = new string[10];
            data.OtherTaxName = OtherTaxName;

            foreach (var rec in vatSumOperationLst)
            {
                if (rec == null)
                    continue;

                var idx = rec._Line;
                if (idx < 14)
                    vatArray[idx] += rec._AmountBase;
                else if (idx >= 14 && idx <= 19)
                {
                    OtherTaxName[idx - 13] = rec._Text;
                    vatArray[idx] = rec._Amount;
                    vatArray[31] += rec._Amount;
                }
                else
                    vatArray[idx] += rec._Amount;
                /*
                 1-10 totals at the buttom
                 14-18 other tax

                 51 TotalAmountUdgående
                 52 TotalAmountIndgående
                 */
            }

            vatArray[32] = vatArray[33] + vatArray[34] + vatArray[35];
            vatArray[30] = vatArray[32] - vatArray[31];

            data.CompanyInfo = api.CompanyEntity._Name;
            data.CompanyRegNr = string.Format("Cvr.nr: {0}", api.CompanyEntity._Id);

            VatDenmarkReportHeader sourcedata = new VatDenmarkReportHeader();
            //sourcedata.exportUri = CorasauDataGrid.GetExportServiceConnection(api);
            VatPrintBaseModuleDenmark printbase = new VatPrintBaseModuleDenmark();
            printbase.DetailTemplate = (DataTemplate)this.Resources["detailTemplate"];
            printbase.ReportHeaderDataContext = (VatDenmarkReportHeader)sourcedata;
            printbase.DetailData = data;
            custPrint.DataContext = printbase;
        }
    }

    public class ReportDataDenmark
    {
        public string VatPeriode { get; set; }
        public string CompanyInfo { get; set; }
        public string CompanyRegNr { get; set; }
        public double[] VatArray { get; set; }
        public string[] OtherTaxName { get; set; }

        public double AfgiftsAmount { get; set; }
        public string AfgiftsTekst { get; set; }
    }
}
