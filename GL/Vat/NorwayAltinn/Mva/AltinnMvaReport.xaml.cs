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
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class AltinnMvaReportHeader
    {
        public string exportUri { get; set; }

    }
    public class AltinnMvaReportBaseModule : BaseModulel
    {
        public AltinnMvaReportHeader ReportHeaderDataContext { get; set; }
        public ReportDataAltinnMva DetailData { get; set; }

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
            link.DetailCount = 1;
            link.CreateDetail += link_CreateDetail;
            return link;
        }

        void link_CreateDetail(object sender, DevExpress.Xpf.Printing.CreateAreaEventArgs e)
        {
            e.Data = DetailData;
        }
    }

    public partial class AltinnMvaReport : BasePage
    {
        protected const int TERMINTYPE_YEAR = 1;
        protected const int TERMINTYPE_TWOMTH = 4;
        protected const int TERMINTYPE_MTH = 5;
        protected const int TERMINTYPE_HALFMTH = 6;


        public override string NameOfControl
        {
            get { return TabControls.AltinnMvaReport; }
        }

        public AltinnMvaReport(CrudAPI api, DateTime fromDate, DateTime toDate, List<VatSumOperationReport> vatSumOperationLst)
        {
            InitializeComponent();

            ReportDataAltinnMva data = new ReportDataAltinnMva();
            data.Line = new int[19];
            data.Text = new string[19];
            data.Amount = new double[19];
            data.AmountBase = new double[19];

            var countIndex = 0;

            if (vatSumOperationLst != null)
            {
                foreach (var ot in vatSumOperationLst)
                {
                    data.Line[countIndex] = ot.Line;
                    data.Text[countIndex] = string.Format("Post {0} {1}", ot.Line, ot.Text);
                    data.Amount[countIndex] = ot.Amount;
                    data.AmountBase[countIndex] = ot.AmountBase;

                    if (ot.Line == 12 && (ot.Amount != 0 || ot.AmountBase != 0))
                        data.AltinnInfoText = "Avgift er beregnet med 10 % av grunnlaget";

                    countIndex++;
                }
            }


            var comp = api.CompanyEntity;
            data.CompanyInfo = comp._Name;
            data.CompanyRegNr = comp._Id;
            data.CompanyBBAN = comp._NationalBank;
            data.CompanyIBAN = comp._IBAN;
            data.CompanyBIC = comp._SWIFT;

            data.PrintDateTime = DateTime.Now.ToString("dd.MM.yyyy HH:mm");

#if !SILVERLIGHT
            var tupleTermin = Altinn.AltinnCalculateTermin(fromDate, toDate);
            data.Year = tupleTermin.Item1.ToString();
            data.TerminType = tupleTermin.Item2 == TERMINTYPE_YEAR ? "Årlig" :
                              tupleTermin.Item2 == TERMINTYPE_TWOMTH ? "To månedlig" :
                              tupleTermin.Item2 == TERMINTYPE_MTH ? "Månedlig" :
                              tupleTermin.Item2 == TERMINTYPE_HALFMTH ? "Halv månedlig" : "Ukentlig";
                              
            data.Termin = string.Format("{0} - {1}", fromDate.ToShortDateString(), toDate.ToShortDateString());
            data.MessageType = "Hovedmelding (første innsending for terminen)";
#endif
            data.FromDate = fromDate.ToShortDateString();
            data.ToDate = toDate.ToShortDateString();

            AltinnMvaReportHeader sourcedata = new AltinnMvaReportHeader();
            AltinnMvaReportBaseModule printbase = new AltinnMvaReportBaseModule();
            printbase.DetailTemplate = (DataTemplate)this.Resources["detailTemplate"];
            printbase.ReportHeaderDataContext = (AltinnMvaReportHeader)sourcedata;
            printbase.DetailData = data;
            custPrint.DataContext = printbase;
        }
    }

    public class ReportDataAltinnMva
    {
        public string FromDate { get; set; }
        public string ToDate { get; set; }
        public string CompanyInfo { get; set; }
        public string CompanyRegNr { get; set; }
        public string CompanyBBAN { get; set; }
        public string CompanyIBAN { get; set; }
        public string CompanyBIC { get; set; }
        public string PrintDateTime { get; set; }
        public string AltinnInfoText { get; set; }

        public string MessageType { get; set; }
        public string TerminType { get; set; }
        public string Termin { get; set; }
        public string Year { get; set; }

        public int[] Line { get; set; }
        public string[] Text { get; set; }
        public double[] AmountBase { get; set; }
        public double[] Amount { get; set; }
    }
}
