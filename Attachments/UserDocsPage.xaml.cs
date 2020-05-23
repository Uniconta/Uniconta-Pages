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
using UnicontaClient.Models;
using Uniconta.ClientTools.DataModel;
using System.Threading.Tasks;
using Uniconta.Common;
using UnicontaClient.Utilities;
using Uniconta.ClientTools;
using System.IO;
using Uniconta.ClientTools.Controls;
using System.Collections;
using System.Windows;
using Uniconta.API.System;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class DocumentInfoGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(UserDocsClient); } }

#if !SILVERLIGHT
        public override bool Readonly { get { return false; } }
        public override bool CanInsert { get { return false; } }
#endif
        public override IComparer GridSorting { get { return new SortUserDocs(); } }

        async public override Task<ErrorCodes> SaveData()
        {
            SelectedItem = null;
#if !SILVERLIGHT
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
                    userDocs[iCtr] = row.DataItem as UserDocsClient;
                    buffers[iCtr] = userDocs[iCtr]._Data;
                    userDocs[iCtr]._Data = null;
                    iCtr++;
                }

                var result = await base.SaveData();

                if (result == ErrorCodes.Succes)
                    UpdateDocsBuffer(buffers, userDocs);

                await Filter(null);

                return result;
            }
            else
                return await base.SaveData();
#else
            return await base.SaveData();
#endif
        }

        void UpdateDocsBuffer(byte[][] buffers, UserDocsClient[] multiDocs)
        {
            int l = multiDocs.Length;
            for (int i = 0; i < l; i++)
            {
                var updateDoc = multiDocs[i];
                updateDoc._Data = buffers[i];
            }
            uploadingDocs = l;
            UpdateDocs(multiDocs, 0);
        }

        int uploadingDocs;
        private void UpdateDocs(UserDocsClient[] userDocs, int iCtr)
        {
            api.Update(userDocs[iCtr]).ContinueWith((e) =>
            {
                if (!e.IsFaulted)
                    iCtr++;

                var remaining = userDocs.Length - iCtr;
                if (remaining > 0)
                {
                    uploadingDocs = remaining;
                    UpdateDocs(userDocs, iCtr);
                }
                else
                    uploadingDocs = 0;
            }, TaskContinuationOptions.None);
        }
    }

    public partial class UserDocsPage : GridBasePage
    {
        public UserDocsPage(UnicontaBaseEntity masterRecord)
            : base(masterRecord)
        {
            InitializeComponent();
            InitPage(masterRecord);
        }
        CrudAPI crudApi;
        bool isCompUserDoc = false;
        public UserDocsPage(UnicontaBaseEntity sourcedata, string companyUserDoc)
           : base(null)
        {
            InitializeComponent();
            CrudAPI Crudapi = new CrudAPI(session, sourcedata as CompanyClient);
            crudApi = Crudapi;
            isCompUserDoc = true;
            InitPage(sourcedata);
        }

        private void InitPage(UnicontaBaseEntity masterRecord)
        {
            if (!isCompUserDoc)
                dgDocsGrid.api = api;
            else
                dgDocsGrid.api = crudApi;

            dgDocsGrid.BusyIndicator = busyIndicator;
            dgDocsGrid.UpdateMaster(masterRecord);
            SetRibbonControl(localMenu, dgDocsGrid);

            localMenu.OnItemClicked += localMenu_OnItemClicked;

#if SILVERLIGHT
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            if (rb != null)
                Uniconta.ClientTools.Util.UtilDisplay.RemoveMenuCommand(rb, new string[] { "Save" });
#else
            dgDocsGrid.View.DataControl.CurrentItemChanged += DataControl_CurrentItemChanged;
            dgDocsGrid.tableView.AllowDragDrop = true;
            dgDocsGrid.tableView.DropRecord += dgDocsGrid_DropRecord;
            dgDocsGrid.tableView.DragRecordOver += dgDocsGrid_DragRecordOver;
#endif
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

#if !SILVERLIGHT

        private void dgDocsGrid_DropRecord(object sender, DevExpress.Xpf.Core.DropRecordEventArgs e)
        {
            dgDocsGrid.tableView.FocusedRowHandle = e.TargetRowHandle > 0 ? e.TargetRowHandle - 1 : -1;
            string[] errors = null;

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = e.Data.GetData(DataFormats.FileDrop) as string[];
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
                e.Effects = DragDropEffects.Copy;
            else if (e.Data.GetFormats().Contains("FileGroupDescriptor"))
                e.Effects = DragDropEffects.All;
            else
                e.Effects = DragDropEffects.None;

            e.Handled = true;
        }
#endif
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

            if (string.IsNullOrEmpty(headerStr)) return;

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
                    AddDockItem(TabControls.UserDocsPage2, param, headerStr, "Add_16x16.png");
                    break;
                case "EditRow":
                    if (selectedItem == null || selectedItem.Created == DateTime.MinValue)
                        return;
                    object[] para = new object[arrayIndex];
                    para[0] = selectedItem;
                    para[1] = true;
                    if (isCompUserDoc) para[2] = crudApi;
                    AddDockItem(TabControls.UserDocsPage2, para, headerStr, "Edit_16x16.png");
                    break;

                case "RefreshGrid":
                    if (dgDocsGrid.HasUnsavedData)
                        Utility.ShowConfirmationOnRefreshGrid(dgDocsGrid);
                    else
                        dgDocsGrid.Filter(null, null, true);
                    break;

                case "ViewDownloadRow":
                    if (selectedItem != null)
                        ViewDocument(TabControls.UserDocsPage3, dgDocsGrid.syncEntity);
                    break;

                case "Save":
                    dgDocsGrid.SaveData();
                    break;

                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
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
    }
}
