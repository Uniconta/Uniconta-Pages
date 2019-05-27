using UnicontaClient.Models;
using UnicontaClient.Pages;
using DevExpress.Xpf.Grid;
using DevExpress.XtraGrid;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class UserProfileControlPageGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(UserProfileControlClient); } }
        public override bool Readonly { get { return false; } }
        public override bool IsAutoSave
        {
            get
            {
                return false;
            }
        }
    }

    public partial class UserProfileControlPage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.UserProfileControlPage; } }


        public UserProfileControlPage(SynchronizeEntity syncEntity)
           : base(syncEntity, false)
        {
            Init(syncEntity.Row);
        }

        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            dgUserProdileControlGrid.UpdateMaster(args);
            SetHeader();
            InitQuery();
        }
        void SetHeader()
        {
            var syncMaster = dgUserProdileControlGrid.masterRecord as UserProfileClient;
            if (syncMaster == null)
                return;
            string header = string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("ProfileLines"), syncMaster.Name);
            SetHeader(header);
        }

        public UserProfileControlPage(UnicontaBaseEntity master)
            : base(master)
        {
            Init(master);
        }

        void Init(UnicontaBaseEntity master)
        {
            InitializeComponent();
            dgUserProdileControlGrid.UpdateMaster(master);
            localMenu.dataGrid = dgUserProdileControlGrid;
            SetRibbonControl(localMenu, dgUserProdileControlGrid);
            dgUserProdileControlGrid.api = api;
            dgUserProdileControlGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgUserProdileControlGrid.SelectedItem as UserProfileControlClient;
            switch (ActionType)
            {
                case "AddRow":
                    dgUserProdileControlGrid.AddRow();
                    break;
                case "SaveGrid":
                    dgUserProdileControlGrid.SaveData();
                    break;
                case "DeleteRow":
                    if (selectedItem != null)
                        dgUserProdileControlGrid.DeleteRow();
                    break;
                case "InsertAll":
                    InsertAll();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        void InsertAll()
        {
            busyIndicator.IsBusy = true;
            var source = dgUserProdileControlGrid.ItemsSource as IEnumerable<UserProfileControlClient>;
            var rows = new List<UserProfileControlClient>();
            foreach (var controlId in UserControlProperties.ControlIdMap.Keys)
            {
                if (source?.Where(s => s.Controlid == controlId).FirstOrDefault() == null)
                {
                    var pline = new UserProfileControlClient();
                    pline._Controlid = controlId;
                    rows.Add(pline);
                }
            }
            dgUserProdileControlGrid.PasteRows(rows);
            busyIndicator.IsBusy = false;
        }
    }

    public class ControlComboboxEditSettings : DevExpress.Xpf.Editors.Settings.ComboBoxEditSettings
    {
        public ControlComboboxEditSettings()
        {
            this.ItemsSource = UserControlProperties.ControlList.Keys.OrderBy(c => c);
            this.AutoComplete = true;
        }
    }
}
