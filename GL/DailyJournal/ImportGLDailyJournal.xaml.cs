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
using Uniconta.Common;
using UnicontaClient.Models;
using Uniconta.API.System;
using UnicontaClient.Utilities;
using Uniconta.ClientTools.Controls;
using Uniconta.DataModel;
using Uniconta.ClientTools.DataModel;
using System.IO;
using Uniconta.API.GeneralLedger;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Util;
using UnicontaClient.Controls.Dialogs;
using System.Windows;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip;
using System.Text.RegularExpressions;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    internal class LedgerBankFilter : SQLCacheFilter
    {
        public LedgerBankFilter(SQLCache cache) : base(cache) { }
        public override bool IsValid(object rec)
        {
            var acc = (GLAccount)rec;
            return acc._AccountType == (byte)GLAccountTypes.Bank;
        }
    }

    public partial class ImportGLDailyJournal : FormBasePage
    {
        public override UnicontaBaseEntity ModifiedRow { get; set; }
        public override Type TableType
        {
            get { return null; }
        }
        VouchersClient voucherClient;
        BankImportFormatClient currentBankFormat;
        int selectedFormatIndex;
        readonly UnicontaBaseEntity importMaster;
        bool ImportToBankStatement;
        string BankAccount;

        public ImportGLDailyJournal(UnicontaBaseEntity sourceData) :
            base(sourceData)
        {
            InitializeComponent();
            importMaster = sourceData;
            currentBankFormat = new BankImportFormatClient();
            BankAccountLookupEditor.api = api;
            this.DataContext = currentBankFormat;
            layoutControl = layoutItems;
            frmRibbon.OnItemClicked += frmRibbon_OnItemClicked;

            this.txtAttachedFile.Text = Uniconta.ClientTools.Localization.lookup("NoFileAttached");
            SetBankFormats(true);
        }

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.ImportGLDailyJournalPage2)
            {
                var arg = argument as object[];
                var bankFormat = arg[1] as BankImportFormatClient;
                Dispatcher.BeginInvoke(new Action(() => { SetBankFormats(false, bankFormat.RowId); }));
            }
        }

        async void SetBankFormats(bool initial, int bankImportid = 0)
        {
            var bsClient = importMaster as Uniconta.DataModel.BankStatement;
            BankAccount = bsClient?._Account;
            if (initial)
                ImportToBankStatement = (bsClient != null);

            var bankFormats = await api.Query<BankImportFormatClient>();
            cmdBankFormats.ItemsSource = bankFormats;

            if (bankFormats != null && bankFormats.Length > 0)
            {
                if ((initial && bsClient != null && bsClient._BankImportId != 0) || (!initial && bankImportid > 0))
                {
                    int bankImportRowId = bankImportid > 0 ? bankImportid : bsClient._BankImportId;
                    for (int i = bankFormats.Length; (--i >= 0);)
                        if (bankFormats[i].RowId == bankImportRowId)
                        {
                            selectedFormatIndex = i;
                            break;
                        }
                }
                cmdBankFormats.SelectedIndex = selectedFormatIndex;
            }
            else
                cmdBankFormats.SelectedIndex = -1;

            if (!initial)
                SetValuesForSelectedBankImport();
            else
            {
                var glClient = importMaster as Uniconta.DataModel.GLDailyJournal;
                if (glClient != null)
                    currentBankFormat.Journal = glClient._Journal;
            }
        }
        async void Import(BankImportFormatClient selectedbankFormat)
        {
            var journal = selectedbankFormat._Journal;
            if (journal == null)
                SetMaster(selectedbankFormat);

            var postingApi = new PostingAPI(api);

            var maxline = await postingApi.MaxLineNumber(journal);
            if (maxline == 0)
                doImport(selectedbankFormat, postingApi, true, journal);
            else
            {
                var text = string.Format(Uniconta.ClientTools.Localization.lookup("JournalContainsLines"), journal);
                CWConfirmationBox dialog = new CWConfirmationBox(text, Uniconta.ClientTools.Localization.lookup("Warning"), true);
                dialog.Closing += delegate
                {
                    var res = dialog.ConfirmationResult;
                    if (res != CWConfirmationBox.ConfirmationResultEnum.Cancel)
                        doImport(selectedbankFormat, postingApi, (res == CWConfirmationBox.ConfirmationResultEnum.No), journal);
                };
                dialog.Show();
            }
        }

        async void doImport(BankImportFormatClient selectedbankFormat, PostingAPI postingApi, bool Append, string journal = null)
        {
            if (selectedbankFormat.Format != BankImportFormatType.LANDSBANKINN && selectedbankFormat.Format != BankImportFormatType.ISLANDSBANKI && selectedbankFormat.Format != BankImportFormatType.ARION)
                if (ctrlBrowseFile.SelectedFileInfos == null && voucherClient == null)
                    return;
            selectedbankFormat.BankAccountPos = cbBankAccountPos.Text;
            selectedbankFormat._Reverse = chkReverse.IsChecked.Value;
            if (!selectedbankFormat._BankReconciliation)
            {
                if (selectedbankFormat.Format == BankImportFormatType.LANDSBANKINN || selectedbankFormat.Format == BankImportFormatType.ISLANDSBANKI || selectedbankFormat.Format == BankImportFormatType.ARION)
                    selectedbankFormat._BankAccountNo = txtBankAccount.Text;
                else
                    selectedbankFormat._BankAccount = BankAccountLookupEditor.Text;
                selectedbankFormat._PutLinesOnHold = chkPutLinesOnHold.IsChecked.Value;
            }
            else
            {
                if (selectedbankFormat.Format == BankImportFormatType.LANDSBANKINN || selectedbankFormat.Format == BankImportFormatType.ISLANDSBANKI || selectedbankFormat.Format == BankImportFormatType.ARION)
                    selectedbankFormat._BankAccountNo = txtBankAccount.Text;
            }

            Task<ErrorCodes> errt;
            ErrorCodes err = ErrorCodes.NoSucces;
            var selectedFileInfo = ctrlBrowseFile.SelectedFileInfos;
            if (selectedFileInfo != null && selectedFileInfo.Length > 0 && (selectedbankFormat.Format != BankImportFormatType.LANDSBANKINN || selectedbankFormat.Format != BankImportFormatType.ISLANDSBANKI || selectedbankFormat.Format != BankImportFormatType.ARION))
            {
                busyIndicator.IsBusy = true;
                foreach (var fileInfo in selectedFileInfo)
                {
                    if (fileInfo.FileExtension == ".zip")
                        err = await ImportFromZipFile(selectedbankFormat, fileInfo, postingApi, Append, txtFromDate.DateTime, txtToDate.DateTime);
                    else
                    {
                        errt = postingApi.ImportJournalLines(selectedbankFormat, new MemoryStream(fileInfo.FileBytes), Append, txtFromDate.DateTime, txtToDate.DateTime);
                        err = await errt;
                    }

                    if (err != ErrorCodes.Succes)
                        break;
                }
            }
            else if (selectedbankFormat.Format == BankImportFormatType.LANDSBANKINN || selectedbankFormat.Format == BankImportFormatType.ISLANDSBANKI || selectedbankFormat.Format == BankImportFormatType.ARION)
            {
                busyIndicator.IsBusy = true;
                errt = postingApi.ImportJournalLines(selectedbankFormat, (Stream)null, Append, txtFromDate.DateTime, txtToDate.DateTime);
                err = await errt;
            }
            else if (voucherClient != null)
            {
                busyIndicator.IsBusy = true;
                errt = postingApi.ImportJournalLines(selectedbankFormat, voucherClient, Append, txtFromDate.DateTime, txtToDate.DateTime);
                err = await errt;
            }
            else
                return;

            busyIndicator.IsBusy = false;

            if (err != ErrorCodes.Succes)
                UtilDisplay.ShowErrorCode(err);
            else
            {
                var bsClient = importMaster as Uniconta.DataModel.BankStatement;
                if (bsClient != null)
                    bsClient._BankImportId = selectedbankFormat.RowId;

                ctrlBrowseFile.ResetControl();
                var text = string.Concat(Uniconta.ClientTools.Localization.lookup("ImportCompleted"), Environment.NewLine, string.Format(Uniconta.ClientTools.Localization.lookup("GoTo"),
                    !string.IsNullOrEmpty(journal) ? Uniconta.ClientTools.Localization.lookup("Journallines") : Uniconta.ClientTools.Localization.lookup("BankStatement")), " ?");

                var select = UnicontaMessageBox.Show(text, Uniconta.ClientTools.Localization.lookup("Information"), MessageBoxButton.OKCancel);
                if (select == MessageBoxResult.OK)
                {
                    if (selectedbankFormat._BankReconciliation)
                        ShowBankStatementLines(selectedbankFormat.BankAccount);
                    else
                        ShowGlDailyJournalLines(journal);
                }
            }
        }

        async private Task<ErrorCodes> ImportFromZipFile(BankImportFormatClient selectedbankFormat, SelectedFileInfo fileInfo, PostingAPI postingapi, bool append, DateTime fromDate, DateTime toDate)
        {
            var zipContent = new MemoryStream(fileInfo.FileBytes);
            var zipFile = new ZipFile(zipContent);
            ErrorCodes importZipResult = ErrorCodes.NoSucces;

            foreach (ZipEntry zipEntry in zipFile)
            {
                if (!zipEntry.IsFile)
                    continue;

                var bufferSize = (int)zipEntry.Size;
                var buffer = UnistreamReuse.Create(bufferSize);
                var stream = zipFile.GetInputStream(zipEntry);
                buffer.CopyFrom(stream);

                var zipFileInfo = new FileInfo(zipEntry.Name);
                importZipResult = await postingapi.ImportJournalLines(selectedbankFormat, buffer, append, fromDate, toDate);
                buffer.Release();

                if (importZipResult != ErrorCodes.Succes)
                    break;
            }

            return importZipResult;
        }

        private void ShowGlDailyJournalLines(string journal)
        {
            var parms = new[] { new BasePage.ValuePair("Journal", journal) };
            var header = string.Concat(Uniconta.ClientTools.Localization.lookup("Journal"), ": ", journal);
            AddDockItem(TabControls.GL_DailyJournalLine, null, header, null, true, null, parms);
        }

        private void ShowBankStatementLines(string bankAccount)
        {
            var bankStatement = importMaster as BankStatementClient;
            if (bankStatement != null)
                bankAccount = bankStatement._Account;
            var parms = new[] { new BasePage.ValuePair("Bank", bankAccount) };
            AddDockItem(TabControls.BankStatementLinePage, null, null, null, true, null, parms);
        }

        void frmRibbon_OnItemClicked(string ActionType)
        {
            MoveFocus(true);
            var selectedbankFormat = cmdBankFormats.SelectedItem as BankImportFormatClient;
            selectedFormatIndex = cmdBankFormats.SelectedIndex;

            switch (ActionType)
            {
                case "Import":
                    if (selectedbankFormat != null)
                    {
                        if (selectedbankFormat.Format == BankImportFormatType.LANDSBANKINN || selectedbankFormat.Format == BankImportFormatType.ISLANDSBANKI || selectedbankFormat.Format == BankImportFormatType.ARION)
                        {
                            if (string.IsNullOrWhiteSpace(txtLoginId.Text) || string.IsNullOrEmpty(txtPassowrd.Text) || string.IsNullOrWhiteSpace(txtBankAccount.Text))
                            {
                                string message = string.IsNullOrEmpty(selectedbankFormat.LoginId) ? "LoginId" : string.IsNullOrEmpty(selectedbankFormat.Password) ? "Passord" : "BankAccount";
                                UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("OBJisEmpty"), Uniconta.ClientTools.Localization.lookup(message)), Uniconta.ClientTools.Localization.lookup("Information"), MessageBoxButton.OK);
                                return;
                            }
                            else
                                this.BankAccount = selectedbankFormat._BankAccount;
                        }
                        selectedbankFormat._BankReconciliation = ImportToBankStatement;
                        if (!ImportToBankStatement) //Importing to Journal Lines
                            Import(selectedbankFormat);
                        else // if (selectedImportInto == 1) //Importing to BankStatement Lines--> no journal required
                            doImport(selectedbankFormat, new PostingAPI(api), true);
                    }
                    break;
                case "AddBankFormat":

                    AddDockItem(TabControls.ImportGLDailyJournalPage2, null, Uniconta.ClientTools.Localization.lookup("BankFormatName"), "Add_16x16.png");
                    break;
                case "EditBankFormat":
                    if (selectedbankFormat != null)
                    {
                        object[] Params = new object[2] { selectedbankFormat, true };
                        AddDockItem(TabControls.ImportGLDailyJournalPage2, Params, Uniconta.ClientTools.Localization.lookup("BankFormatName"), "Edit_16x16.png");
                    }
                    break;
                case "LookupAccounts":
                    if (selectedbankFormat != null)
                        AddDockItem(TabControls.BankImportMapping, selectedbankFormat, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Mappings"), selectedbankFormat._Name));
                    break;
                case "CopyRow":
                    if (selectedbankFormat == null)
                        return;
                    CopyBankFormat(selectedbankFormat);
                    break;
                case "ViewBankstatement":
                    if (selectedbankFormat == null)
                        return;
                    ViewBankStatemnt(selectedbankFormat);
                    break;
            }
        }

        void ViewBankStatemnt(BankImportFormatClient selectedBankFormat)
        {
#if !SILVERLIGHT
            var objCw = new CwViewBankStatementData(ctrlBrowseFile.FilePath, selectedBankFormat);
            objCw.Closed += delegate
                {
                    if (objCw.DialogResult == true)
                    {
                        api.Update(selectedBankFormat);
                        UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("PositionUpdateMsg"), Uniconta.ClientTools.Localization.lookup("Message"));
                    }
                };
            objCw.Show();
#endif
        }

        void CopyBankFormat(BankImportFormatClient selectedItem)
        {
            var bankFormat = new BankImportFormatClient();
            StreamingManager.Copy(selectedItem, bankFormat);
            bankFormat.SetMaster(api.CompanyEntity);
            var parms = new object[2] { bankFormat, false };
            AddDockItem(TabControls.ImportGLDailyJournalPage2, parms, Uniconta.ClientTools.Localization.lookup("BankFormatName"), "Add_16x16.png");
        }

        public override void OnClosePage(object[] refreshParams)
        {
            globalEvents.OnRefresh(NameOfControl, refreshParams);
        }

        public override string NameOfControl
        {
            get { return TabControls.ImportGLDailyJournal.ToString(); }
        }

        protected override async void LoadCacheInBackGround()
        {
            var api = this.api;
            var Comp = api.CompanyEntity;

            var Cache = Comp.GetCache(typeof(Uniconta.DataModel.GLAccount));
            if (Cache == null)
                Cache = await Comp.LoadCache(typeof(Uniconta.DataModel.GLAccount), api).ConfigureAwait(false);

            var banks = new LedgerBankFilter(Cache);
            BankAccountLookupEditor.cacheFilter = banks;
        }

        private void btnAttach_Click(object sender, RoutedEventArgs e)
        {
            CWAttachVouchers attachVouchersDialog = new CWAttachVouchers(api);
            attachVouchersDialog.Closing += async delegate
            {
                if (attachVouchersDialog.DialogResult == true)
                {
                    if (attachVouchersDialog.Voucher != null)
                    {
                        voucherClient = attachVouchersDialog.Voucher;
                        if (voucherClient._Data == null)
                            await UtilDisplay.GetData(voucherClient, api);
                        this.txtAttachedFile.Text = voucherClient.Text;
                        this.txtAttachedFile.TextWrapping = TextWrapping.Wrap;
                    }
                }
            };
            attachVouchersDialog.Show();
        }

        private void cmdBankFormats_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            currentBankFormat = cmdBankFormats.SelectedItem as BankImportFormatClient;
            if (currentBankFormat != null)
                SetValuesForSelectedBankImport();
        }

        void SetValuesForSelectedBankImport()
        {
            SetMaster(currentBankFormat);
            this.DataContext = currentBankFormat;
            if (currentBankFormat.Format == BankImportFormatType.LANDSBANKINN || currentBankFormat.Format == BankImportFormatType.ISLANDSBANKI || currentBankFormat.Format == BankImportFormatType.ARION)
            {
                if (!string.IsNullOrEmpty(currentBankFormat._BankAccountNo))
                    txtBankAccount.Text = currentBankFormat._BankAccountNo;
                grpUserLogin.Visibility = Visibility.Visible;
            }
            else
                grpUserLogin.Visibility = Visibility.Collapsed;
        }

        void SetMaster(BankImportFormatClient currentBankFormat)
        {
            txtBankAccount.Text = string.Empty;
            var glClient = importMaster as Uniconta.DataModel.GLDailyJournal;
            if (glClient != null)
            {
                currentBankFormat.Journal = glClient._Journal;
                currentBankFormat._ImportInto = 0;
                currentBankFormat._BankReconciliation = false;
                liBankAccount.Visibility = Visibility.Visible;
            }
            else
            {
                var bsClient = importMaster as Uniconta.DataModel.BankStatement;
                if (bsClient != null)
                {
                    currentBankFormat.BankAccount = bsClient._Account;
                    currentBankFormat._ImportInto = 1;
                    currentBankFormat._BankReconciliation = true;
                    liBankAccount.Visibility = Visibility.Collapsed;
                    liBankAccountPos.Visibility = Visibility.Collapsed;
                    liAddVoucherNumber.Visibility = Visibility.Collapsed;
                    if (currentBankFormat.Format == BankImportFormatType.LANDSBANKINN || currentBankFormat.Format == BankImportFormatType.ISLANDSBANKI || currentBankFormat.Format == BankImportFormatType.ARION)
                        txtBankAccount.Text = bsClient._Account;
                    else
                        grpUserLogin.Visibility = Visibility.Collapsed;
                }
            }
        }
    }
}
