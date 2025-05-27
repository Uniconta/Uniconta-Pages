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
    public class CreditorFieldLogGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(CreditorAccountFieldLogClient); } }
        public override bool Readonly { get { return true; } }
        public override IComparer GridSorting { get { return new LogFieldDateSort(); } }
    }

    /// <summary>
    /// Interaction logic for DCLogGridPage.xaml
    /// </summary>
    public partial class CreditorFieldLogPage : GridBasePage
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

        public CreditorFieldLogPage(BaseAPI Api) : base(Api, string.Empty)
        {
            InitPage();
        }

        public CreditorFieldLogPage(UnicontaBaseEntity master) : base(master)
        {
            InitPage(master);
        }
        private void InitPage(UnicontaBaseEntity master = null)
        {
            InitializeComponent();
            localMenu.dataGrid = dgCreditorFieldLogGrid;
            SetRibbonControl(localMenu, dgCreditorFieldLogGrid);

            if (master == null)
            {
                filterDate = BasePage.GetSystemDefaultDate().AddMonths(-2);
                Account.Visible = true;
            }
            else
            {
                LogType.Visible = false;
                var x = master as CreditorPaymentAccountClient;
                var cache = api.GetCache(typeof(Uniconta.DataModel.Creditor));
                var cred = cache.Get(x._Account) as Uniconta.DataModel.Creditor;
                dgCreditorFieldLogGrid.UpdateMaster(cred);
            }

            SetHeader();
            dgCreditorFieldLogGrid.BusyIndicator = busyIndicator;
            dgCreditorFieldLogGrid.api = api;
            Loaded += CreditorFieldLogPage_Loaded;
        }

        private void CreditorFieldLogPage_Loaded(object sender, RoutedEventArgs e)
        {
            var curpanel = this.ParentControl;
            curpanel.AllowDock = false;
            if (curpanel?.IsFloating == true)
                curpanel.Parent.FloatSize = new System.Windows.Size(950, 500);
            curpanel.UpdateLayout();
        }

        void SetHeader()
        {
            UnicontaBaseEntity masterClient = dgCreditorFieldLogGrid.masterRecord;
            if (masterClient is CreditorClient)
            {
                var credPaymentMaster = masterClient as CreditorClient;
                SetHeader(string.Format("{0} {1}/{2}", Uniconta.ClientTools.Localization.lookup("BankDetails"), Uniconta.ClientTools.Localization.lookup("Log"), credPaymentMaster.Account));
            }
            else
                SetHeader(string.Format("{0} {1}", Uniconta.ClientTools.Localization.lookup("BankDetails"), Uniconta.ClientTools.Localization.lookup("Log")));

        }

        public override string NameOfControl { get { return TabControls.CreditorFieldLogPage; } }

    }
}
