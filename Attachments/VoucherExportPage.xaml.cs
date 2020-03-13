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

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{

    public partial class VoucherExportPage : FormBasePage
    {
        public override string NameOfControl
        {
            get { return TabControls.VoucherExportPage; }
        }
        public override Type TableType
        {
            get { return null; }
        }

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
            string defaultName = string.Format("{0} {1}", Uniconta.ClientTools.Localization.lookup("ExportVouchers"), api.CompanyEntity.Name);

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
                    using(Stream stream = File.Create(saveDialog.FileName))
#endif
                    {
                        await CreateZip(vouchers, stream);
                        WriteLogLine(Uniconta.ClientTools.Localization.lookup("ExportComplete"));
                        stream.Close();
                    }
                }
                catch (Exception ex)
                {
                    UnicontaMessageBox.Show(ex, Uniconta.ClientTools.Localization.lookup("Exception"), MessageBoxButton.OK);
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
                    if (voucher._Envelope)
                        continue;
                    if (voucher._Data == null)
                        await api.Read(voucher);
                    byte[] attachment = voucher.Buffer;
                    if (attachment == null)
                        continue;
                    // Write the data to the ZIP file  
                    string name = string.Format("{0}_{1}.{2}", voucher._Text, voucher.RowId, Enum.GetName(typeof(FileextensionsTypes), voucher._Fileextension));
                    name = name.Replace("/", "-").Replace(@"\", "-");
                    ZipEntry entry = new ZipEntry(name);
                    zipOutputStream.PutNextEntry(entry);
                    zipOutputStream.Write(attachment, 0, attachment.Length);
                    WriteLogLine(string.Format(Uniconta.ClientTools.Localization.lookup("ExportingFile"), name));

                    voucher._Data = null;
                    voucher._LoadedData = null;
                }
                zipOutputStream.Finish();
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
