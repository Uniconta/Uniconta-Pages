using UnicontaClient.Models;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Uniconta.ClientTools;
using System.Collections;
using Uniconta.API.GeneralLedger;
using Uniconta.ClientTools.Util;
using Uniconta.ClientTools.Controls;
using Uniconta.API.Service;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class CorasauDataGridPostedJournal : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(GLDailyJournalPostedClient); } }
        public override IComparer GridSorting { get { return new GLDailyJournalPostedClientSort(); } }

        protected override void DataLoaded(UnicontaBaseEntity[] Arr)
        {
            var cache = this.api.CompanyEntity.GLPosted;
            if (cache != null)
            {
                var a = (GLDailyJournalPostedClient[])Arr;
                for (int i = 0; (i < a.Length); i++)
                    if (cache.Get(a[i].RowId) == null)
                        cache.Add(a[i]);
            }
        }
    }

    public partial class PostedJournals : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.PostedJournals; } }

        protected override Filter[] DefaultFilters()
        {
            if (this.api.CompanyEntity._BackupFrom == 0)
            {
                if ((BasePage.GetSystemDefaultDate() - this.api.CompanyEntity._Created).TotalDays < 365)
                    return null;
            }
            return new Filter[] { new Filter()
            {
                name = "Posted",
                value = BasePage.GetFilterDate(api.CompanyEntity, false, 2).ToShortDateString() + ".."
            } };
        }
        public PostedJournals(UnicontaBaseEntity master)
            : base(master)
        {
            InitializeComponent();
            dgPostedJournal.UpdateMaster(master);
            Init();
        }
        public PostedJournals(BaseAPI API) : base(API, string.Empty)
        {
            InitializeComponent();
            Init();
        }

        private void Init()
        {
            SetRibbonControl(localMenu, dgPostedJournal);
            dgPostedJournal.api = api;
            if (api.session.User._Role < (byte)Uniconta.Common.User.UserRoles.Distributor)
            {
                var CountryId = api.CompanyEntity._CountryId;
                if ((CountryId == CountryCode.Iceland && api.session.User._Role < (byte)Uniconta.Common.User.UserRoles.Reseller) || CountryId == CountryCode.SouthAfrica || CountryId == CountryCode.UnitedKingdom || CountryId == CountryCode.Germany)
                    RemoveMenuItemDelete();
            }
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgPostedJournal.BusyIndicator = busyIndicator;
        }

        void RemoveMenuItemDelete()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            UtilDisplay.RemoveMenuCommand(rb, "Delete");
        }

        protected override void LoadCacheInBackGround()
        {
            LoadType(new Type[] { typeof(Uniconta.DataModel.GLDailyJournal), typeof(Uniconta.DataModel.NumberSerie), typeof(Uniconta.DataModel.Debtor), typeof(Uniconta.DataModel.Creditor), typeof(Uniconta.DataModel.GLAccount) });
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            GLDailyJournalPostedClient selectedItem = dgPostedJournal.SelectedItem as GLDailyJournalPostedClient;
            switch (ActionType)
            {
                case "PostedTransaction":
                    if (selectedItem == null)
                        return;
                    string header = string.Format("{0} / {1}", Uniconta.ClientTools.Localization.lookup("PostedTransactions"), selectedItem.RowId);
                    AddDockItem(TabControls.PostedTransactions, selectedItem, header);
                    break;
                case "Delete":
                    if (selectedItem != null)
                        DeleteJournal(selectedItem);
                    break;
                case "AccountActivity":
                    if (selectedItem != null)
                        AddDockItem(TabControls.GLTransLogPage, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("GLTransLog"), selectedItem.RowId));
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        private void DeleteJournal(GLDailyJournalPostedClient selectedItem)
        {
            var deleteDialog = new DeletePostedJournal();
            deleteDialog.Closed += async delegate
            {
                if (deleteDialog.DialogResult == true)
                {
                    PostingAPI pApi = new PostingAPI(api);
                    ErrorCodes res = await pApi.DeletePostedJournal(selectedItem, deleteDialog.Comment);
                    if (res == ErrorCodes.Succes)
                    {
                        UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("Journaldeleted"), selectedItem.JournalPostedId), Uniconta.ClientTools.Localization.lookup("Message"));
                        dgPostedJournal.UpdateItemSource(2, selectedItem);
                    }
                    else
                        UtilDisplay.ShowErrorCode(res);
                }
            };
            deleteDialog.Show();
        }
    }
}
