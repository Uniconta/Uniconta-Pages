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
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.DataModel;
using Localization = Uniconta.ClientTools.Localization;
using Uniconta.ClientTools.Controls;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class EmployeeCommissionGrid : CorasauDataGridClient
    {
        public override Type TableType => typeof(EmployeeCommissionClient);

        public override IComparer GridSorting
        { 
            get { return new EmployeeCommissionClientSort(); }
        }

        public override bool Readonly
        {
            get { return false; }
        }
    }

    

    /// <summary>
    /// Interaction logic for EmployeeCommissionPage.xaml
    /// </summary>
    public partial class EmployeeCommissionPage : GridBasePage
    {
        public class InvoiceSort : IComparer<DebtorInvoiceClient>, IComparer
        {
            public int Compare(DebtorInvoiceClient _x, DebtorInvoiceClient _y)
            {
                long c = _x._InvoiceNumber - _y.InvoiceNumber;
                if (c > 0)
                    return 1;
                if (c < 0)
                    return -1;
                var cx = DateTime.Compare(_x._Date, _y._Date);
                if (cx != 0)
                    return cx;
                return string.Compare(_x._DCAccount, _y._DCAccount);
            }
            public int Compare(object _x, object _y) { return Compare((EmployeeCommission)_x, (EmployeeCommission)_y); }
        }

        private UnicontaBaseEntity master;

        public EmployeeCommissionPage(BaseAPI API)
            : base(API, string.Empty)
        {
            InitPage();
        }

        public EmployeeCommissionPage(UnicontaBaseEntity master)
           : base(master)
        {
            this.master = master;
            InitPage();
        }

        private void InitPage()
        {
            InitializeComponent();
            dgEmployeeCommissionGrid.UpdateMaster(master);
            localMenu.dataGrid = dgEmployeeCommissionGrid;
            SetRibbonControl(localMenu, dgEmployeeCommissionGrid);
            dgEmployeeCommissionGrid.api = api;
            dgEmployeeCommissionGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgEmployeeCommissionGrid.RowDoubleClick += DgEmployeeCommissionGrid_RowDoubleClick;
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            //TODO: Create Master for Item and Account
            if (master is Uniconta.DataModel.Employee)
                Employee.Visible = false;
            else if (master is Uniconta.DataModel.InvItem)
                Item.Visible = false;
            else if (master is Uniconta.DataModel.Debtor)
                Account.Visible = false;
        }

        private void DgEmployeeCommissionGrid_RowDoubleClick()
        {
            localMenu_OnItemClicked("EditRow");
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgEmployeeCommissionGrid.SelectedItem as EmployeeCommissionClient;
            switch (ActionType)
            {
                case "AddRow":
                    dgEmployeeCommissionGrid.AddRow();
                    break;
                case "CopyRow":
                    dgEmployeeCommissionGrid.CopyRow();
                    break;
                case "SaveGrid":
                    dgEmployeeCommissionGrid.SaveData();
                    break;
                case "DeleteRow":
                    if (selectedItem != null)
                        dgEmployeeCommissionGrid.DeleteRow();
                    break;
                case "CalculateGrid":
                    CheckItemNumber();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        protected override void LoadCacheInBackGround()
        {
            LoadType( new Type[] { typeof(Uniconta.DataModel.Employee), typeof(Uniconta.DataModel.InvItem), typeof(Uniconta.DataModel.Debtor), typeof(Uniconta.DataModel.InvGroup), typeof(Uniconta.DataModel.DebtorGroup) });
        }

        private CalCommissionClient CalculateCommissionInvTran(InvTransClient tran, EmployeeCommission commission)
        {        
            double amount;

            if (commission._FixedPrice != 0)
                amount = -commission._FixedPrice * tran._Qty;
            else
            {
                if (commission._IsRevenue)
                    amount = tran._NetAmount() * commission._Rate / -100d;
                else
                    amount = tran.Margin * commission._Rate / -100d;
            }
            if (amount == 0)
                return null;

            var calCom = new CalCommissionClient();
            calCom.SetMaster(tran);
            calCom._InvoiceNumber = tran._InvoiceNumber;
            calCom._Commission = Math.Round(amount, 2);
            return calCom;
        }

        private CalCommissionClient CalculateCommissionDebInvoice(DebtorInvoiceClient dic, EmployeeCommission commission)
        {
            double amount;

            if (commission._FixedPrice != 0)
                amount = commission._FixedPrice;
            else
            {
                if (commission._IsRevenue)
                    amount = dic._NetAmountCur * commission._Rate / 100d;
                else
                    amount = dic._Margin * commission._Rate / 100d;
            }
            if (amount == 0)
                return null;

            var calCom = new CalCommissionClient();
            calCom._CompanyId = dic.CompanyId;
            calCom._Employee = dic._Employee;
            calCom._Account = dic._DCAccount;
            calCom._InvoiceNumber = (int)dic._InvoiceNumber;
            calCom._Commission = Math.Round(amount, 2);
            return calCom;
        }

        void CheckItemNumber()
        {
            var calCommission = new CWCalculateCommission(api);
            calCommission.Closing += async delegate
            {
                if (calCommission.DialogResult == true)
                {
                    var fromDate = calCommission.FromDateTime;
                    var toDate = calCommission.ToDateTime;

                    var commAll = ((IList)dgEmployeeCommissionGrid.ItemsSource).Cast<EmployeeCommissionClient>();
                    var commlstLine = new List<EmployeeCommissionClient>();
                    var commlstHead = new List<EmployeeCommissionClient>();
                    var employee = (master as Uniconta.DataModel.Employee)?._Number;

                    var company = api.CompanyEntity;
                    var debtors = company.GetCache(typeof(Uniconta.DataModel.Debtor)) ??
                                           await company.LoadCache(typeof(Uniconta.DataModel.Debtor), api);

                    var invItems = company.GetCache(typeof(Uniconta.DataModel.InvItem)) ??
                           await company.LoadCache(typeof(Uniconta.DataModel.InvItem), api);

                    var propValuePairList = new List<PropValuePair>()
                {
                    PropValuePair.GenereteWhereElements("Date", typeof(DateTime), string.Format("{0}..{1}", fromDate.ToShortDateString(), toDate.ToShortDateString())),
                    PropValuePair.GenereteWhereElements("Disabled", typeof(int), "0")
                };
                    var invoiceHeaders = await api.Query<DebtorInvoiceClient>(dgEmployeeCommissionGrid.masterRecords, propValuePairList);
                    foreach (var rec in commAll)
                    {
                        if (rec._Disabled)
                            continue;

                        if (rec._FromDate != DateTime.MinValue && rec._FromDate > toDate)
                            continue;

                        if (rec._ToDate != DateTime.MinValue && rec._ToDate < fromDate)
                            continue;

                        if (employee != null && employee != rec._Employee)
                            continue;

                        if (rec._PerLine)
                            commlstLine.Add(rec);
                        else
                            commlstHead.Add(rec);
                    }

                    var sort = new EmployeeCommissionClientSort();
                    commlstLine.Sort(sort);
                    commlstHead.Sort(sort);

                    var calComs = new List<CalCommissionClient>();
                    if (commlstLine.Count > 0)
                    {
                        propValuePairList.RemoveAt(1); // remove disabled
                        propValuePairList.Add(PropValuePair.GenereteWhereElements("MovementType", typeof(int), "1"));

                        var trans = await api.Query<InvTransClient>(dgEmployeeCommissionGrid.masterRecords, propValuePairList); //sandt
                        if (trans != null)
                        {
                            // lets sort invoices so we can find employee on invoice header
                            var invSort = new InvoiceSort();
                            DebtorInvoiceClient invKey = null;
                            if (invoiceHeaders != null && invoiceHeaders.Length > 0)
                            {
                                Array.Sort(invoiceHeaders, invSort);
                                invKey = new DebtorInvoiceClient();
                            }

                            foreach (var tran in trans)
                            {
                                var item = tran._Item;
                                var acc = tran._DCAccount;
                                var emp = tran._Employee;
                                string debGroup = null;
                                string itemGroup = null;

                                if (item != null)
                                {
                                    var inv = (InvItem)invItems.Get(item);
                                    itemGroup = inv?._Group;
                                }
                                if (acc != null)
                                {
                                    var deb = (Debtor)debtors.Get(acc);
                                    debGroup = deb?._Group;
                                }
                                if (emp == null && invKey != null)
                                {
                                    invKey._InvoiceNumber = tran._InvoiceNumber;
                                    invKey._DCAccount = tran._DCAccount;
                                    invKey._Date = tran._Date;
                                    var pos = Array.BinarySearch(invoiceHeaders, invKey, invSort);
                                    if (pos >= 0 && pos < invoiceHeaders.Length)
                                    {
                                        var rec = invoiceHeaders[pos];
                                        emp = tran._Employee = rec._Employee;
                                    }
                                }

                                foreach (var c in commlstLine)
                                {
                                    var cmp = string.Compare(c._Employee, emp);
                                    if (cmp > 0)
                                        break;

                                    if (cmp == 0 &&
                                         CompareKey(c._Item, item) && CompareKey(c._Account, acc) && CompareKey(c._ItemGroup, itemGroup) &&
                                         CompareKey(c._DebGroup, debGroup))
                                    {
                                        var calculatedCommission = CalculateCommissionInvTran(tran, c);
                                        if (calculatedCommission == null || calculatedCommission._Commission == 0)
                                            continue;

                                        calComs.Add(calculatedCommission);

                                        if (!c._KeepLooking)
                                            break;
                                    }
                                }
                            }
                        }
                    }
                    if (commlstHead.Count > 0 && invoiceHeaders != null)
                    {
                        foreach (var it in invoiceHeaders)
                        {
                            string debGroup = null;
                            var emp = it._Employee;
                            var acc = it._DCAccount;
                            if (acc != null)
                            {
                                var deb = (Debtor)debtors.Get(acc);
                                debGroup = deb?._Group;
                            }

                            foreach (var c in commlstHead)
                            {
                                var cmp = string.Compare(c._Employee, emp);
                                if (cmp > 0)
                                    break;

                                if (cmp == 0 && CompareKey(c._Account, acc) && CompareKey(c._DebGroup, debGroup))
                                {
                                    var calculatedCommission = CalculateCommissionDebInvoice(it, c);
                                    if (calculatedCommission == null || calculatedCommission._Commission == 0)
                                        continue;

                                    calComs.Add(calculatedCommission);

                                    if (!c._KeepLooking)
                                        break;
                                }
                            }
                        }
                    }
                    if (calComs.Count <= 0)
                    {
                        UnicontaMessageBox.Show(Localization.lookup("NoRecordExport"), Uniconta.ClientTools.Localization.lookup("Message"));
                        return;
                    }
                    var arr = calComs.ToArray();
                    Array.Sort(arr, new CalCommissionClientSort());
                    AddDockItem(TabControls.CalculatedCommissionPage, new object[] { arr }, Uniconta.ClientTools.Localization.lookup("CalculateCommission"), null, true);
                }
            };
            calCommission.Show();
        }

        static bool CompareKey(string filter, string value)
        {
            return filter == null || filter == value;
        }
    }
}   