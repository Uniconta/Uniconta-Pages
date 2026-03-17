using DevExpress.Pdf;
using DevExpress.Xpf.Core.FilteringUI;
using Scanner;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Uniconta.API.GeneralLedger;
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.Common.Utility;
using Uniconta.DataModel;
using Localization = Uniconta.ClientTools.Localization;

namespace UnicontaClient.Controls.Dialogs
{
    public class AzureScanCorrectionWithWords : AzureScanCorrectionClient
    {
        public ScanFieldType _PropertyType;
        public List<AzureScanWordOnLine> WordLine { get; set; }

        public int PageNumberResolved => WordLine?.Select(w => w.word.PageNumber)?.FirstOrDefault() ?? _PageNumber;

        public bool AddWords(AzureScanWordOnLine wordLine, bool isAppend)
        {
            if (wordLine == null)
                return true;

            var word = wordLine.word;
            if (word == null) 
                return true;

            bool isFirstWord = !isAppend || WordLine == null || WordLine.Count == 0;
            if (!isFirstWord)
            {
                if (!WordLine.Any(w => w.word != null && w.word.IsAppendAllowed(word)))
                    return true;
                if (WordLine.Any(w => w.word != null && w.word.SameWord(word)))
                    return true;
            }

            string temp = isFirstWord
                ? word.Text
                : string.Join(" ", WordLine.Select(w => w.word.Text)) + " " + word.Text;

            string newValue = null;
            switch (_PropertyType)
            {
                case ScanFieldType.Date:
                    if (AzureScanUtil.TryParseScanValue<DateTime>(temp, out var date, CultureInfo.CurrentCulture))
                        newValue = date.ToString("g", CultureInfo.CurrentCulture);
                    break;
                case ScanFieldType.Double:
                    if (AzureScanUtil.TryParseScanValue<double>(temp, out var d, CultureInfo.CurrentCulture))
                        newValue = d.ToString("N2", CultureInfo.CurrentCulture);
                    break;
                case ScanFieldType.Int64:
                    if (AzureScanUtil.TryParseScanValue<long>(temp, out var l, CultureInfo.CurrentCulture))
                        newValue = l.ToString("N0", CultureInfo.CurrentCulture);
                    break;
                case ScanFieldType.Boolean:
                    if (AzureScanUtil.TryParseScanValue<bool>(temp, out var b, CultureInfo.CurrentCulture))
                        newValue = b.ToString();
                    break;
                default:
                    newValue = temp;
                    break;
            }

            if (string.IsNullOrWhiteSpace(newValue))
                return false;

            if (isFirstWord)
            {
                NormalizedBounds = word.GetNormalizedBox();
                _PageNumber = word.PageNumber;
                WordLine = new List<AzureScanWordOnLine> { wordLine };
            }
            else
            {
                WordLine.Add(wordLine);
                NormalizedBounds = MergeNormalizedBounds();
                _PageNumber = PageNumberResolved;
            }

            CorrectedValue = newValue;
            return true;
        }

        private NBox MergeNormalizedBounds()
        {
            if (WordLine == null || WordLine.Count == 0)
                return NormalizedBounds;

            double minX = double.MaxValue, minY = double.MaxValue;
            double maxX = double.MinValue, maxY = double.MinValue;

            foreach (var wl in WordLine)
            {
                var b = wl?.word?.GetNormalizedBox() ?? default;
                if (b.MaxX <= b.MinX || b.MaxY <= b.MinY)
                    continue;

                if (b.MinX < minX) minX = b.MinX;
                if (b.MinY < minY) minY = b.MinY;
                if (b.MaxX > maxX) maxX = b.MaxX;
                if (b.MaxY > maxY) maxY = b.MaxY;
            }

            if (minX == double.MaxValue)
                return NormalizedBounds;

            return new NBox
            {
                MinX = minX,
                MinY = minY,
                MaxX = maxX,
                MaxY = maxY
            };
        }

        public static AzureScanCorrectionWithWords[] GetAzureScanExtractions(
            AzureScannerResult result, ContentTypes contentType)
        {
            var dict = new Dictionary<string, AzureScanCorrectionWithWords>(StringComparer.Ordinal);

            var bankClassId = new CreditorPaymentAccountClient().ClassId();
            var creditorClassId = CreditorClient.CLASSID;
            var documnetClassId = VouchersClient.CLASSID;

            // Creditor
            AddIfMissing(dict, AzureCorrectionPropertiesAllowed.CompanyRegNo, creditorClassId, contentType,
                ScanFieldType.String, result.GetCreditorCVRExtended());
            AddIfMissing(dict, AzureCorrectionPropertiesAllowed.Name, creditorClassId, contentType,
                ScanFieldType.String, result.GetNameExtended());
            AddIfMissing(dict, AzureCorrectionPropertiesAllowed.Address1, creditorClassId, contentType,
                ScanFieldType.String, result.GetAddress1Extended());
            AddIfMissing(dict, AzureCorrectionPropertiesAllowed.City, creditorClassId, contentType,
                ScanFieldType.String, result.GetCityExtended());
            AddIfMissing(dict, AzureCorrectionPropertiesAllowed.ZipCode, creditorClassId, contentType,
                ScanFieldType.String, result.GetPostalCodeExtended());
            AddIfMissing(dict, AzureCorrectionPropertiesAllowed.ContactEmail, creditorClassId, contentType,
                ScanFieldType.String, result.GetEmailExtended());
            AddIfMissing(dict, AzureCorrectionPropertiesAllowed.Phone, creditorClassId, contentType,
                ScanFieldType.String, result.GetPhoneExtended());

            // Bank
            AddIfMissing(dict, AzureCorrectionPropertiesAllowed.BankAccount, bankClassId, contentType,
                ScanFieldType.String, result.GetBbanExtended());
            AddIfMissing(dict, AzureCorrectionPropertiesAllowed.IBAN, bankClassId, contentType,
                ScanFieldType.String, result.GetIbanExtended());
            AddIfMissing(dict, AzureCorrectionPropertiesAllowed.SWIFT, bankClassId, contentType,
                ScanFieldType.String, result.GetSwiftNoExtended());

            var fik = result.GetFikExtended();
            var fikEntity = !string.IsNullOrWhiteSpace(fik?.Item3) ? Tuple.Create(fik.Item1, fik.Item3) : null;
            AddIfMissing(dict, AzureCorrectionPropertiesAllowed.FIKCode, bankClassId, contentType,
                ScanFieldType.String, fikEntity);

            // Voucher
            AddIfMissing(dict, AzureCorrectionPropertiesAllowed.Invoice, documnetClassId, contentType,
                ScanFieldType.String, result.GetInvoiceNumberExtended());
            AddIfMissing(dict, AzureCorrectionPropertiesAllowed.PostingDate, documnetClassId, contentType,
                ScanFieldType.Date, Format(result.GetInvoiceDateExtended()));
            AddIfMissing(dict, AzureCorrectionPropertiesAllowed.DueDate, documnetClassId, contentType,
                ScanFieldType.Date, Format(result.GetDueDateExtended()));
            AddIfMissing(dict, AzureCorrectionPropertiesAllowed.PurchaseNumber, documnetClassId, contentType,
                ScanFieldType.String, Format(result.GetPurchaseNumberExtended()));
            AddIfMissing(dict, AzureCorrectionPropertiesAllowed.Project, documnetClassId, contentType,
                ScanFieldType.String, result.GetProjectNumberExtended());
            AddIfMissing(dict, AzureCorrectionPropertiesAllowed.Currency, creditorClassId, contentType,
                ScanFieldType.String, Format(result.GetCurrencyExtended()));
            AddIfMissing(dict, AzureCorrectionPropertiesAllowed.Amount, documnetClassId, contentType,
                ScanFieldType.Double, Format(result.GetAmountExtended()));

            return dict
                .Select(k => k.Value)
                .ToArray();
        }

        private static void AddIfMissing(Dictionary<string, AzureScanCorrectionWithWords> dict,
            string key, int classId, ContentTypes contentType, ScanFieldType fieldType,
            Tuple<RootAzureScanElement, string> value)
        {
            if (dict.TryGetValue(key, out var existing))
            {
                if (string.IsNullOrWhiteSpace(existing.CorrectedValue) && !string.IsNullOrWhiteSpace(value?.Item2))
                    existing.CorrectedValue = value?.Item2;
                return;
            }

            var element = value?.Item1;

            var nBox = element?.GetNormalizedBox()
                ?? new NBox { MinX = 0, MinY = 0, MaxX = 0, MaxY = 0 };

            var extract = new AzureScanCorrectionWithWords
            {
                Property = key,
                CorrectedValue = value?.Item2,
                _ContentType = contentType,
                _PropertyType = fieldType,
                _TableId = classId,
                _PageNumber = element?.PageNumber ?? 0,
                NormalizedBounds = nBox,
            };

            dict[key] = extract;
        }

        private static Tuple<RootAzureScanElement, string> Format<T>(Tuple<RootAzureScanElement, T> element)
        {
            if (element == null)
                return null;

            T val = element.Item2;
            if (val == null)
                return null;
            else if (val is double d)
                return Tuple.Create(element.Item1, d.ToString("N2", CultureInfo.CurrentCulture));
            else if (val is short date)
            {
                var dt = SmallDate.Unpack(date);
                var dtStr = dt.TimeOfDay == TimeSpan.Zero
                    ? dt.ToString("d", CultureInfo.CurrentCulture)
                    : dt.ToString("g", CultureInfo.CurrentCulture);
                return Tuple.Create(element.Item1, dtStr);
            }
            else if (val is Currencies curr)
            {
                var currStr = AppEnums.Currencies.ToString((int)curr);
                return Tuple.Create(element.Item1, currStr);
            }
            else
                return Tuple.Create(element.Item1, val.ToString());
        }
    }

    public partial class CWCorrectScannedData : UnicontaBaseWindow
    {
        private static readonly ConcurrentDictionary<int, AzureScannerResult> _ScanResultCache
            = new ConcurrentDictionary<int, AzureScannerResult>();

        private const string UnipediaScanUrl =
            "https://www.uniconta.com/da/unipedia/uniconta-scan/";

        private readonly CrudAPI api;
        private readonly VouchersClient voucher;

        private AzureScannerResult scan;

        // DevExpress PDF rendering state
        private PdfDocumentProcessor _pdfProcessor;
        private MemoryStream _pdfStream;     // keep stream alive while _pdfProcessoressor uses it
        private const int renderDpi = 80;

        private int _pdfPageCount = 0;
        private List<PdfPageVm> _pdfPageVms;

        public bool SaveSucceeded { get; private set; }

        public CWCorrectScannedData(VouchersClient voucher, CrudAPI api)
        {
            InitializeComponent();
            Title = Localization.lookup("CorrectScannedData");
            PageHost.ToolTip = Localization.lookup("CorrectScanResultDocTip");

            this.api = api;
            this.voucher = voucher;

            Loaded += (_, __) =>
            {
                // Catch wheel even if a child control marked it handled
                DocScroll.AddHandler(UIElement.PreviewMouseWheelEvent,
                    new MouseWheelEventHandler(DocScroll_PreviewMouseWheel), true);
            };

            Loaded += async (_, __) => await InitAsync();
        }

        private async Task InitAsync()
        {
            busyIndicator.BusyContent = Localization.lookup("Scanning") + "...";
            busyIndicator.IsBusy = true;

            try
            {
                var data = await GetData();
                if (data == null)
                {
                    Close();
                    return;
                }

                scan = await GetScanResult();
                if (scan == null)
                {
                    Close();
                    return;
                }

                DisposePdf();

                if (voucher.Fileextension == FileextensionsTypes.PDF)
                {
                    // Keep the stream alive for the lifetime of the _pdfProcessoressor
                    _pdfStream = new MemoryStream(data, writable: false);

                    _pdfProcessor = new PdfDocumentProcessor();
                    _pdfProcessor.LoadDocument(_pdfStream);

                    _pdfPageCount = _pdfProcessor.Document?.Pages?.Count ?? 0;
                    RenderAllPdfPages();
                }
                else
                    RenderImageDocument(data);

                FieldsList.ItemsSource = AzureScanCorrectionWithWords.GetAzureScanExtractions(scan, voucher._Content);
                FieldsList.SelectionChanged += FieldsList_SelectionChanged;
                if (scan.Any)
                    FieldsList.SelectedIndex = 0;
            }
            catch
            {
                UtilDisplay.ShowErrorCode(ErrorCodes.Exception);
                Close();
            }
            finally
            {
                busyIndicator.IsBusy = false;
            }
        }

        private async Task<byte[]> GetData()
        {
            var err = await UtilDisplay.GetData(voucher, api);
            var data = voucher?._Data;

            if (err != ErrorCodes.Succes || data == null || data.Length == 0)
            {
                if (err == ErrorCodes.Succes)
                    err = ErrorCodes.CouldNotLoad;

                UtilDisplay.ShowErrorCode(err);
                return null;
            }

            return data;
        }

        private async Task<AzureScannerResult> GetScanResult()
        {
            _ScanResultCache.TryGetValue(voucher.RowId, out scan);

            var docApi = new DocumentAPI(api);
            if (scan == null || !scan.Any)
                scan = await docApi.GetAzureScanResult(voucher);

            if (scan == null || !scan.Any)
                UtilDisplay.ShowErrorCode(docApi.LastError);

            _ScanResultCache[voucher.RowId] = scan;
            return scan;
        }

        private void RenderImageDocument(byte[] data)
        {
            var vms = new List<PdfPageVm>();
            // Single-frame images (png/jpg/bmp/gif)
            var bmp = new BitmapImage();
            using (var ms = new MemoryStream(data, writable: false))
            {
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.StreamSource = ms;
                bmp.EndInit();
            }
            if (bmp.CanFreeze) bmp.Freeze();

            vms.Add(new PdfPageVm { PageNumber = 1, Image = bmp });

            _pdfPageVms = vms;
            PdfPagesItems.ItemsSource = _pdfPageVms;
        }

        private void RenderAllPdfPages()
        {
            if (_pdfProcessor == null || _pdfPageCount <= 0)
                return;

            _pdfPageVms = new List<PdfPageVm>(_pdfPageCount);

            for (int i = 1; i <= _pdfPageCount; i++)
            {
                using (var bmp = RenderPdfPageToBitmap(i))
                {
                    if (bmp == null)
                        continue;

                    var src = ToBitmapSource(bmp);
                    if (src.CanFreeze)
                        src.Freeze();

                    _pdfPageVms.Add(new PdfPageVm
                    {
                        PageNumber = i,
                        Image = src
                    });
                }
            }

            PdfPagesItems.ItemsSource = _pdfPageVms;
        }

        private Bitmap RenderPdfPageToBitmap(int pageNumber)
        {
            if (_pdfProcessor?.Document == null ||
                _pdfProcessor.Document.Pages == null ||
                _pdfProcessor.Document.Pages.Count == 0)
                return null;

            var t = _pdfProcessor.GetType();

            // 1) Preferred (some versions): CreateBitmap(int pageNumber, int dpiX, int dpiY)
            var mDpi = t.GetMethod("CreateBitmap", new[] { typeof(int), typeof(int), typeof(int) });
            if (mDpi != null)
                return (Bitmap)mDpi.Invoke(_pdfProcessor, new object[] { pageNumber, renderDpi, renderDpi });

            // 2) Some versions: CreateBitmap(int pageNumber, float scaleFactor)
            // scaleFactor is typically relative to 72 DPI (PDF points/inch)
            var mScale = t.GetMethod("CreateBitmap", new[] { typeof(int), typeof(float) });
            if (mScale != null)
            {
                float scale = renderDpi / 72f;
                var img = mScale.Invoke(_pdfProcessor, new object[] { pageNumber, scale });

                if (img is Bitmap b1)
                    return b1;

                if (img is Image i1)
                    return new Bitmap(i1);
            }

            // 3) Some versions: CreateBitmap(int pageNumber, int largestEdgeLength)
            var mEdge = t.GetMethod("CreateBitmap", new[] { typeof(int), typeof(int) });
            if (mEdge != null)
            {
                // DevExpress page boxes are usually in PDF points (1/72 inch)
                var page = _pdfProcessor.Document.Pages[pageNumber - 1];

                double wPt = 0, hPt = 0;

                // CropBox is commonly available; fallback to MediaBox if not.
                try
                {
                    wPt = page.CropBox.Width;
                    hPt = page.CropBox.Height;
                }
                catch
                {
                    try
                    {
                        wPt = page.MediaBox.Width;
                        hPt = page.MediaBox.Height;
                    }
                    catch
                    {
                        // last resort: assume A4-ish size in points
                        wPt = 595; // ~8.27in * 72
                        hPt = 842; // ~11.69in * 72
                    }
                }

                int pixW = (int)Math.Round((wPt / 72.0) * renderDpi);
                int pixH = (int)Math.Round((hPt / 72.0) * renderDpi);
                int largestEdge = Math.Max(pixW, pixH);

                if (largestEdge <= 0)
                    largestEdge = renderDpi * 11; // fallback

                var img = mEdge.Invoke(_pdfProcessor, new object[] { pageNumber, largestEdge });

                if (img is Bitmap b2)
                    return b2;

                if (img is Image i2)
                    return new Bitmap(i2);
            }

            return null;
        }

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        private BitmapSource ToBitmapSource(Bitmap bitmap)
        {
            // Convert System.Drawing.Bitmap -> WPF BitmapSource
            IntPtr hBitmap = bitmap.GetHbitmap();

            try
            {
                return Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                DeleteObject(hBitmap);
            }
        }

        private Rect GetUniformRenderRect(System.Windows.Controls.Image img)
        {
            // Image stretches Uniform within its layout slot.
            // We compute the actual rendered image rectangle (letterboxing).
            if (img.Source == null || img.ActualWidth <= 0 || img.ActualHeight <= 0)
                return new Rect(0, 0, img.ActualWidth, img.ActualHeight);

            double sourceW = img.Source.Width;
            double sourceH = img.Source.Height;

            if (sourceW <= 0 || sourceH <= 0)
                return new Rect(0, 0, img.ActualWidth, img.ActualHeight);

            double scale = Math.Min(img.ActualWidth / sourceW, img.ActualHeight / sourceH);
            double renderW = sourceW * scale;
            double renderH = sourceH * scale;
            double offsetX = (img.ActualWidth - renderW) / 2.0;
            double offsetY = (img.ActualHeight - renderH) / 2.0;

            return new Rect(offsetX, offsetY, renderW, renderH);
        }

        private void HowDoesThisWork_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = UnipediaScanUrl,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                UnicontaMessageBox.Show(
                    string.Format(Localization.lookup("WebPageNaviagationFailed"), ex.Message),
                    Localization.lookup("Error"));
            }
        }

        private void FieldsList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var selectedExtraction = FieldsList.SelectedItem as AzureScanCorrectionWithWords;
            ClearHighlight();
            if (selectedExtraction == null || scan == null)
                return;

            if ((selectedExtraction.WordLine == null || selectedExtraction.WordLine.Count == 0)
                && !string.IsNullOrWhiteSpace(selectedExtraction.CorrectedValue))
            {
                var value = selectedExtraction.CorrectedValue;
                var bounds = selectedExtraction.NormalizedBounds;
                var page = selectedExtraction.PageNumberResolved;

                // 1) Anchor word: bounds first, value fallback
                var anchor = scan.GetWordAndLine(
                    pageNumber: page,
                    bounds: AzureScanUtil.IsMeaningful(bounds) ? bounds : (NBox?)null,
                    nx: null,
                    ny: null,
                    value: selectedExtraction.CorrectedValue,
                    allowNeighborPages: true);

                List<AzureScanWordOnLine> toadd;
                if (anchor != null)
                    toadd = scan.ResolveWordLineForExtraction(anchor,
                        bounds: AzureScanUtil.IsMeaningful(bounds) ? bounds : (NBox?)null,
                        value: selectedExtraction.CorrectedValue);
                else
                    toadd = scan.FindWordsByExtractedValue(selectedExtraction.CorrectedValue);

                toadd.ForEach(ta => selectedExtraction.AddWords(ta, true));
                selectedExtraction.CorrectedValue = value;
            }

            HighlightAzureWords(selectedExtraction);
        }

        private void OverlayCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var selectedExtraction = FieldsList.SelectedItem as AzureScanCorrectionWithWords;
            if (selectedExtraction == null || scan == null)
                return;

            var overlay = sender as System.Windows.Controls.Canvas;
            if (overlay == null)
                return;

            var pageVm = overlay.DataContext as PdfPageVm;
            if (pageVm == null)
                return;

            // The canvas and image are siblings inside the same Grid
            var grid = System.Windows.Media.VisualTreeHelper.GetParent(overlay) as System.Windows.Controls.Grid;
            if (grid == null)
                return;

            // Find the Image sibling
            var image = grid.Children
                .OfType<System.Windows.Controls.Image>()
                .FirstOrDefault(i => i.Name == "PageImage");

            if (image == null)
                return;

            // keep overlay sizing consistent
            overlay.Width = image.ActualWidth;
            overlay.Height = image.ActualHeight;

            var p = e.GetPosition(image);
            var rect = GetUniformRenderRect(image);
            if (!rect.Contains(p))
                return;

            double nx = (p.X - rect.X) / rect.Width;
            double ny = (p.Y - rect.Y) / rect.Height;

            var word = scan.GetWordAndLine(pageVm.PageNumber, null, nx, ny, null, false);
            if (word == null)
                return;

            bool isAppend = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;

            var isAdded = selectedExtraction.AddWords(word, isAppend);
            if (!isAdded)
            {
                ShowToast(string.Format(Localization.lookup("OBJNotValidFormat"), "'" + (word?.word?.Text ?? "") + "'"));
                return;
            }

            if (!isAppend)
                ClearHighlight();

            HighlightAzureWords(selectedExtraction);
            e.Handled = true;
        }

        private System.Windows.Threading.DispatcherTimer _toastTimer;
        private void ShowToast(string message, int visibleMs = 3500)
        {
            _toastTimer?.Stop();

            // Remove any in-progress animation BEFORE setting Opacity,
            // otherwise WPF animation ownership overrides the direct assignment
            ToastBorder.BeginAnimation(UIElement.OpacityProperty, null);

            ToastText.Text = message;
            ToastBorder.Opacity = 1.0;
            ToastBorder.Visibility = Visibility.Visible;

            _toastTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(visibleMs)
            };
            _toastTimer.Tick += (s, e) =>
            {
                _toastTimer.Stop();

                var fade = new System.Windows.Media.Animation.DoubleAnimation(1.0, 0.0,
                    TimeSpan.FromMilliseconds(500));
                fade.Completed += (_, __) =>
                {
                    ToastBorder.BeginAnimation(UIElement.OpacityProperty, null); // release ownership
                    ToastBorder.Visibility = Visibility.Collapsed;
                };
                ToastBorder.BeginAnimation(UIElement.OpacityProperty, fade);
            };
            _toastTimer.Start();
        }

        private void HighlightAzureWords(
            AzureScanCorrectionWithWords selectedExtraction)
        {
            var words = selectedExtraction?.WordLine;
            if (words == null || words.Count == 0)
                return;

            // For now: require same page (simple & predictable)
            int pageNumber = selectedExtraction.PageNumberResolved;

            int idx = (_pdfPageVms == null)
                ? -1
                : _pdfPageVms.FindIndex(p => p.PageNumber == pageNumber);

            if (idx < 0)
                return;

            PdfPagesItems.UpdateLayout();

            var container = PdfPagesItems.ItemContainerGenerator.ContainerFromIndex(idx) as FrameworkElement;
            if (container == null)
                return;

            var overlay = FindChild<System.Windows.Controls.Canvas>(container, "OverlayCanvas");
            var image = FindChild<System.Windows.Controls.Image>(container, "PageImage");

            if (overlay == null || image == null)
                return;

            overlay.Width = image.ActualWidth;
            overlay.Height = image.ActualHeight;

            // clear existing on that overlay
            _lastOverlay?.Children.Clear();
            overlay.Children.Clear();
            _lastOverlay = overlay;

            var n = selectedExtraction.NormalizedBounds;

            var rect = GetUniformRenderRect(image);

            var minX = rect.X + n.MinX * rect.Width;
            var minY = rect.Y + n.MinY * rect.Height;
            var maxX = rect.X + n.MaxX * rect.Width;
            var maxY = rect.Y + n.MaxY * rect.Height;

            var r = new System.Windows.Shapes.Rectangle
            {
                Width = Math.Max(0, maxX - minX),
                Height = Math.Max(0, maxY - minY),
                StrokeThickness = 2,
                Stroke = System.Windows.Media.Brushes.Gold,
                Fill = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromArgb(70, 255, 215, 0))
            };

            System.Windows.Controls.Canvas.SetLeft(r, minX);
            System.Windows.Controls.Canvas.SetTop(r, minY);

            overlay.Children.Add(r);
        }

        private T FindChild<T>(DependencyObject parent, string childName) where T : FrameworkElement
        {
            if (parent == null)
                return null;

            int count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);

            for (int i = 0; i < count; i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);

                if (child is T fe && (string.IsNullOrEmpty(childName) || fe.Name == childName))
                    return fe;

                var sub = FindChild<T>(child, childName);
                if (sub != null)
                    return sub;
            }

            return null;
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var corrections = FieldsList.ItemsSource as IEnumerable<AzureScanCorrectionWithWords>;
            if (corrections == null || voucher == null)
            {
                Close();
                return;
            }

            var correctionLst = corrections.ToList();
            var creditor = await GetCreditor(correctionLst);
            var bank = await GetBank(creditor);

            bool bankAccSetOnCreditor = false;
            bool fikSetOnVoucher = false;

            var headerNormalized = scan.NormalizedHeader;
            var scanCreditorId = scan._CreditorLegalIdent;

            var paymentClassId = new CreditorPaymentAccountClient().ClassId();
            var creditorClassId = CreditorClient.CLASSID;

            var errors = new List<string>();
            var modified = new List<AzureScanCorrection>();

            foreach (var moded in correctionLst)
            {
                try
                {
                    if (moded == null || string.IsNullOrWhiteSpace(moded.Property) || string.IsNullOrEmpty(moded.CorrectedValue))
                        continue;
                    if (!moded.IsModified || !moded.OutputOrder.Any(p => p == moded.Property))
                        continue;

                    var target = moded._TableId == paymentClassId ? bank
                        : moded._TableId == creditorClassId ? (UnicontaBaseEntity)creditor
                        : voucher;

                    var newText = moded.CorrectedValue;
                    if (bank != null && moded.Property == "FIKCode")
                    {
                        var fik = scan.ExtractAndSetFIKWithRegex(newText, voucher.Invoice);
                        bank.FICreditorNumber = fik?.Item1;
                        bank.FIKMask = fik?.Item2;
                        newText = bank.FIKMask + bank.FICreditorNumber;
                    }
                    else if (bank == null && creditor != null &&
                        ((!bankAccSetOnCreditor && moded.Property == "IBAN") || moded.Property == "BankAccount"))
                    {
                        creditor.PaymentId = newText;
                        creditor._PaymentMethod = moded.Property == "IBAN" ? PaymentTypes.IBAN
                            : moded.Property == "BankAccount" ? PaymentTypes.VendorBankAccount
                            : creditor._PaymentMethod;

                        bankAccSetOnCreditor = moded.Property == "BankAccount";
                    }
                    else if (bank == null && creditor == null &&
                        (!bankAccSetOnCreditor && !fikSetOnVoucher && moded.Property == "IBAN") ||
                            (!fikSetOnVoucher && moded.Property == "BankAccount") ||
                            moded.Property == "FIKCode")
                    {
                        voucher.PaymentId = newText;
                        voucher._PaymentMethod = moded.Property == "IBAN" ? PaymentTypes.IBAN
                            : moded.Property == "BankAccount" ? PaymentTypes.VendorBankAccount
                            : newText.StartsWith("+71") ? PaymentTypes.PaymentMethod3
                            : newText.StartsWith("+73") ? PaymentTypes.PaymentMethod4
                            : newText.StartsWith("+75") ? PaymentTypes.PaymentMethod5
                            : newText.StartsWith("+04") ? PaymentTypes.PaymentMethod6
                            : voucher._PaymentMethod;

                        fikSetOnVoucher = moded.Property == "FIKCode";
                        bankAccSetOnCreditor = moded.Property == "BankAccount";
                    }
                    else
                    {
                        if (creditor == null && CreditorClient.CLASSID == moded._TableId)
                            continue;
                        if (bank == null && moded._TableId == new CreditorPaymentAccountClient().ClassId())
                            continue;

                        var prop = target.GetType().GetProperty(moded.Property);
                        if (prop == null || !prop.CanWrite || prop.GetIndexParameters().Length != 0)
                        {
                            errors.Add(moded.Label);
                            continue;
                        }

                        if (!AzureScanUtil.TryParseScanValue(prop.PropertyType, newText, out var converted, scan.GetCultureInfo()))
                            continue;

                        prop.SetValue(target, converted);
                    }

                    var existingCorrection = scan.GetCorrection(moded._TableId, moded._Property);
                    moded.RowId = existingCorrection?.RowId ?? 0;
                    moded.CompanyId = existingCorrection?.CompanyId ?? api.CompanyId;

                    if (scanCreditorId == null || existingCorrection?._CreditorLegalIdent == null)
                        moded._CreditorLegalIdent = existingCorrection?._CreditorLegalIdent ?? scanCreditorId;
                    else
                        moded._CreditorLegalIdent = existingCorrection._CreditorLegalIdent.Length > scanCreditorId.Length
                                ? existingCorrection._CreditorLegalIdent : scanCreditorId;

                    moded._HeaderNormalized = headerNormalized;
                    moded._CorrectedValue = newText;
                    moded._UseCorrectedValueAlways = !(target is VouchersClient);

                    var lineSigs = AzureScanUtil.NormalizedLineTextsAndWordIndicesFromWordLine(
                        scan, moded.PageNumberResolved, moded.WordLine);

                    moded._LineTextNormalized = lineSigs.Item1;
                    moded._WordIndexInLines = lineSigs.Item2;
                    moded.NormalizedBounds = moded.NormalizedBounds;

                    moded._Uid = api.session.Uid;
                    moded._UserLogidId = api.session.LoginId;

                    moded._DocumentRef = voucher.RowId;
                    modified.Add(moded);
                }
                catch
                {
                    errors.Add(moded.Label);
                    continue;
                }
            }

            if (errors.Count > 0)
            {
                var errorMessage = string.Join("\n", errors);
                UnicontaMessageBox.Show(string.Format(Localization.lookup("NotAllowToChange") + ":\n", errorMessage),
                    Localization.lookup("Error"));
            }

            var err = ErrorCodes.Succes;
            if (creditor?.Account != null || creditor?.CompanyRegNo != null)
            {
                creditor.Account = creditor.Account ?? creditor.CompanyRegNo;
                err = await (creditor.RowId != 0 ? api.Update(creditor) : api.Insert(creditor));
            }
            if (err == 0 && bank != null && creditor?.Account != null)
            {
                bank.SetMaster(creditor);
                err = await (bank.RowId != 0 ? api.Update(bank) : api.Insert(bank));
            }
            if (err == 0)
            {
                if (creditor?.Account != null)
                    voucher.CreditorAccount = creditor.Account;

                err = await api.Update(voucher);
            }
            if (err == 0 && modified.Count > 0)
            {
                var inserts = modified.Where(c => c.RowId == 0).ToList();
                var updates = modified.Where(c => c.RowId != 0).ToList();
                err = await api.MultiCrud(inserts, updates, null);
                if (err == 0)
                {
                    inserts.AddRange(updates);
                    scan._Corrections = inserts.ToArray();
                }
            }
            if (err != 0)
            {
                _ScanResultCache.TryRemove(voucher.RowId, out _);
                UtilDisplay.ShowErrorCode(err);
            }
            else
                SaveSucceeded = true;

            Close();
        }

        private async Task<CreditorPaymentAccountClient> GetBank(CreditorClient creditor)
        {
            var hasBankApproveFlow = api.CompanyEntity.CreditorBankApprovement;
            var bank = hasBankApproveFlow ? creditor?.CreditorPaymentAccountRef : null;
            bank = null;
            if (creditor != null && hasBankApproveFlow &&
                (bank == null || bank.Account != creditor.Account))
            {
                var Comp = api.CompanyEntity;
                var Cache = Comp.GetCache(typeof(CreditorPaymentAccountClient));
                if (Cache == null)
                    Cache = await Comp.LoadCache(typeof(CreditorPaymentAccountClient), api);
                if (Cache == null)
                    return bank;

                var arr = (CreditorPaymentAccount[])Cache?.GetNotNullArray;
                bank = arr.Select(b => (CreditorPaymentAccountClient)b)
                    .FirstOrDefault(b => b.Account == creditor.Account);
            }

            return bank;
        }

        private async Task<CreditorClient> GetCreditor(
            List<AzureScanCorrectionWithWords> corrections)
        {
            var correction = corrections?.FirstOrDefault(c =>
                c._TableId == CreditorClient.CLASSID &&
                c._Property == AzureCorrectionPropertiesAllowed.CompanyRegNo &&
                !string.IsNullOrWhiteSpace(c?._CorrectedValue));

            var creditor = api.CompanyEntity._PaperFlowSaveCreditors ? voucher?.Creditor : null;

            var correctedValue = Regex.Replace(correction._CorrectedValue, "[^0-9]", "");
            if (string.IsNullOrWhiteSpace(correctedValue) ||
                correctedValue == Regex.Replace(creditor?.CompanyRegNo ?? "", "[^0-9]", ""))
                return creditor;

            var Comp = api.CompanyEntity;
            var Cache = Comp.GetCache(typeof(CreditorClient));
            if (Cache == null)
                Cache = await Comp.LoadCache(typeof(CreditorClient), api);
            if (Cache == null)
                return creditor;

            var arr = (CreditorClient[])Cache?.GetNotNullArray;
            var creditors = arr?.Where(s => s?.CompanyRegNo != null && Regex.Replace(s.CompanyRegNo, "[^0-9]", "") == correctedValue)?.ToList();
            if (creditors.Count > 0)
                creditor = creditors.FirstOrDefault(c => c.PaymentId != null) ?? creditors[0];
            if (creditor == null)
                creditor = (CreditorClient)Cache.Get(correction._CorrectedValue);
            if (creditor == null)
                creditor = (CreditorClient)Cache.Get(correctedValue);

            return creditor;
        }

        private const double ZoomStep = 0.10;     // 10% per notch
        private const double ZoomMin = 0.25;
        private const double ZoomMax = 4.0;
        private void DocScroll_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control)
                return; // let normal scrolling happen

            e.Handled = true;

            var sv = (System.Windows.Controls.ScrollViewer)sender;

            double oldZoom = ZoomTransform.ScaleX;
            double delta = e.Delta > 0 ? ZoomStep : -ZoomStep;
            double newZoom = Math.Max(ZoomMin, Math.Min(ZoomMax, oldZoom * (1.0 + delta)));

            if (Math.Abs(newZoom - oldZoom) < 0.0001)
                return;

            // Mouse position relative to the ZoomHost (content)
            var mousePos = e.GetPosition(ZoomHost);

            // Current scroll offsets
            double offsetX = sv.HorizontalOffset;
            double offsetY = sv.VerticalOffset;

            // Compute the absolute position (in "content coords") under the mouse before zoom
            double absX = offsetX + mousePos.X;
            double absY = offsetY + mousePos.Y;

            // Apply zoom
            ZoomTransform.ScaleX = newZoom;
            ZoomTransform.ScaleY = newZoom;

            // Force re-measure so scroll extent updates before we set offsets
            ZoomHost.UpdateLayout();

            // Keep the same content point under the mouse after zoom
            double scale = newZoom / oldZoom;
            sv.ScrollToHorizontalOffset(absX * scale - mousePos.X);
            sv.ScrollToVerticalOffset(absY * scale - mousePos.Y);
        }

        private void DisposePdf()
        {
            try { _pdfProcessor?.Dispose(); } catch { }
            _pdfProcessor = null;

            try { _pdfStream?.Dispose(); } catch { }
            _pdfStream = null;

            _pdfPageCount = 0;
        }

        private sealed class PdfPageVm
        {
            public int PageNumber { get; set; }          // 1-based
            public BitmapSource Image { get; set; }
        }

        private System.Windows.Controls.Canvas _lastOverlay;
        private void ClearHighlight()
        {
            if (_lastOverlay != null)
                _lastOverlay.Children.Clear();
            _lastOverlay = null;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e) => Close();
        protected override void OnClosed(EventArgs e)
        {
            DisposePdf();
            base.OnClosed(e);
        }
    }
}
