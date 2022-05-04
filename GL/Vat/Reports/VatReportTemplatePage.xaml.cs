using UnicontaClient.Models;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.DataModel;
using DevExpress.Xpf.Printing;
using DevExpress.XtraPrinting;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;

using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Resources;
using System.Windows.Shapes;
using System.Windows;
using Uniconta.Common;
using Uniconta.API.System;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class VatTemplateReportHeader
    {
        public string exportUri { get; set; }

    }
    public class VatPrintBaseModule : BaseModulel
    {
        public VatTemplateReportHeader ReportHeaderDataContext { get; set; }
        public double[] SapinVatArray { get; set; }
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
#if !SILVERLIGHT
            link.PaperKind = System.Drawing.Printing.PaperKind.A4;
#endif
            link.PageHeaderTemplate = PageHeaderTemplate;
            link.DetailTemplate = DetailTemplate;
            link.DetailCount = 1;
            link.CreateDetail += link_CreateDetail;
            return link;
        }

        void link_CreateDetail(object sender, DevExpress.Xpf.Printing.CreateAreaEventArgs e)
        {
            e.Data = SapinVatArray;
        }
    }
    public partial class VatReportTemplatePage : BasePage
    {
        public override string NameOfControl
        {
            get { return TabControls.VatReportSpain.ToString(); }
        }
        public VatReportTemplatePage(CrudAPI api, double[] vatArray)
        {
            InitializeComponent();
            VatTemplateReportHeader sourcedata = new VatTemplateReportHeader();
            //sourcedata.exportUri = CorasauDataGrid.GetExportServiceConnection(api);
            VatPrintBaseModule printbase = new VatPrintBaseModule();
            printbase.DetailTemplate = (DataTemplate)this.Resources["detailTemplate"];
            printbase.ReportHeaderDataContext = (VatTemplateReportHeader)sourcedata;
            printbase.SapinVatArray = vatArray;
            custPrint.DataContext = printbase;
        }
    }
    public class SpainVatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var IndexCode = System.Convert.ToInt32(parameter);
            var lin = IndexCode / 10;
            var col = IndexCode % 10;
            var index = (lin - 1) * 3 + (col - 1);
            var arrspainVat = (double[])((DevExpress.Xpf.Printing.RowContent)(value)).Content;
            return arrspainVat[index].ToString("N2");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
