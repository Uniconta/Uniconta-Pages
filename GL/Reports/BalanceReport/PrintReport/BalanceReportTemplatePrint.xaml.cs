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
using Uniconta.Common;
using Uniconta.API.System;
using System.Threading.Tasks;
using System.Windows;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class CustomColumn
    {
        readonly long[] amount;
        readonly int index;
        readonly bool header;

        public CustomColumn(CustomColumn org, Visibility showDebitCredit, bool hide, int aWidth, bool header) : this(org, showDebitCredit, header)
        {
            AmountWidth = (aWidth * 2);
            if (hide)
                ShowDebitCredit = ShowAmount = Visibility.Collapsed;
        }

        public CustomColumn(CustomColumn org, Visibility showDebitCredit, bool header)
        {
            this.header = header;
            this.amount = org.amount;
            this.index = org.index;
            ShowDebitCredit = showDebitCredit;
            ShowAmount = ShowDebitCredit == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        }

        public CustomColumn(long[] amount, int index, bool header)
        {
            this.header = header;
            this.amount = amount;
            this.index = index;
            ShowAmount = Visibility.Visible;
            ShowDebitCredit = Visibility.Collapsed;
        }

        public void SetProps(Visibility showDebitCredit)
        {
            ShowDebitCredit = showDebitCredit;
            ShowAmount = ShowDebitCredit == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        }

        public void SetProps(Visibility showDebitCredit, bool hide, int aWidth)
        {
            AmountWidth = (aWidth * 2);
            if (hide)
                ShowDebitCredit = ShowAmount = Visibility.Collapsed;
            else
                SetProps(showDebitCredit);
        }

        [Display(Name = "Amount", ResourceType = typeof(GLDailyJournalText))]
        public double? Amount { get { var d = GetAmountValue(this.amount, index); return (d != 0) ? d / 100d : (header ? (double?)null : (double?)0d); } }

        [Display(Name = "Debit", ResourceType = typeof(GLDailyJournalText))]
        public double? Debit { get { var d = GetAmountValue(this.amount, index); return (d > 0) ? d / 100d : (double?)null; } }

        [Display(Name = "Credit", ResourceType = typeof(GLDailyJournalText))]
        public double? Credit { get { var d = GetAmountValue(this.amount, index); return (d < 0) ? d / -100d : (double?)null; } }

        public Visibility ShowDebitCredit { get; set; }
        public Visibility ShowAmount { get; set; }
        public int AmountWidth { get; set; }

        static long GetAmountValue(long[] amount, int index)
        {
            return index < amount.Length ? amount[index] : 0;
        }
    }

    public class TemplateDataItems
    {
        public readonly BalanceClient blc;
        readonly HeaderData hdrData;
        public readonly GLReportLine line;
        public HeaderData HeaderData { get { return hdrData; } }

        public TemplateDataItems(BalanceClient blc, HeaderData hdrData, GLReportLine line)
        {
            this.blc = blc;

            var hide = line._Hide;
            var asize = hdrData.AmountSize;
            var header = (blc.Acc == null || blc.AccountTypeEnum == GLAccountTypes.Header);

            this.Col1 = new CustomColumn(blc.Col1, hdrData.ShowDCCol1, hide, asize, header);
            this.Col2 = new CustomColumn(blc.Col2, hdrData.ShowDCCol2, hide, asize, header);
            this.Col3 = new CustomColumn(blc.Col3, hdrData.ShowDCCol3, hide, asize, header);
            this.Col4 = new CustomColumn(blc.Col4, hdrData.ShowDCCol4, hide, asize, header);
            this.Col5 = new CustomColumn(blc.Col5, hdrData.ShowDCCol5, hide, asize, header);
            this.Col6 = new CustomColumn(blc.Col6, hdrData.ShowDCCol6, hide, asize, header);
            this.Col7 = new CustomColumn(blc.Col7, hdrData.ShowDCCol7, hide, asize, header);
            this.Col8 = new CustomColumn(blc.Col8, hdrData.ShowDCCol8, hide, asize, header);
            this.Col9 = new CustomColumn(blc.Col9, hdrData.ShowDCCol9, hide, asize, header);
            this.Col10 = new CustomColumn(blc.Col10, hdrData.ShowDCCol10, hide, asize, header);
            this.Col11 = new CustomColumn(blc.Col11, hdrData.ShowDCCol11, hide, asize, header);
            this.Col12 = new CustomColumn(blc.Col12, hdrData.ShowDCCol12, hide, asize, header);
            this.Col13 = new CustomColumn(blc.Col13, hdrData.ShowDCCol13, hide, asize, header);
            Columns = new List<CustomColumn>();
            foreach (var col in blc.Columns)
            {
                Columns.Add(new CustomColumn(col, col.ShowDebitCredit, hide, asize, header));
            }
            this.hdrData = hdrData;
            this.line = line;
        }

        public string Text { get { return line._Text; } }
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
        public List<CustomColumn> Columns { get; set; }

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
        //New Prop 
        public FontFamily Font { get; set; }
        public int Masterfontsize;
        public int TextSize { get { return hdrData.AccountNameColWidth; } }
        public int AmountSize { get { return hdrData.AmountSize; } }
        public float GridHeight { get { return hdrData.LineSpace.Bottom != 0 ? 3 * (int)hdrData.LineSpace.Bottom / 2 : 30f; } }
        public Thickness Indent { get { return new Thickness(hdrData.LeftMargin.Left + line._Indent != 0d ? hdrData.LeftMargin.Left + line._Indent : 2, 0, 0, 0); } }
        public int FontSize { get { return (line._FontSize != 0) ? line._FontSize : Masterfontsize; } }
        public Visibility Hide { get { return line._Hide ? Visibility.Collapsed : Visibility.Visible; } }
        public FontWeight IsBold { get { return line._Bold ? FontWeights.Bold : FontWeights.Normal; } }
        public FontStyle IsItalic { get { return line._Italic ? FontStyles.Italic : FontStyles.Normal; } }
        public TextDecorationCollection IsUnderline { get { return line._Underline ? TextDecorations.Underline : null; } }
        public Visibility IsNewline { get { return line._NewLine && !line._NewPage ? Visibility.Visible : Visibility.Collapsed; } }
        public Visibility IsNewPage { get { return line._NewPage ? Visibility.Visible : Visibility.Collapsed; } }
        public byte[] Line { get { return line._Underline == true ? Utilities.Utility.GetImageData("BlackLine.png") : null; } }
        public Brush LineBackground { get { return line._Underline == true ? new SolidColorBrush(Colors.Black) : new SolidColorBrush(Colors.Transparent); } }
        public Thickness LineSpace { get { return hdrData.LineSpace; } }
        public Thickness LeftMargin { get { return hdrData.LeftMargin; } }
        //byte[] GetImageData(string imageName)
        //{
        //    string path = Uniconta.ClientTools.Util.UtilFunctions.GetImgToolsAssemblyPath(";component/Assets/Img/" + imageName);
        //    Uri resourceUri = new Uri(path, UriKind.RelativeOrAbsolute);
        //    StreamResourceInfo resourceInfo = Application.GetResourceStream(resourceUri);
        //    using (resourceInfo.Stream)
        //    using (MemoryStream imageStream = new MemoryStream())
        //    {
        //        resourceInfo.Stream.CopyTo(imageStream);
        //        return imageStream.ToArray();
        //    }
        //}
    }


    public class TemplateDataContext
    {
        public TemplateDataContext()
        {
            this.TemplateReportlist = new List<TemplateDataItems>();
        }
        public byte[] Line { get { return Utilities.Utility.GetImageData("BlackLine.png"); } }
        public readonly List<TemplateDataItems> TemplateReportlist;
        //byte[] GetImageData(string imageName)
        //{
        //    Uri resourceUri = new Uri("Uniconta.SLTools;component/Assets/Img/" + imageName, UriKind.RelativeOrAbsolute);
        //    StreamResourceInfo resourceInfo = Application.GetResourceStream(resourceUri);
        //    using (resourceInfo.Stream)
        //    using (MemoryStream imageStream = new MemoryStream())
        //    {
        //        resourceInfo.Stream.CopyTo(imageStream);
        //        return imageStream.ToArray();
        //    }
        //}
    }


    public partial class BalanceReportTemplatePrint : BasePage
    {
        public string PrintBalanceReportPageFormat;
        public override object GetPrintParameter()
        {
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
            return base.GetPrintParameter();
        }
        public override string NameOfControl
        {
            get { return TabControls.BalanceReportTemplatePrint.ToString(); }
        }
        public BalanceReportTemplatePrint(object sourceData, object sourceHeaderData, object objBalance, object frontPageReport)
        {
            InitializeComponent();
            var bal = (Balance)objBalance;
            HeaderData headerdata = (HeaderData)sourceHeaderData;
            if (string.IsNullOrEmpty(headerdata.BalanceName))
                headerdata.BalanceName = Uniconta.ClientTools.Localization.lookup("ReportCriteria");
            headerdata.AccountColWidth = bal.ColumnSizeAccount == (byte)0 ? 80 : bal.ColumnSizeAccount;
            headerdata.AccountNameColWidth = bal.ColumnSizeName == (byte)0 ? 200 : bal.ColumnSizeName;
            headerdata.LineSpace = new Thickness(0, 0, 0, bal.LineSpace);
            headerdata.LeftMargin = new Thickness(bal.LeftMargin, 0, 0, 0);
            headerdata.DimColWidth = bal.ColumnSizeDim == (byte)0 ? 90 : bal.ColumnSizeDim;
            headerdata.DClblWidth = bal.ColumnSizeAmount == (byte)0 ? 100 : bal.ColumnSizeAmount;
            headerdata.FontSize = bal.FontSize == 0 ? 12 : bal.FontSize;
            headerdata.AmountSize = bal.ColumnSizeAmount == (byte)0 ? 100 : bal.ColumnSizeAmount;
            List<List<TemplateDataItems>> simpleLinkItems = new List<List<TemplateDataItems>>();
            var currentItems = ((TemplateDataContext)sourceData).TemplateReportlist;
            List<TemplateDataItems> currentLinkItems = new List<TemplateDataItems>();
            for (int i = 0; i < currentItems.Count; i++)
            {
                currentLinkItems.Add(currentItems[i]);
                if (currentItems[i].IsNewPage == Visibility.Visible)
                {
                    simpleLinkItems.Add(currentLinkItems);
                    currentLinkItems = new List<TemplateDataItems>();
                }
                if (i == currentItems.Count - 1)
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
                    var frontPageLink = BalanceReportPrint.CreateFrontPageLink(frontPageText, bal._Landscape, bal.FontSize, (DataTemplate)this.Resources["frontPageHeaderTemplate"]);
                    frontPageLink.CreateDocument(false);
                    links.Add(frontPageLink);
                }
            }
            foreach (List<TemplateDataItems> e in simpleLinkItems)
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
        }

        List<TemplateDataItems> obdata;
        private void link_CreateDetail(object sender, DevExpress.Xpf.Printing.CreateAreaEventArgs e)
        {
            e.Data = obdata[e.DetailIndex];
        }
        SimpleLink CreateLink(HeaderData hdrdata, List<TemplateDataItems> listbalance, bool _landscape)
        {
            SimpleLink link = new SimpleLink();
            link.PaperKind = DevExpress.Drawing.Printing.DXPaperKind.A4;
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
    }

    public class TemplatePrintBaseModule : BaseModulel
    {

        public HeaderData HeaderTemplateDataContext { get; set; }
        public TemplateDataContext DetailsTemplateDataContext { get; set; }
        public string ReportName { get; set; }
        protected override TemplatedLink CreateLink()
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
            if (HeaderTemplateDataContext._Landscape)
                link.Landscape = true;

            link.Margins.Right = 5;
            link.Margins.Left = 5;
            link.Margins.Top = 10;
            link.Margins.Bottom = 10;
            options.SetOptionsVisibility(OptionsKinds, true);
            link.PageHeaderData = HeaderTemplateDataContext;
            link.PageHeaderTemplate = PageHeaderTemplate;
            link.DetailTemplate = DetailTemplate;
            link.DetailCount = DetailsTemplateDataContext.TemplateReportlist.Count;
            link.CreateDetail += link_CreateDetail;
            return link;

        }

        private void link_CreateDetail(object sender, DevExpress.Xpf.Printing.CreateAreaEventArgs e)
        {
            e.Data = DetailsTemplateDataContext.TemplateReportlist[e.DetailIndex];
        }
    }
}
