using UnicontaClient.Models;
using UnicontaClient.Utilities;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Uniconta.DataModel;
using System.Collections;
using Uniconta.ClientTools.Controls;
using Uniconta.API.Service;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class ContactGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(ContactClient); } }
        public override IComparer GridSorting { get { return new ContactClientSort(); } }
    }
    public partial class ContactPage : GridBasePage
    {
        public override string NameOfControl
        {
            get { return TabControls.ContactPage.ToString(); }
        }
        public ContactPage(BaseAPI API)
            : base(API, string.Empty)
        {
            Init(null);
        }
        public ContactPage(UnicontaBaseEntity master)
            : base(master)
        {
            Init(master);
        }

        public ContactPage(SynchronizeEntity syncEntity)
            : base(syncEntity, true)
        {
            this.syncEntity = syncEntity;
            Init(syncEntity.Row);
            SetHeader();
        }

        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            dgContactGrid.UpdateMaster(args);
            SetHeader();
            InitQuery();
        }
        void SetHeader()
        {
            string header;
            var syncMaster = dgContactGrid.masterRecord as DCAccount;
            if (syncMaster != null)
                header = string.Format("{0}: {1}, {2}", Localization.lookup("Contacts"), syncMaster._Account, syncMaster._Name);
            else
            {
                var syncMaster2 = dgContactGrid.masterRecord as CrmProspect;
                if (syncMaster2 != null)
                    header = string.Format("{0}: {1}", Localization.lookup("Contacts"), syncMaster2._Name);
                else
                    return;
            }
            SetHeader(header);
        }
        private void Init(UnicontaBaseEntity master)
        {
            InitializeComponent();
            dgContactGrid.UpdateMaster(master);
            localMenu.dataGrid = dgContactGrid;
            dgContactGrid.api = api;
            dgContactGrid.BusyIndicator = busyIndicator;
            SetRibbonControl(localMenu, dgContactGrid);
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            var load = new List<Type>();
            if (api.CompanyEntity.CRM)
            {
                load.Add(typeof(Uniconta.DataModel.CrmInterest));
                load.Add(typeof(Uniconta.DataModel.CrmProduct));
                if (master == null)
                    load.Add(typeof(Uniconta.DataModel.CrmProspect));
            }
            else
            {
                RibbonBase rb = (RibbonBase)localMenu.DataContext;
                UtilDisplay.RemoveMenuCommand(rb, "FollowUp");
            }
            if (master == null)
            {
                load.Add(typeof(Uniconta.DataModel.Debtor));
                load.Add(typeof(Uniconta.DataModel.Creditor));
            }
            if (load.Count != 0)
                LoadType(load.ToArray());
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            bool showFields = (dgContactGrid.masterRecords == null);
            DCAccount.Visible = showFields;
            DCType.Visible = showFields;
            AccountName.Visible = showFields;

            bool showIntProdCol = api.CompanyEntity.CRM;
            Interests.Visible = showIntProdCol;
            Products.Visible = showIntProdCol;
        }

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.ContactPage2)
                dgContactGrid.UpdateItemSource(argument);
            if (screenName == TabControls.UserNotesPage || screenName == TabControls.UserDocsPage && argument != null)
                dgContactGrid.UpdateItemSource(argument);
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgContactGrid.SelectedItem as ContactClient;
            switch (ActionType)
            {
                case "AddRow":
                    var newItem = dgContactGrid.GetChildInstance();
                    object[] param = new object[3];
                    param[0] = newItem;
                    param[1] = false;
                    param[2] = dgContactGrid.masterRecord;
                    AddDockItem(TabControls.ContactPage2, param, Uniconta.ClientTools.Localization.lookup("Contacts"), ";component/Assets/img/Add_16x16.png");
                    break;
                case "EditRow":
                    if (selectedItem == null)
                        return;
                    object[] para = new object[3];
                    para[0] = selectedItem;
                    para[1] = true;
                    para[2] = dgContactGrid.masterRecord;
                    AddDockItem(TabControls.ContactPage2, para, selectedItem.Name, null, true);
                    break;
                case "FollowUp":
                    if (selectedItem != null)
                    {
                        var header = string.Format("{0}:{2} {1}", Uniconta.ClientTools.Localization.lookup("FollowUp"), selectedItem._Name, Uniconta.ClientTools.Localization.lookup("Contact"));
                        AddDockItem(TabControls.CrmFollowUpPage, dgContactGrid.syncEntity, header);
                    }
                    break;
                case "AddNote":
                    if (selectedItem != null)
                        AddDockItem(TabControls.UserNotesPage, dgContactGrid.syncEntity);
                    break;
                case "AddDoc":
                    if (selectedItem != null)
                        AddDockItem(TabControls.UserDocsPage, dgContactGrid.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Documents"), selectedItem._Name));
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        public override bool CheckIfBindWithUserfield(out bool isReadOnly, out bool useBinding)
        {
            isReadOnly = true;
            useBinding = true;
            return true;
        }

        private void HasDocImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var contactClient = (sender as Image).Tag as ContactClient;
            if (contactClient != null)
                AddDockItem(TabControls.UserDocsPage, dgContactGrid.syncEntity);
        }

        private void HasNoteImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var contactClient = (sender as Image).Tag as ContactClient;
            if (contactClient != null)
                AddDockItem(TabControls.UserNotesPage, dgContactGrid.syncEntity);
        }

#if !SILVERLIGHT
        private void HasEmailImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var contact = (sender as TextBlock).Tag as ContactClient;
            if (contact != null)
            {
                var mail = string.Concat("mailto:", contact._Email);
                System.Diagnostics.Process proc = new System.Diagnostics.Process();
                proc.StartInfo.FileName = mail;
                proc.Start();
            }
        }
#endif
    }
}
