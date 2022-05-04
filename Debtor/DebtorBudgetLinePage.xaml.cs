using UnicontaClient.Pages;
using System;
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
using Uniconta.ClientTools.Page;
using Uniconta.DataModel;
using System.Collections;
using Uniconta.Common;
using Uniconta.ClientTools.DataModel;
using UnicontaClient.Models;
using DevExpress.Xpf.Grid;
using Uniconta.ClientTools.Util;
using Uniconta.Common.Utility;
using Uniconta.API.System;
using Uniconta.ClientTools;
using UnicontaClient.Utilities;
using Uniconta.API.Service;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class DebtorBudgetLineGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(DebtorBudgetLineClient); } }
        public override bool Readonly { get { return false; } }
        public override bool SingleBufferUpdate { get { return false; } }
        protected override List<string> GridSkipFields { get { return new List<string>(2) { "AccountName", "EmployeeName" }; } }
    }

    public partial class DebtorBudgetLinePage : GridBasePage
    {
        UnicontaBaseEntity master;

        public DebtorBudgetLinePage(BaseAPI API)
           : base(API, string.Empty)
        {
            InitPage(null);
        }
        public DebtorBudgetLinePage(UnicontaBaseEntity master)
            : base(master)
        {
            InitPage(master);
        }

        public DebtorBudgetLinePage(SynchronizeEntity syncEntity) : base(syncEntity, true)
        {
            InitPage(syncEntity.Row);
            SetHeader();
        }

        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            dgDebtorBudgetLine.UpdateMaster(args);
            SetHeader();
            InitQuery();
        }

        private void SetHeader()
        {
            string key = Utility.GetHeaderString(dgDebtorBudgetLine.masterRecord);
            if (string.IsNullOrEmpty(key))
                return;
            string header = string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("BudgetLines"), key);
            SetHeader(header);
        }
        void InitPage(UnicontaBaseEntity masterRecord)
        {
            InitializeComponent();
            this.master = masterRecord;
            dgDebtorBudgetLine.UpdateMaster(master);
            dgDebtorBudgetLine.api = api;
            SetRibbonControl(localMenu, dgDebtorBudgetLine);
            localMenu.dataGrid = dgDebtorBudgetLine;
            dgDebtorBudgetLine.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            if(master is DebtorClient)
            {
                Account.Visible= AccountName.Visible=false;
                Account.ShowInColumnChooser= AccountName.ShowInColumnChooser= false;
            }
            else if(master is EmployeeClient)
            {
                Employee.Visible= EmployeeName.Visible= false;
                Employee.ShowInColumnChooser= EmployeeName.ShowInColumnChooser= false;
            }
            UnicontaClient.Utilities.Utility.SetDimensionsGrid(api, coldim1, coldim2, coldim3, coldim4, coldim5);
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "AddRow":
                    dgDebtorBudgetLine.AddRow();
                    break;
                case "CopyRow":
                    dgDebtorBudgetLine.CopyRow();
                    break;
                case "SaveGrid":
                    dgDebtorBudgetLine.SaveData();
                    break;
                case "DeleteRow":
                    dgDebtorBudgetLine.DeleteRow();
                    break;
                case "Simulate":
                    Simulate();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        static DebtorBudgetLineClient createPost(DCBudgetLine rec, CrudAPI api)
        {
            var post = new DebtorBudgetLineClient();
            post.SetMaster(api.CompanyEntity);
            post.PrimaryKeyId = rec.PrimaryKeyId;
            post._Account = rec._Account;
            post._Comment = rec._Comment;
            post._Dim1 = rec._Dim1;
            post._Dim2 = rec._Dim2;
            post._Dim3 = rec._Dim3;
            post._Dim4 = rec._Dim4;
            post._Dim5 = rec._Dim5;
            return post;
        }

        void Simulate()
        {
            DateTime BudgetFromDate = DateTime.MinValue, BudgetToDate = DateTime.MinValue;
            var Year = BasePage.GetSystemDefaultDate().Year;
            if (BudgetFromDate == DateTime.MinValue)
                BudgetFromDate = new DateTime(Year, 1, 1);
            if (BudgetToDate == DateTime.MinValue)
                BudgetToDate = new DateTime(Year, 12, 31);

            var lst = new List<DebtorBudgetLineClient>();
            var itemSource = (IEnumerable<DCBudgetLine>)dgDebtorBudgetLine.ItemsSource;
            if (itemSource == null)
                return;
            foreach (var rec in itemSource)
            {
                if (rec._Disable)
                    continue;

                var recDate = rec._Date;
                if (recDate == DateTime.MinValue)
                    recDate = BudgetFromDate;

                if (recDate > BudgetToDate)
                    continue;

                var recToDate = rec._ToDate;
                if (recToDate == DateTime.MinValue)
                    recToDate = BudgetToDate;

                int n = (int)rec._Recurring;
                if (n == 0)
                {
                    if (recDate >= BudgetFromDate && recDate <= recToDate && rec._Amount != 0d)
                    {
                        var post = createPost(rec, api);
                        post._Amount = rec._Amount;
                        post._Date = recDate;
                        lst.Add(post);
                    }
                }
                else
                {
                    if (n > 4)
                    {
                        if (n == 5) // half year
                            n = 6;
                        else if (n == 6) // year
                            n = 12;
                    }
                    var RegulatePct = rec._Regulate / 100d;
                    var recAmount = rec._Amount;
                    int i = 0;
                    for (; ; )
                    {
                        var newDate = recDate.AddMonths(i * n);
                        if (newDate > recToDate)
                            break;
                        if (newDate >= BudgetFromDate)
                        {
                            var Amount = Math.Round(recAmount + (recAmount * RegulatePct * i), 2);
                            if (Amount != 0d)
                            {
                                var post = createPost(rec, api);
                                post._Amount = Amount;
                                post._Date = newDate;
                                lst.Add(post);
                            }
                        }
                        i++;
                    }
                }
            }

            AddDockItem(TabControls.SimulatedDebtorBudgetLine, new object[] { lst }, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Simulate"), ((Debtor)master)?.KeyName));
        }

        protected override void LoadCacheInBackGround()
        {
            LoadType(new Type[] { typeof(Uniconta.DataModel.Debtor), typeof(Uniconta.DataModel.Employee) });
        }
    }
}
