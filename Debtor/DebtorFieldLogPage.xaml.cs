using DevExpress.Xpf.Grid;
using System;
using System.Collections;
using System.Collections.Generic;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Uniconta.API.DebtorCreditor;
using Uniconta.API.Service;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using UnicontaClient.Models;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class DebtorFieldLogGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(DebtorAccountFieldLogClient); } }
        public override bool Readonly { get { return true; } }
        public override IComparer GridSorting { get { return new LogFieldDateSort(); } }
    }

    /// <summary>
    /// Interaction logic for DCLogGridPage.xaml
    /// </summary>
    public partial class DebtorFieldLogPage : GridBasePage
    {
        DateTime filterDate;

        protected override Filter[] DefaultFilters()
        {
            if (filterDate != DateTime.MinValue)
            {
                Filter dateFilter = new Filter() { name = "Time", value = String.Format("{0:d}..", filterDate) };
                return new Filter[] { dateFilter };
            }
            return base.DefaultFilters();
        }

        public DebtorFieldLogPage(BaseAPI Api) : base(Api, string.Empty)
        {
            InitPage();
        }

        public DebtorFieldLogPage(SynchronizeEntity syncEntity)
           : base(syncEntity, true)
        {
            this.syncEntity = syncEntity;
            InitPage(syncEntity.Row);
            SetHeader();
        }

        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            SetMaster(args);
            SetHeader();
            InitQuery();
        }

        public DebtorFieldLogPage(UnicontaBaseEntity master) : base(master)
        {
            InitPage(master);
        }
        private void InitPage(UnicontaBaseEntity master = null)
        {
            InitializeComponent();
            localMenu.dataGrid = dgDebtorFieldLogGrid;
            SetRibbonControl(localMenu, dgDebtorFieldLogGrid);
            localMenu.OnItemClicked += localMenu_OnItemClicked;

            if (master == null)
            {
                filterDate = BasePage.GetSystemDefaultDate().AddMonths(-2);
                Account.Visible = true;
            }
            else
            {
                SetMaster(master);
            }

            SetHeader();
            dgDebtorFieldLogGrid.BusyIndicator = busyIndicator;
            dgDebtorFieldLogGrid.api = api;
            Loaded += DebtorFieldLogPage_Loaded;
        }

        private void SetMaster(UnicontaBaseEntity master)
        {
            if (master != null)
            {
                LogType.Visible = false;

                Uniconta.DataModel.Debtor debtor;
                if (master.GetType() == typeof(EUSaleWithoutVAT))
                {
                    var x = master as EUSaleWithoutVAT;
                    var cache = api.GetCache(typeof(Uniconta.DataModel.Debtor));
                    debtor = cache.Get(x._Account) as Uniconta.DataModel.Debtor;
                }
                else if (master.GetType() == typeof(IntrastatClient))
                {
                    var x = master as IntrastatClient;
                    var cache = api.GetCache(typeof(Uniconta.DataModel.Debtor));
                    debtor = cache.Get(x._DCAccount) as Uniconta.DataModel.Debtor;
                }
                else
                    debtor = master as Uniconta.DataModel.Debtor;

                dgDebtorFieldLogGrid.UpdateMaster(debtor);
            }
        }

        private void DebtorFieldLogPage_Loaded(object sender, RoutedEventArgs e)
        {
            var curpanel = this.ParentControl;
            curpanel.AllowDock = false;
            if (curpanel?.IsFloating == true)
                curpanel.Parent.FloatSize = new System.Windows.Size(1300, 500);
            curpanel.UpdateLayout();
        }

        void SetHeader()
        {
            UnicontaBaseEntity masterClient = dgDebtorFieldLogGrid.masterRecord;
            if (masterClient is DebtorClient)
            {
                var master = masterClient as DebtorClient;
                SetHeader(string.Format("{0}/{1}", Uniconta.ClientTools.Localization.lookup("VatNumberValidationService"), master.Account));
            }
            else
                SetHeader(Uniconta.ClientTools.Localization.lookup("VatNumberValidationService"));

        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            gridRibbon_BaseActions(ActionType);
        }


        public override string NameOfControl { get { return TabControls.DebtorFieldLogPage; } }

    }
}
