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
            if ((tableId >= 301 && tableId <= 309) || tableId == Uniconta.DataModel.User.CLASS_ID)
                Group.Visible = false;
            dgNotesGrid.UpdateMaster(masterRecord);
            InitPage();
            ((TableView)dgNotesGrid.View).RowStyle = Application.Current.Resources["StyleRow"] as Style;
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
                    SaveNotes();
                    break;
                case "DeleteRow":
                    dgNotesGrid.DeleteRow();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        async void SaveNotes()
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
    }
}
