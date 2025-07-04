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
using UnicontaClient.Models;
using System.Threading.Tasks;
using Uniconta.Common;
using UnicontaClient.Utilities;
using Uniconta.API.Service;
using Uniconta.ClientTools;
using System.Collections;
using System.Windows.Data;
using DevExpress.Xpf.Grid;
using System.Windows;
using Uniconta.API.System;
using Uniconta.DataModel;
using Uniconta.ClientTools.Util;
using UnicontaClient.Pages.Attachments;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Uniconta.ClientTools.Controls;
using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class NotesInfoGrid : CorasauDataGridClient
    {
        public override Type TableType
        {
            get { return typeof(UserNotesClient); }
        }
        public override IComparer GridSorting { get { return new SortNote(); } }
        public override bool SingleBufferUpdate => false;

        public override bool Readonly { get { return false; } }
        public override bool AddRowOnPageDown()
        {
            var selectedItem = (UserNotesClient)this.SelectedItem;
            return (selectedItem?._Text != null);
        }
    }
    public partial class UserNotesPage : GridBasePage
    {
        public UserNotesPage(UnicontaBaseEntity masterRecord)
           : base(masterRecord)
        {
            InitializeComponent();
            InitPage(masterRecord);
        }

        CrudAPI crudApi;
        bool isComNotesInfo = false;
        public UserNotesPage(UnicontaBaseEntity sourcedata, string companyNotesInfo)
           : base(null)
        {
            InitializeComponent();
            CrudAPI Crudapi = new CrudAPI(session, sourcedata as CompanyClient);
            crudApi = Crudapi;
            isComNotesInfo = true;
            InitPage(sourcedata);
        }

        private void InitPage(UnicontaBaseEntity masterRecord)
        {
            var tableId = masterRecord.ClassId();
            if ((tableId >= 301 && tableId <= 309) || tableId == Uniconta.DataModel.User.CLASSID)
                Group.Visible = false;
            dgNotesGrid.UpdateMaster(masterRecord);
            InitPage();
            ((TableView)dgNotesGrid.View).RowStyle = System.Windows.Application.Current.Resources["GridRowControlCustomHeightStyle"] as Style;
        }

        public UserNotesPage(SynchronizeEntity syncEntity) : base(syncEntity, true)
        {
            InitializeComponent();
            InitPage(syncEntity.Row);
            SetHeader();
        }
        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            dgNotesGrid.UpdateMaster(args);
            SetHeader();
            InitQuery();
        }

        private void SetHeader()
        {
            string key = Utility.GetHeaderString(dgNotesGrid.masterRecord);
            if (string.IsNullOrEmpty(key)) return;
            string header = string.Format("{0} : {1}", Uniconta.ClientTools.Localization.lookup("UserNotesInfo"), key);
            SetHeader(header);
        }

        public UserNotesPage(BaseAPI api, string lookupkey)
            : base(api, lookupkey)
        {
            InitializeComponent();
            InitPage();
        }

        private void InitPage()
        {
            localMenu.dataGrid = dgNotesGrid;
            SetRibbonControl(localMenu, dgNotesGrid);
            if (!isComNotesInfo)
                dgNotesGrid.api = api;
            else
                dgNotesGrid.api = crudApi;
            dgNotesGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
        }

        protected override void LoadCacheInBackGround() { LoadType(typeof(Uniconta.DataModel.NoteGroup)); }

        void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgNotesGrid.SelectedItem as UserNotesClient;

            switch (ActionType)
            {
                case "AddRow":
                    dgNotesGrid.Focus();
                    dgNotesGrid.AddRow();
                    dgNotesGrid.View.ShowEditor();
                    dgNotesGrid.CurrentColumn = dgNotesGrid.Columns["Text"];
                    break;
                case "SaveGrid":
                    var list = dgNotesGrid.ItemsSource as List<UserNotesClient>;
                    foreach (var item in list)
                    {
                        if (item.Created == DateTime.Now)
                        {
                            item.Created = DateTime.MinValue;
                            item.NotifyPropertyChanged("Created");
                        }
                    }
                    SaveNotesAndExit();
                    break;
                case "DeleteRow":
                    dgNotesGrid.DeleteRow();
                    break;
                case "ImportMail":
                    var dialogImport = new CWImportOutlookMails();
                    dialogImport.Closing += delegate
                    {
                        if (dialogImport.DialogResult == true)
                        {
                            var note = dialogImport.userNote;
                            if (note != null)
                            {
                                note.SetMaster(dgNotesGrid.masterRecord);
                                dgNotesGrid.AddRow(note);
                                SaveNotes();
                            }
                        }
                    };
                    dialogImport.Show();
                    break;
                case "SendMail":
                    SendMailFromMaster();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        void SendMailFromMaster()
        {
            var master = dgNotesGrid.masterRecord;
            var emailProperties = master.GetType().GetProperties().Where(p => p.GetCustomAttribute<DataTypeAttribute>()?.DataType == DataType.EmailAddress).ToList();
            bool propertyFound = false;
            foreach (var emailProperty in emailProperties)
            {
                string email = emailProperty.GetValue(master, null) as string;
                if (!string.IsNullOrEmpty(email))
                {
                    propertyFound = true;
                    OutlookNotes outNotes = new OutlookNotes(api, master);
                    outNotes.OpenOutLook(new UserNotesClient() { SendTo = email }, false);
                    break;
                }
            }
            if (!propertyFound)
            {
                OutlookNotes outNotes = new OutlookNotes(api, master);
                outNotes.OpenOutLook(null, false);
            }
        }
        async void SaveNotesAndExit()
        {
            await SaveNotes();
            this.CloseDockItem();
        }
        async Task SaveNotes()
        {
            dgNotesGrid.SelectedItem = null;

            await dgNotesGrid.SaveData();
            var master = dgNotesGrid.masterRecord as IdKey;
            if (master != null)
            {
                if (dgNotesGrid.VisibleRowCount > 0)
                    master.HasNotes = true;
                else
                    master.HasNotes = false;
                globalEvents.OnRefresh(TabControls.UserNotesPage, master);
            }
            else if (master == null)
            {
                var tableType = dgNotesGrid.masterRecord?.GetType();
                var hasNotesProp = tableType?.GetProperty("HasNotes");
                if (hasNotesProp != null)
                {
                    var hasRecords = dgNotesGrid.VisibleRowCount > 0 ? true : false;
                    hasNotesProp.SetValue(dgNotesGrid.masterRecord, hasRecords, null);
                    globalEvents.OnRefresh(TabControls.UserNotesPage, dgNotesGrid.masterRecord);
                }
            }
        }

        public override string NameOfControl { get { return TabControls.UserNotesPage; } }

        private void IsMailNote_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var selectedItem = dgNotesGrid.SelectedItem as UserNotesClient;
            if (selectedItem.IsMailNote)
            {
                OutlookNotes outNotes = new OutlookNotes(api, dgNotesGrid.masterRecord);
                outNotes.OpenOutLook(selectedItem, true, true);
            }
        }

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == "UserNotes")
            {
                object[] editParams = new object[2];
                editParams[0] = 1;
                editParams[1] = argument;
                Dispatcher.BeginInvoke(new Action(() => { dgNotesGrid.UpdateItemSource(editParams); }));
                
            } 
    }
    }
}
