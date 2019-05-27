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
                SetBankFormats(false);
        }

        async void SetBankFormats(bool initial)
        {
            var bsClient = importMaster as Uniconta.DataModel.BankStatement;
            BankAccount = bsClient?._Account;
            if (initial)
                ImportToBankStatement = (bsClient != null);

            var bankFormats = await api.Query<BankImportFormatClient>();
            cmdBankFormats.ItemsSource = bankFormats;

            if (bankFormats != null && bankFormats.Length > 0)
            {
                if (initial && bsClient != null && bsClient._BankImportId != 0)
                {
                    for (int i = bankFormats.Length; (--i >= 0);)
                        if (bankFormats[i].RowId == bsClient._BankImportId)
                        {
                            selectedFormatIndex = i;
                            break;
                        }
                }
                cmdBankFormats.SelectedIndex = selectedFormatIndex;
            }
            else
                cmdBankFormats.SelectedIndex = -1;
        }

        async void Import(BankImportFormatClient selectedbankFormat)
        {
            if (selectedbankFormat._Journal == null)
                SetMaster(selectedbankFormat);

            var postingApi = new PostingAPI(api);

            var maxline = await postingApi.MaxLineNumber(selectedbankFormat._Journal);
            if (maxline == 0)
                doImport(selectedbankFormat, postingApi, true, selectedbankFormat._Journal);
            else
            {
                var text = string.Format(Uniconta.ClientTools.Localization.lookup("JournalContainsLines"), selectedbankFormat._Journal);
                CWConfirmationBox dialog = new CWConfirmationBox(text, Uniconta.ClientTools.Localization.lookup("Warning"), true);
                dialog.Closing += delegate
                {
                    var res = dialog.ConfirmationResult;
                    if (res != CWConfirmationBox.ConfirmationResultEnum.Cancel)
                        doImport(selectedbankFormat, postingApi, (res == CWConfirmationBox.ConfirmationResultEnum.No), selectedbankFormat._Journal);
                };
                dialog.Show();
            }
        }

        void doImport(BankImportFormatClient selectedbankFormat, PostingAPI postingApi, bool Append, string journal = null)
        {
            if (ctrlBrowseFile.SelectedFileInfos == null && voucherClient == null)
                return;

            selectedbankFormat.BankAccountPos = cbBankAccountPos.Text;
            selectedbankFormat._Reverse = chkReverse.IsChecked.Value;
            if (!selectedbankFormat._BankReconciliation)
            {
                selectedbankFormat._BankAccount = BankAccountLookupEditor.Text;
                selectedbankFormat._PutLinesOnHold = chkPutLinesOnHold.IsChecked.Value;
            }
            else
                selectedbankFormat._BankAccount = this.BankAccount;

            var defaultdate = DateTime.MinValue;
            CWInterval winInterval = new CWInterval(defaultdate, defaultdate);
            winInterval.Closed += async delegate
            {
                if (winInterval.DialogResult == true)
                {
                    Task<ErrorCodes> errt;
                    ErrorCodes err = ErrorCodes.NoSucces;
                    var selectedFileInfo = ctrlBrowseFile.SelectedFileInfos;
                    if (selectedFileInfo != null && selectedFileInfo.Length > 0)
                    {
                        busyIndicator.IsBusy = true;
                        foreach (var fileInfo in selectedFileInfo)
                        {
                            if (fileInfo.FileExtension == ".zip")
                                err = await ImportFromZipFile(selectedbankFormat, fileInfo, postingApi, Append, winInterval.FromDate, winInterval.ToDate);
                            else
                            {
                                errt = postingApi.ImportJournalLines(selectedbankFormat, new MemoryStream(fileInfo.FileBytes), Append, winInterval.FromDate, winInterval.ToDate);
                                err = await errt;
                            }

                            if (err != ErrorCodes.Succes)
                                break;
                        }
                    }
                    else if (voucherClient != null)
                    {
                        busyIndicator.IsBusy = true;
                        errt = postingApi.ImportJournalLines(selectedbankFormat, voucherClient, Append, winInterval.FromDate, winInterval.ToDate);
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
                            errt = selectedbankFormat._BankReconciliation ? ShowBankStatementLines(selectedbankFormat.BankAccount) : ShowGlDailyJournalLines(journal);
                            err = await errt;
                        }

                        if (err != ErrorCodes.Succes)
                            UtilDisplay.ShowErrorCode(err);
                    }
                }
            };
            winInterval.Show();
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
                byte[] buffer = new byte[bufferSize];

                var zipFileInfo = new FileInfo(zipEntry.Name);
                importZipResult = await postingapi.ImportJournalLines(selectedbankFormat, new MemoryStream(buffer), append, fromDate, toDate);

                if (importZipResult != ErrorCodes.Succes)
                    break;
            }

            return importZipResult;
        }

        private async Task<ErrorCodes> ShowGlDailyJournalLines(string journal)
        {
            ErrorCodes err = ErrorCodes.Succes;
            var jour = importMaster as GLDailyJournalClient;
            if (jour == null)
            {
                jour = new GLDailyJournalClient();
                jour._Journal = journal;
                err = await api.Read(jour);
            }
            if (err == ErrorCodes.Succes)
                AddDockItem(TabControls.GL_DailyJournalLine, jour, null, null, true);

            return err;
        }

        private async Task<ErrorCodes> ShowBankStatementLines(string bankAccount)
        {
            ErrorCodes err = ErrorCodes.Succes;
            var bankStatement = importMaster as BankStatementClient;
            if (bankStatement == null)
            {
                bankStatement = new BankStatementClient();
                bankStatement._Account = bankAccount;
                err = await api.Read(bankStatement);
            }
            if (err == ErrorCodes.Succes)
                AddDockItem(TabControls.BankStatementLinePage, bankStatement, null, null, true);

            return err;
        }

        void frmRibbon_OnItemClicked(string ActionType)
        {
            var selectedbankFormat = cmdBankFormats.SelectedItem as BankImportFormatClient;
            selectedFormatIndex = cmdBankFormats.SelectedIndex;

            switch (ActionType)
            {
                case "Import":
                    if (selectedbankFormat != null)
                    {
                        selectedbankFormat._BankReconciliation = ImportToBankStatement;
                        if (!ImportToBankStatement) //Importing to Journal Lines
                            Import(selectedbankFormat);
                        else // if (selectedImportInto == 1) //Importing to BankStatement Lines--> no journal required
                            doImport(selectedbankFormat, new PostingAPI(api), true);
                    }
                    break;
                case "AddBankFormat":

                    AddDockItem(TabControls.ImportGLDailyJournalPage2, null, Uniconta.ClientTools.Localization.lookup("BankFormatName"), ";component/Assets/img/Add_16x16.png");
                    break;
                case "EditBankFormat":
                    if (selectedbankFormat != null)
                    {
                        object[] Params = new object[2] { selectedbankFormat, true };
                        AddDockItem(TabControls.ImportGLDailyJournalPage2, Params, Uniconta.ClientTools.Localization.lookup("BankFormatName"), ";component/Assets/img/Edit_16x16.png");
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
            }
        }

        void CopyBankFormat(BankImportFormatClient selectedItem)
        {
            var bankFormat = new BankImportFormatClient();
            StreamingManager.Copy(selectedItem, bankFormat);
            bankFormat.SetMaster(api.CompanyEntity);
            var parms = new object[2] { bankFormat, false };
            AddDockItem(TabControls.ImportGLDailyJournalPage2, parms, Uniconta.ClientTools.Localization.lookup("BankFormatName"), ";component/Assets/img/Add_16x16.png");
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
                            await api.Read(voucherClient);
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
            {
                SetMaster(currentBankFormat);
                this.DataContext = currentBankFormat;
            }
        }

        void SetMaster(BankImportFormatClient currentBankFormat)
        {
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
                }
            }
        }
    }
}
