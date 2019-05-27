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

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class GLBudgetLineGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(GLBudgetLineClient); } }
        public override bool Readonly { get { return false; } }
        public override IComparer GridSorting { get { return new GLBudgetLineSort(); } }
        protected override List<string> GridSkipFields
        {
            get
            {
                return new List<string>() { "AccountName", "AccountType" };
            }
        }
    }
    public partial class GLBudgetLinePage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.GLBudgetLinePage; } }
        UnicontaBaseEntity master;
        string budgetName;
        public GLBudgetLinePage(UnicontaBaseEntity master)
            : base(master)
        {
            InitializeComponent();
            InitPage(master);
        }

        public GLBudgetLinePage(SynchronizeEntity syncEntity): base(syncEntity,true)
        {
            InitializeComponent();
            InitPage(syncEntity.Row);
            SetHeader();
        }

        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            dgGLBudgetLine.UpdateMaster(args);
            SetHeader();
            InitQuery();
        }

        private void SetHeader()
        {
            string key = Utility.GetHeaderString(dgGLBudgetLine.masterRecord);

            if (string.IsNullOrEmpty(key)) return;

            string header = string.Format("{0} : {1}", Uniconta.ClientTools.Localization.lookup("BudgetLines"), key);
            SetHeader(header);
        }

        void InitPage(UnicontaBaseEntity masterRecord)
        {
            this.master = masterRecord;
            dgGLBudgetLine.UpdateMaster(master);
            dgGLBudgetLine.api = api;
            SetRibbonControl(localMenu, dgGLBudgetLine);
            localMenu.dataGrid = dgGLBudgetLine;
            dgGLBudgetLine.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            //dgGLBudgetLine.View.DataControl.CurrentItemChanged += DataControl_CurrentItemChanged;
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            setDim();
            if (master is GLBudget)
            {
                BudgetId.Visible = false;
                Account.Visible = true;
                AccountName.Visible = true;
                budgetName = (this.master as GLBudget)._Name;
            }
            else if (master is GLAccount)
            {
                BudgetId.Visible = true;
                Account.Visible = false;
                AccountName.Visible = false;
                budgetName = (this.master as GLAccount)._Name;
            }
            else
            {
                BudgetId.Visible = true;
                Account.Visible = true;
                AccountName.Visible = true;
            }
        }

        /*
        GLBudgetLineClient currentOldItem;
        private void DataControl_CurrentItemChanged(object sender, CurrentItemChangedEventArgs e)
        {
            GLBudgetLineClient oldselectedItem = e.OldItem as GLBudgetLineClient;
            if (oldselectedItem != null)
            {
                oldselectedItem.PropertyChanged -= GLBudgetLineClient_PropertyChanged;
            }

            GLBudgetLineClient selectedItem = e.NewItem as GLBudgetLineClient;
            if (selectedItem != null)
            {
                currentOldItem = StreamingManager.Clone(selectedItem) as GLBudgetLineClient;
                selectedItem.PropertyChanged += GLBudgetLineClient_PropertyChanged;
            }
        }
        void GLBudgetLineClient_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var rec = sender as GLBudgetLineClient;
            switch (e.PropertyName)
            {
                case "Amount":
                    if (currentOldItem != null)
                    {
                        var oldAmount = currentOldItem.Amount;
                    }
                    var newAmount = rec.Amount;
                    break;
                default:
                    break;
            }
        }
        */

        private void localMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "AddRow":
                    dgGLBudgetLine.AddRow();
                    break;
                case "CopyRow":
                    dgGLBudgetLine.CopyRow();
                    break;
                case "SaveGrid":
                    dgGLBudgetLine.SaveData();
                    break;
                case "DeleteRow":
                    dgGLBudgetLine.DeleteRow();
                    break;
                case "FetchChartOfAccount":
                    OpenChartOfAccount();
                    break;
                case "Simulate":
                    Simulate();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        private void OpenChartOfAccount()
        {
            DateTime fromDate = DateTime.MinValue, toDate = DateTime.MinValue;
            var budgetMaster = this.master as GLBudget;
            if (budgetMaster != null)
            {
                fromDate = budgetMaster._FromDate;
                toDate = budgetMaster._ToDate;
            }
            CWBudgetLineDialog objCWBudgetLineDialog = new CWBudgetLineDialog(api, fromDate, toDate);
            objCWBudgetLineDialog.Closed += delegate
            {              
                if (objCWBudgetLineDialog.DialogResult == true)
                {
                    var Cache = api.CompanyEntity.GetCache(typeof(Uniconta.DataModel.GLAccount));
                    if (Cache == null)
                        return;

                    var fromAccount = objCWBudgetLineDialog.fromAccount;
                    if (fromAccount == null)
                        fromAccount = string.Empty;
                    var toAccount = objCWBudgetLineDialog.toAccount;
                    if (toAccount == string.Empty)
                        toAccount = null;
                    var gridData = (IEnumerable<GLBudgetLine>)dgGLBudgetLine.ItemsSource;
                    if (gridData.Count() == 1)
                    {
                        dgGLBudgetLine.SelectedItem = gridData.FirstOrDefault();
                        dgGLBudgetLine.DeleteRow();
                    }
                    foreach (var acc in (Uniconta.DataModel.GLAccount[])Cache.GetKeyStrRecords)
                    {
                        var Account = acc._Account;
                        if (string.Compare(Account, fromAccount) >= 0 && (toAccount == null || string.Compare(Account, toAccount) <= 0))
                        {
                            var objGLBudgetLineClient = new GLBudgetLineClient();
                            StreamingManager.Copy(objCWBudgetLineDialog.editrow, objGLBudgetLineClient);
                            objGLBudgetLineClient._Account = Account;
                            dgGLBudgetLine.AddRow(objGLBudgetLineClient);
                        }              
                    }           
                }
            };
            objCWBudgetLineDialog.Show();
        }

        static GLBudgetLineClient createPost(GLBudgetLine rec, CrudAPI api)
        {
            var post = new GLBudgetLineClient();
            post.SetMaster(api.CompanyEntity);
            post._BudgetId = rec._BudgetId;
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
            var bud = master as GLBudget;
            if (bud != null)
            {
                BudgetFromDate = bud._FromDate;
                BudgetToDate = bud._ToDate;
            }
            if (BudgetFromDate == DateTime.MinValue)
                BudgetFromDate = new DateTime(Year, 1, 1);
            if (BudgetToDate == DateTime.MinValue)
                BudgetToDate = new DateTime(Year, 12, 31);

            var lst = new List<GLBudgetLineClient>();
            var itemSource = (IEnumerable<GLBudgetLine>)dgGLBudgetLine.ItemsSource;
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
                    for (;;)
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
          
            object[] param = new object[1];
            param[0] = lst;
            AddDockItem(TabControls.SimulatedGLBudgetLinePage, param, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Simulate"), this.budgetName));
        }

        public async override Task InitQuery()
        {
            await dgGLBudgetLine.Filter(null);

            var itemSource = (IList)dgGLBudgetLine.ItemsSource;
            if (itemSource == null || itemSource.Count == 0)
                dgGLBudgetLine.AddFirstRow();
        }

        protected override void LoadCacheInBackGround()
        {
            LoadType(new Type[] { typeof(Uniconta.DataModel.GLBudget), typeof(Uniconta.DataModel.GLAccount) });
        }

        void setDim()
        {
            UnicontaClient.Utilities.Utility.SetDimensionsGrid(api, coldim1, coldim2, coldim3, coldim4, coldim5);
        }
    }
}
