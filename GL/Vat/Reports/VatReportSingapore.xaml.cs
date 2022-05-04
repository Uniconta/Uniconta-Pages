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

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class VatSingaporeReportHeader
    {
        public string exportUri { get; set; }
    }

    public class VatPrintBaseModuleSingapore : BaseModulel
    {
        public VatSingaporeReportHeader ReportHeaderDataContext { get; set; }
        public ReportDataSingapore DetailData { get; set; }
        protected override TemplatedLink CreateLink()
        {
            SimpleLink link = new SimpleLink();
#if SILVERLIGHT
            link.ExportServiceUri = ReportHeaderDataContext.exportUri;
#endif
            link.PrintingSystem.ExportOptions.Html.EmbedImagesInHTML = true;
#if !SILVERLIGHT
            link.PrintingSystem.Graph.PageBackColor = System.Drawing.Color.Transparent;
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

    public partial class VatReportSingapore : BasePage
    {
        public override string NameOfControl
        {
            get { return TabControls.VatReportSingapore; }
        }

        public VatReportSingapore(CrudAPI api, double[] vatArray, DateTime fromDate, DateTime toDate)
        {
            vatArray[4] = vatArray[1] + vatArray[2] + vatArray[3];
            vatArray[8] = vatArray[6] - vatArray[7];
            InitializeComponent();
            var reportDataSingapore = new ReportDataSingapore();
            reportDataSingapore.VatArray = vatArray;
            VatSingaporeReportHeader sourcedata = new VatSingaporeReportHeader();
            VatPrintBaseModuleSingapore printbase = new VatPrintBaseModuleSingapore();
            printbase.DetailTemplate = (DataTemplate)this.Resources["detailTemplate"];
            printbase.ReportHeaderDataContext = (VatSingaporeReportHeader)sourcedata;
            printbase.DetailData = reportDataSingapore;
            custPrint.DataContext = printbase;
        }
    }

    public class ReportDataSingapore
    {
        public double[] VatArray { get; set; }
    }
}
