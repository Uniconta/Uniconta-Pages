using Uniconta.API.System;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
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
using Uniconta.ClientTools.Controls;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{

    public partial class CWAccountingPeriod : ChildWindow
    {
        CompanyFinancePeriodClient[] accountPeriods;
        UnicontaBaseEntity ModifiedRow;
        UnicontaBaseEntity LoadedRow;
        CrudAPI API;
        public CWAccountingPeriod(CompanyFinanceYearClient accountYears, CompanyFinancePeriodClient[] accountPeriodClient, CrudAPI api)
        {
            this.DataContext = this;
            InitializeComponent();
            this.Title = Uniconta.ClientTools.Localization.lookup("AccountingPeriod");
            API = api;
            ModifiedRow = accountYears;
            LoadedRow = StreamingManager.Clone((UnicontaBaseEntity)ModifiedRow);
            accountPeriods = accountPeriodClient;
            dgAccountPeriod.ItemsSource = accountPeriods;
            this.Loaded += CW_Loaded;
        }
        void CW_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => { btnSave.Focus(); }));
        }
        private void ChildWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                SetDialogResult(false);
            }
            else
                if (e.Key == Key.Enter)
                {
                    if (btnSave.IsFocused)
                        btnSave_Click(null, null);
                    else if (btnCancel.IsFocused)
                        SetDialogResult(false);
                }
        }
        private async void btnSave_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < accountPeriods.Length; i++)
                ((CompanyFinanceYearClient)ModifiedRow).PeriodStateSet(i, accountPeriods[i]._State);
            ErrorCodes res = await API.Update(LoadedRow, ModifiedRow);
            if (res == ErrorCodes.Succes)
                SetDialogResult(true);
            else
                UtilDisplay.ShowErrorCode(res);
        }
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }

        private void PART_Editor_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {

            var cmb = sender as ComboBoxEditor;
            var value = e.NewValue as string;
            if (value == AppEnums.GLFinancePeriodeState.Values[3])
                if (UnicontaMessageBox.Show(string.Format("{0}. {1}", Uniconta.ClientTools.Localization.lookup("ConfirmClosing"), Uniconta.ClientTools.Localization.lookup("AreYouSureToContinue")), Uniconta.ClientTools.Localization.lookup("Message"), MessageBoxButton.YesNo) == MessageBoxResult.No)
                {
                    cmb.SelectedItem = e.OldValue;
                    e.Handled = true;
                }

        }
    }
}

