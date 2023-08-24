using System;
using System.Text;
using System.Windows;
using Uniconta.Common;
using Uniconta.DataModel;
using System.ComponentModel;
using System.IO;
using System.Windows.Shapes;
using System.Linq;
using Uniconta.API.System;
using Uniconta.Common.Utility;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Forms;
using Uniconta.ClientTools.Page;
using UnicontaClient.Controls;
using Uniconta.API.Service;
using Uniconta.ClientTools.Util;

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
            _logs.logTarget = txtLogs;
            this.DataContext = _logs;
            InitializeComponent();
            btnImport.IsEnabled = false;
            txtImportFromDirectory.TextChanged += TxtImportFromDirectory_TextChanged;
        }

        private void TxtImportFromDirectory_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            btnImport.IsEnabled = true;
            txtImportFromDirectory.TextChanged -= TxtImportFromDirectory_TextChanged;

        }


        DevExpress.Xpf.Dialogs.DXFolderBrowserDialog openFolderDialog;
        string path;
        private void btnImportFromDir_Click(object sender, RoutedEventArgs e)
        {
            openFolderDialog = UtilDisplay.LoadFolderBrowserDialog;
            if (openFolderDialog.ShowDialog() == true)
            {
                txtImportFromDirectory.Text = openFolderDialog.SelectedPath;
                btnImport.IsEnabled = true;
            }
        }
        private async void btnImport_Click(object sender, RoutedEventArgs e)
        {
            path = txtImportFromDirectory.Text;
            company = api.CompanyEntity;
            if (company == null)
                return;
            if (string.IsNullOrEmpty(path))
            {
                System.Windows.MessageBox.Show("Select directory");
                return;
            }
            company = await BasePage.session.OpenCompany(company.RowId, false, company).ConfigureAwait(false);
            ReadDirectory(new DirectoryInfo(path));
        }
        void ReadDirectory(DirectoryInfo dirFrom)
        {
            foreach (var subDFrom in dirFrom.GetDirectories())
            {
                ReadDirectory(subDFrom);
            }
            CopyFiles(dirFrom);
        }

        async void CopyFiles(DirectoryInfo dirFrom)
        {
            var imp = new ImportVoucher(new CrudAPI(BasePage.session, company), _logs);
            int fileCount = 0;
            var sp = new StringSplit('_');
            var lst = new List<string>();
            var st = UnistreamReuse.Create();
            var files = dirFrom.GetFiles();

            foreach (var file in files)
            {
                try
                {
                    fileCount++;
                    _logs.ReadMsg = string.Concat("File: ", file.Name, " ", NumberConvert.ToString(fileCount));

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

                        if (Voucher.Length == 0 || Voucher == "0")
                        {
                            _logs.AppendLogLine("Voucher not correctly formattet: '" + Voucher + "' " + file.Name);
                            continue;
                        }

                        using (var fs = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, 4096 * 16))
                        {
                            st.Reset();
                            st.CopyFrom(fs);
                        }
                        await imp.Import(Voucher, date, _ext, Text, st);
                    }
                }
                catch (Exception ex)
                {
                    _logs.ReadMsg = ex.Message;
                }
            }
            st.Release();
        }
    }
}
