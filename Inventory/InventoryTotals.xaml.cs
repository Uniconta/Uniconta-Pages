using UnicontaClient.Models;
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

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class InventoryTotalsGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(InvSumClient); } }
        public override IComparer GridSorting { get { return new InvSumInvDbCmp(); } }
    }

    public partial class InventoryTotals : GridBasePage
    {
        CWServerFilter itemFilterDialog;
        UnicontaBaseEntity master;
        byte DCType;

        public InventoryTotals(BaseAPI API) : base(API, string.Empty)
        {
            InitPage(null);
        }
        public InventoryTotals(UnicontaBaseEntity _master)
            : base(_master)
        {
            InitPage(_master);
        }

        public InventoryTotals(SynchronizeEntity syncEntity) : base(syncEntity, true)
        {
            this.syncEntity = syncEntity;
            InitPage(syncEntity.Row);
            SetHeader();
        }
        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            dgInventoryTotals.UpdateMaster(args);
            SetHeader();
        }

        private void SetHeader()
        {
            string key = Utility.GetHeaderString(dgInventoryTotals.masterRecord);
            if (string.IsNullOrEmpty(key))
                return;

            string header = string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("ItemAccountStat"), key);
            SetHeader(header);
        }

        private void InitPage(UnicontaBaseEntity _master)
        {
            InitializeComponent();
            master = _master;
            dgInventoryTotals.UpdateMaster(_master);
            dgInventoryTotals.tableView.ShowGroupPanel = true;
            dgInventoryTotals.ShowTotalSummary();
            localMenu.dataGrid = dgInventoryTotals;
            SetRibbonControl(localMenu, dgInventoryTotals);
            gridControl.api = api;
            gridControl.BusyIndicator = busyIndicator;
            var strDCType = new string[] { Uniconta.ClientTools.Localization.lookup("Debtor"), Uniconta.ClientTools.Localization.lookup("Creditor") };
            cmbDCType.ItemsSource = strDCType;
            cmbDCType.SelectedIndex = 0;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            localMenu.DisableButtons( "Aggregate" );
        }

        public override Task InitQuery()
        {
            return BindGrid();
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            if (master is InvItemClient)
            {
                txtItem.Visibility = Visibility.Collapsed;
                cbxItem.Visibility = Visibility.Collapsed;

                txtItemGrp.Visibility = Visibility.Collapsed;
                cbxInvGrp.Visibility = Visibility.Collapsed;

                Item.Visible = false;
                ItemName.Visible = false;
                ItemGroup.Visible = false;
            }
            else if (master is DCAccount)
            {
                DCType = (master as DCAccount).__DCType();

                txtAccount.Visibility = Visibility.Collapsed;
                cbxAccount.Visibility = Visibility.Collapsed;

                txtAccGrp.Visibility = Visibility.Collapsed;
                cbxAccountGrp.Visibility = Visibility.Collapsed;

                Account.Visible = false;
                AccountName.Visible = false;
                Group.Visible = false;

                txtDCType.Visibility = Visibility.Collapsed;
                cmbDCType.Visibility = Visibility.Collapsed;
            }

#if !SILVERLIGHT
            Utility.SetupVariants(api, colVariant, VariantName, colVariant1, colVariant2, colVariant3, colVariant4, colVariant5, Variant1Name, Variant2Name, Variant3Name, Variant4Name, Variant5Name);
#else
            Utility.SetupVariants(api, null, colVariant1, colVariant2, colVariant3, colVariant4, colVariant5, Variant1Name, Variant2Name, Variant3Name, Variant4Name, Variant5Name);
#endif
            LoadType(new Type[] { typeof(Uniconta.DataModel.InvItem), typeof(Uniconta.DataModel.Debtor), typeof(Uniconta.DataModel.Creditor) });
        }

        void localMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "ItemFilter":
                    if (itemFilterDialog == null)
                    {
                        itemFilterDialog = new CWServerFilter(api, typeof(InvSumClient), null, null);
                        itemFilterDialog.Closing += itemFilterDialog_Closing;
#if !SILVERLIGHT
                        itemFilterDialog.Show();
                    }
                    else
                        itemFilterDialog.Show(true);
#elif SILVERLIGHT
                    }

                    itemFilterDialog.Show();
#endif
                    break;
                case "ClearItemFilter":
                    itemFilterDialog = null;
                    itemFilterValues = null;
                    break;
                case "Search":
                    BindGrid();
                    break;
                case "Aggregate":
                    aggregate((bool)cbxItem.IsChecked, (bool)cbxInvGrp.IsChecked, (bool)cbxAccount.IsChecked, (bool)cbxAccountGrp.IsChecked);
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

        Task BindGrid()
        {
            localMenu.EnableButtons("Aggregate");

            Date.Visible = cbxGrpByDte.IsChecked.Value;

            cbxItem.IsEnabled= true;
            cbxInvGrp.IsEnabled= true;
            cbxAccount .IsEnabled= true;
            cbxAccountGrp.IsEnabled= true;

            mainList = null;
            var inputs = new List<PropValuePair>();
            inputs.Add(PropValuePair.GenereteParameter("DCType", typeof(string), Convert.ToString(DCType != 0 ? DCType : cmbDCType.SelectedIndex + 1)));
            if (cbxGrpByDte.IsChecked == true)
                inputs.Add(PropValuePair.GenereteParameter("GroupByDate", typeof(string), "1"));

            if (itemFilterValues != null)
                inputs.AddRange(itemFilterValues);

            return dgInventoryTotals.Filter(inputs);
        }

        IList<InvSumClient> mainList;
        void aggregate(bool aggregateItem, bool aggregateItemGroup, bool aggregateAccount, bool aggregateAccountGroup)
        {
            IList<InvSumClient> lst;

            if (mainList == null)
                mainList = (IList<InvSumClient>)dgInventoryTotals.ItemsSource;
            if (mainList == null || mainList.Count == 0)
            {
                UnicontaMessageBox.Show( Uniconta.ClientTools.Localization.lookup("NoDataCollected"), Uniconta.ClientTools.Localization.lookup("Information"), MessageBoxButton.OK);
                return;
            }

            if (aggregateItem)
            {
                if (aggregateAccountGroup)
                {
                    lst = (from rec in mainList
                           group rec by new { rec._Item, rec.Group } into g
                           select new InvSumClient
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
                           select new InvSumClient
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
                           select new InvSumClient
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
                           select new InvSumClient
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
                           select new InvSumClient
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
                           select new InvSumClient
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
                       select new InvSumClient
                       {
                           _debGroup = g.Key,
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
                foreach(var item in lst)
                {
                    item._CompanyId = companyID;
                }
                dgInventoryTotals.ItemsSource = lst;
            }
                
        }
    }
}
