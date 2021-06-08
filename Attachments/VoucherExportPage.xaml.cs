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
            InitializeComponent();
            vouchers = _vouchers;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
        }

        IEnumerable<GLTransClient> glTransLst;
        public VoucherExportPage(IEnumerable<GLTransClient> _glTransLst) : base(_glTransLst.FirstOrDefault())
        {
            InitializeComponent();
            glTransLst = _glTransLst;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
        }

        void localMenu_OnItemClicked(string ActionType)
        {
            if (ActionType == "LocalBackup")
            {
                logs = new ExportLog();
                this.DataContext = logs;
                SaveVouchers();
            }
        }

        async void SaveVouchers()
        {
            string defaultName = string.Concat(Uniconta.ClientTools.Localization.lookup("ExportVouchers"), " ", api.CompanyEntity.Name);

            var saveDialog = Uniconta.ClientTools.Util.UtilDisplay.LoadSaveFileDialog;
#if SILVERLIGHT
            saveDialog.DefaultFileName = defaultName;
#else
            saveDialog.FileName = defaultName;
#endif
            saveDialog.Filter = "ZIP Files (*.zip)|*.zip";
            bool? dialogResult = saveDialog.ShowDialog();
            if (dialogResult == true)
            {
                try
                {
                    leLog.Visibility = Visibility.Visible;
#if SILVERLIGHT
                    using (Stream stream = (Stream)saveDialog.OpenFile())
#else
                    using (Stream stream = File.Create(saveDialog.FileName))
#endif
                    {
                        if (vouchers != null)
                            await CreateZip(vouchers, stream);
                        else if (glTransLst != null)
                            await CreateZip(glTransLst, stream);
                        WriteLogLine(Uniconta.ClientTools.Localization.lookup("ExportComplete"));
                        stream.Close();
                    }
                }
                catch (Exception ex)
                {
                    UnicontaMessageBox.Show(ex);
                    return;
                }
            }
        }

        private async Task CreateZip(IEnumerable<VouchersClient> vouchers, Stream outputStream)
        {
            using (ZipOutputStream zipOutputStream = new ZipOutputStream(outputStream))
            {
                // Highest compression rating
                zipOutputStream.SetLevel(9);
                zipOutputStream.UseZip64 = UseZip64.Dynamic;
                foreach (var voucher in vouchers)
                {
                    if (!voucher._Envelope)
                    {
                        if (voucher._Data == null)
                            await UtilDisplay.GetData(voucher, api);
                        ExportFile(voucher, zipOutputStream);
                    }
                }
                zipOutputStream.Finish();
            }
        }

        private async Task CreateZip(IEnumerable<GLTransClient> glTransLst, Stream outputStream)
        {
            var voucherExpLst = new HashSet<int>();
            var docApi = new Uniconta.API.GeneralLedger.DocumentAPI(api);
            using (ZipOutputStream zipOutputStream = new ZipOutputStream(outputStream))
            {
                // Highest compression rating
                zipOutputStream.SetLevel(9);
                zipOutputStream.UseZip64 = UseZip64.Dynamic;
                foreach (var glTrans in glTransLst)
                {
                    if (!voucherExpLst.Contains(glTrans._DocumentRef))
                    {
                        voucherExpLst.Add(glTrans._DocumentRef);
                        var voucher = new VouchersClient() { RowId = glTrans._DocumentRef };
                        if (await UtilDisplay.GetData(voucher, api) == ErrorCodes.Succes)
                        {
                            if (!voucher._Envelope)
                                ExportFile(voucher, zipOutputStream);
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
                                                    ExportFile(voucher, zipOutputStream);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                zipOutputStream.Finish();
            }
        }

        void ExportFile(VouchersClient voucher, ZipOutputStream zipOutputStream)
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
#if !SILVERLIGHT                           
                            attachment = new WebClient().DownloadData(voucher._Url);
                            var ext = System.IO.Path.GetExtension(voucher._Url);
                            if (ext != null)
                                voucher._Fileextension = DocumentConvert.GetDocumentType(DocumentConvert.GetExtension(ext));
#endif
                        }
                        else
                        {
#if SILVERLIGHT
                            attachment = UtilFunctions.LoadFile(voucher._Url);
#else
                            attachment = UtilFunctions.LoadFile(voucher._Url).GetAwaiter().GetResult();
#endif
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
            var sb = StringBuilderReuse.Create(voucher._Text).Replace('/', '-').Replace('\\', '-');
            sb.Append('_').Append(voucher.RowId).Append('.').Append(Enum.GetName(typeof(FileextensionsTypes), IdObject.get((byte)voucher._Fileextension)));
            var name = sb.ToStringAndRelease();
            zipOutputStream.PutNextEntry(new ZipEntry(name));
            zipOutputStream.Write(attachment, 0, attachment.Length);
            WriteLogLine(string.Format(Uniconta.ClientTools.Localization.lookup("ExportingFile"), name));
        }

        private void WriteLogLine(string text)
        {
            logs.AppendLogLine(text);
            txeLog.Focus();
            txeLog.SelectionStart = txeLog.Text.Length - 1;
        }
    }
}
