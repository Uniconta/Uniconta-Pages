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

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{

    public partial class BalanceReportPrint : BasePage
    {
        public string PrintBalanceReportPageFormat;
        public override object GetPrintParameter()
        {
#if !SILVERLIGHT
            switch (PrintBalanceReportPageFormat)
            {
                case "Excel":
                    custPrint.Export(ExportFormat.Xlsx);
                    break;
                case "Docs":
                    custPrint.Export(ExportFormat.Docx);
                    break;
                case "CSV":
                    custPrint.Export(ExportFormat.Csv);
                    break;
                default:
                    custPrint.Print();
                    break;
            }
#endif
            return base.GetPrintParameter();
        }
        public override string NameOfControl
        {
            get { return TabControls.BalanceReportPrint.ToString(); }
        }

        public BalanceReportPrint(object sourceHeaderData, object sourceLineData, object objBalance, object frontPageReport)
        {
            InitializeComponent();
            var bal = (Balance)objBalance;
            HeaderData headerdata = (HeaderData)sourceHeaderData;
            if (string.IsNullOrEmpty(headerdata.BalanceName))
                headerdata.BalanceName = Uniconta.ClientTools.Localization.lookup("ReportCriteria");
            headerdata.AccountColWidth = bal.ColumnSizeAccount == (byte)0 ? 80 : bal.ColumnSizeAccount;
            headerdata.AccountNameColWidth = bal.ColumnSizeName == (byte)0 ? 200 : bal.ColumnSizeName;
            headerdata.LineSpace = new Thickness(0, 0, 0, bal.LineSpace);
            headerdata.FontSize = bal.FontSize;
            headerdata.LeftMargin = new Thickness(bal.LeftMargin, 0, 0, 0);
            headerdata.DimColWidth = bal.ColumnSizeDim == (byte)0 ? 90 : bal.ColumnSizeDim;
            headerdata.DClblWidth = bal.ColumnSizeAmount == (byte)0 ? 100 : bal.ColumnSizeAmount;
#if !SILVERLIGHT
            List<List<BalanceReportdata>> simpleLinkItems = new List<List<BalanceReportdata>>();
            var currentItems = ((LineDetailsData)sourceLineData).BalanceReportlist;
            List<BalanceReportdata> currentLinkItems = new List<BalanceReportdata>();
            int n = currentItems.Count;
            for (int i = 0; i < n; i++)
            {
                var row = currentItems[i];
                if (row.ISPageBreak == Visibility.Visible)
                {
                    simpleLinkItems.Add(currentLinkItems);
                    currentLinkItems = new List<BalanceReportdata>();
                }
                currentLinkItems.Add(row);
                if (i == n - 1)
                    simpleLinkItems.Add(currentLinkItems);
            }

            DevExpress.XtraPrinting.PrintingSystem ps = new DevExpress.XtraPrinting.PrintingSystem();
            ps.Graph.PageBackColor = System.Drawing.Color.Transparent;
            //Setting the default Printer
            if (!string.IsNullOrEmpty(session.User._Printer))
                ps.PageSettings.PrinterName = session.User._Printer;
            List<TemplatedLink> links = new List<TemplatedLink>();
            var frontPageText = bal._FrontPage;

            if (!string.IsNullOrEmpty(frontPageText) && bal._PrintFrontPage)
            {
                if (frontPageReport is DevExpress.XtraReports.UI.XtraReport)
                {
                    var balanceFrontPageList = StandardPrintReportPage.AssignWatermark(frontPageReport as DevExpress.XtraReports.UI.XtraReport);
                    ps.Pages.AddRange(balanceFrontPageList);
                }
                else
                {
                    var frontPageLink = CreateFrontPageLink(frontPageText, bal._Landscape, bal.FontSize, (DataTemplate)this.Resources["frontPageHeaderTemplate"]);
                    frontPageLink.CreateDocument(false);
                    links.Add(frontPageLink);
                }
            }
            foreach (List<BalanceReportdata> e in simpleLinkItems)
            {
                var templateLink = CreateLink(headerdata, e, ((Balance)objBalance)._Landscape);
                templateLink.CreateDocument(false);
                links.Add(templateLink);
            }

            foreach (var link in links)
            {
                ps.Pages.AddRange(link.PrintingSystem.Pages);
            }
            custPrint.DocumentSource = ps;
#endif

#if SILVERLIGHT
            PrintBaseModule printbase = new PrintBaseModule();
            printbase.PageHeaderTemplate = (DataTemplate)this.Resources["pageHeaderTemplate"];
            printbase.DetailTemplate = (DataTemplate)this.Resources["detailTemplate"];
            printbase.ReportHeaderTemplateDataContext = headerdata;
            printbase.ReportDetailsTemplateDataContext = (LineDetailsData)sourceLineData;
            printbase.ReportHeaderTemplateDataContext._Landscape = ((Balance)objBalance)._Landscape;
            if (!((Balance)objBalance)._Landscape)
                printbase.ReportHeaderTemplateDataContext.HeaderMargins = new Thickness(390, 0, 0, 0);
            else
                printbase.ReportHeaderTemplateDataContext.HeaderMargins = new Thickness(590, 0, 0, 0);
            custPrint.DataContext = printbase;
#endif
        }
#if !SILVERLIGHT
        List<BalanceReportdata> obdata;
        private void link_CreateDetail(object sender, DevExpress.Xpf.Printing.CreateAreaEventArgs e)
        {
            e.Data = obdata[e.DetailIndex];
        }
        SimpleLink CreateLink(HeaderData hdrdata, List<BalanceReportdata> listbalance, bool _landscape)
        {
            SimpleLink link = new SimpleLink();
            link.PaperKind = System.Drawing.Printing.PaperKind.A4;
            link.PrintingSystem.ExportOptions.Html.EmbedImagesInHTML = true;
            ExportOptions options = link.PrintingSystem.ExportOptions;
            ExportOptionKind[] OptionsKinds = new ExportOptionKind[]{
                ExportOptionKind.PdfConvertImagesToJpeg,
                ExportOptionKind.PdfACompatibility,
                ExportOptionKind.PdfDocumentAuthor,
                ExportOptionKind.PdfDocumentKeywords,
                ExportOptionKind.PdfDocumentSubject,
                ExportOptionKind.PdfDocumentKeywords,
            };

            if (_landscape)
                link.Landscape = true;

            link.Margins.Right = 5;
            link.Margins.Left = 5;
            link.Margins.Top = 10;
            link.Margins.Bottom = 10;
            options.SetOptionsVisibility(OptionsKinds, true);
            link.PageHeaderData = hdrdata;
            link.PageHeaderTemplate = (DataTemplate)this.Resources["pageHeaderTemplate"];
            link.DetailTemplate = (DataTemplate)this.Resources["detailTemplate"];
            link.DetailCount = listbalance.Count;
            obdata = listbalance;
            link.CreateDetail += link_CreateDetail;
            return link;
        }
        public static SimpleLink CreateFrontPageLink(string frontPageText, bool _landscape, int fontSize, DataTemplate headerTemplate)
        {
            SimpleLink link = new SimpleLink();
            link.PrintingSystem.ExportOptions.Html.EmbedImagesInHTML = true;
            ExportOptions options = link.PrintingSystem.ExportOptions;
            ExportOptionKind[] OptionsKinds = new ExportOptionKind[]{
                ExportOptionKind.PdfConvertImagesToJpeg,
                ExportOptionKind.PdfACompatibility,
                ExportOptionKind.PdfDocumentAuthor,
                ExportOptionKind.PdfDocumentKeywords,
                ExportOptionKind.PdfDocumentSubject,
                ExportOptionKind.PdfDocumentKeywords,
            };

            if (_landscape)
                link.Landscape = true;

            link.Margins.Right = 5;
            link.Margins.Left = 5;
            link.Margins.Top = 10;
            link.Margins.Bottom = 10;
            options.SetOptionsVisibility(OptionsKinds, true);
            link.PageHeaderData = new FrontPageData() { Text = frontPageText, FontSize = fontSize };
            link.PageHeaderTemplate = headerTemplate;
            return link;
        }
#endif

    }
    public class FrontPageData
    {
        public string Text { get; set; }
        int fontsize;
        public int FontSize { get { return fontsize == 0 ? 12 : fontsize; } set { fontsize = value; } }
    }
    public class HeaderData
    {
        readonly string dattime;
        public HeaderData()
        {
            this.Lines = new CustomLines();
            this.Lines.Line = Utilities.Utility.GetImageData("black.png");
            var s = DateTime.Now.ToString();
            dattime = s.Remove(s.Length - 3, 3);
        }
        public Thickness HeaderMargins { get; set; }
        public Thickness LeftMargin { get; set; }
        public string exportServiceUrl { get; set; }
        public string BalanceName { get; set; }
        public CompanyClient Company { get; set; }
        public string Dim1 { get; set; }
        public string Dim2 { get; set; }
        public string Dim3 { get; set; }
        public string Dim4 { get; set; }
        public string Dim5 { get; set; }
        public int AccountColWidth { get; set; }
        public int AccountNameColWidth { get; set; }
        public int DimColWidth { get; set; }
        public int DClblWidth { get; set; }

        public int Dim1Width { get { return string.IsNullOrEmpty(Dim1) == true ? 0 : DimColWidth; } }
        public int Dim2Width { get { return string.IsNullOrEmpty(Dim2) == true ? 0 : DimColWidth; } }
        public int Dim3Width { get { return string.IsNullOrEmpty(Dim3) == true ? 0 : DimColWidth; } }
        public int Dim4Width { get { return string.IsNullOrEmpty(Dim4) == true ? 0 : DimColWidth; } }
        public int Dim5Width { get { return string.IsNullOrEmpty(Dim5) == true ? 0 : DimColWidth; } }
        public Visibility ShowDCCol1 { get; set; }
        public Visibility ShowDCCol2 { get; set; }
        public Visibility ShowDCCol3 { get; set; }
        public Visibility ShowDCCol4 { get; set; }
        public Visibility ShowDCCol5 { get; set; }
        public Visibility ShowDCCol6 { get; set; }
        public Visibility ShowDCCol7 { get; set; }
        public Visibility ShowDCCol8 { get; set; }
        public Visibility ShowDCCol9 { get; set; }
        public Visibility ShowDCCol10 { get; set; }
        public Visibility ShowDCCol11 { get; set; }
        public Visibility ShowDCCol12 { get; set; }
        public Visibility ShowDCCol13 { get; set; }
        public Visibility ShowAmountCol1 { get { return ShowDCCol1 == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed; } }
        public Visibility ShowAmountCol2 { get { return ShowDCCol2 == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed; } }
        public Visibility ShowAmountCol3 { get { return ShowDCCol3 == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed; } }
        public Visibility ShowAmountCol4 { get { return ShowDCCol4 == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed; } }
        public Visibility ShowAmountCol5 { get { return ShowDCCol5 == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed; } }
        public Visibility ShowAmountCol6 { get { return ShowDCCol6 == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed; } }
        public Visibility ShowAmountCol7 { get { return ShowDCCol7 == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed; } }
        public Visibility ShowAmountCol8 { get { return ShowDCCol8 == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed; } }
        public Visibility ShowAmountCol9 { get { return ShowDCCol9 == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed; } }
        public Visibility ShowAmountCol10 { get { return ShowDCCol10 == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed; } }
        public Visibility ShowAmountCol11 { get { return ShowDCCol11 == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed; } }
        public Visibility ShowAmountCol12 { get { return ShowDCCol12 == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed; } }
        public Visibility ShowAmountCol13 { get { return ShowDCCol13 == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed; } }
        public string ColName1 { get; set; }
        public string ColName2 { get; set; }
        public string ColName3 { get; set; }
        public string ColName4 { get; set; }
        public string ColName5 { get; set; }
        public string ColName6 { get; set; }
        public string ColName7 { get; set; }
        public string ColName8 { get; set; }
        public string ColName9 { get; set; }
        public string ColName10 { get; set; }
        public string ColName11 { get; set; }
        public string ColName12 { get; set; }
        public string ColName13 { get; set; }

        public string Col1AmountHeader { get; set; }
        public string Col2AmountHeader { get; set; }
        public string Col3AmountHeader { get; set; }
        public string Col4AmountHeader { get; set; }
        public string Col5AmountHeader { get; set; }
        public string Col6AmountHeader { get; set; }
        public string Col7AmountHeader { get; set; }
        public string Col8AmountHeader { get; set; }
        public string Col9AmountHeader { get; set; }
        public string Col10AmountHeader { get; set; }
        public string Col11AmountHeader { get; set; }
        public string Col12AmountHeader { get; set; }
        public string Col13AmountHeader { get; set; }

        // public int TextSize { get; set; }
        public int AmountSize { get; set; }
        public Visibility Col1Width { get { return string.IsNullOrEmpty(ColName1) == true ? Visibility.Collapsed : Visibility.Visible; } }
        public Visibility Col2Width { get { return string.IsNullOrEmpty(ColName2) == true ? Visibility.Collapsed : Visibility.Visible; } }
        public Visibility Col3Width { get { return string.IsNullOrEmpty(ColName3) == true ? Visibility.Collapsed : Visibility.Visible; } }
        public Visibility Col4Width { get { return string.IsNullOrEmpty(ColName4) == true ? Visibility.Collapsed : Visibility.Visible; } }
        public Visibility Col5Width { get { return string.IsNullOrEmpty(ColName5) == true ? Visibility.Collapsed : Visibility.Visible; } }
        public Visibility Col6Width { get { return string.IsNullOrEmpty(ColName6) == true ? Visibility.Collapsed : Visibility.Visible; } }
        public Visibility Col7Width { get { return string.IsNullOrEmpty(ColName7) == true ? Visibility.Collapsed : Visibility.Visible; } }
        public Visibility Col8Width { get { return string.IsNullOrEmpty(ColName8) == true ? Visibility.Collapsed : Visibility.Visible; } }
        public Visibility Col9Width { get { return string.IsNullOrEmpty(ColName9) == true ? Visibility.Collapsed : Visibility.Visible; } }
        public Visibility Col10Width { get { return string.IsNullOrEmpty(ColName10) == true ? Visibility.Collapsed : Visibility.Visible; } }
        public Visibility Col11Width { get { return string.IsNullOrEmpty(ColName11) == true ? Visibility.Collapsed : Visibility.Visible; } }
        public Visibility Col12Width { get { return string.IsNullOrEmpty(ColName12) == true ? Visibility.Collapsed : Visibility.Visible; } }
        public Visibility Col13Width { get { return string.IsNullOrEmpty(ColName13) == true ? Visibility.Collapsed : Visibility.Visible; } }
        public CustomLines Lines { get; set; }
        public string CurDateTime { get { return string.Format("{0 :}  {1}", Uniconta.ClientTools.Localization.lookup("Date"), dattime); } }

        public bool _Landscape { get; set; }
        public Thickness LineSpace { get; set; }
        public string Col1Format { get; set; }
        public string Col2Format { get; set; }
        public string Col3Format { get; set; }
        public string Col4Format { get; set; }
        public string Col5Format { get; set; }
        public string Col6Format { get; set; }
        public string Col7Format { get; set; }
        public string Col8Format { get; set; }
        public string Col9Format { get; set; }
        public string Col10Format { get; set; }
        public string Col11Format { get; set; }
        public string Col12Format { get; set; }
        public string Col13Format { get; set; }
        int fontsize;
        public int FontSize { get { return fontsize == 0 ? 12 : fontsize; } set { fontsize = value; } }
    }

    public class CustomPrintBalanceCol
    {
        public CustomPrintBalanceCol(double? amount, double? debit, double? credit)
        {
            Amount = amount;
            Debit = debit;
            Credit = credit;
        }
        public double? Amount { get; set; }
        public double? Debit { get; set; }
        public double? Credit { get; set; }
    }
    public class BalanceReportdata
    {
        readonly BalanceClient blc;
        readonly HeaderData hdrData;
        public HeaderData HeaderData { get { return hdrData; } }
        public BalanceReportdata(BalanceClient blc, HeaderData hdrData)
        {
            this.blc = blc;
            this.hdrData = hdrData;
            var header = (blc.Acc == null || blc.AccountTypeEnum == GLAccountTypes.Header);

            this.Col1 = new CustomColumn(blc.Col1, hdrData.ShowDCCol1, header);
            this.Col2 = new CustomColumn(blc.Col2, hdrData.ShowDCCol2, header);
            this.Col3 = new CustomColumn(blc.Col3, hdrData.ShowDCCol3, header);
            this.Col4 = new CustomColumn(blc.Col4, hdrData.ShowDCCol4, header);
            this.Col5 = new CustomColumn(blc.Col5, hdrData.ShowDCCol5, header);
            this.Col6 = new CustomColumn(blc.Col6, hdrData.ShowDCCol6, header);
            this.Col7 = new CustomColumn(blc.Col7, hdrData.ShowDCCol7, header);
            this.Col8 = new CustomColumn(blc.Col8, hdrData.ShowDCCol8, header);
            this.Col9 = new CustomColumn(blc.Col9, hdrData.ShowDCCol9, header);
            this.Col10 = new CustomColumn(blc.Col10, hdrData.ShowDCCol10, header);
            this.Col11 = new CustomColumn(blc.Col11, hdrData.ShowDCCol11, header);
            this.Col12 = new CustomColumn(blc.Col12, hdrData.ShowDCCol12, header);
            this.Col13 = new CustomColumn(blc.Col13, hdrData.ShowDCCol13, header);
        }

        public GLAccountTypes AccountTypeEnum { get { return blc.AccountTypeEnum; } }
        public string AccountNo { get { return IsVisible == Visibility.Visible ? (!blc._UseExternal ? blc.Acc._Account : blc.Acc._ExternalNo ?? blc.Acc._Account) : null; } }
        public string AccountName { get { return (!blc._UseExternal ? blc.Acc._Name : blc.Acc._ExternalName ?? blc.Acc._Name); } }

        public string ColumnDate { get { return blc.ColumnDate; } }
        public string Dim1 { get { return blc.Dim1; } }
        public string Dim2 { get { return blc.Dim2; } }
        public string Dim3 { get { return blc.Dim3; } }
        public string Dim4 { get { return blc.Dim4; } }
        public string Dim5 { get { return blc.Dim5; } }

        public CustomColumn Col1 { get; set; }
        public CustomColumn Col2 { get; set; }
        public CustomColumn Col3 { get; set; }
        public CustomColumn Col4 { get; set; }
        public CustomColumn Col5 { get; set; }
        public CustomColumn Col6 { get; set; }
        public CustomColumn Col7 { get; set; }
        public CustomColumn Col8 { get; set; }
        public CustomColumn Col9 { get; set; }
        public CustomColumn Col10 { get; set; }
        public CustomColumn Col11 { get; set; }
        public CustomColumn Col12 { get; set; }
        public CustomColumn Col13 { get; set; }

        public Visibility IsVisible { get; set; }
        public FontWeight isBold { get; set; }
        public byte[] Line { get; set; }
        public string Underline { get; set; }
        public Visibility isSumOrExpression { get; set; }

        public int Dim1Width { get { return hdrData.Dim1Width; } }
        public int Dim2Width { get { return hdrData.Dim2Width; } }
        public int Dim3Width { get { return hdrData.Dim3Width; } }
        public int Dim4Width { get { return hdrData.Dim4Width; } }
        public int Dim5Width { get { return hdrData.Dim5Width; } }

        public Visibility Col1Width { get { return hdrData.Col1Width; } }
        public Visibility Col2Width { get { return hdrData.Col2Width; } }
        public Visibility Col3Width { get { return hdrData.Col3Width; } }
        public Visibility Col4Width { get { return hdrData.Col4Width; } }
        public Visibility Col5Width { get { return hdrData.Col5Width; } }
        public Visibility Col6Width { get { return hdrData.Col6Width; } }
        public Visibility Col7Width { get { return hdrData.Col7Width; } }
        public Visibility Col8Width { get { return hdrData.Col8Width; } }
        public Visibility Col9Width { get { return hdrData.Col9Width; } }
        public Visibility Col10Width { get { return hdrData.Col10Width; } }
        public Visibility Col11Width { get { return hdrData.Col11Width; } }
        public Visibility Col12Width { get { return hdrData.Col12Width; } }
        public Visibility Col13Width { get { return hdrData.Col13Width; } }

        public float isSumOrExpressionHeight { get { return hdrData.LineSpace.Bottom != 0 ? 3 * (int)hdrData.LineSpace.Bottom / 2 : 30f; } }
        public bool PageBreak { get; set; }

        public Visibility ISPageBreak { get; set; }
        public int DClblWidth { get { return hdrData.DClblWidth; } }
        public int AccountColWidth { get { return hdrData.AccountColWidth; } }
        public int AccountNameColWidth { get { return hdrData.AccountNameColWidth; } }
        public Thickness LineSpace { get { return hdrData.LineSpace; } }
        public Thickness LeftMargin { get { return hdrData.LeftMargin; } }
        public int FontSize { get { return hdrData.FontSize; } }
    }
    public class LineDetailsData
    {

        public LineDetailsData()
        {
            this.balClient = new List<BalanceClient>();
            this.BalanceReportlist = new List<BalanceReportdata>();
        }
        public List<BalanceClient> balClient { get; set; }
        public byte[] Line { get { return Utilities.Utility.GetImageData("black.png"); } }
        public List<BalanceReportdata> BalanceReportlist { get; set; }
    }
    public class PrintBaseModule : BaseModulel
    {

        public HeaderData ReportHeaderTemplateDataContext { get; set; }
        public LineDetailsData ReportDetailsTemplateDataContext { get; set; }
        public string ReportName { get; set; }
        protected override TemplatedLink CreateLink()
        {
            SimpleLink link = new SimpleLink();
#if SILVERLIGHT
            link.ExportServiceUri = ReportHeaderTemplateDataContext.exportServiceUrl;
#else
            link.PaperKind = System.Drawing.Printing.PaperKind.A4;
            if (!string.IsNullOrEmpty(BasePage.session.User._Printer))
                link.PrintingSystem.PageSettings.PrinterName = BasePage.session.User._Printer;
#endif
            link.PrintingSystem.ExportOptions.Html.EmbedImagesInHTML = true;
            ExportOptions options = link.PrintingSystem.ExportOptions;
            ExportOptionKind[] OptionsKinds = new ExportOptionKind[]{
                ExportOptionKind.PdfConvertImagesToJpeg,
                ExportOptionKind.PdfACompatibility,
                ExportOptionKind.PdfDocumentAuthor,
                ExportOptionKind.PdfDocumentKeywords,
                ExportOptionKind.PdfDocumentSubject,
                ExportOptionKind.PdfDocumentKeywords,
            };
            if (ReportHeaderTemplateDataContext._Landscape)
                link.Landscape = true;
            link.Margins.Right = 5;
            link.Margins.Left = 5;
            link.Margins.Top = 10;
            link.Margins.Bottom = 10;
            options.SetOptionsVisibility(OptionsKinds, true);
            link.PageHeaderData = ReportHeaderTemplateDataContext;
            link.PageHeaderTemplate = PageHeaderTemplate;
            link.DetailTemplate = DetailTemplate;
            link.DetailCount = ReportDetailsTemplateDataContext.BalanceReportlist.Count;
            link.CreateDetail += link_CreateDetail;
            return link;

        }
        private void link_CreateDetail(object sender, DevExpress.Xpf.Printing.CreateAreaEventArgs e)
        {
            e.Data = ReportDetailsTemplateDataContext.BalanceReportlist[e.DetailIndex];
        }
    }
}
