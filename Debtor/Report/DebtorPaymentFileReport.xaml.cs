using UnicontaClient.Models;
using UnicontaClient.Pages;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
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
using Uniconta.API.Service;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;
using Uniconta.ClientTools;
using ICSharpCode.SharpZipLib.Zip;
#if !SILVERLIGHT
using System.IO.Compression;
#endif

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{

    public class DebtorPaymentFileReportGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(DebtorPaymentFileClient); } }
    }

    public partial class DebtorPaymentFileReport : GridBasePage
    {
        SQLCache paymentFormatCache;
        DateTime filterDate;

        public override string NameOfControl { get { return TabControls.DebtorPaymentFileReport.ToString(); } }

        protected override Filter[] DefaultFilters()
        {
            if (filterDate != DateTime.MinValue)
            {
                Filter dateFilter = new Filter() { name = "Created", value = String.Format("{0:d}..", filterDate) };
                return new Filter[] { dateFilter };
            }
            return base.DefaultFilters();
        }

        public DebtorPaymentFileReport(BaseAPI API) : base(API, string.Empty)
        {
            InitializeComponent();

            filterDate = BasePage.GetSystemDefaultDate().AddMonths(-2);
            localMenu.dataGrid = dgDebtorPaymentFileReportGrid;
            dgDebtorPaymentFileReportGrid.api = api;
            SetRibbonControl(localMenu, dgDebtorPaymentFileReportGrid);
            dgDebtorPaymentFileReportGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
            GetShowHideStatusInfoSection();
            SetShowHideStatusInfoSection(true);
        }


        protected override SortingProperties[] DefaultSort()
        {
            SortingProperties createdDateSort = new SortingProperties("Created");
            createdDateSort.Ascending = false;

            return new SortingProperties[] { createdDateSort };
        }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgDebtorPaymentFileReportGrid.SelectedItem as DebtorPaymentFileClient;
            var selectedItems = dgDebtorPaymentFileReportGrid.SelectedItems;
            switch (ActionType)
            {
                case "EnableStatusInfoSection":
                    hideStatusInfoSection = !hideStatusInfoSection;
                    SetShowHideStatusInfoSection(hideStatusInfoSection);
                    break;
                case "ExportFiles":
                    if (dgDebtorPaymentFileReportGrid.ItemsSource == null) return;
                    ExportFiles(selectedItems);
                    break;
                case "ImportFile":
                    if (dgDebtorPaymentFileReportGrid.ItemsSource == null) return;
                    ImportFile();
                    break;
                case "ViewFile":
                    if (selectedItem != null)
                        ViewFile(selectedItem);
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        ItemBase ibase;
        bool hideStatusInfoSection = true;
        void GetShowHideStatusInfoSection()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            ibase = UtilDisplay.GetMenuCommandByName(rb, "EnableStatusInfoSection");
        }
        private void SetShowHideStatusInfoSection(bool _hideStatusInfoSection)
        {
            if (ibase == null)
                return;
            if (_hideStatusInfoSection)
            {
                rowgridSplitter.Height = new GridLength(0);
                rowStatusInfoSection.Height = new GridLength(0);
                ibase.Caption = string.Format(Uniconta.ClientTools.Localization.lookup("ShowOBJ"), Uniconta.ClientTools.Localization.lookup("StatusInfo"));
            }
            else
            {
                if (rowgridSplitter.Height.Value == 0 && rowStatusInfoSection.Height.Value == 0)
                {
                    rowgridSplitter.Height = new GridLength(2);
                    rowStatusInfoSection.Height = new GridLength(1, GridUnitType.Auto);
                }
                ibase.Caption = string.Format(Uniconta.ClientTools.Localization.lookup("HideOBJ"), Uniconta.ClientTools.Localization.lookup("StatusInfo"));
            }
        }

        protected override async System.Threading.Tasks.Task LoadCacheInBackGroundAsync()
        {
            var api = this.api;
            var Comp = api.CompanyEntity;

            paymentFormatCache = Comp.GetCache(typeof(Uniconta.DataModel.DebtorPaymentFormat)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.DebtorPaymentFormat), api).ConfigureAwait(false);
        }


        async void ExportFiles(IList lstFiles)
        {
            try
            {
                int countFiles = 0;
                DevExpress.Xpf.Dialogs.DXFolderBrowserDialog folderBrowserDialog = null;

                var lstUpdate = new List<DebtorPaymentFileClient>();

                var qrFiles = lstFiles.Cast<DebtorPaymentFileClient>();

                foreach (var rec in qrFiles)
                {
                    countFiles++;

                    if (folderBrowserDialog == null)
                    {
                        folderBrowserDialog = UtilDisplay.LoadFolderBrowserDialog;
                        var dialogResult = folderBrowserDialog.ShowDialog();
                        if (dialogResult != true)
                            break;
                    }
                    if (rec._Data == null)
                        await api.Read(rec);

                    var filepath = folderBrowserDialog.SelectedPath;

                    if (paymentFormatCache == null)
                        paymentFormatCache = api.CompanyEntity.GetCache(typeof(Uniconta.DataModel.DebtorPaymentFormat)) ?? await api.CompanyEntity.LoadCache(typeof(Uniconta.DataModel.DebtorPaymentFormat), api);

                    var paymentFormat = (DebtorPaymentFormatClient)paymentFormatCache.Get(rec.PaymentFormat);

                    if (rec._Filename.EndsWith(".zip"))
                    {
                        if (paymentFormat._ExportFormat == (byte)Uniconta.DataModel.DebtorPaymFormatType.SEPA)
                        {
                            Stream data = new MemoryStream(rec._Data);

                            ZipArchive archive = new ZipArchive(data);
                            foreach (ZipArchiveEntry entry in archive.Entries)
                            {
                                using (var stream = entry.Open())
                                {
                                    var filename = string.Concat(filepath, "\\", entry.FullName);
                                    using (var fileStream = new FileStream(filename, FileMode.Create, FileAccess.Write))
                                    {
                                        stream.CopyTo(fileStream);
                                    }
                                }
                            }
                        }
                        else
                        {
                            File.WriteAllBytes(string.Concat(filepath, @"\", rec._Filename), rec._Data);
                        }
                    }
                    else
                    {
                        File.WriteAllText(string.Concat(filepath, @"\", rec._Filename), Encoding.GetEncoding("iso-8859-1").GetString(rec._Data));
                    }

                    if (paymentFormat != null && (paymentFormat._ExportFormat == (byte)Uniconta.DataModel.DebtorPaymFormatType.SEPA || paymentFormat._ExportFormat == (byte)Uniconta.DataModel.DebtorPaymFormatType.Iceland))
                    {
                        rec.StatusInfo = string.Format("({0}) {1}\n{2}", Uniconta.DirectDebitPayment.Common.GetTimeStamp(), Uniconta.ClientTools.Localization.lookup("Exported"), rec._StatusInfo);
                        rec.Status = AppEnums.DebtorPaymentStatus.ToString((int)DebtorPaymentStatus.Ok);
                        lstUpdate.Add(rec);
                    }
                }

                if (lstUpdate != null && lstUpdate.Count > 0)
                    api.UpdateNoResponse(lstUpdate);

                if (countFiles != 0)
                    UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("TotalFilesExported"), countFiles), Uniconta.ClientTools.Localization.lookup("Message"));
            }
            catch (Exception ex)
            {
                UnicontaMessageBox.Show(ex.Message, Uniconta.ClientTools.Localization.lookup("Exception"));
            }
        }

        async void ViewFile(DebtorPaymentFileClient selectedItem)
        {
            try
            {
                if (selectedItem._Data == null)
                {
                    busyIndicator.IsBusy = true;
                    await api.Read(selectedItem);
                    busyIndicator.IsBusy = false;
                }

                if (selectedItem._Data != null)
                {
                    if (TryUnZip(selectedItem._Data, out byte[] xmlData))
                        ContentPreview(xmlData);
                    else
                        ContentPreview(selectedItem._Data);
                }
            }
            catch
            {
                busyIndicator.IsBusy = false;
            }
        }

        private void ContentPreview(byte[] xmlData)
        {
            ChildWindow cw;
            var encoding = Encoding.GetEncoding("iso-8859-1");
            var dataLen = xmlData.Length;
            var encodedText = encoding.GetString(xmlData, 0, dataLen);

            if (UtilDisplay.TryGetValidXML(encodedText, out string xml))
                cw = dataLen <= 1024000 ? new CWPreviewXMLViewer(xml) : new CWPreviewXMLViewer(xml, null, false);
            else
                cw = new CWPreviewTextViewer(encodedText);

            cw?.Show();
        }

        private bool TryUnZip(byte[] data, out byte[] xmlData)
        {
            xmlData = null;
            Unistream stream = null;
            try
            {
                stream = new Unistream(data);

                if (IsZip(stream))
                {
                    var zipFile = new ICSharpCode.SharpZipLib.Zip.ZipFile(stream);

                    foreach (ZipEntry zipEntry in zipFile)
                    {
                        if (!zipEntry.IsFile)
                            continue;

                        var buffer = UnistreamReuse.Create((int)zipEntry.Size);
                        var xmlstream = zipFile.GetInputStream(zipEntry);
                        buffer.CopyFrom(xmlstream);
                        xmlData = buffer.ToArrayAndRelease();
                    }
                    stream.Close();
                    return true;
                }
            }
            catch { stream?.Close(); }
            finally { stream?.Dispose(); }

            return false;
        }

        private bool IsZip(Unistream streamZip)
        {
            try
            {
                if (streamZip.Position > 0)
                {
                    streamZip.Seek(0, SeekOrigin.Begin);
                }

                bool isZip = CheckSignature(streamZip, 4, "50-4B-03-04");
                bool isGzip = CheckSignature(streamZip, 3, "1F-8B-08");

                return isZip || isGzip;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public bool CheckSignature(Unistream stream, int signatureSize, string expectedSignature)
        {
            try
            {
                if (stream.Length < signatureSize)
                    return false;

                byte[] signature = new byte[signatureSize];
                int bytesRequired = signatureSize;
                int index = 0;
                while (bytesRequired > 0)
                {
                    int bytesRead = stream.Read(signature, index, bytesRequired);
                    bytesRequired -= bytesRead;
                    index += bytesRead;
                }
                string actualSignature = BitConverter.ToString(signature);

                return actualSignature == expectedSignature;
            }
            catch (Exception ex) { throw ex; }
        }

        async void ImportFile()
        {
            CWDirectDebit cwwin = new CWDirectDebit(api, Uniconta.ClientTools.Localization.lookup("Upload File"));
            DebtorPaymentFormatClient debPaymentFormat = null;

            cwwin.Closing += delegate
            {
                if (cwwin.DialogResult == true)
                {
                    debPaymentFormat = cwwin.PaymentFormat;
                }
            };
            cwwin.Show();

            if (debPaymentFormat == null)
                return;

            ErrorCodes error = ErrorCodes.Succes;
            var showError = false;
            try
            {
                var sfd = UtilDisplay.LoadOpenFileDialog;
                var userClickedSave = sfd.ShowDialog();
                if (userClickedSave != true)
                    return;

                var nextPaymentFileIdTest = 1;

                using (var stream = File.Open(sfd.FileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var sr = new StreamReader(stream);
                    var filecontentText = await sr.ReadToEndAsync();
                    stream.Close();

                    var debPaymFile = new DebtorPaymentFile
                    {
                        _Created = DateTime.Now,
                        _CredDirectDebitId = debPaymentFormat._CredDirectDebitId,
                        _Filename = System.IO.Path.GetFileName(sfd.FileName),
                        _Data = Encoding.GetEncoding("iso-8859-1").GetBytes(filecontentText),
                        _FileId = string.Format("{0}_{1}_{2}", debPaymentFormat._Format, DateTime.Now.ToString("yyMMdd"), nextPaymentFileIdTest.ToString().PadLeft(5, '0')),
                        _Format = debPaymentFormat._Format,
                        _StatusInfo = string.Format("{0} return file", debPaymentFormat._Format),
                        _Status = DebtorPaymentStatus.Pending,
                        _Output = false
                    };
                    showError = true;
                    error = await api.Insert(debPaymFile);
                }
                gridRibbon_BaseActions("RefreshGrid");
            }
            catch (Exception ex)
            {
                if (showError)
                    UnicontaMessageBox.Show(ex.Message + "\n" + error, Uniconta.ClientTools.Localization.lookup("Execption"));
                else
                    UnicontaMessageBox.Show(ex);
            }
        }

        void ChangeStatus(DebtorPaymentStatus changeToStatus, IList lstTransPaym)
        {
            Uniconta.DirectDebitPayment.Common.ChangeStatusFileArchive(api, lstTransPaym, changeToStatus);
        }
    }
}
