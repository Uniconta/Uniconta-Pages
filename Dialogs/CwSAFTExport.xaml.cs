using System;
using System.ComponentModel.DataAnnotations;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.Common;
using Uniconta.DataModel;
using UnicontaClient.Controls;
using UnicontaClient.Utilities;
using System.Linq;
using System.IO;
using Uniconta.ClientTools.Util;
using Uniconta.WindowsAPI.GL.SAFT;
using DevExpress.CodeParser;
using Uniconta.ClientTools.Controls;
using DevExpress.Emf;

namespace UnicontaClient.Controls.Dialogs
{
    public partial class CwSAFTExport : ChildWindow
    {
        [InputFieldData]
        [Display(Name = "FromDate", ResourceType = typeof(InputFieldDataText))]
        public static DateTime FromDate { get; set; }

        [InputFieldData]
        [Display(Name = "ToDate", ResourceType = typeof(InputFieldDataText))]
        public static DateTime ToDate { get; set; }

        readonly CrudAPI api;

        public CwSAFTExport(CrudAPI crudApi)
        {
            this.Title = string.Format(Uniconta.ClientTools.Localization.lookup("ExportOBJ"), Uniconta.ClientTools.Localization.lookup("AuditStandardSAFT"));

            var accountingYears = crudApi.QuerySync<CompanyFinanceYearClient>();
            var accountYear = accountingYears.Where(y => y.Current == true).FirstOrDefault();

            FromDate = FromDate != DateTime.MinValue ? FromDate : accountYear.FromDate;
            ToDate = ToDate != DateTime.MinValue ? ToDate : accountYear.ToDate;
            this.DataContext = this;
            InitializeComponent();

            fromDate.DateTime = FromDate;
            toDate.DateTime = ToDate;
            api = crudApi;
        }

        private async void OKButton_Click(object sender, RoutedEventArgs e)
        {
            var folderBrowserDialog = UtilDisplay.LoadFolderBrowserDialog;
            var dialogResult = folderBrowserDialog.ShowDialog();
            if (dialogResult != true)
                return;

            var docInfo = new SAFTDocumentInfo() { Api = api, FromDate = FromDate, ToDate = ToDate, FileName = folderBrowserDialog.SelectedPath };
            try
            {
                if (busyIndicator != null)
                    busyIndicator.IsBusy = true;

                await SAFT.Create(docInfo);
            }
            finally
            {
                if (busyIndicator != null)
                    busyIndicator.IsBusy = false;
            }

            docInfo.XmlDoc.Save(Path.Combine(folderBrowserDialog.SelectedPath, docInfo.FileName));
            SetDialogResult(true);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }
    }
}
