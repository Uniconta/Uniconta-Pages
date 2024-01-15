using UnicontaClient.Utilities;
using Uniconta.API.Service;
using Uniconta.ClientTools.Page;
using Uniconta.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Uniconta.ClientTools;
using System.Windows;
using Uniconta.Common.User;
using Uniconta.API.System;
using UnicontaClient.Pages;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Util;
using DevExpress.Xpf.Grid;
using DevExpress.DashboardWpf.Internal;
using DevExpress.Mvvm.Native;
using Uniconta.Common.Utility;
using Uniconta.Common;
using Uniconta.API.GeneralLedger;

namespace UnicontaClient.Controls
{
    internal class FinancialYearLocal : CompanyFinanceYearClient
    {
        public string FromTo { get { return FromDate.ToShortDateString() + " - " + ToDate.ToShortDateString(); } }
    }

    public partial class CWExport2BasisRegnskab : ChildWindow
    {
        SQLCache Accounts, StdAccounts;
        CrudAPI api;
        public CWExport2BasisRegnskab(CrudAPI api)
        {
            this.DataContext = this;
            this.api = api;
            InitializeComponent();
            this.Title = Uniconta.ClientTools.Localization.lookup("AccountingYear");
            this.Loaded += CW_Loaded;
            LoadFinancialYear(api);
        }

        async void LoadFinancialYear(CrudAPI api)
        {
            Accounts = api.GetCache(typeof(GLAccount)) ?? await api.LoadCache(typeof(GLAccount));
            StdAccounts = api.GetCache(typeof(GLStandardCharOfAccount)) ?? await api.LoadCache(typeof(GLStandardCharOfAccount));
            var accountingYears = await api.Query<FinancialYearLocal>();
            cmbFinancialYear.ItemsSource = accountingYears;
            cmbFinancialYear.SelectedItem = accountingYears.Where(y => y.Current == true).FirstOrDefault();
        }
        void CW_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => { cmbFinancialYear.Focus(); }));
        }
        private void ChildWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                SetDialogResult(false);
            }
            else
                if (e.Key == Key.Enter)
                {
                    if (OKButton.IsFocused)
                        OKButton_Click(null, null);
                    else if (CancelButton.IsFocused)
                        SetDialogResult(false);
                }
        }
        private async void OKButton_Click(object sender, RoutedEventArgs e)
        {
            var financeYear = cmbFinancialYear.SelectedItem as CompanyFinanceYearClient;
            if (financeYear != null)
            {
                bool FoundWith = false;
                string FoundWithOut = null;
                var financialBalances = await new ReportAPI(api).GenerateTotals(financeYear._FromDate, financeYear._ToDate);
                int i;
                FinancialBalance rec;
                for (i = 0; (i < financialBalances.Length); i++)
                {
                    rec = financialBalances[i];
                    var acc = (GLAccount)Accounts.Get(rec.AccountRowId);
                    if (acc._StandardAccount != null)
                    {
                        rec._AccountNumber = acc._StandardAccount;
                        rec._Name = StdAccounts.Get(acc._StandardAccount).KeyName;
                        FoundWith = true;
                    }
                    else
                        FoundWithOut = acc._Account;
                }
                if (FoundWith && FoundWithOut != null)
                {
                    MessageBox.Show(string.Format("Konto '{0}' mangler standardkonto", FoundWithOut));
                    return;
                }
                if (FoundWith)
                {
                    var dict = new Dictionary<string, FinancialBalance>(financialBalances.Length);
                    for (i = 0; (i < financialBalances.Length); i++)
                    {
                        FinancialBalance rec2;
                        rec = financialBalances[i];
                        if (dict.TryGetValue(rec._AccountNumber, out rec2))
                        {
                            rec2._Debit += rec._Debit;
                            rec2._Credit += rec._Credit;
                            rec._Debit = 0;
                            rec._Credit = 0;
                        }
                        else if (rec._Debit != rec._Credit)
                            dict.Add(rec._AccountNumber, rec);
                    }
                }
                var lst = new List<FinancialBalance>(financialBalances.Length);
                for (i = 0; (i < financialBalances.Length); i++)
                {
                    rec = financialBalances[i];
                    
                    if (rec._Debit != rec._Credit) // it is not 0
                        lst.Add(rec);
                }
                lst.Sort(SQLCache.KeyStrSorter);
                CSVHelper.PrintGridCSV(lst, new String[] { "KeyStr", "KeyName", "Amount" }, new String[] { "KONTONUMMER", "KONTONAVN", "VAERDI" }, new string[] { null, null, "N0" });
            }
            SetDialogResult(true);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }
    }
}

