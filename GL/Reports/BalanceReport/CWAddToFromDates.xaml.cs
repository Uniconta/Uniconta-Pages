using System;
using System.Collections.Generic;
using System.Globalization;
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
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.Common;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    /// <summary>
    /// Interaction logic for CWAddToFromDates.xaml
    /// </summary>
    public partial class CWAddToFromDates : ChildWindow
    {
        private List<string> glBudgetSource;
        private List<SelectedCriteria> _selectedCriteriaList;
        public BalanceFromToDateList[] BalanceFromToDate { get; set; }
        public CWAddToFromDates(List<string> glBudgetsource, List<SelectedCriteria> objCriteria)
        {
            InitializeComponent();
            this.DataContext = this;
            _selectedCriteriaList = objCriteria;
            glBudgetSource = glBudgetsource;
            Title = string.Format(Uniconta.ClientTools.Localization.lookup("AddOBJ"), Uniconta.ClientTools.Localization.lookup("Date"));
            LoadBalanceFromDateToList();
        }

        private void LoadBalanceFromDateToList()
        {
            if (_selectedCriteriaList == null || _selectedCriteriaList.Count == 0)
                return;

            List<BalanceFromToDateList> balFromToDateList = new List<BalanceFromToDateList>(_selectedCriteriaList.Count);
            foreach (var criteria in _selectedCriteriaList)
            {

                if (criteria.balanceColumnMethod == BalanceColumnMethod.FromTrans || criteria.balanceColumnMethod == BalanceColumnMethod.FromBudget ||
                    criteria.balanceColumnMethod == BalanceColumnMethod.TransQty || criteria.balanceColumnMethod == BalanceColumnMethod.BudgetQty)
                    balFromToDateList.Add(new BalanceFromToDateList(criteria, glBudgetSource));
            }
            BalanceFromToDate = balFromToDateList.ToArray();
        }

        private void ChildWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                SetDialogResult(false);
            }
        }
        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(true);
        }
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }
    }

    public class BalanceFromToDateList
    {
        public int ColumnIndex { get; set; }
        public string ColumnName { get; set; }
        public string TypedName { get; set; }
        public string BudgetModel { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public bool IsBudget { get; set; }
        public string[] GLBudgetModels { get; set; }
        public BalanceFromToDateList(SelectedCriteria objSelectedCriteria,List<string> glBudgetList)
        {
            ColumnIndex = objSelectedCriteria.ColNo;
            ColumnName = objSelectedCriteria.ColNameNumber;
            TypedName = objSelectedCriteria.CriteriaName;
            BudgetModel = objSelectedCriteria.BudgetModel;
            FromDate = objSelectedCriteria.FromDate;
            ToDate = objSelectedCriteria.ToDate;
            if (objSelectedCriteria.balanceColumnMethod == BalanceColumnMethod.BudgetQty ||
                objSelectedCriteria.balanceColumnMethod == BalanceColumnMethod.FromBudget)
            {
                GLBudgetModels = glBudgetList.ToArray();
                IsBudget = true;
            }
            else
            {
                IsBudget = false;
                GLBudgetModels = null;
            }
        }
    }
}
