using UnicontaClient.Pages;
using UnicontaClient.Utilities;
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
using System.Windows.Shapes;
using Uniconta.API.Service;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.DataModel;
using Uniconta.ClientTools.Util;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class SalesRepCustomerStatPageGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(EmployeeDebSales); } }

        public override IComparer GridSorting { get { return new InvSumInvDbCmp(); } }
    }

    public partial class SalesRepCustomerStatPage : GridBasePage
    {
        SynchronizeEntity syncEntity;
        CWServerFilter itemFilterDialog;
        UnicontaBaseEntity master;

        public SalesRepCustomerStatPage(BaseAPI API) : base(API, string.Empty)
        {
            InitPage(null);
        }
        public SalesRepCustomerStatPage(UnicontaBaseEntity _master)
            : base(_master)
        {
            InitPage(_master);
        }

        public SalesRepCustomerStatPage(SynchronizeEntity syncEntity) : base(syncEntity, true)
        {
            this.syncEntity = syncEntity;
            InitPage(syncEntity.Row);
            SetHeader();
        }
        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            dgSalesRepCustomerStat.UpdateMaster(args);
            SetHeader();
        }

        private void SetHeader()
        {
            string key = Utility.GetHeaderString(dgSalesRepCustomerStat.masterRecord);

            if (string.IsNullOrEmpty(key)) return;

            string header = string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("ItemAccountStat"), key);
            SetHeader(header);
        }

        private void InitPage(UnicontaBaseEntity _master)
        {
            InitializeComponent();
            master = _master;
            dgSalesRepCustomerStat.UpdateMaster(_master);
            dgSalesRepCustomerStat.tableView.ShowGroupPanel = true;
            dgSalesRepCustomerStat.ShowTotalSummary();
            localMenu.dataGrid = dgSalesRepCustomerStat;
            SetRibbonControl(localMenu, dgSalesRepCustomerStat);
            gridControl.api = api;
            gridControl.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            localMenu.DisableButtons("Aggregate");
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            UtilDisplay.RemoveMenuCommand(rb, new string[] { "PerWarehouse","PerLocation" });
        }

        public override Task InitQuery()
        {
            return null;
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            if (master is EmployeeClient)
                Employee.Visible = false;

            Utility.SetupVariants(api, colVariant, VariantName, colVariant1, colVariant2, colVariant3, colVariant4, colVariant5, Variant1Name, Variant2Name, Variant3Name, Variant4Name, Variant5Name);
            LoadType(new Type[] { typeof(Uniconta.DataModel.InvItem), typeof(Uniconta.DataModel.Debtor), typeof(Uniconta.DataModel.Creditor) });
        }

        void localMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "ItemFilter":
                    if (itemFilterDialog == null)
                    {
                        itemFilterDialog = new CWServerFilter(api, typeof(EmployeeDebSales), null, null);
                        itemFilterDialog.GridSource = dgSalesRepCustomerStat.ItemsSource as IList<UnicontaBaseEntity>;
                        itemFilterDialog.Closing += itemFilterDialog_Closing;
                        itemFilterDialog.Show();
                    }
                    else
                    {
                        itemFilterDialog.GridSource = dgSalesRepCustomerStat.ItemsSource as IList<UnicontaBaseEntity>;
                        itemFilterDialog.Show(true);
                    }
                    break;
                case "ClearItemFilter":
                    itemFilterDialog = null;
                    itemFilterValues = null;
                    break;
                case "Search":
                    BindGrid();
                    break;
                case "Aggregate":
                    aggregate((bool)cbxItem.IsChecked, (bool)cbxInvGrp.IsChecked, (bool)cbxAccount.IsChecked, (bool)cbxAccountGrp.IsChecked, (bool)cbxEmpGrp.IsChecked);
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        IEnumerable<PropValuePair> itemFilterValues;
        FilterSorter itemPropSort;

        void itemFilterDialog_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (itemFilterDialog.DialogResult == true)
            {
                itemFilterValues = itemFilterDialog.PropValuePair;
                itemPropSort = itemFilterDialog.PropSort;
            }
#if !SILVERLIGHT
            e.Cancel = true;
            itemFilterDialog.Hide();
#endif
        }

        private void BindGrid()
        {
            localMenu.EnableButtons(new string[] { "Aggregate" });

            cbxItem.IsEnabled = true;
            cbxInvGrp.IsEnabled = true;
            cbxAccount.IsEnabled = true;
            cbxAccountGrp.IsEnabled = true;
            cbxEmpGrp.IsEnabled = true;

            mainList = null;
            var inputs = new List<PropValuePair>();

            if (itemFilterValues != null)
                inputs.AddRange(itemFilterValues);

            var t = Filter(inputs);
        }

        private Task Filter(IEnumerable<PropValuePair> propValuePair)
        {
            return dgSalesRepCustomerStat.Filter(propValuePair);
        }

        IList<EmployeeDebSales> mainList;
        void aggregate(bool aggregateItem, bool aggregateItemGroup, bool aggregateAccount, bool aggregateAccountGroup, bool aggregateEmployee)
        {
            IList<EmployeeDebSales> lst;

            if (mainList == null)
                mainList = (IList<EmployeeDebSales>)dgSalesRepCustomerStat.ItemsSource;
            if (mainList == null || mainList.Count == 0)
            {
                UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("NoDataCollected"), Uniconta.ClientTools.Localization.lookup("Information"), MessageBoxButton.OK);
                return;
            }

            if (aggregateItem)
            {
                if (aggregateAccountGroup)
                {
                    lst = (from rec in mainList
                           group rec by new { rec._Item, rec.Group } into g
                           select new EmployeeDebSales
                           {
                               _Item = g.Key._Item,
                               _debGroup = g.Key.Group,
                               _Qty = g.Sum(d => d._Qty),
                               _Cost = g.Sum(d => d._Cost),
                               _Sales = g.Sum(d => d._Sales)
                           }).ToList();
                }
                else
                {
                    lst = (from rec in mainList
                           group rec by rec._Item into g
                           select new EmployeeDebSales
                           {
                               _Item = g.Key,
                               _Qty = g.Sum(d => d._Qty),
                               _Cost = g.Sum(d => d._Cost),
                               _Sales = g.Sum(d => d._Sales)
                           }).ToList();
                }
            }
            else if (aggregateAccount)
            {
                if (aggregateItemGroup)
                {
                    lst = (from rec in mainList
                           group rec by new { rec._Account, rec.ItemGroup } into g
                           select new EmployeeDebSales
                           {
                               _Account = g.Key._Account,
                               _invGroup = g.Key.ItemGroup,
                               _Qty = g.Sum(d => d._Qty),
                               _Cost = g.Sum(d => d._Cost),
                               _Sales = g.Sum(d => d._Sales)
                           }).ToList();
                }
                else
                {
                    lst = (from rec in mainList
                           group rec by rec._Account into g
                           select new EmployeeDebSales
                           {
                               _Account = g.Key,
                               _Qty = g.Sum(d => d._Qty),
                               _Cost = g.Sum(d => d._Cost),
                               _Sales = g.Sum(d => d._Sales)
                           }).ToList();
                }
            }
            else if (aggregateItemGroup)
            {
                if (aggregateAccountGroup)
                {
                    lst = (from rec in mainList
                           group rec by new { rec.ItemGroup, rec.Group } into g
                           select new EmployeeDebSales
                           {
                               _invGroup = g.Key.ItemGroup,
                               _debGroup = g.Key.Group,
                               _Qty = g.Sum(d => d._Qty),
                               _Cost = g.Sum(d => d._Cost),
                               _Sales = g.Sum(d => d._Sales)
                           }).ToList();
                }
                else
                {
                    lst = (from rec in mainList
                           group rec by rec.ItemGroup into g
                           select new EmployeeDebSales
                           {
                               _invGroup = g.Key,
                               _Qty = g.Sum(d => d._Qty),
                               _Cost = g.Sum(d => d._Cost),
                               _Sales = g.Sum(d => d._Sales)
                           }).ToList();
                }
            }
            else if (aggregateAccountGroup)
            {
                lst = (from rec in mainList
                       group rec by rec.Group into g
                       select new EmployeeDebSales
                       {
                           _debGroup = g.Key,
                           _Qty = g.Sum(d => d._Qty),
                           _Cost = g.Sum(d => d._Cost),
                           _Sales = g.Sum(d => d._Sales)
                       }).ToList();
            }
            else if (aggregateEmployee)
            {
                lst = (from rec in mainList
                       group rec by rec.Employee into g
                       select new EmployeeDebSales
                       {
                           _Employee = g.Key,
                           _Qty = g.Sum(d => d._Qty),
                           _Cost = g.Sum(d => d._Cost),
                           _Sales = g.Sum(d => d._Sales)
                       }).ToList();
            }
            else
                lst = mainList;

            if (lst != null)
            {
                var companyID = api.CompanyId;
                foreach (var item in lst)
                {
                    item._CompanyId = companyID;
                }
                dgSalesRepCustomerStat.ItemsSource = lst;
            }

        }
    }
}
