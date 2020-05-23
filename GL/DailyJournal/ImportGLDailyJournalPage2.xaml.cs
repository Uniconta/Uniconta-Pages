using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Navigation;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.DataModel;
using Uniconta.Common;
using UnicontaClient.Models;
using UnicontaClient.Utilities;
using Uniconta.ClientTools.Util;
using Uniconta.API.Service;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class ImportGLDailyJournalPage2 : FormBasePage
    {
        BankImportFormatClient _bankImportFormatClient;
        bool isReadOnly;
        bool isCopiedRow = false;
        public ImportGLDailyJournalPage2(BaseAPI API) : base(API, string.Empty)
        {
            InitializeComponent();
            InitPage();
        }

        public ImportGLDailyJournalPage2(UnicontaBaseEntity corasauBaseEntity, bool IsEdit) :
            base(corasauBaseEntity, IsEdit)
        {
            InitializeComponent();
            isCopiedRow = !IsEdit;
            InitPage();
        }
        private void InitPage()
        {
            BusyIndicator = busyIndicator;
            layoutControl = layoutItems;
            if (LoadedRow == null)
            {
                frmRibbon.DisableButtons(new string[] { "Delete" });
                if (!isCopiedRow)
                {
                    _bankImportFormatClient = CreateNew() as BankImportFormatClient;
                    _bankImportFormatClient.Seperator = UtilFunctions.GetDefaultDeLimiter();
                    _bankImportFormatClient.CountryId = api.CompanyEntity._CountryId;
                }
            }
            else if (LoadedRow.CompanyId == -1)
            {
                isReadOnly = true;
                _bankImportFormatClient = CreateNew() as BankImportFormatClient;
                StreamingManager.Copy(LoadedRow, _bankImportFormatClient);
                _bankImportFormatClient.ClearCompany();
            }
            cmbFormat.ItemsSource = Enum.GetValues(typeof(Uniconta.DataModel.BankImportFormatType));
            layoutItems.DataContext = _bankImportFormatClient;
            frmRibbon.OnItemClicked += frmRibbon_OnItemClicked;
        }

        private void frmRibbon_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "ViewBankstatement":
                    ViewBankStatemnt(_bankImportFormatClient);
                    break;
                default:
                    if (ActionType == "Save" && isReadOnly)
                    {
                        LoadedRow = null;
                        // will make an insert
                    }
                    frmRibbon_BaseActions(ActionType);
                    break;
            }
        }

        void ViewBankStatemnt(BankImportFormatClient selectedBankFormat)
        {
#if !SILVERLIGHT
            var objCw = new CwViewBankStatementData(string.Empty, selectedBankFormat);
            objCw.Closed += delegate
            {
                if (objCw.DialogResult == true)
                {
                    api.Update(selectedBankFormat);
                    Uniconta.ClientTools.Controls.UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("PositionUpdateMsg"), Uniconta.ClientTools.Localization.lookup("Message"));
                }
            };
            objCw.Show();
#endif
        }

        public override UnicontaBaseEntity ModifiedRow { get { return _bankImportFormatClient; } set { _bankImportFormatClient = (BankImportFormatClient)value; } }

        public override Type TableType { get { return typeof(BankImportFormatClient); } }

        public override void OnClosePage(object[] refreshParams) { globalEvents.OnRefresh(NameOfControl, refreshParams); }

        public override string NameOfControl { get { return TabControls.ImportGLDailyJournalPage2; } }
    }
}
