using UnicontaClient.Models;
using UnicontaClient.Pages;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
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
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.Common.Utility;
using Uniconta.API.Service;
using System.Net;
using Uniconta.ClientTools.Util;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class VoucherExportPage : FormBasePage
    {
        public override string NameOfControl { get { return TabControls.VoucherExportPage; } }
        public override Type TableType { get { return null; } }

        public override UnicontaBaseEntity ModifiedRow { get { return null; } set { } }

        public override void OnClosePage(object[] RefreshParams) { }

        ExportLog logs;
        IEnumerable<VouchersClient> vouchers;
        public VoucherExportPage(IEnumerable<VouchersClient> _vouchers) : base(_vouchers.FirstOrDefault())
        {
            InitPage();
            vouchers = _vouchers;
        }

        IEnumerable<GLTransClient> glTransLst;
        public VoucherExportPage(IEnumerable<GLTransClient> _glTransLst) : base(_glTransLst.FirstOrDefault())
        {
            InitPage();
            glTransLst = _glTransLst;
        }

        void InitPage()
        {
            InitializeComponent();
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            var format1 = string.Concat(Uniconta.ClientTools.Localization.lookup("UniqueId"), "_", Uniconta.ClientTools.Localization.lookup("Text"));
            var format2 = string.Concat(Uniconta.ClientTools.Localization.lookup("PostingDate"), "_", Uniconta.ClientTools.Localization.lookup("Voucher"), "_", Uniconta.ClientTools.Localization.lookup("Text"));
            cmbFormat.ItemsSource = new List<string>() { format1, format2 };
            cmbFormat.SelectedIndex = 0;
        }
        bool zip;
        int exportOption;
        void localMenu_OnItemClicked(string ActionType)
        {
            if (ActionType == "LocalBackup")
            {
                logs = new ExportLog();
                this.DataContext = logs;
                zip = chkZip.IsChecked == true;
                exportOption = cmbFormat.SelectedIndex;
                SaveVouchers();
            }
        }

        async void SaveVouchers()
        {
            bool? dialogResult = null;
            string path = null;
            Stream stream = null;
            if (zip)
            {
                string defaultName = string.Concat(Uniconta.ClientTools.Localization.lookup("ExportVouchers"), " ", api.CompanyEntity.Name);

                var saveDialog = Uniconta.ClientTools.Util.UtilDisplay.LoadSaveFileDialog;
                saveDialog.FileName = defaultName;
                saveDialog.Filter = "ZIP Files (*.zip)|*.zip";
                dialogResult = saveDialog.ShowDialog();
                if (dialogResult == true)
                    stream = File.Create(saveDialog.FileName);
            }
            else
            {
                var openFolderDialog = UtilDisplay.LoadFolderBrowserDialog;
                dialogResult = openFolderDialog.ShowDialog();
                if (dialogResult == true)
                    path = openFolderDialog.SelectedPath;
            }
            if (dialogResult == true)
            {
                try
                {
                    leLog.Visibility = Visibility.Visible;
                    if (vouchers != null)
                        await CreateZip(vouchers, stream, path);
                    else if (glTransLst != null)
                        await CreateZip(glTransLst, stream, path);
                    WriteLogLine(Uniconta.ClientTools.Localization.lookup("ExportComplete"));
                    stream?.Close();
                }
                catch (Exception ex)
                {
                    UnicontaMessageBox.Show(ex);
                    return;
                }
            }
        }

        private async Task CreateZip(IEnumerable<UnicontaBaseEntity> lst, Stream outputStream, string folderpath)
        {
            var voucherExpLst = new HashSet<int>();
            ZipOutputStream zipOutputStream = null;
            if (outputStream != null)
            {
                zipOutputStream = new ZipOutputStream(outputStream);
                // Highest compression rating
                zipOutputStream.SetLevel(9);
                zipOutputStream.UseZip64 = UseZip64.Dynamic;
            }
            foreach (var tr in lst)
            {
                GLTransClient glTrans = null;
                VouchersClient voucher = tr as VouchersClient;
                if (voucher == null)
                    glTrans = tr as GLTransClient;
                if (glTrans == null || !voucherExpLst.Contains(glTrans._DocumentRef))
                {
                    if (voucher == null)
                    {
                        voucherExpLst.Add(glTrans._DocumentRef);
                        voucher = new VouchersClient() { RowId = glTrans._DocumentRef };
                    }
                    ErrorCodes err = ErrorCodes.Succes;
                    if (voucher._Data == null)
                        await UtilDisplay.GetData(voucher, api);
                    if (err == ErrorCodes.Succes)
                    {
                        if (!voucher._Envelope)
                            ExportFile(voucher, zipOutputStream, folderpath);
                        else
                        {
                            var content = voucher.GetEnvelopeContent();
                            if (content != null)
                            {
                                foreach (var vou in content)
                                {
                                    if (!voucherExpLst.Contains(vou.RowId))
                                    {
                                        voucherExpLst.Add(vou.RowId);
                                        voucher = new VouchersClient() { RowId = vou.RowId };
                                        if (await UtilDisplay.GetData(voucher, api) == ErrorCodes.Succes)
                                        {
                                            if (!voucher._Envelope)
                                                ExportFile(voucher, zipOutputStream, folderpath);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            zipOutputStream?.Finish();
        }

        void ExportFile(VouchersClient voucher, ZipOutputStream zipOutputStream, string folderpath)
        {
            byte[] attachment = voucher.Buffer;
            voucher._Data = null;
            voucher._LoadedData = null;
            if (attachment == null)
            {
                if (voucher._Url != null)
                {
                    try
                    {
                        if (voucher.IsWebUrl)
                        {
                            using (var st = new WebClient())
                            {
                                attachment = st.DownloadData(voucher._Url);
                            }
                            var ext = System.IO.Path.GetExtension(voucher._Url);
                            if (ext != null)
                                voucher._Fileextension = DocumentConvert.GetDocumentType(DocumentConvert.GetExtension(ext));
                        }
                        else
                        {
                            attachment = UtilFunctions.LoadFile(voucher._Url).GetAwaiter().GetResult();
                        }
                        if (attachment == null)
                            return;
                    }
                    catch
                    {
                        return;
                    }
                }
                else
                    return;
            }
            // Write the data to the ZIP file  
            var sb = StringBuilderReuse.Create();
            if (exportOption == 1)
            {
                var dt = voucher._PostingDate != DateTime.MinValue ? voucher._PostingDate :
                        (voucher._DocumentDate != DateTime.MinValue ? voucher._DocumentDate : voucher.Created.Date);
                sb.Append(dt).Append('_').Append(voucher._Voucher);
            }
            else
                sb.Append(voucher.RowId);

            if (voucher._Text != null)
                sb.Append('_').Append(voucher._Text.Replace('_', ' ')).Replace('/', '-').Replace('\\', '-').Replace('.', ' ')
                    .Replace('"', '\'').Replace('<', ' ').Replace('>', ' ')
                    .Replace('?', ' ').Replace('*', ' ').Replace(':', ' ');

            if (voucher._Fileextension == FileextensionsTypes.PDF)
                sb.Append('.').Append("PDF");
            else if (voucher._Fileextension == FileextensionsTypes.JPEG)
                sb.Append('.').Append("JPEG");
            else
                sb.Append('.').Append(voucher._Fileextension.ToString());
            var name = sb.ToStringAndRelease();
            try
            {
                if (zipOutputStream != null)
                {
                    zipOutputStream.PutNextEntry(new ZipEntry(name) { IsUnicodeText = true });
                    zipOutputStream.Write(attachment, 0, attachment.Length);
                }
                else
                {
                    File.WriteAllBytes(System.IO.Path.Combine(folderpath, name), attachment);
                }
                WriteLogLine(string.Format(Uniconta.ClientTools.Localization.lookup("ExportingFile"), name));
            }
            catch
            {
                WriteLogLine(string.Format(Uniconta.ClientTools.Localization.lookup("Error"), name));
            }
        }

        private void WriteLogLine(string text)
        {
            logs.AppendLogLine(text);
            txeLog.Focus();
            txeLog.SelectionStart = txeLog.Text.Length - 1;
        }
    }
}
