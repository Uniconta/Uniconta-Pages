using UnicontaClient.Models;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Uniconta.ClientTools.Util;
using System.Collections.ObjectModel;
using Uniconta.ClientTools.Controls;
using Uniconta.Common.Utility;
using Uniconta.API.Service;
using UnicontaClient.Controls;
using Uniconta.API.System;
using Uniconta.ClientTools.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class Criteria
    {
        public List<SelectedCriteria> selectedCriteria { get; set; }
        public string FromAccount { get; set; }
        public string ToAccount { get; set; }
        public bool Skip0Account { get; set; }
        public bool SkipSumAccount { get; set; }
        public bool OnlySumAccounts { get; set; }
        public string Template { get; set; }
        public bool dim1details { get; set; }
        public bool dim2details { get; set; }
        public bool dim3details { get; set; }
        public bool dim4details { get; set; }
        public bool dim5details { get; set; }
        public bool ShowDimName { get; set; }
        public string PrintOrientation { get; set; }
        public Balance ObjBalance { get; set; }
        public bool UseExternal { get; set; }
        public bool SortExtern { get; set; }
        public int ShowType { get; set; }
    }

    public partial class ReportCriteria : FormBasePage
    {
        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.GLAccount))]
        public string FromAccount { get; set; }
        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.GLAccount))]
        public string ToAccount { get; set; }
        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.GLReportTemplate))]
        public string RTemplate { get; set; }
        UnicontaBaseEntity editrow;
        public override void OnClosePage(object[] RefreshParams)
        {
            globalEvents.OnRefresh(NameOfControl, RefreshParams);
        }
        public override Type TableType
        {
            get { return null; }
        }

        public override string NameOfControl { get { return TabControls.ReportCriteria.ToString(); } }
        public override UnicontaBaseEntity ModifiedRow { get { return editrow; } set { editrow = value; } }
        protected override bool IsLayoutSaveRequired()
        {
            return false;
        }

        Criteria objCriteria;
        List<BalanceColumn> balanceCollist;
        ObservableCollection<Balance> itemsBalance;
        Balance objBalance;
        UnicontaBaseEntity master;
        Company[] companies;
        static Balance LastGeneratedBalance;
        BalanceFrontPageReportDataClient balanceFrontPageReportData;
        string DefaultBalance;
        public ReportCriteria(UnicontaBaseEntity sourceData) : base(sourceData)
        {
            master = sourceData;
            Init();
        }
        public ReportCriteria(BaseAPI API)
            : base(API, string.Empty)
        {
            Init();
        }

        void Init()
        {
            var t = api.Query<Balance>();
            StartLoadCache(t);
            this.DataContext = this;
            InitializeComponent();
            FocusManager.SetFocusedElement(txtbalanceName, txtbalanceName);
            cbFromAccount.api = cbToAccount.api = cbTemplate.api = api;
            ribbonControl = frmRibbon;
            setDim();
            frmRibbon.OnItemClicked += frmRibbon_OnItemClicked;
            objCriteria = new Criteria();
            objCriteria.selectedCriteria = new List<SelectedCriteria>();
            GetCompanyList();
            defaultCriteria.companyList = companyList;
            defaultCriteria.API = api;
            var itemdataContext = defaultCriteria.DataContext as SelectedCriteria;
            SetDefaultDate(itemdataContext);
            objCriteria.selectedCriteria.Add(itemdataContext);
            setColNameAndNumber(itemdataContext, 1);
            string[] strPrintOrientation = new string[] { Uniconta.ClientTools.Localization.lookup("Landscape"), Uniconta.ClientTools.Localization.lookup("Portrait") };
            cbPrintOrientation.ItemsSource = strPrintOrientation;
            txtColWidth.Text = Util.ConcatParenthesis(Uniconta.ClientTools.Localization.lookup("ColumnWidth"), Uniconta.ClientTools.Localization.lookup("Printout"));
            var sumAccounts = new string[] { Uniconta.ClientTools.Localization.lookup("Show"), Uniconta.ClientTools.Localization.lookup("Hide"), Uniconta.ClientTools.Localization.lookup("OnlyShow") };
            cmbSumAccount.ItemsSource = sumAccounts;
            cmbSumAccount.IsEditable = false;

            cmbAccountType.ItemsSource = new string[] { Uniconta.ClientTools.Localization.lookup("All"), Uniconta.ClientTools.Localization.lookup("AccountTypePL"), Uniconta.ClientTools.Localization.lookup("AccountTypeBalance") };
            cmbAccountType.IsEditable = false;
            this.BeforeClose += ReportCriteria_BeforeClose;
            LoadBalance(t);
        }
        public override void SetParameter(IEnumerable<ValuePair> Parameters)
        {
            foreach (var rec in Parameters)
            {
                if (string.IsNullOrEmpty(rec.Name) || string.Compare(rec.Name, "Balance", StringComparison.CurrentCultureIgnoreCase) == 0)
                {
                    this.DefaultBalance = rec.Value;
                    if (itemsBalance != null)
                        cbBalance.SelectedItem = itemsBalance.Where(x => x._Name == DefaultBalance).FirstOrDefault();
                }
            }
            base.SetParameter(Parameters);
        }
        private void ReportCriteria_BeforeClose()
        {
            if (BasePage.session.User._AutoSave)
            {
                var hasMissignModel = objCriteria.selectedCriteria.Any(p => p.balcolMethod == BalanceColumnMethod.FromBudget && string.IsNullOrEmpty(p.budgetModel));

                if (!hasMissignModel && !string.IsNullOrEmpty(txtbalanceName.Text))
                {
                    if (objBalance == null || objBalance.CompanyId != api.CompanyId /* For balance copy from other company */)
                        SaveBalance();
                    else
                        update();
                }
            }
            this.BeforeClose -= ReportCriteria_BeforeClose;
        }

        void SetDefaultDate(SelectedCriteria Selectedcol)
        {
            var Now = BasePage.GetSystemDefaultDate().Date;
            Selectedcol.FromDate = Now.AddDays(1 - Now.Day).AddMonths(-1);
            Selectedcol.ToDate = Now;
        }

        void setColNameAndNumber(SelectedCriteria Selectedcol, int colnum)
        {
            Selectedcol.ColNo = colnum;
            Selectedcol.ColNameNumber = string.Concat(Uniconta.ClientTools.Localization.lookup("Name"), " (", NumberConvert.ToString(colnum), ")");
        }
        class balSort : IComparer<Balance>
        {
            int IComparer<Balance>.Compare(Balance x, Balance y)
            {
                return string.Compare(x._Name, y._Name, StringComparison.CurrentCultureIgnoreCase);
            }
        }
        async void LoadBalance(Task<Balance[]> balTsk)
        {
            busyIndicator.IsBusy = true;
            companies = CWDefaultCompany.loadedCompanies;
            var lstEntity = await balTsk;
            if (lstEntity != null && lstEntity.Length > 0)
            {
                Array.Sort(lstEntity, new balSort());
                itemsBalance = new ObservableCollection<Balance>(lstEntity);
                cbBalance.ItemsSource = itemsBalance;
                if (LastGeneratedBalance != null)
                    cbBalance.SelectedItem = lstEntity.Where(x => x.RowId == LastGeneratedBalance.RowId).FirstOrDefault();
                else
                    cbBalance.SelectedIndex = 0;
                if (DefaultBalance != null)
                    cbBalance.SelectedItem = lstEntity.Where(x => x._Name == DefaultBalance).FirstOrDefault();
            }
            else
                cbBalance.ItemsSource = new ObservableCollection<Balance>();
            busyIndicator.IsBusy = false;
        }

        protected override void LoadCacheInBackGround()
        {
            var noofDimensions = api.CompanyEntity.NumberOfDimensions;
            var lst = new List<Type>(noofDimensions + 3) { typeof(Uniconta.DataModel.GLAccount) };
            if (noofDimensions >= 1)
                lst.Add(typeof(Uniconta.DataModel.GLDimType1));
            if (noofDimensions >= 2)
                lst.Add(typeof(Uniconta.DataModel.GLDimType2));
            if (noofDimensions >= 3)
                lst.Add(typeof(Uniconta.DataModel.GLDimType3));
            if (noofDimensions >= 4)
                lst.Add(typeof(Uniconta.DataModel.GLDimType4));
            if (noofDimensions >= 5)
                lst.Add(typeof(Uniconta.DataModel.GLDimType5));
            lst.Add(typeof(Uniconta.DataModel.GLBudget));
            lst.Add(typeof(Uniconta.DataModel.GLDailyJournal));
            LoadType(lst);
        }
        List<Company> companyList = null;
        List<Company> GetCompanyList()
        {
            if (companyList == null)
            {
                List<Company> compList = new List<Company>();
                var companies = CWDefaultCompany.loadedCompanies;
                if (companies != null)
                {
                    compList.Capacity = companies.Length + 1;
                    compList.Add(new Company() { _Name = "" });
                    compList.AddRange(companies);
                    compList.Sort(SQLCache.KeyStrSorter);
                }
                companyList = compList;
            }
            return companyList;
        }
        private void populateValue(Balance obBalance)
        {
            balanceCollist = obBalance.ColumnList;
            if (balanceCollist != null)
            {
                bool first = true;
                foreach (var colBalance in balanceCollist)
                {
                    if (first)
                    {
                        first = false;
                        var Crit = objCriteria.selectedCriteria[0];
                        Crit.CriteriaName = colBalance._Name;
                        Crit.Journal = colBalance._InclJournal;
                        Crit.BudgetModel = colBalance._BudgetId;
                        Crit.FromDate = colBalance._FromDate;
                        Crit.ToDate = colBalance._ToDate;
                        Crit.dimval1 = colBalance.Dims1;
                        Crit.dimval2 = colBalance.Dims2;
                        Crit.dimval3 = colBalance.Dims3;
                        Crit.dimval4 = colBalance.Dims4;
                        Crit.dimval5 = colBalance.Dims5;
                        Crit.NotifyPropertyChanged("Dim1");
                        Crit.NotifyPropertyChanged("Dim2");
                        Crit.NotifyPropertyChanged("Dim3");
                        Crit.NotifyPropertyChanged("Dim4");
                        Crit.NotifyPropertyChanged("Dim5");
                        Crit.balanceColumnFormat = colBalance.ColumnFormatEnum;
                        Crit.balanceColumnMethod = colBalance.ColumnMethodEnum;
                        Crit.ShowDebitCredit = colBalance._ShowDebitCredit;
                        Crit.InvertSign = colBalance._InvertSign;
                        Crit.InclPrimo = colBalance._InclPrimo;
                        Crit.ColA = colBalance._ColA;
                        Crit.ColB = colBalance._ColB;
                        Crit.Account100 = colBalance._Account100;
                        if (colBalance._ForCompanyId > 0)
                            Crit.ForCompany = companies?.Where(x => x.CompanyId == colBalance._ForCompanyId).FirstOrDefault();
                        else
                            Crit.ForCompany = null;
                        Crit.Hide = colBalance._Hide;
                        GetBalanceDimUsed(obBalance);
                        setColNameAndNumber(Crit, 1);
                    }
                    else
                    {
                        CriteriaControl crControl = new CriteriaControl();
                        crControl.companyList = companyList;
                        crControl.API = api;
                        crControl.MouseLeftButtonDown += CrControl_MouseLeftButtonDown;
                        ControlContainer.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
                        Grid.SetRow(crControl, ControlContainer.Children.Count);
                        ControlContainer.Children.Add(crControl);
                        objCriteria.selectedCriteria.Add(SetCriteriaControlValue(colBalance, crControl));
                    }
                }
            }
        }
        CriteriaControl selectedColumnControl = null;
        private void CrControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (selectedColumnControl != sender)
                selectedColumnControl?.Unhighlight();
            selectedColumnControl = sender as CriteriaControl;
        }

        void MoveColumnUp()
        {
            if (selectedColumnControl == null)
                return;
            var rowNo = Convert.ToInt32(selectedColumnControl.GetValue(Grid.RowProperty));
            CriteriaControl upperCtrl = null;
            foreach (var child in ControlContainer.Children)
            {
                var ctrl = child as CriteriaControl;
                if (ctrl != null)
                {
                    var upperrow = Convert.ToInt32(ctrl.GetValue(Grid.RowProperty));
                    if (upperrow == rowNo - 1)
                    {
                        upperCtrl = ctrl;
                        break;
                    }
                }
            }
            if (upperCtrl != null)
            {
                int oldIndex = rowNo - 2;
                if (oldIndex > 0)
                {
                    Grid.SetRow(upperCtrl, rowNo);
                    var upperCtrlCriteria = upperCtrl.DataContext as SelectedCriteria;
                    if (upperCtrlCriteria != null)
                        setColNameAndNumber(upperCtrlCriteria, upperCtrlCriteria.ColNo + 1);
                    Grid.SetRow(selectedColumnControl, rowNo - 1);
                    var selCtrlCriteria = selectedColumnControl.DataContext as SelectedCriteria;
                    if (selCtrlCriteria != null)
                        setColNameAndNumber(selCtrlCriteria, selCtrlCriteria.ColNo - 1);

                    var crit = objCriteria.selectedCriteria;
                    if (crit?.ElementAtOrDefault(oldIndex) != null)
                    {
                        var col = crit[oldIndex];
                        crit.RemoveAt(oldIndex);
                        crit.Insert(oldIndex - 1, col);
                    }
                }
            }
        }

        void MoveColumnDown()
        {
            if (selectedColumnControl == null)
                return;
            var rowNo = Convert.ToInt32(selectedColumnControl.GetValue(Grid.RowProperty));
            CriteriaControl lowerCtrl = null;
            foreach (var child in ControlContainer.Children)
            {
                var ctrl = child as CriteriaControl;
                if (ctrl != null)
                {
                    var lowerrow = Convert.ToInt32(ctrl.GetValue(Grid.RowProperty));
                    if (lowerrow == rowNo + 1)
                    {
                        lowerCtrl = ctrl;
                        break;
                    }
                }
            }
            if (lowerCtrl != null)
            {
                var crit = objCriteria.selectedCriteria;
                int oldIndex = rowNo - 2;
                if (oldIndex + 1 < crit.Count)
                {
                    Grid.SetRow(lowerCtrl, rowNo);
                    var lowerCtrlCriteria = lowerCtrl.DataContext as SelectedCriteria;
                    if (lowerCtrlCriteria != null)
                        setColNameAndNumber(lowerCtrlCriteria, lowerCtrlCriteria.ColNo - 1);
                    Grid.SetRow(selectedColumnControl, rowNo + 1);
                    var selCtrlCriteria = selectedColumnControl.DataContext as SelectedCriteria;
                    if (selCtrlCriteria != null)
                        setColNameAndNumber(selCtrlCriteria, selCtrlCriteria.ColNo + 1);

                    var col = crit[oldIndex];
                    crit.RemoveAt(oldIndex);
                    crit.Insert(oldIndex + 1, col);
                }
            }
        }

        SelectedCriteria SetCriteriaControlValue(BalanceColumn objBalanceColumn, CriteriaControl crControl)
        {
            SelectedCriteria objSelectedCriteria = crControl.DataContext as SelectedCriteria;
            objSelectedCriteria.CriteriaName = objBalanceColumn._Name;
            objSelectedCriteria.Journal = objBalanceColumn._InclJournal;
            objSelectedCriteria.BudgetModel = objBalanceColumn._BudgetId;
            objSelectedCriteria.FromDate = objBalanceColumn._FromDate;
            objSelectedCriteria.ToDate = objBalanceColumn._ToDate;
            objSelectedCriteria.dimval1 = objBalanceColumn.Dims1;
            objSelectedCriteria.dimval2 = objBalanceColumn.Dims2;
            objSelectedCriteria.dimval3 = objBalanceColumn.Dims3;
            objSelectedCriteria.dimval4 = objBalanceColumn.Dims4;
            objSelectedCriteria.dimval5 = objBalanceColumn.Dims5;
            objSelectedCriteria.NotifyPropertyChanged("Dim1");
            objSelectedCriteria.NotifyPropertyChanged("Dim2");
            objSelectedCriteria.NotifyPropertyChanged("Dim3");
            objSelectedCriteria.NotifyPropertyChanged("Dim4");
            objSelectedCriteria.NotifyPropertyChanged("Dim5");
            objSelectedCriteria.balanceColumnFormat = objBalanceColumn.ColumnFormatEnum;
            objSelectedCriteria.balanceColumnMethod = objBalanceColumn.ColumnMethodEnum;
            objSelectedCriteria.ShowDebitCredit = objBalanceColumn._ShowDebitCredit;
            objSelectedCriteria.InvertSign = objBalanceColumn._InvertSign;
            objSelectedCriteria.InclPrimo = objBalanceColumn._InclPrimo;
            objSelectedCriteria.ColA = objBalanceColumn._ColA;
            objSelectedCriteria.ColB = objBalanceColumn._ColB;
            objSelectedCriteria.Account100 = objBalanceColumn._Account100;
            if (objBalanceColumn._ForCompanyId > 0)
                objSelectedCriteria.ForCompany = companies?.Where(x => x.CompanyId == objBalanceColumn._ForCompanyId).FirstOrDefault() as Company;
            else
                objSelectedCriteria.ForCompany = null;
            objSelectedCriteria.Hide = objBalanceColumn._Hide;
            setColNameAndNumber(objSelectedCriteria, objCriteria.selectedCriteria.Count + 1);
            return objSelectedCriteria;
        }

        private void frmRibbon_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "AddBalance":
                    AddBalance();
                    break;
                case "SaveBalance":
                    if (ValidateBalanceBudgetField())
                    {
                        if (string.IsNullOrEmpty(txtbalanceName.Text))
                        {
                            UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("CannotBeBlank"), Uniconta.ClientTools.Localization.lookup("Name")), Uniconta.ClientTools.Localization.lookup("Warning"));
                            return;
                        }
                        SaveBalance();
                    }
                    break;
                case "DeleteBalance":
                    DeleteBalance();
                    break;
                case "NewColumn":

                    CriteriaControl crControl = new CriteriaControl();
                    crControl.companyList = companyList;
                    crControl.API = api;
                    var numberOfChild = ControlContainer.Children.Count;
                    if (numberOfChild >= ControlContainer.RowDefinitions.Count)
                        ControlContainer.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
                    Grid.SetRow(crControl, ControlContainer.Children.Count);
                    ControlContainer.Children.Add(crControl);
                    var datacontext = crControl.DataContext as SelectedCriteria;
                    SetDefaultDate(datacontext);
                    setColNameAndNumber(datacontext, objCriteria.selectedCriteria.Count + 1);
                    objCriteria.selectedCriteria.Add(datacontext);
                    SaveNewCritera();
                    break;
                case "DeleteColumn":
                    DeleteCriteria();
                    break;
                case "MoveColumnUp":
                    MoveColumnUp();
                    break;
                case "MoveColumnDown":
                    MoveColumnDown();
                    break;
                case "BalanceReport":
                    if (ValidateBalanceBudgetField())
                    {
                        RunBalance();
                        LastGeneratedBalance = cbBalance.SelectedItem as Balance;
                    }
                    break;
                case "SetFrontPage":
                    SetFrontPage();
                    break;
                case "CopyRecord":
                    if (ValidateBalanceBudgetField())
                        CopyBalance();
                    break;
                case "CopyBalanceFromCompany":
                    var childWindow = new CwCompanyBalances();
                    childWindow.Closing += delegate
                     {
                         if (childWindow.DialogResult == true)
                         {
                             if (ValidateBalanceBudgetField())
                             {
                                 objBalance = childWindow.Balance;
                                 var newItem = new Balance();
                                 StreamingManager.Copy(objBalance, newItem);
                                 var name = newItem.Name;
                                 newItem._Name = name;
                                 if (itemsBalance == null)
                                     itemsBalance = new ObservableCollection<Balance>();
                                 itemsBalance.Add(newItem);
                                 cbBalance.SelectedItem = newItem;
                             }
                         }
                     };
                    childWindow.Show();
                    break;
                case "AddMonth":
                    var addMonthsDialog = new CWAddMonths();
                    addMonthsDialog.Closing += delegate
                    {
                        if (addMonthsDialog.DialogResult == true)
                        {
                            AddMonths(addMonthsDialog.NumberOfMonths);
                        }
                    };
                    addMonthsDialog.Show();
                    break;
                case "AddDate":
                    AddDates();
                    break;

            }
        }

        private void SetFrontPage()
        {
            balanceFrontPageReportData = GetBalanceFrontPageReportData();
            var frontPageDialog = new CWFrontPage(api, Uniconta.ClientTools.Localization.lookup("FrontPage"), objBalance?._FrontPage, objBalance?._FrontPage2, objBalance?._ReportFrontPage, balanceFrontPageReportData);
            frontPageDialog.Closing += delegate
            {
                if (frontPageDialog.DialogResult == true)
                {
                    objBalance._FrontPage = frontPageDialog.FrontPageText;
                    objBalance._FrontPage2 = frontPageDialog.FrontPageText2;
                    objBalance._ReportFrontPage = frontPageDialog.FrontPageReport;
                }
            };
            frontPageDialog.Show();
        }

        private BalanceFrontPageReportDataClient GetBalanceFrontPageReportData()
        {
            var balanceFrontPageReportData = new BalanceFrontPageReportDataClient();
            balanceFrontPageReportData.FromAccount = cbFromAccount.Text;
            balanceFrontPageReportData.ToAccount = cbToAccount.Text;
            balanceFrontPageReportData.SkipEmptyAccount = (bool)chk0Account.IsChecked.GetValueOrDefault();
            balanceFrontPageReportData.AccountType = Convert.ToString(cmbAccountType.SelectedItem);
            balanceFrontPageReportData.SumAccount = Convert.ToString(cmbSumAccount.SelectedItem);
            balanceFrontPageReportData.UseExternalName = (bool)chkUseExternal.IsChecked.GetValueOrDefault();
            balanceFrontPageReportData.Template = cbTemplate.Text;
            var criteria = objCriteria.selectedCriteria.FirstOrDefault();
            balanceFrontPageReportData.BalanceMethod = criteria.BalanceMethod;
            balanceFrontPageReportData.FromDate = criteria.FromDate;
            balanceFrontPageReportData.ToDate = criteria.ToDate;
            balanceFrontPageReportData.IsInitialized = true;
            return balanceFrontPageReportData;
        }
        void RunBalance()
        {
            if (balanceFrontPageReportData == null || !balanceFrontPageReportData.IsInitialized)
                balanceFrontPageReportData = GetBalanceFrontPageReportData();

            balanceFrontPageReportData.IsInitialized = false;

            if (objBalance == null || objBalance.CompanyId != api.CompanyId /* For balance copy from other company */)
                SaveBalance();
            else
                update();
            if (objBalance == null)
                return;

            objBalance.FrontPageData = balanceFrontPageReportData;
            objCriteria.dim1details = (bool)chkdim1.IsChecked.GetValueOrDefault();
            objCriteria.dim2details = (bool)chkdim2.IsChecked.GetValueOrDefault();
            objCriteria.dim3details = (bool)chkdim3.IsChecked.GetValueOrDefault();
            objCriteria.dim4details = (bool)chkdim4.IsChecked.GetValueOrDefault();
            objCriteria.dim5details = (bool)chkdim5.IsChecked.GetValueOrDefault();
            objCriteria.ShowDimName = (bool)chkShowDimName.IsChecked.GetValueOrDefault();
            objCriteria.FromAccount = cbFromAccount.Text;
            objCriteria.ToAccount = cbToAccount.Text;
            objCriteria.Skip0Account = (bool)chk0Account.IsChecked.GetValueOrDefault();
            objCriteria.ShowType = cmbAccountType.SelectedIndex;

            var i = cmbSumAccount.SelectedIndex;
            objCriteria.SkipSumAccount = (i == 1);
            objCriteria.OnlySumAccounts = (i == 2);
            objCriteria.UseExternal = chkUseExternal.IsChecked.GetValueOrDefault();
            objCriteria.SortExtern = chkSortExtern.IsChecked.GetValueOrDefault();
            objCriteria.Template = cbTemplate.Text;
            objCriteria.ObjBalance = objBalance;
            if (cbPrintOrientation.SelectedItem != null)
                objCriteria.PrintOrientation = Convert.ToString(cbPrintOrientation.SelectedItem);
            else
                objCriteria.PrintOrientation = Uniconta.ClientTools.Localization.lookup("Landscape");

            AddDockItem(TabControls.BalanceReport, new object[2] { master as GLClosingSheet, objCriteria }, 
                objBalance != null ? objBalance.Name : Uniconta.ClientTools.Localization.lookup("ReportCriteria"), null, true);
        }

        void AddBalance()
        {
            objBalance = null;
            ClearcolumnList();
            ClearGrid();
            var Crit = objCriteria.selectedCriteria[0];
            Crit._ShowDebitCredit = Crit._InclPrimo = true;
            Crit.NotifyPropertyChanged("InclPrimo");
            Crit.balcolFormat = BalanceColumnFormat.Decimal2;
            SetDefaultDate(Crit);
            objCriteria.dim1details = objCriteria.dim2details = objCriteria.dim3details = objCriteria.dim4details = objCriteria.dim5details = false;
            chkdim1.IsChecked = chkdim2.IsChecked = chkdim3.IsChecked = chkdim4.IsChecked = chkdim5.IsChecked = false;
            defaultCriteria.SetcmbIndex();
            txtbalanceName.Text = string.Empty;
            cbBalance.SelectedIndex = -1;
        }

        void SaveNewCritera()
        {
            if (objBalance == null)
                SaveBalance();
            else
            {
                var ColNo = objCriteria.selectedCriteria.Count - 1;
                CreateCriteraColumn(objCriteria.selectedCriteria[ColNo], ColNo);
            }
        }

        void DeleteCriteria()
        {
            if (objCriteria.selectedCriteria.Count > 1)
            {
                balanceCollist.RemoveAt(balanceCollist.Count - 1);
                objCriteria.selectedCriteria.Remove(objCriteria.selectedCriteria[objCriteria.selectedCriteria.Count - 1]);
                ControlContainer.Children.RemoveAt(ControlContainer.Children.Count - 1);
            }
        }
        private void setDim()
        {
            var c = api.CompanyEntity;
            if (c == null || dim1 == null)
                return;
            dim1.Text = c._Dim1;
            dim2.Text = c._Dim2;
            dim3.Text = c._Dim3;
            dim4.Text = c._Dim4;
            dim5.Text = c._Dim5;
            var noofDimensions = c.NumberOfDimensions;
            if (noofDimensions == 0)
                pnlDim.Visibility = Visibility.Collapsed;
            if (noofDimensions < 5)
            {
                chkdim5.Visibility = dim5.Visibility = Visibility.Collapsed;
                rowdim5.Height = GridLength.Auto;
            }
            if (noofDimensions < 4)
            {
                chkdim4.Visibility = dim4.Visibility = Visibility.Collapsed;
                rowdim4.Height = GridLength.Auto;
            }
            if (noofDimensions < 3)
            {
                chkdim3.Visibility = dim3.Visibility = Visibility.Collapsed;
                rowdim3.Height = GridLength.Auto;
            }
            if (noofDimensions < 2)
            {
                chkdim2.Visibility = dim2.Visibility = Visibility.Collapsed;
                rowdim2.Height = GridLength.Auto;
            }
            if (noofDimensions < 1)
            {
                chkdim1.Visibility = dim1.Visibility = Visibility.Collapsed;
                rowdim1.Height = GridLength.Auto;
            }
        }
        public void SaveBalance()
        {
            if (objBalance == null || objBalance.RowId == 0 || objBalance.CompanyId != api.CompanyId /* For balance copy from other company */)
            {
                if (itemsBalance == null)
                    itemsBalance = new ObservableCollection<Balance>();
                if (objBalance == null)
                {
                    objBalance = new Balance();
                    itemsBalance.Add(objBalance);
                }

                setbalanceFields(objBalance);
                SetBalanceDimUsed(objBalance);
                // if we can't save it, we still generate the colums, so he can run it
                balanceCollist = null;
                var cols = new List<BalanceColumn>(objCriteria.selectedCriteria.Count);
                int i = 0;
                foreach (var crit in objCriteria.selectedCriteria)
                    cols.Add(CreateCriteraColumn(crit, i++));

                cbBalance.SelectedIndex = itemsBalance.Count - 1;
                objBalance.ColumnList = cols;

                if (!string.IsNullOrEmpty(objBalance._Name))
                {
                    objBalance.SetMaster(api.CompanyEntity);
                    api.InsertNoResponse(objBalance);
                    UpdateBalancelist(cbBalance.SelectedIndex);
                }
            }
            else
            {
                update();
                UpdateBalancelist(cbBalance.SelectedIndex);
            }
        }

        public async void CopyBalance()
        {
            ErrorCodes res = ErrorCodes.FieldCannotBeBlank;
            busyIndicator.IsBusy = true;
            Balance newItem = null;
            if (objBalance != null && objBalance.RowId != 0)
            {
                newItem = new Balance();
                StreamingManager.Copy(objBalance, newItem);

                var name = newItem.Name + Uniconta.ClientTools.Localization.lookup("Copy");
                newItem._Name = name;
                if (objBalance._Name != null)
                    res = await api.Insert(newItem);
                else
                    res = ErrorCodes.FieldCannotBeBlank;
            }
            busyIndicator.IsBusy = false;
            if (res == ErrorCodes.Succes)
            {
                if (itemsBalance == null)
                    itemsBalance = new ObservableCollection<Balance>();
                itemsBalance.Add(newItem);
                cbBalance.SelectedItem = newItem;
            }
            else
                UtilDisplay.ShowErrorCode(res);
        }

        private bool ValidateBalanceBudgetField()
        {
            var hasMissignModel = objCriteria.selectedCriteria.Any(p => p.balcolMethod == BalanceColumnMethod.FromBudget && string.IsNullOrEmpty(p.budgetModel));
            if (hasMissignModel)
            {
                UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("CannotBeBlank"), Uniconta.ClientTools.Localization.lookup("BudgetModel")),
                    Uniconta.ClientTools.Localization.lookup("Warning"), MessageBoxButton.OK);
                return false;
            }
            return true;
        }

        public void DeleteBalance()
        {
            if (objBalance != null)
            {
                if (UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("ConfirmDeleteOBJ"), string.Format("'{0}'", objBalance._Name)), Uniconta.ClientTools.Localization.lookup("Confirmation"), MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                {
                    if (objBalance.CompanyId == api.CompanyId)/* In case user copied fom other company but not saved it */
                        api.DeleteNoResponse(objBalance);
                    itemsBalance.Remove(objBalance);
                    UpdateBalancelist(0);
                }
            }
        }

        private void UpdateBalancelist(int ind)
        {
            cbBalance.Items.Clear();
            cbBalance.ItemsSource = null;
            cbBalance.ItemsSource = itemsBalance;
            cbBalance.SelectedIndex = ind;
        }
        BalanceColumn CreateUpdateRow(SelectedCriteria objColCriteria, BalanceColumn row)
        {
            row._InclJournal = objColCriteria.AllJournals == true ? null : objColCriteria.journal;
            row._Name = objColCriteria.criteriaName;
            row._BudgetId = objColCriteria.budgetModel;
            row._FromDate = objColCriteria.frmdateval;
            row._ToDate = objColCriteria.todateval;
            row.ColumnFormatEnum = objColCriteria.balcolFormat;
            row.ColumnMethodEnum = objColCriteria.balcolMethod;
            row._ShowDebitCredit = objColCriteria._ShowDebitCredit;
            row._InvertSign = objColCriteria._InvertSign;
            row._InclPrimo = objColCriteria._InclPrimo;
            row._ColA = (byte)objColCriteria.colA;
            row._ColB = (byte)objColCriteria.colB;
            row.Dims1 = objColCriteria.dimval1;
            row.Dims2 = objColCriteria.dimval2;
            row.Dims3 = objColCriteria.dimval3;
            row.Dims4 = objColCriteria.dimval4;
            row.Dims5 = objColCriteria.dimval5;
            row._ForCompanyId = objColCriteria.ForCompany == null ? 0 : objColCriteria.ForCompany.CompanyId;
            row._Hide = objColCriteria._Hide;
            row._Account100 = objColCriteria.account100;
            return row;
        }

        private BalanceColumn CreateCriteraColumn(SelectedCriteria selectedCriteria, int colno)
        {
            var row = CreateUpdateRow(selectedCriteria, new BalanceColumn());
            row._ColumnNo = colno;
            if (balanceCollist == null)
                balanceCollist = new List<BalanceColumn>();
            balanceCollist.Add(row);
            return row;
        }

        Balance setbalanceFields(Balance updaterow)
        {
            updaterow._FromAccount = cbFromAccount.Text;
            updaterow._ToAccount = cbToAccount.Text;
            updaterow._Skip0Accounts = (bool)chk0Account.IsChecked.GetValueOrDefault();
            updaterow._ShowType = cmbAccountType.SelectedIndex;

            if (cmbSumAccount.SelectedIndex == 0) //Show
                updaterow._SkipSumAccounts = updaterow._OnlySumAccounts = false;
            else if (cmbSumAccount.SelectedIndex == 1) // Don't Show
            {
                updaterow._SkipSumAccounts = true;
                updaterow._OnlySumAccounts = false;
            }
            else if (cmbSumAccount.SelectedIndex == 2) // Only Show
            {
                updaterow._SkipSumAccounts = false;
                updaterow._OnlySumAccounts = true;
            }

            updaterow._UseExternal = (bool)chkUseExternal.IsChecked.GetValueOrDefault();
            updaterow._SortExtern = (bool)chkSortExtern.IsChecked.GetValueOrDefault();
            objBalance._Template = cbTemplate.Text;
            updaterow._Name = txtbalanceName.Text;
            updaterow._Landscape = (cbPrintOrientation.SelectedIndex <= 0);
            updaterow.ColumnSizeAccount = (byte)NumberConvert.ToInt(txtColumnSizeAccount.Text);
            updaterow.ColumnSizeName = (byte)NumberConvert.ToInt(txtColoumnSizeName.Text);
            updaterow.ColumnSizeDim = (byte)NumberConvert.ToInt(txtColoumnSizeDim.Text);
            updaterow.ColumnSizeAmount = (byte)NumberConvert.ToInt(txtColoumnSizeAmount.Text);
            updaterow._PrintFrontPage = (bool)chkPrintFrtPage.IsChecked.GetValueOrDefault();
            updaterow.LineSpace = (byte)NumberConvert.ToInt(txtLineSpace.Text);
            updaterow.FontSize = (byte)NumberConvert.ToInt(txtFontSize.Text);
            updaterow.LeftMargin = (byte)NumberConvert.ToInt(txtLeftMargin.Text);
            updaterow._ShowZero = (bool)chkShowZero.IsChecked.GetValueOrDefault();

            return updaterow;
        }
        void update()
        {
            Balance bal;
            var idx = cbBalance.SelectedIndex;
            if (idx < 0)
            {
                if (itemsBalance != null && itemsBalance.Count > 0)
                    bal = itemsBalance[0];
                else
                    bal = null;
            }
            else if (idx < itemsBalance.Count)
                bal = itemsBalance[idx];
            else
                bal = null;

            if (bal != null)
            {
                setbalanceFields(bal);
                SetBalanceDimUsed(bal);

                bal.ColumnList = new List<BalanceColumn>(objCriteria.selectedCriteria.Count);
                foreach (var crit in objCriteria.selectedCriteria)
                    bal.ColumnList.Add(CreateUpdateRow(crit, new BalanceColumn()));

                api.UpdateNoResponse(bal);
            }
        }

        void SetBalanceDimUsed(Balance objBalance)
        {
            objBalance.SetDimUsed(1, (bool)chkdim1.IsChecked.GetValueOrDefault());
            objBalance.SetDimUsed(2, (bool)chkdim2.IsChecked.GetValueOrDefault());
            objBalance.SetDimUsed(3, (bool)chkdim3.IsChecked.GetValueOrDefault());
            objBalance.SetDimUsed(4, (bool)chkdim4.IsChecked.GetValueOrDefault());
            objBalance.SetDimUsed(5, (bool)chkdim5.IsChecked.GetValueOrDefault());
            objBalance._InclDimName = (bool)chkShowDimName.IsChecked.GetValueOrDefault();
        }

        void GetBalanceDimUsed(Balance obBalance)
        {
            chkdim1.IsChecked = obBalance.GetDimUsed(1);
            chkdim2.IsChecked = obBalance.GetDimUsed(2);
            chkdim3.IsChecked = obBalance.GetDimUsed(3);
            chkdim4.IsChecked = obBalance.GetDimUsed(4);
            chkdim5.IsChecked = obBalance.GetDimUsed(5);
            chkShowDimName.IsChecked = obBalance._InclDimName;
        }

        private void cbBalance_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            if (cbBalance.SelectedItem != null && cbBalance.SelectedIndex > -1)
            {
                ClearcolumnList();
                ClearGrid();
                objBalance = cbBalance.SelectedItem as Balance;
                txtbalanceName.Text = objBalance.Name;
                cbFromAccount.Text = objBalance._FromAccount;
                cbToAccount.Text = objBalance._ToAccount;
                txtColumnSizeAccount.Text = Convert.ToString(objBalance.ColumnSizeAccount);
                txtColoumnSizeName.Text = Convert.ToString(objBalance.ColumnSizeName);
                txtColoumnSizeDim.Text = Convert.ToString(objBalance.ColumnSizeDim);
                txtColoumnSizeAmount.Text = Convert.ToString(objBalance.ColumnSizeAmount);
                chkPrintFrtPage.IsChecked = objBalance._PrintFrontPage;
                txtLineSpace.Text = Convert.ToString(objBalance.LineSpace);
                txtFontSize.Text = Convert.ToString(objBalance.FontSize);
                txtLeftMargin.Text = Convert.ToString(objBalance.LeftMargin);
                chk0Account.IsChecked = objBalance._Skip0Accounts;
                cmbAccountType.SelectedIndex = objBalance._ShowType;
                chkShowZero.IsChecked = objBalance._ShowZero;

                if (!objBalance._SkipSumAccounts && !objBalance._OnlySumAccounts)
                    cmbSumAccount.SelectedIndex = 0; // Show
                else if (objBalance._SkipSumAccounts && !objBalance._OnlySumAccounts)
                    cmbSumAccount.SelectedIndex = 1; // Don't Show
                else if (!objBalance._SkipSumAccounts && objBalance._OnlySumAccounts)
                    cmbSumAccount.SelectedIndex = 2; // Only Show

                chkUseExternal.IsChecked = objBalance._UseExternal;
                chkSortExtern.IsChecked = objBalance._SortExtern;
                cbTemplate.EditValue = objBalance._Template;
                objBalance._Name = txtbalanceName.Text;
                cbPrintOrientation.SelectedIndex = objBalance._Landscape ? 0 : 1;
                populateValue(objBalance);
                balanceFrontPageReportData = null;
            }
        }
        void ClearcolumnList()
        {
            int colCount = objCriteria.selectedCriteria.Count;
            while (colCount > 1)
            {
                colCount--;
                objCriteria.selectedCriteria.RemoveAt(colCount);
            }
        }
        void ClearGrid()
        {
            int ChildCount = ControlContainer.Children.Count;
            while (ChildCount > 3)
            {
                ChildCount--;
                ControlContainer.Children.RemoveAt(ChildCount);
            }
        }

        private void AddMonths(int numberOfMonths)
        {
            var Crits = objCriteria?.selectedCriteria;
            if (Crits != null)
            {
                for (int i = 0; (i < Crits.Count); i++)
                {
                    var Crit = Crits[i];
                    if (Crit.balcolMethod == BalanceColumnMethod.FromTrans || Crit.balcolMethod == BalanceColumnMethod.FromBudget || Crit.balcolMethod == BalanceColumnMethod.TransQty || Crit.balcolMethod == BalanceColumnMethod.BudgetQty)
                    {
                        Crit.FromDate = GetMonth(Crit.FromDate, numberOfMonths);
                        Crit.ToDate = GetMonth(Crit.ToDate, numberOfMonths);
                    }
                }
            }
        }

        static DateTime GetMonth(DateTime date, int months)
        {
            var lastDay = DateTime.DaysInMonth(date.Year, date.Month);
            var newDate = date.AddMonths(months);
            if (date.Day == lastDay)
            {
                var newDateLastDay = DateTime.DaysInMonth(newDate.Year, newDate.Month);
                newDate = new DateTime(newDate.Year, newDate.Month, newDateLastDay);
            }
            return newDate;
        }


        async private void AddDates()
        {
            var columnListCount = objBalance.ColumnList.Count;
            var balanceColFromToList = new List<BalanceFromToDateList>(columnListCount);
            var glBudgetCache = api.CompanyEntity.GetCache(typeof(GLBudget)) ?? await api.CompanyEntity.LoadCache(typeof(GLBudget), api);
            var glBudgetModels = new List<string>();
            if (glBudgetCache != null)
            {
                glBudgetModels.Capacity = glBudgetCache.Count;
                foreach (var rec in glBudgetCache.GetKeyStrRecords)
                    glBudgetModels.Add(rec.KeyStr);
            }

            var addToFromDate = new CWAddToFromDates(glBudgetModels, objCriteria.selectedCriteria);
            addToFromDate.Closing += delegate
            {
                if (addToFromDate.DialogResult == true)
                {
                    var balanceColDateList = addToFromDate.BalanceFromToDate;
                    var Crits = objCriteria?.selectedCriteria;
                    if (Crits != null)
                    {
                        for (int i = 0; (i < Crits.Count); i++)
                        {
                            var balColumnToDateObj = balanceColDateList.Where(p => p.ColumnIndex == i + 1).FirstOrDefault();
                            if (balColumnToDateObj == null)
                                continue;

                            var Crit = Crits[i];
                            if (balColumnToDateObj.FromDate != DateTime.MinValue)
                                Crit.FromDate = balColumnToDateObj.FromDate;
                            if (balColumnToDateObj.ToDate != DateTime.MinValue)
                                Crit.ToDate = balColumnToDateObj.ToDate;

                            Crit.CriteriaName = balColumnToDateObj.TypedName;
                            Crit.BudgetModel = balColumnToDateObj.BudgetModel;
                        }
                    }
                }
            };
            addToFromDate.Show();

        }
    }
}
