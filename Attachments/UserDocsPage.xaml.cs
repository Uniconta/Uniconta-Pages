using System;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using Uniconta.ClientTools.Page;
using UnicontaClient.Models;
using Uniconta.ClientTools.DataModel;
using System.Threading.Tasks;
using Uniconta.Common;
using UnicontaClient.Utilities;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using System.Collections;
using System.Windows;
using Uniconta.API.System;
using Uniconta.ClientTools.Util;
using Uniconta.API.Service;
using Uniconta.Common.Utility;
using UnicontaClient.Controls.Dialogs;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class DocumentInfoGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(UserDocsClient); } }
        public override bool Readonly { get { return false; } }
        public override bool CanInsert { get { return false; } }
        public override bool IsAutoSave { get { return false; } }
        public override IComparer GridSorting { get { return new SortUserDocs(); } }

        async public override Task<ErrorCodes> SaveData()
        {
            SelectedItem = null;
            /*
             * Awaiting the save data as we need to have rowid
             * With row id we would be able to save the buffers seperately
             * So first we save the records and then update the buffers
             */
            var addedRows = AddedRows;
            var addedRowCount = addedRows.Count();

            if (addedRowCount > 0)
            {
                var userDocs = new UserDocsClient[addedRowCount];
                var buffers = new byte[addedRowCount][];
                int iCtr = 0;

                foreach (var row in addedRows)
                {
                    var doc = row.DataItem as UserDocsClient;
                    userDocs[iCtr] = doc;
                    if (doc._Data != null)
                    {
                        if (doc._DocumentType == FileextensionsTypes.JPEG ||
                            doc._DocumentType == FileextensionsTypes.BMP ||
                            doc._DocumentType == FileextensionsTypes.GIF ||
                            doc._DocumentType == FileextensionsTypes.TIFF)
                        {
                            var imageBytes = FileBrowseControl.ImageResize(doc._Data, ".jpg");
                            if (imageBytes != null)
                            {
                                doc._Data = imageBytes;
                                doc._NoCompression = (doc._DocumentType == FileextensionsTypes.JPEG);
                                doc._DocumentType = FileextensionsTypes.JPEG;
                            }
                        }
                        doc._SizeKB = (short)((doc._Data.Length + 512) >> 10);
                        buffers[iCtr] = doc._Data;
                        doc._Data = null;
                        iCtr++;
                    }
                }

                var result = await base.SaveData();
                if (result != ErrorCodes.Succes)
                    return result;

                UpdateDocsBuffer(api, buffers, userDocs, iCtr);
                this.RefreshData();

                return 0;
            }
            else
                return await base.SaveData();
        }

        static void UpdateDocsBuffer(CrudAPI api, byte[][] buffers, UserDocsClient[] multiDocs, int Cnt)
        {
            for (int i = 0; i < Cnt; i++)
            {
                var updateDoc = multiDocs[i];
                updateDoc._Data = buffers[i];

                var org = new UserDocsClient();
                org.Copy(updateDoc);
                org._Data = updateDoc._Data;
                multiDocs[i] = org;
            }
            uploadingDocs = Cnt;
            UpdateBuffersOne(api, multiDocs, 0);
        }

        static public int uploadingDocs;

        static void UpdateBuffersOne(CrudAPI api, UserDocsClient[] multiVouchers, int i)
        {
            var vouchersClient = multiVouchers[i];
            if (vouchersClient == null)
            {
                for (int j = 0; (j < multiVouchers.Length); j++)
                    if (multiVouchers[j] != null)
                    {
                        UpdateBuffersOne(api, multiVouchers, j);
                        return;
                    }
                uploadingDocs = 0;
                return;
            }

            var org = new UserDocsClient();
            org.Copy(vouchersClient);

            int cnt = 0;
        retry:

            try
            {
                var tsk = api.Update(org, vouchersClient);
                tsk.ContinueWith((e) =>
                {
                    int next;
                    if (!e.IsFaulted)
                    {
                        multiVouchers[i] = null;
                        next = i + 1; // go to next
                    }
                    else
                        next = i;

                    var remaining = multiVouchers.Length - next;
                    if (remaining > 0)
                    {
                        uploadingDocs = remaining;
                        UpdateBuffersOne(api, multiVouchers, next);
                    }
                    else
                        uploadingDocs = 0;

                }, TaskContinuationOptions.None);

                var tskw = Task.Delay(60000);
                tskw.ContinueWith((e) =>
                {
                    if (multiVouchers[i] != null)
                    {
                        if ((i + 1) < multiVouchers.Length)
                            UpdateBuffersOne(api, multiVouchers, i + 1);

                        Task.Delay(5000).ContinueWith((x) =>
                        {
                            UpdateBuffersOne(api, multiVouchers, i);
                        }, TaskContinuationOptions.None);
                    }

                }, TaskContinuationOptions.None);
            }
            catch (Exception ex)
            {
                if (++cnt < 3)
                    goto retry;
                throw ex;
            }
        }
    }

    public partial class UserDocsPage : GridBasePage
    {
        private bool isDragDropAllowed = true;
        public UserDocsPage(UnicontaBaseEntity masterRecord)
            : base(masterRecord)
        {
            InitializeComponent();
            InitPage(masterRecord);
        }
        CrudAPI crudApi;
        bool isCompUserDoc;
        public UserDocsPage(UnicontaBaseEntity sourcedata, string companyUserDoc)
           : base(null)
        {
            InitializeComponent();
            crudApi = new CrudAPI(session, sourcedata as CompanyClient ?? Uniconta.DataModel.Company.Get(sourcedata.CompanyId));
            isCompUserDoc = true;
            InitPage(sourcedata);
        }

        private void InitPage(UnicontaBaseEntity masterRecord)
        {
            if (!isCompUserDoc)
            {
                dgDocsGrid.api = api;
                isDragDropAllowed = masterRecord is CompanyClient ? false : true;
            }
            else
                dgDocsGrid.api = crudApi;

            dgDocsGrid.BusyIndicator = busyIndicator;
            dgDocsGrid.UpdateMaster(masterRecord);
            SetRibbonControl(localMenu, dgDocsGrid);

            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgDocsGrid.View.DataControl.CurrentItemChanged += DataControl_CurrentItemChanged;
            dgDocsGrid.tableView.AllowDragDrop = isDragDropAllowed;
            dgDocsGrid.tableView.DropRecord += dgDocsGrid_DropRecord;
            dgDocsGrid.tableView.DragRecordOver += dgDocsGrid_DragRecordOver;
            if (!(masterRecord is Uniconta.DataModel.DCOrder || masterRecord is Uniconta.DataModel.DCInvoice || masterRecord is Uniconta.DataModel.CompanySMTP))
            {
                Invoice.Visible = Invoice.ShowInColumnChooser = false;
                Offer.Visible = Offer.ShowInColumnChooser = false;
                Confirmation.Visible = Confirmation.ShowInColumnChooser = false;
                PackNote.Visible = PackNote.ShowInColumnChooser = false;
                Requisition.Visible = Requisition.ShowInColumnChooser = false;
                PurchaseOrder.Visible = PurchaseOrder.ShowInColumnChooser = false;
            }
            if (!api.CompanyEntity._AllowDocumentGuid)
            {
                DocumentGuid.Visible = DocumentGuid.ShowInColumnChooser = false;
                HideMenuItems();
            }
        }

        private void HideMenuItems()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            if (rb != null)
                UtilDisplay.RemoveMenuCommand(rb, "ViewInWeb");
        }

        UserDocsClient SelectedItem;
        private void DataControl_CurrentItemChanged(object sender, DevExpress.Xpf.Grid.CurrentItemChangedEventArgs e)
        {
            var oldSelectedItem = e.OldItem as UserDocsClient;
            var selectedItem = e.NewItem as UserDocsClient;
            SelectedItem = selectedItem;

            if (oldSelectedItem != selectedItem && dgDocsGrid._syncEntity != null)
            {
                dgDocsGrid._syncEntity.Row = selectedItem;
                dgDocsGrid._syncEntity.RowChaged();
            }
        }

        private void dgDocsGrid_DropRecord(object sender, DevExpress.Xpf.Core.DropRecordEventArgs e)
        {
            dgDocsGrid.tableView.FocusedRowHandle = e.TargetRowHandle > 0 ? e.TargetRowHandle - 1 : -1;
            string[] errors = null;

            if (e.Data.GetDataPresent(System.Windows.Forms.DataFormats.FileDrop))
            {
                var files = e.Data.GetData(System.Windows.Forms.DataFormats.FileDrop) as string[];
                if (files == null || files.Length == 0)
                    return;

                errors = Utility.DropFilesToGrid(dgDocsGrid, files, false);
            }
            else if (e.Data.GetDataPresent("FileGroupDescriptor"))
                errors = Utility.DropOutlookMailsToGrid(dgDocsGrid, e.Data, false);

            if (errors != null && errors.Length > 0)
            {
                var cwErrorBox = new CWErrorBox(errors, true);
                cwErrorBox.Show();
            }
        }

        private void dgDocsGrid_DragRecordOver(object sender, DevExpress.Xpf.Core.DragRecordOverEventArgs e)
        {
            if (e.Data.GetFormats().Contains("FileName"))
                e.Effects = System.Windows.DragDropEffects.Copy;
            else if (e.Data.GetFormats().Contains("FileGroupDescriptor"))
                e.Effects = System.Windows.DragDropEffects.All;
            else
                e.Effects = System.Windows.DragDropEffects.None;

            e.Handled = true;
        }
        void SetColumn()
        {
            if (dgDocsGrid.ItemsSource != null)
            {
                var source = dgDocsGrid.ItemsSource as UserDocsClient[];
                if (source != null && source.Length > 0)
                {
                    var userDocsClientRow = source[0];
                    if (userDocsClientRow.TableId == 71) /* Sales Order */
                        Requisition.Visible = false;
                    else if (userDocsClientRow.TableId == 72) /*Purchase Order */
                        Invoice.Visible = Confirmation.Visible = Offer.Visible = Invoice.Visible = PackNote.Visible = false;
                    else if (userDocsClientRow.TableId == 73) /*Offer */
                        Invoice.Visible = Confirmation.Visible = Requisition.Visible = Invoice.Visible = PackNote.Visible = false;
                    else
                        Requisition.Visible = Invoice.Visible = Confirmation.Visible = Offer.Visible = Invoice.Visible = PackNote.Visible = false;
                }
            }
        }
        SynchronizeEntity synchronizeEntity;

        public UserDocsPage(SynchronizeEntity entity) : base(entity, true)
        {
            InitializeComponent();
            synchronizeEntity = entity;
            InitPage(entity.Row);
            SetHeader();
        }

        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            dgDocsGrid.UpdateMaster(args);
            SetHeader();
            InitQuery();
        }

        private void SetHeader()
        {
            string headerStr = Utility.GetHeaderString(dgDocsGrid.masterRecord);
            if (string.IsNullOrEmpty(headerStr))
                return;
            string header = string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Documents"), headerStr);
            SetHeader(header);
        }

        public async override Task InitQuery()
        {
            await dgDocsGrid.Filter(null);
            SetColumn();
        }

        protected override void LoadCacheInBackGround() { LoadType(typeof(Uniconta.DataModel.AttachmentGroup)); }

        private void localMenu_OnItemClicked(string ActionType)
        {
            string headerStr = Utility.GetHeaderString(dgDocsGrid.masterRecord);
            headerStr = headerStr != null ? string.Format("{0}:{1}", Uniconta.ClientTools.Localization.lookup("Documents"), headerStr) : Uniconta.ClientTools.Localization.lookup("Documents");
            var selectedItem = SelectedItem ?? dgDocsGrid.SelectedItem as UserDocsClient;
            int arrayIndex = 2;
            if (isCompUserDoc) arrayIndex = 3;
            switch (ActionType)
            {
                case "AddRow":
                    var newItem = dgDocsGrid.GetChildInstance();
                    object[] param = new object[arrayIndex];
                    param[0] = newItem;
                    param[1] = false;
                    if (isCompUserDoc) param[2] = crudApi;
                    AddDockItem(TabControls.UserDocsPage2, param, headerStr, "Add_16x16");
                    break;
                case "EditRow":
                    if (selectedItem == null || selectedItem.Created == DateTime.MinValue)
                        return;
                    object[] para = new object[arrayIndex];
                    para[0] = selectedItem;
                    para[1] = true;
                    if (isCompUserDoc) para[2] = crudApi;
                    AddDockItem(TabControls.UserDocsPage2, para, headerStr, "Edit_16x16");
                    break;

                case "RefreshGrid":
                    if (dgDocsGrid.HasUnsavedData)
                        Utility.ShowConfirmationOnRefreshGrid(dgDocsGrid);
                    else
                        gridRibbon_BaseActions(ActionType);
                    break;

                case "ViewDownloadRow":
                    if (selectedItem != null)
                        ViewDocument(TabControls.UserDocsPage3, dgDocsGrid.syncEntity);
                    break;

                case "Save":
                    dgDocsGrid.SaveData();
                    break;
                case "JoinPDF":
                    JoinPDFAttachments();
                    break;
                case "ConvertToPDF":
                    if (selectedItem != null)
                        ImageToPdfConverter(selectedItem);
                    break;
                case "ViewInWeb":
                    if (selectedItem == null || selectedItem._DocumentGuid == Guid.Empty)
                        return;
                    var url = string.Concat("https://web.uniconta.com/document/ViewAttachment?", selectedItem._DocumentGuid);
                    if (BasePage.session.Connection.Target == APITarget.Demo || BasePage.session.Connection.Target == APITarget.Testing)
                        url = string.Concat("https://test.uniconta.com:8080/document/ViewAttachment?", selectedItem._DocumentGuid);
                    System.Diagnostics.Process.Start(url);
                    break;
                case "AttachInOutlook":
                    if (dgDocsGrid.SelectedItems == null || selectedItem == null)
                        return;

                    OpenOutlook(dgDocsGrid.SelectedItems == null ? new UserDocsClient[1] { selectedItem } :
                        dgDocsGrid.SelectedItems.Cast<UserDocsClient>().ToArray());
                    break;
                case "UpdateRow":
                    if (selectedItem != null)
                        UploadData(selectedItem);
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        async private void OpenOutlook(UserDocsClient[] userDocClients)
        {
            string subject = null;
            StringBuilderReuse filePath = null;
            StringBuilderReuse body = null;
            int index = 0;
            var count = userDocClients.Length;
            if (count != 0)
            {
                subject = count > 1 ? null : userDocClients[0].Text;
                foreach (var userDocClient in userDocClients)
                {
                    var userDoc = await UtilDisplay.GetUserDocDataOrUrl(userDocClient, api);
                    if (userDoc._Data != null)
                    {
                        var path = GetPath(userDoc._Data, userDoc.DocumentType, string.Format("{0}_{1}",
                            Uniconta.ClientTools.Localization.lookup("Attachment"), ++index));

                        if (string.IsNullOrEmpty(path))
                            continue;

                        if (filePath == null)
                            filePath = StringBuilderReuse.Create().Append(path);
                        else
                            filePath.Append('|').Append(path);
                    }
                    else if (!string.IsNullOrEmpty(userDoc._Url))
                    {
                        if (body == null)
                            body = StringBuilderReuse.Create().AppendLine(userDoc._Url);
                        else
                            body.AppendLine(userDoc._Url);
                    }
                }
            }

            string emailBody = body?.ToString();
            string attachments = filePath?.ToString();

            if(string.IsNullOrEmpty(emailBody) && string.IsNullOrEmpty(attachments))
            {
                UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("NoRecords"), Uniconta.ClientTools.Localization.lookup("Warning"));
                return;
            }

            Utility.DisplayOutlookAsync(string.Empty, string.Empty, string.Empty, emailBody, subject, attachments, string.Empty, false);
        }


        private string GetPath(byte[] userDocData, FileextensionsTypes fileExt, string documentName)
        {
            var tempPath = System.IO.Path.GetTempPath();
            var curFilePath = fileExt != FileextensionsTypes.UNK ? string.Concat(tempPath, documentName, ".", fileExt.ToString().ToLower()) :
                   string.Concat(tempPath, documentName);

            using (System.IO.Stream file = System.IO.File.OpenWrite(curFilePath))
                file.Write(userDocData, 0, userDocData.Length);

            return curFilePath;
        }

        private async void ImageToPdfConverter(UserDocsClient userDocClient)
        {
            if (userDocClient._DocumentType != FileextensionsTypes.JPEG || userDocClient._DocumentType != FileextensionsTypes.PNG)
            {
                if (UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("AreYouSureToContinue"),
                    Uniconta.ClientTools.Localization.lookup("Confirmation"), MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                    return;

                busyIndicator.IsBusy = true;
                var userDoc = await UtilDisplay.GetUserDocDataOrUrl(userDocClient, api);
                var pdfBytes = UtilDisplay.ConvertImageSourceToPDF(userDoc._Data);

                if (pdfBytes == null || pdfBytes.Length == 0)
                {
                    busyIndicator.IsBusy = false;
                    UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("PdfStreamfailed"), Uniconta.ClientTools.Localization.lookup("Error"));
                    return;
                }

                //To save any information on the editable grid
                saveGrid();

                userDocClient._DocumentType = FileextensionsTypes.PDF;
                userDocClient._Data = pdfBytes;
                var result = await api.Update(userDocClient);

                if (result != ErrorCodes.Succes)
                    UtilDisplay.ShowErrorCode(result);
                else
                    dgDocsGrid.Filter(null);
            }
            else
                UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("ConversionNotSupport"), Uniconta.ClientTools.Localization.lookup("Warning"));
        }

        async void JoinPDFAttachments()
        {
            UserDocsClient attachement1 = null, attachement2 = null;
            CWJoinPDFDocument cwJoinPdfDoc = null;

            if (dgDocsGrid.SelectedItems != null)
            {
                var attachements = dgDocsGrid.SelectedItems.Cast<UserDocsClient>().ToArray();
                if (attachements.Length == 0)
                    return;
                if (attachements.Any(v => v._DocumentType != FileextensionsTypes.PDF))
                {
                    UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("InvalidFileFormat"), Uniconta.ClientTools.Localization.lookup("Error"), MessageBoxButton.OK);
                    return;
                }

                if (attachements.Length >= 2)
                    attachement2 = attachements[1];

                attachement1 = attachements[0];

                if (attachement1 != null && attachement1._Data == null)
                    await api.Read(attachement1);

                if (attachement2 != null && attachement2._Data == null)
                    await api.Read(attachement2);
            }

            //To save any information on the editable grid
            saveGrid();

            if (attachement1 != null && attachement2 != null)
                cwJoinPdfDoc = new CWJoinPDFDocument(attachement1._Data, attachement2._Data);
            else if (attachement1 != null)
                cwJoinPdfDoc = new CWJoinPDFDocument(attachement1._Data);
            else if (attachement2 != null)
                cwJoinPdfDoc = new CWJoinPDFDocument(null, attachement2._Data);
            else
                cwJoinPdfDoc = new CWJoinPDFDocument();

            cwJoinPdfDoc.Closed += delegate
           {
               if (cwJoinPdfDoc.DialogResult == true)
               {
                   var mergedContents = cwJoinPdfDoc.MergedPDFContents;
                   var deleteMsg = string.Empty;

                   if (cwJoinPdfDoc.IsLeftPdfDelete)
                       deleteMsg = string.Format(Uniconta.ClientTools.Localization.lookup("ConfirmDeleteOBJ"), string.Format("{0} {1}",
                           Uniconta.ClientTools.Localization.lookup("Left"), Uniconta.ClientTools.Localization.lookup("Attachment")));
                   else if (cwJoinPdfDoc.IsRightPdfDelete)
                       deleteMsg = string.Format(Uniconta.ClientTools.Localization.lookup("ConfirmDeleteOBJ"), string.Format("{0} {1}",
                          Uniconta.ClientTools.Localization.lookup("Right"), Uniconta.ClientTools.Localization.lookup("Attachment")));

                   var deleteAttachment = !string.IsNullOrEmpty(deleteMsg) && attachement1.RowId > 0 && UnicontaMessageBox.Show(deleteMsg, Uniconta.ClientTools.Localization.lookup("Warning"), MessageBoxButton.YesNo)
                          == MessageBoxResult.Yes ? true : false;

                   if (cwJoinPdfDoc.IsLeftJoin)
                       UpdateJoinedPDFContents(attachement1, attachement2, mergedContents, deleteAttachment);
                   else
                       UpdateJoinedPDFContents(attachement2, attachement1, mergedContents, deleteAttachment);
               }
           };

            cwJoinPdfDoc.Show();
        }

        async private void UpdateJoinedPDFContents(UserDocsClient saveAttachment, UserDocsClient copiedAttachment, byte[] mergedContents, bool isDeleteAttachment)
        {
            saveAttachment._Data = mergedContents;
            busyIndicator.IsBusy = true;

            if (saveAttachment.RowId > 0)
            {
                api.CompanyEntity.AttachmentCacheSet(saveAttachment);
                api.UpdateNoResponse(saveAttachment);
            }
            else
                await api.Insert(saveAttachment);

            if (isDeleteAttachment && copiedAttachment != null)
            {
                api.CompanyEntity.AttachmentCacheDelete(copiedAttachment);
                await api.Delete(copiedAttachment);
            }

            await dgDocsGrid.Filter(null);
            busyIndicator.IsBusy = false;
        }

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.UserDocsPage2)
            {
                dgDocsGrid.UpdateItemSource(argument);
                var master = dgDocsGrid.masterRecord as IdKey;
                if (master != null)
                {
                    if (dgDocsGrid.VisibleRowCount > 0)
                        master.HasDocs = true;
                    else
                        master.HasDocs = false;
                }
                else if (master == null)
                {
                    var tableType = dgDocsGrid.masterRecord?.GetType();
                    var hasDocsProp = tableType?.GetProperty("HasDocs");
                    if (hasDocsProp != null)
                    {
                        var hasRecords = dgDocsGrid.VisibleRowCount > 0 ? true : false;
                        hasDocsProp.SetValue(dgDocsGrid.masterRecord, hasRecords, null);
                    }
                }
                SetColumn();
            }
        }

        public override bool IsDataChaged { get { return dgDocsGrid.dragStarted ? false : base.IsDataChaged; } }

        public override string NameOfControl
        {
            get { return TabControls.UserDocsPage.ToString(); }
        }

        private void UploadData(UserDocsClient selectedItem)
        {
            var fileExt = selectedItem.DocumentType;
            var cwUpdateFile = new CWUpdateFile(fileExt, Uniconta.ClientTools.Localization.lookup("Document"));
            cwUpdateFile.Closed += async delegate
            {
                if (cwUpdateFile.DialogResult == true)
                {
                    byte[] buffer = cwUpdateFile.Contents;
                    string url = cwUpdateFile.Url;
                    if (selectedItem.RowId != 0 && (buffer != null || !string.IsNullOrEmpty(url)))
                    {
                        var org = new UserDocsClient();
                        selectedItem._Data = null;
                        org.Copy(selectedItem);
                        selectedItem._Data = buffer;
                        selectedItem._Url = url;
                        selectedItem._NoCompression = cwUpdateFile.Compress;
                        selectedItem._DocumentType = cwUpdateFile.fileExtensionType;
                        busyIndicator.IsBusy = true;
                        await api.Update(org, selectedItem);
                        busyIndicator.IsBusy = false;
                        dgDocsGrid.UpdateItemSource(2, selectedItem);
                    }
                    else
                    {
                        UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("ViewerFailed"), Uniconta.ClientTools.Localization.lookup("Error"), MessageBoxButton.OK);
                        return;
                    }
                }
            };
            cwUpdateFile.Show();
        }

        private void Text_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            ribbonControl.PerformRibbonAction("ViewDownloadRow");
        }
    }
}
