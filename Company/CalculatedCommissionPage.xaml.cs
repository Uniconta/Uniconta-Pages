using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
using UnicontaClient.Models;
using UnicontaClient.Pages;
using UnicontaClient.Utilities;
using DevExpress.Xpo.DB.Helpers;
using Uniconta.API.Service;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class CalculatedCommissionGrid : CorasauDataGridClient
    {
        public override Type TableType
        {
            get { return typeof(CalCommissionClient); }
        }
    }

    public class CalCommissionClientSort : IComparer<CalCommissionClient>, IComparer
    {
        static int Cmp(string x, string y)
        {
            if (x == null && y != null)
                return -1;
            if (x != null && y == null)
                return 1;
            return string.Compare(x, y);
        }
        public int Compare(CalCommissionClient _x, CalCommissionClient _y)
        {
            int c = string.Compare(_x._Employee, _y._Employee);
            if (c != 0)
                return c;
            c = Cmp(_x._Item, _y._Item);
            if (c != 0)
                return c;
            c = Cmp(_x._ItemGroup, _y._ItemGroup);
            if (c != 0)
                return c;
            c = Cmp(_x._Account, _y._Account);
            if (c != 0)
                return c;
            c = Cmp(_x._DebGroup, _y._DebGroup);
            if (c != 0)
                return c;
            return _x._InvoiceNumber - _y._InvoiceNumber;
        }
        public int Compare(object _x, object _y) { return Compare((EmployeeCommission)_x, (EmployeeCommission)_y); }
    }

    [ClientTableAttribute(LabelKey = "EmployeeCommission")]
    public class CalCommissionClient : EmployeeCommissionClient
    {
        public double _Commission;
        public int _InvoiceNumber;

        [Display(Name = "Commission", ResourceType = typeof(EmployeeCommissionClientText))]
        public double Commission
        {
            get { return _Commission; }
        }

        [ForeignKey(ForeignKeyTable = typeof(DCInvoice))]
        [Display(Name = "InvoiceNumber", ResourceType = typeof(DCInvoiceText))]
        public int InvoiceNumber
        {
            get { return _InvoiceNumber; }
        }
    }

    /// <summary>
    /// Interaction logic for CalculatedCommissionPage.xaml
    /// </summary>
    public partial class CalculatedCommissionPage : GridBasePage
    {
        CWServerFilter itemFilterDialog;
        IList<CalCommissionClient> mainList;
        IEnumerable<PropValuePair> itemFilterValues;
        FilterSorter itemPropSort;

        public CalculatedCommissionPage(CalCommissionClient[] calucaltedCommission)
           : base(null)
        {
           // var calucaltedCommission = obj as CalCommissionClient[];
            if (calucaltedCommission != null)
                InitPage(calucaltedCommission);
        }
        private void InitPage(CalCommissionClient[] calucaltedCommission)
        {
            InitializeComponent();
            dgCalculatedCommissionGrid.tableView.ShowGroupPanel = true;
            localMenu.dataGrid = dgCalculatedCommissionGrid;
            SetRibbonControl(localMenu, dgCalculatedCommissionGrid);
            dgCalculatedCommissionGrid.api = api;
            dgCalculatedCommissionGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgCalculatedCommissionGrid.ItemsSource = calucaltedCommission;
            dgCalculatedCommissionGrid.Visibility = Visibility.Visible;
            dgCalculatedCommissionGrid.ShowTotalSummary();
        }

        public override Task InitQuery(){ return null;}

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
                case "Aggregate":
                    Aggregate((bool)cbxItem.IsChecked, (bool)cbxInvGrp.IsChecked, (bool)cbxAccount.IsChecked, (bool)cbxAccountGrp.IsChecked, (bool) cbxInvoiceNumber.IsChecked);
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

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

        
        private void Aggregate(bool aggregateItem, bool aggregateItemGroup, bool aggregateAccount, bool aggregateAccountGroup, bool aggregateInvoiceNumber)
        {
            IList<CalCommissionClient> lst = null;

            if (mainList == null)
                mainList = (IList<CalCommissionClient>)dgCalculatedCommissionGrid.ItemsSource;
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
                        group rec by new { rec.Employee, rec.Item, rec.DebGroup}
                        into g
                        select new CalCommissionClient
                        {
                            _Employee = g.Key.Employee,
                            _Item = g.Key.Item,
                            _DebGroup = g.Key.DebGroup,
                            _Commission = g.Sum(d => d.Commission),
                        }).ToList();
                }
                else
                {
                    lst = (from rec in mainList
                        group rec by new{rec.Employee, rec.Item}
                        into g
                        select new CalCommissionClient
                        {
                            _Employee = g.Key.Employee,
                            _Item = g.Key.Item,
                            _Commission = g.Sum(d => d._Commission)
                        }).ToList();
                }
            }
            else if (aggregateAccount)
            {
                if (aggregateItemGroup)
                {
                    lst = (from rec in mainList
                        group rec by new { rec.Employee, rec.Account, rec.ItemGroup}
                        into g
                        select new CalCommissionClient
                        {
                            _Employee = g.Key.Employee,
                            _Account = g.Key.Account,
                            _ItemGroup = g.Key.ItemGroup,
                            _Commission = g.Sum(d => d._Commission)
                        }).ToList();
                }
                else
                {
                    lst = (from rec in mainList
                        group rec by new{rec.Employee, rec.Account}
                        into g
                        select new CalCommissionClient
                        {
                            _Employee = g.Key.Employee,
                            _Account = g.Key.Account,
                            _Commission = g.Sum(d => d._Commission)
                        }).ToList();
                }
            }
            else if (aggregateItemGroup)
            {
                if (aggregateAccountGroup)
                {
                    lst = (from rec in mainList
                        group rec by new { rec.Employee, rec.ItemGroup, rec.DebGroup}
                        into g
                        select new CalCommissionClient
                        {
                            _Employee = g.Key.Employee,
                            _ItemGroup = g.Key.ItemGroup,
                            _DebGroup = g.Key.DebGroup,
                            _Commission = g.Sum(d => d._Commission)
                        }).ToList();
                }
                else
                {
                    lst = (from rec in mainList
                        group rec by new {rec.Employee,rec.ItemGroup}
                        into g
                        select new CalCommissionClient
                        {
                            _Employee = g.Key.Employee,
                            _ItemGroup = g.Key.ItemGroup,
                            _Commission = g.Sum(d => d._Commission)
                        }).ToList();
                }
            }
            else if (aggregateAccountGroup)
            {
                lst = (from rec in mainList
                    group rec by new {rec.Employee, rec.DebGroup}
                    into g
                    select new CalCommissionClient
                    {
                        _Employee = g.Key.Employee,
                        _DebGroup = g.Key.DebGroup,
                        _Commission = g.Sum(d => d._Commission)
                    }).ToList();
            }
            else if (aggregateInvoiceNumber)
            {
                lst = (from rec in mainList
                       group rec by new { rec.Employee, rec.InvoiceNumber }
                    into g
                       select new CalCommissionClient
                       {
                           _Employee = g.Key.Employee,
                           _InvoiceNumber = g.Key.InvoiceNumber,
                           _Commission = g.Sum(d => d._Commission),
                       }).ToList();
            }
            else
                lst = mainList;

            if (lst != null)
            {
                var companyID = api.CompanyId;
                foreach (var rec in lst)
                {
                    rec._CompanyId = companyID;
                }
                dgCalculatedCommissionGrid.ItemsSource = lst;
            }

        }
    }
}