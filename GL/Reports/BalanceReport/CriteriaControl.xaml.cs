using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.Common;
using Uniconta.DataModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;

using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Uniconta.ClientTools.Controls;
using System.Windows;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using UnicontaClient.Controls;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class SelectedCriteria : INotifyPropertyChanged
    {
        public SelectedCriteria()
        {
            _ShowDebitCredit = _InclPrimo = true;
        }

        internal string journal;
        internal string budgetModel;
        internal string account100;
        public DateTime frmdateval;
        public DateTime todateval;
        internal BalanceColumnFormat balcolFormat;
        internal BalanceColumnMethod balcolMethod;
        internal string colNameNumber;
        public BalanceColumnFormat balanceColumnFormat { get { return balcolFormat; } set { balcolFormat = value; NotifyPropertyChanged("BalanceFormat"); } }
        public BalanceColumnMethod balanceColumnMethod { get { return balcolMethod; } set { balcolMethod = value; NotifyPropertyChanged("BalanceMethod"); } }
        internal int colA, colB;
        internal bool _ShowDebitCredit, _InvertSign, _Hide, _InclPrimo;
        internal string criteriaName, fromaccount, toaccount;
        internal List<int> dimval1, dimval2, dimval3, dimval4, dimval5;
        public DateTime FromDate { get { return frmdateval; } set { frmdateval = value; NotifyPropertyChanged("FromDate"); } }
        public DateTime ToDate { get { return todateval; } set { todateval = value; NotifyPropertyChanged("ToDate"); } }
        static List<object> CheckDim(List<int> dimVal)
        {
            return dimVal?.Cast<object>().ToList();
        }
        static List<int> SetDim(List<object> dimVal)
        {
           return dimVal?.Cast<int>().ToList();
        }
        public List<object> Dim1 { get { return CheckDim(dimval1); } set { dimval1 = SetDim(value); NotifyPropertyChanged("Dim1"); } }
        public List<object> Dim2 { get { return CheckDim(dimval2); } set { dimval2 = SetDim(value); NotifyPropertyChanged("Dim2"); } }
        public List<object> Dim3 { get { return CheckDim(dimval3); } set { dimval3 = SetDim(value); NotifyPropertyChanged("Dim3"); } }
        public List<object> Dim4 { get { return CheckDim(dimval4); } set { dimval4 = SetDim(value); NotifyPropertyChanged("Dim4"); } }
        public List<object> Dim5 { get { return CheckDim(dimval5); } set { dimval5 = SetDim(value); NotifyPropertyChanged("Dim5"); } }
        public bool ShowDebitCredit { get { return _ShowDebitCredit; } set { _ShowDebitCredit = value; NotifyPropertyChanged("ShowDebitCredit"); } }
        public bool InvertSign { get { return _InvertSign; } set { _InvertSign = value; NotifyPropertyChanged("InvertSign"); } }
        public bool InclPrimo { get { return _InclPrimo; } set { _InclPrimo = value; NotifyPropertyChanged("InclPrimo"); } }
        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.GLDailyJournal))]
        public string Journal { get { return journal; } set { journal = value; SetAllJournal(value); NotifyPropertyChanged("Journal"); } }
        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.GLBudget))]
        public string BudgetModel { get { return budgetModel; } set { budgetModel = value; NotifyPropertyChanged("BudgetModel"); } }
        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.GLAccount))]
        public string Account100 { get { return account100; } set { account100 = value; NotifyPropertyChanged("Account100"); } }
        public string CriteriaName { get { return criteriaName; } set { criteriaName = value; NotifyPropertyChanged("CriteriaName"); } }
        public string BalanceFormat { get { return AppEnums.BalanceFormat.ToString((int)balanceColumnFormat); } set { balanceColumnFormat = (BalanceColumnFormat)AppEnums.BalanceFormat.IndexOf(value); } }
        public string BalanceMethod { get { return AppEnums.BalanceMethod.ToString((int)balanceColumnMethod); } set { balanceColumnMethod = (BalanceColumnMethod)AppEnums.BalanceMethod.IndexOf(value); } }
        public int ColA { get { return colA; } set { colA = value; NotifyPropertyChanged("ColA"); } }
        public int ColB { get { return colB; } set { colB = value; NotifyPropertyChanged("ColB"); } }
        public int ColNo;
        public string ColNameNumber { get { return colNameNumber; } set { colNameNumber = value; NotifyPropertyChanged("ColNameNumber"); } }
        public string FromAccount { get { return fromaccount; } set { fromaccount = value; NotifyPropertyChanged("FromAccount"); } }
        public string ToAccount { get { return toaccount; } set { toaccount = value; NotifyPropertyChanged("ToAccount"); } }
        public string BudgetId { get; set; }
        public bool AllJournals { get; set; }
        internal Company forCompany;
        public Company ForCompany { get { return forCompany; } set { forCompany = value; NotifyPropertyChanged("ForCompany"); } }
        public bool Hide { get { return _Hide; } set { _Hide = value; NotifyPropertyChanged("Hide"); } }
        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        private void SetAllJournal(string journal)
        {
            AllJournals = (journal != null) && journal.IndexOf('*') >= 0;
        }
    }
    public partial class CriteriaControl : UserControl
    {
        CrudAPI api;
        public CrudAPI API
        {
            get { return api; }
            set
            {
                api = value;
                SetSources();
            }
        }
        public CriteriaControl()
        {
            this.DataContext = this;
            InitializeComponent();
          
            SelectedCriteria ob = new SelectedCriteria();
            this.DataContext = ob;
            //  cmbBudgetModel.SelectedItem = null;
            cmbBudgetModel.IsEnabled = false;
            cmbAccount100.Visibility = Visibility.Collapsed;
            cbvalue.ItemsSource = AppEnums.BalanceMethod.Values;
            cbformat.ItemsSource = AppEnums.BalanceFormat.Values;
            txtColA.IsEnabled = txtColB.IsEnabled = false;
            this.MouseLeftButtonDown += CriteriaControl_MouseLeftButtonDown;
        }

        private void CriteriaControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Highlight();
        }

        public void Highlight()
        {
            this.CriteriaBorder.BorderBrush = Application.Current.Resources["HighlightBorderColor"] as SolidColorBrush;
        }

        public void Unhighlight()
        {
            this.CriteriaBorder.BorderBrush = Application.Current.Resources["LightBoxBorderColor"] as SolidColorBrush;
        }

        async void SetSources()
        {
            var api = this.api;
            var Comp = api.CompanyEntity;
            cmbBudgetModel.api = api;
            cmbAccount100.api = api;
            var noofDimensions = Comp.NumberOfDimensions;
            dim1.Text = Comp._Dim1;
            dim2.Text = Comp._Dim2;
            dim3.Text = Comp._Dim3;
            dim4.Text = Comp._Dim4;
            dim5.Text = Comp._Dim5;
            company.Text = Uniconta.ClientTools.Localization.lookup("Company");

            List<Company> compList = new List<Company>();
            var companies = CWDefaultCompany.loadedCompanies;
            if (companies != null)
            {
                compList.Capacity = companies.Length + 1;
                compList.Add(new Company() { _Name = "" });
                compList.AddRange(companies);
                compList.Sort(SQLCache.KeyStrSorter);
            }
            cbCompany.ItemsSource = compList;

            var journalSource = new List<string>();
            var cache = Comp.GetCache(typeof(Uniconta.DataModel.GLDailyJournal)) ?? await api.LoadCache(typeof(Uniconta.DataModel.GLDailyJournal));
            if (cache != null)
            {
                journalSource.Capacity = cache.Count;
                foreach (var rec in cache.GetKeyStrRecords)
                    journalSource.Add(rec.KeyStr);
            }

            cmbJournal.ItemsSource = journalSource;

            var actSource = new List<string>();
            cache = Comp.GetCache(typeof(Uniconta.DataModel.GLAccount)) ?? await api.LoadCache(typeof(Uniconta.DataModel.GLAccount));
            if (cache != null)
            {
                actSource.Capacity = cache.Count;
                foreach (var rec in cache.GetKeyStrRecords)
                    actSource.Add(rec.KeyStr);
            }

            cmbAccount100.ItemsSource = actSource;

            if (noofDimensions < 5)
            {
                cbdim5.Visibility = dim5.Visibility = Visibility.Collapsed;
                rowdim5.Height = GridLength.Auto;
            }
            else
                await TransactionReport.SetDimValues(typeof(GLDimType5), cbdim5, api);

            if (noofDimensions < 4)
            {
                cbdim4.Visibility = dim4.Visibility = Visibility.Collapsed;
                rowdim4.Height = GridLength.Auto;
            }
            else
                await TransactionReport.SetDimValues(typeof(GLDimType4), cbdim4, api);

            if (noofDimensions < 3)
            {
                cbdim3.Visibility = dim3.Visibility = Visibility.Collapsed;
                rowdim3.Height = GridLength.Auto;
            }
            else
                await TransactionReport.SetDimValues(typeof(GLDimType3), cbdim3, api);

            if (noofDimensions < 2)
            {
                cbdim2.Visibility = dim2.Visibility = Visibility.Collapsed;
                rowdim2.Height = GridLength.Auto;
            }
            else
                await TransactionReport.SetDimValues(typeof(GLDimType2), cbdim2, api);

            if (noofDimensions < 1)
            {
                cbdim1.Visibility = dim1.Visibility = Visibility.Collapsed;
                rowdim1.Height = GridLength.Auto;
            }
            else
                await TransactionReport.SetDimValues(typeof(GLDimType1), cbdim1, api);
        }

        public void SetcmbIndex()
        {
            cbdim1.SelectedIndex = cbdim2.SelectedIndex = cbdim3.SelectedIndex = cbdim4.SelectedIndex = cbdim5.SelectedIndex = 0;
        }

        private void cbvalue_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            SelectedCriteria objCriteria = null;
            if (cbvalue.SelectedItem != null)
                objCriteria = this.DataContext as SelectedCriteria;
            var SelectedVal = cbvalue.SelectedIndex;
            if (SelectedVal > (int)BalanceColumnMethod.FromBudget && SelectedVal < (int)BalanceColumnMethod.OnlyJournals)
            {
                dateFrom.IsEnabled = dateTo.IsEnabled = false;
                txtColA.IsEnabled = txtColB.IsEnabled = true;
            }
            else
            {
                dateFrom.IsEnabled = dateTo.IsEnabled = true;
                if (objCriteria == null)
                    return;
                objCriteria.ColA = objCriteria.ColB = 0;
                txtColA.IsEnabled = txtColB.IsEnabled = false;
            }

            if (SelectedVal == (int)BalanceColumnMethod.FromTrans || SelectedVal == (int)BalanceColumnMethod.OnlyJournals || SelectedVal == (int)BalanceColumnMethod.TransQty)
            {
                cmbJournal.IsEnabled = true;
            }
            else
            {
                cmbJournal.SelectedItem = null;
                cmbJournal.IsEnabled = false;
            }
            if (SelectedVal == (int)BalanceColumnMethod.FromBudget || SelectedVal == (int)BalanceColumnMethod.BudgetQty)
            {
                cmbBudgetModel.IsEnabled = true;
            }
            else
            {
                cmbBudgetModel.SelectedItem = null;
                cmbBudgetModel.IsEnabled = false;
            }
            if (SelectedVal == (int)BalanceColumnMethod.Account100)
            {
                cmbBudgetModel.Visibility = Visibility.Collapsed;
                cmbAccount100.Visibility = Visibility.Visible;
                txtBudgetModel.Text = Uniconta.ClientTools.Localization.lookup("100Account");
                txtColA.IsEnabled = true;
                txtColB.IsEnabled = false;
            }
            else
            {
                cmbAccount100.Visibility = Visibility.Collapsed;
                cmbBudgetModel.Visibility = Visibility.Visible;
                txtBudgetModel.Text = Uniconta.ClientTools.Localization.lookup("BudgetModel");
            }
        }

        private void dateTo_LostFocus(object sender, RoutedEventArgs e)
        {
            setDateTime(sender, "ToDate");
        }

        private void dateFrom_LostFocus(object sender, RoutedEventArgs e)
        {
            setDateTime(sender, "FromDate");
        }

        private void setDateTime(object sender, string valueFor)
        {
            DateEditor de = (DateEditor)sender;
            if (de.EditValue == null || de.DateTime == DateTime.MinValue)
            {
                SelectedCriteria ob = (SelectedCriteria)this.DataContext;
                if (valueFor == "FromDate")
                {
                    ob.frmdateval = BasePage.GetSystemDefaultDate();
                    de.EditValue = null;
                }
                if (valueFor == "ToDate")
                {
                    ob.todateval = BasePage.GetSystemDefaultDate();
                    de.EditValue = null;
                }
            }
        }
    }
}
