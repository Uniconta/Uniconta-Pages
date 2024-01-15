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
            FromDate = FromDate != DateTime.MinValue ? FromDate : new DateTime(DateTime.Now.Year, 1, 1);
            ToDate = ToDate != DateTime.MinValue ? ToDate : FromDate.AddYears(1).AddSeconds(-1);
            this.DataContext = this;
            InitializeComponent();

            fromDate.DateTime = FromDate;
            toDate.DateTime = ToDate;
            api = crudApi;
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            var docInfo = new SAFTDocumentInfo() { Api = api, FromDate = fromDate.DateTime, ToDate = toDate.DateTime };
            new SAFTCreate(docInfo);
            var folderBrowserDialog = UtilDisplay.LoadFolderBrowserDialog;
            var dialogResult = folderBrowserDialog.ShowDialog();
            if (dialogResult != true)
                return;

            docInfo.XmlDoc.Save(Path.Combine(folderBrowserDialog.SelectedPath, docInfo.FileName));

            SetDialogResult(true);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }
    }
}
