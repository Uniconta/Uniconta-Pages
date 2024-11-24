using System;
using Uniconta.Common;
using Uniconta.DataModel;
using System.IO;
using Uniconta.API.System;
using Uniconta.Common.Utility;
using System.Collections.Generic;
using Uniconta.ClientTools.Page;
using Uniconta.API.Service;
using Uniconta.ClientTools.Util;
using Uniconta.ClientTools.Controls;
using static UnicontaClient.Pages.NewImportPhysicalVouchersPage;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class ImportVouchersPage : ControlBasePage
    {
        private ImportLogVoucher _logs;
        Company company;

        public ImportVouchersPage(BaseAPI API)
            : base(API, string.Empty)
        {
            _logs = new ImportLogVoucher();
            this.DataContext = this;
            InitializeComponent();
            company = api.CompanyEntity;
            leReading.Label = string.Format(Uniconta.ClientTools.Localization.lookup("LoadOBJ"), Uniconta.ClientTools.Localization.lookup("Status"));
            leWriting.Label = string.Format(Uniconta.ClientTools.Localization.lookup("SaveOBJ"), Uniconta.ClientTools.Localization.lookup("Status"));
            browseCtrlColumn.Label = Uniconta.ClientTools.Localization.lookup("SelectDirectory");
            txtLogs.DataContext = txtReading.DataContext = txtWriting.DataContext = _logs;
            progressBarSave.Minimum = progressBarLoad.Minimum = 0;
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
            UtilDisplay.RemoveMenuCommand((RibbonBase)localMenu.DataContext, new string[] { "Terminate" });
            localMenu.DisableButtons(new string[] { "ImportData", "CopyData" });
        }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "ImportData":
                    Import();
                    break;
                case "CopyData":
                    if (!string.IsNullOrEmpty(txtLogs.Text))
                    {
                        System.Windows.Forms.Clipboard.SetText(txtLogs.Text);
                        if (api.CompanyEntity._CountryId == CountryCode.Denmark)
                            UnicontaMessageBox.Show("Du kan nu Ctrl+V i dit ønskede textprogram", Uniconta.ClientTools.Localization.lookup("Message"));
                        else
                            UnicontaMessageBox.Show("You can now Ctrl+V in your choosen texteditor", Uniconta.ClientTools.Localization.lookup("Message"));
                    }
                    break;
            }
        }

        List<FileInfo> filesToCopy;

        void Import()
        {
            var path = txtImportFromDirectory.Text;
            filesToCopy = new List<FileInfo>();
            ReadDirectory(new DirectoryInfo(path));

            if (filesToCopy.Count != 0)
                CopyFiles();
        }

        void ReadDirectory(DirectoryInfo dirFrom)
        {
            foreach (var subDFrom in dirFrom.GetDirectories())
                ReadDirectory(subDFrom);

            filesToCopy.AddRange(dirFrom.GetFiles());
        }

        async void CopyFiles()
        {
            var imp = new ImportVoucher(new CrudAPI(BasePage.session, company), _logs);
            int fileCount = 0;
            var sp = new StringSplit('_');
            var lst = new List<string>();
            var st = UnistreamReuse.Create();

            foreach (var file in filesToCopy)
            {
                try
                {
                    fileCount++;
                    _logs.ReadMsg = string.Concat("File: ", file.Name, " ", NumberConvert.ToString(fileCount));
                    Dispatcher.Invoke(new Action(() => { progressBarLoad.Value = fileCount; }));

                    sp.Split(file.Name, lst);
                    if (lst.Count < 2)
                    {
                        _logs.AppendLogLine("File not correctly formattet " + file.Name);
                        continue;
                    }
                    var date = StringSplit.DateParse(lst[0], DateFormat.ymd);
                    if (date == DateTime.MinValue)
                        date = StringSplit.DateParse(lst[0], DateFormat.FromSettings);
                    if (date == DateTime.MinValue)
                    {
                        _logs.AppendLogLine("Date not correctly formattet" + file.Name);
                        continue;
                    }

                    var s = lst[lst.Count - 1];
                    var idx = s.IndexOf('.');
                    if (idx >= 0)
                    {
                        var ext = s.Substring(idx + 1);
                        var _ext = DocumentConvert.GetDocumentType(ext);

                        string Text = null;
                        var Voucher = lst[1];
                        if (lst.Count == 2)
                            Voucher = Voucher.Substring(0, idx);
                        else if (lst.Count == 3)
                            Text = lst[2].Substring(0, idx);
                        else
                            Text = lst[2];

                        bool HasLetters;
                        var n = NumberConvert.GetOnlyNumbers(Voucher, out HasLetters);
                        if (HasLetters)
                            Voucher = NumberConvert.ToString(n);

                        if (Voucher.Length == 0 || Voucher == "0")
                        {
                            _logs.AppendLogLine("Voucher not correctly formattet: '" + Voucher + "' " + file.Name);
                            continue;
                        }

                        using (var fs = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, 4096 * 16))
                        {
                            st.Reset();
                            st.CopyFrom(fs);
                            st.SetPosition(0);
                        }

                        byte[] buf;
                        if (_ext == FileextensionsTypes.JPEG)
                            buf = FileBrowseControl.ImageResize(st, ".jpg");
                        else
                            buf = null;

                        await imp.Import(Voucher, date, _ext, Text, buf ?? st.ToArray());
                        _logs.WriteMsg = string.Format("{0} / {1}", fileCount, filesToCopy.Count);
                        Dispatcher.Invoke(new Action(() =>
                        {
                            progressBarSave.Value = fileCount;
                        }));
                    }
                }
                catch (Exception ex)
                {
                    _logs.ReadMsg = ex.Message;
                }
            }
            st.Release();
            _logs.AppendLogLine(Uniconta.ClientTools.Localization.lookup("Done"));
        }

        private void browseCtrlColumn_ButtonClicked(object sender)
        {
            var openFolderDialog = UtilDisplay.LoadFolderBrowserDialog;
            if (openFolderDialog.ShowDialog() == true)
            {
                txtImportFromDirectory.Text = openFolderDialog.SelectedPath;
                localMenu.EnableButtons(new string[] { "ImportData", "CopyData" });
            }
        }
    }
}
