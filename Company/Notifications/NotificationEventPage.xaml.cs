using UnicontaClient.Models;
using UnicontaClient.Pages;
using System;
using System.Collections;
using System.Linq;
using Uniconta.API.Service;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using UnicontaClient.Controls.Dialogs;
using Uniconta.API.System;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.Util;
using System.Threading.Tasks;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class NotificationEventGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(NotificationEventClient); } }
        public override bool Readonly { get { return false; } }

    }

    public partial class NotificationEventPage : GridBasePage
    {
        SQLCache LedgerCache, DebtorCache, CreditorCache, FAMCache, InventoryCache, ProjectCache;
        protected override async System.Threading.Tasks.Task LoadCacheInBackGroundAsync()
        {
            var api = this.api;
            var Comp = api.CompanyEntity;

            if (Comp.Debtor)
                DebtorCache = Comp.GetCache(typeof(Uniconta.DataModel.Debtor)) ?? await api.LoadCache(typeof(Uniconta.DataModel.Debtor)).ConfigureAwait(false);
            if (Comp.Creditor)
                CreditorCache = Comp.GetCache(typeof(Uniconta.DataModel.Creditor)) ?? await api.LoadCache(typeof(Uniconta.DataModel.Creditor)).ConfigureAwait(false);
            if (Comp.Ledger)
                LedgerCache = Comp.GetCache(typeof(Uniconta.DataModel.GLAccount)) ?? await api.LoadCache(typeof(Uniconta.DataModel.GLAccount)).ConfigureAwait(false);
            if (Comp.FixedAsset)
                FAMCache = Comp.GetCache(typeof(Uniconta.DataModel.FAM)) ?? await api.LoadCache(typeof(Uniconta.DataModel.FAM)).ConfigureAwait(false);
            if (Comp.Inventory)
                InventoryCache = Comp.GetCache(typeof(Uniconta.DataModel.InvItem)) ?? await api.LoadCache(typeof(Uniconta.DataModel.InvItem)).ConfigureAwait(false);
            if (Comp.Project)
                ProjectCache = Comp.GetCache(typeof(Uniconta.DataModel.Project)) ?? await api.LoadCache(typeof(Uniconta.DataModel.Project)).ConfigureAwait(false);
        }
        public NotificationEventPage(BaseAPI API) : base(API, string.Empty)
        {
            Init(null);
        }
        public NotificationEventPage(UnicontaBaseEntity master) : base(master)
        {
            Init(master);
        }

        private void View_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {

        }

        UnicontaBaseEntity master;
        private void Init(UnicontaBaseEntity master)
        {
            InitializeComponent();
            this.master = master;
            if (master != null)
                dgNotificationEvent.UpdateMaster(master);
            dgNotificationEvent.api = api;
            dgNotificationEvent.BusyIndicator = busyIndicator;
            SetRibbonControl(localMenu, dgNotificationEvent);
            localMenu.OnItemClicked += localMenu_OnItemClicked;
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "Delete":
                    DeleteNotification();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
        async void DeleteNotification()
        {
            if (master != null)
            {
                var selectedItem = dgNotificationEvent.SelectedItem as NotificationEventClient;
                if (selectedItem != null)
                {
                    var uapi = new UserAPI(api);
                    var err = await uapi.NotificationRead(selectedItem._RowId);
                    if (err != ErrorCodes.Succes)
                    {
                        UtilDisplay.ShowErrorCode(err);
                    }
                    else
                        dgNotificationEvent.RemoveFocusedRowFromGrid();
                }
            }
            else
                dgNotificationEvent.DeleteRow();
        }

        async Task<IdKey> GetFromCacheOrUpdate(SQLCache cache, string account)
        {
            IdKey act = cache.Get(account);
            if (act == null)
            {
                var queryAPI = new QueryAPI(api);
                var err = await queryAPI.UpdateCache();
                if (err == ErrorCodes.Succes)
                    act = cache.Get(account);
            }
            return act;
        }
        private async void DCAccount_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var selectedItem = dgNotificationEvent.SelectedItem as NotificationEventClient;
            if (selectedItem?._DCAccount == null)
                return;
            IdKey selectedAct = null;
            switch (selectedItem._DCType)
            {
                case 1:
                    selectedAct = await GetFromCacheOrUpdate(DebtorCache, selectedItem._DCAccount);
                    if (selectedAct != null)
                        AddDockItem(TabControls.DebtorAccountPage2, new object[2] { selectedAct, true }, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("DebtorAccount"), selectedAct?.KeyStr));
                    break;
                case 2:
                    selectedAct = await GetFromCacheOrUpdate(CreditorCache, selectedItem._DCAccount);
                    if (selectedAct != null)
                        AddDockItem(TabControls.CreditorAccountPage2, new object[2] { selectedAct, true }, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Creditorsaccount"), selectedAct?.KeyStr));
                    break;
                case 3:
                    selectedAct = await GetFromCacheOrUpdate(LedgerCache, selectedItem._DCAccount);
                    if (selectedAct != null)
                        AddDockItem(TabControls.GLAccountPage2, new object[2] { selectedAct, true }, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Accounts"), selectedAct?.KeyStr));
                    break;
                case 4:
                    selectedAct = await GetFromCacheOrUpdate(FAMCache, selectedItem._DCAccount);
                    if (selectedAct != null)
                        AddDockItem(TabControls.FAMPage2, new object[2] { selectedAct, true }, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Asset"), selectedAct?.KeyStr));
                    break;
                case 5:
                    selectedAct = await GetFromCacheOrUpdate(InventoryCache, selectedItem._DCAccount);
                    if (selectedAct != null)
                        AddDockItem(TabControls.InventoryItemPage2, new object[2] { selectedAct, true }, string.Format("{0}:{1}", Uniconta.ClientTools.Localization.lookup("InventoryItems"), selectedAct?.KeyStr));
                    break;
                case 6:
                    selectedAct = await GetFromCacheOrUpdate(ProjectCache, selectedItem._DCAccount);
                    if (selectedAct != null)
                        AddDockItem(TabControls.ProjectPage2, new object[] { selectedAct, IdObject.get(true) }, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Project"), selectedAct?.KeyStr));
                    break;
            }
        }

        protected override LookUpTable HandleLookupOnLocalPage(LookUpTable lookup, CorasauDataGrid dg)
        {
            var ne = dgNotificationEvent.SelectedItem as NotificationEventClient;
            if (ne == null)
                return lookup;
            if (dgNotificationEvent.CurrentColumn?.Name == "DCAccount")
            {
                switch (ne._DCType)
                {
                    case 1:
                        lookup.TableType = typeof(Uniconta.DataModel.Debtor);
                        break;
                    case 2:
                        lookup.TableType = typeof(Uniconta.DataModel.Creditor);
                        break;
                    case 3:
                        lookup.TableType = typeof(Uniconta.DataModel.GLAccount);
                        break;
                    case 4:
                        lookup.TableType = typeof(Uniconta.DataModel.FAM);
                        break;
                    case 5:
                        lookup.TableType = typeof(Uniconta.DataModel.InvItem);
                        break;
                    case 6:
                        lookup.TableType = typeof(Uniconta.DataModel.Project);
                        break;
                }
            }
            return lookup;
        }

        protected override void OnLayoutLoaded()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            if (master != null)
            {
                var deleteMenu = UtilDisplay.GetMenuCommandByName(rb, "Delete");
                deleteMenu.Caption = Uniconta.ClientTools.Localization.lookup("Remove");
            }
            UtilDisplay.RemoveMenuCommand(rb, new string[] { "AddRow", "EditRow", "SaveGrid" });
            base.OnLayoutLoaded();
        }
    }
}
