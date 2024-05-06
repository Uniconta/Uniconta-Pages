using DevExpress.Xpf.Printing;
using DevExpress.XtraPrinting;
using System;
using System.Windows;
using Uniconta.API.System;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.DataModel;
using UnicontaClient.Models;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    /// <summary>
    /// Interaction logic for VATSettlementReport.xaml
    /// </summary>
    public partial class VATSettlementReport : BasePage
    {
        public override string NameOfControl
        {
            get { return TabControls.VATSettlementReport; }
        }

        public VATSettlementReport(GLVatReported glVatReportClient, double[] vatArray1, double[] vatArray2, string[] otherTaxes)
        {
            InitializeComponent();
            var fromDate = glVatReportClient._FromDate;
            var toDate = glVatReportClient._ToDate;
            var reportHeaderData = new VatSettlementHeaderData(string.Format("Periode: {0} - {1}", fromDate.ToShortDateString(), toDate.ToShortDateString()),
                api.CompanyEntity._Name, string.Concat("Cvr.nr: ", api.CompanyEntity._Id));

            var vatArray3 = new double[vatArray1.Length];
            for (int i = 0; i < vatArray1.Length; i++)
                vatArray3[i] = vatArray1[i] + vatArray2[i];

            custPrint.DataContext = new VatSettlementBaseModule()
            {
                ReportHeaderData = reportHeaderData,
                DetailData = new VatSettlementDetailData(vatArray1, vatArray2, vatArray3, otherTaxes),
                DetailTemplate = (DataTemplate)this.Resources["DetailTemplate"],
                PageHeaderTemplate = (DataTemplate)this.Resources["PageHeaderTemplate"],
                ReportHeaderTemplate = (DataTemplate)this.Resources["ReportHeaderTemplate"],
            };
        }
    }

    public class VatSettlementBaseModule : BaseModulel
    {
        public VatSettlementHeaderData ReportHeaderData { get; set; }
        public VatSettlementDetailData DetailData { get; set; }

        protected override TemplatedLink CreateLink()
        {
            SimpleLink link = new SimpleLink();
            link.PrintingSystem.ExportOptions.Html.EmbedImagesInHTML = true;
            link.PrintingSystem.Graph.PageBackColor = System.Drawing.Color.Transparent;
            if (!string.IsNullOrEmpty(BasePage.session.User._Printer))
                link.PrintingSystem.PageSettings.PrinterName = BasePage.session.User._Printer;
            link.Landscape = true;

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

            link.Margins.Left = 40;
            link.Margins.Right = 20;
            link.ReportHeaderData = ReportHeaderData;
            link.PageHeaderTemplate = PageHeaderTemplate;
            link.ReportHeaderTemplate = ReportHeaderTemplate;
            link.DetailTemplate = DetailTemplate;
            link.DetailCount = 1;
            link.PaperKind = DevExpress.Drawing.Printing.DXPaperKind.A4;

            link.CreateDetail += link_CreateDetail;
            return link;
        }

        void link_CreateDetail(object sender, DevExpress.Xpf.Printing.CreateAreaEventArgs e)
        {
            e.Data = DetailData;
        }
    }

    public class VatSettlementHeaderData
    {
        public string VatPeriod { get; set; }
        public string CompanyInfo { get; set; }
        public string CompanyRegNr { get; set; }

        public VatSettlementHeaderData(string vatPeriod, string companyInfo, string companyRegNr)
        {
            VatPeriod = vatPeriod;
            CompanyInfo = companyInfo;
            CompanyRegNr = companyRegNr;
        }
    }

    public class VatSettlementDetailData
    {
        public double[] vatArray1 { get; set; }
        public double[] vatArray2 { get; set; }
        public double[] vatArray3 { get; set; }
        public string[] OtherTaxName { get; set; }

        public VatSettlementDetailData(double[] vatArray1, double[] vatArray2, double[] vatArray3, string[] otherTaxName)
        {
            this.vatArray1 = vatArray1;
            this.vatArray2 = vatArray2;
            this.vatArray3 = vatArray3;
            OtherTaxName = otherTaxName;
        }
    }
}
