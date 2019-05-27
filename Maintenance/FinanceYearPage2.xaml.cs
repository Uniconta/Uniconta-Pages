using Uniconta.API.System;
using UnicontaClient.Models;
using UnicontaClient.Utilities;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Uniconta.ClientTools;
using Uniconta.Common.Utility;
using Uniconta.ClientTools.Controls;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class FinanceYearPage2 : FormBasePage
    {
        CompanyFinanceYearClient editrow;
        private FinanceYearGrid updateMainGrid;
        public override void OnClosePage(object[] RefreshParams)
        {
            globalEvents.OnRefresh(NameOfControl, RefreshParams);         
        }
        public override string NameOfControl { get { return TabControls.FinanceYearPage2.ToString(); } }
        public override Type TableType { get { return typeof(CompanyFinanceYearClient); } }
        public override UnicontaBaseEntity ModifiedRow { get { return editrow; } set { editrow = (CompanyFinanceYearClient)value; } }

        protected override bool IsLayoutSaveRequired()
        {
            return false;
        }
        public FinanceYearPage2(UnicontaBaseEntity sourcedata, FinanceYearGrid mainGrid)
            : base(sourcedata, true)
        {

            updateMainGrid = mainGrid;
            InitPage(api);
        }
        public FinanceYearPage2(CrudAPI crudApi, FinanceYearGrid mainGrid, string dummy)
            : base(crudApi, dummy)
        {
            updateMainGrid = mainGrid;
            InitPage(crudApi);
#if !SILVERLIGHT
            FocusManager.SetFocusedElement(txtDateFrom, txtDateFrom);
#endif
        }
        async void SetDefault(CrudAPI crudapi)
        {
            if (LoadedRow == null)
            {
                frmRibbon.DisableButtons("Delete");
                editrow =CreateNew() as CompanyFinanceYearClient;
                editrow._State = FinancePeriodeState.Open;
                var lst = await crudapi.Query(editrow, null, null);
                if (lst != null && lst.Length > 0)
                {
                    var p = lst[lst.Length - 1];
                    editrow.FromDate = p._ToDate.AddDays(1);
                    editrow.ToDate = editrow._FromDate.AddYears(1).AddDays(-1);                   
                }
            }
        }

        bool Validate()
        {
            if (LoadedRow == null)
                return true;
            if (api.session.User._Role >= (byte)Uniconta.Common.User.UserRoles.Distributor)
                return true;
            var fyear = (CompanyFinanceYearClient)LoadedRow;
            if (fyear._ToDate >= BasePage.GetSystemDefaultDate())
                return true;
            var Oldstate = fyear._State;
            if (Oldstate != FinancePeriodeState.NotOpen && editrow._State == FinancePeriodeState.NotOpen)
            {
                UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("CannotChangeFieldOBJ"), Uniconta.ClientTools.Localization.lookup("Status")), Uniconta.ClientTools.Localization.lookup("Error"));
                return false;
            }
            if (Oldstate == FinancePeriodeState.Closed && editrow._State != FinancePeriodeState.Closed)
            {
                UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("PeriodCanBeReopen"), Uniconta.ClientTools.Localization.lookup("Error"));
                return false;
            }
            return true;
        }

        void InitPage(CrudAPI crudapi)
        {
            InitializeComponent();
            BusyIndicator = busyIndicator;
            SetDefault(crudapi);
            cmbPeriodState.ItemsSource = AppEnums.GLFinancePeriodeState.Values;
            layoutItems.DataContext = editrow;
            layoutControl = layoutItems;
            lookupNumberserie.api = crudapi;

            frmRibbon.OnItemClicked += frmRibbon_OnItemClicked;
        }

        private async void frmRibbon_OnItemClicked(string ActionType)
        {
            if (ActionType == "Save")
            {
                if (!ValidateState())
                    return;
                if (otherAccountOpen && accountYears != null && accountYears.Count > 0 && Validate())
                {
                    CompanyFinanceYearClient editrowUpdate = null;

                    foreach (var year in accountYears)
                    {
                        if (year._FromDate == editrow._FromDate && year._ToDate == editrow._ToDate)
                            editrowUpdate = year;
                        else if (year._Current)
                            year._Current = false;
                    }

                    if (editrowUpdate != null)
                    {
                        accountYears[accountYears.IndexOf(editrowUpdate)] = editrow;
                        accountYears.Sort(updateMainGrid.GridSorting.Compare);
                    }
                    else
                    {
                        var err3 = await api.Insert(editrow);
                        if (err3 != ErrorCodes.Succes)
                            UnicontaMessageBox.Show(err3.ToString(), Uniconta.ClientTools.Localization.lookup("Error"));
                        else
                        {
                            accountYears.Add(editrow);
                            accountYears.Sort(updateMainGrid.GridSorting.Compare);
                        }
                    }

                    var err = await api.Update(accountYears);
                    if (err != ErrorCodes.Succes)
                        UnicontaMessageBox.Show(err.ToString(), Uniconta.ClientTools.Localization.lookup("Error"));
                    else
                    {
                        if (updateMainGrid != null)
                        {
                            updateMainGrid.ItemsSource = accountYears;
                            var err2 = await updateMainGrid.SaveData();
                            if (err2 == ErrorCodes.Succes)
                            {
                                updateMainGrid.Visibility = Visibility.Visible;
                                CloseDockItem("FinanceYearPage2", null);
                            }
                            else
                                UnicontaMessageBox.Show(err2.ToString(), Uniconta.ClientTools.Localization.lookup("Error"));
                        }
                    }
                }
                else if (Validate())
                    frmRibbon_BaseActions(ActionType);
            }
            else
                frmRibbon_BaseActions(ActionType);
        }

        private bool otherAccountOpen = false;
        private List<CompanyFinanceYearClient> accountYears = null;

        private async void CheckEdit_OnChecked(object sender, RoutedEventArgs e)
        {
            if (accountYears == null || accountYears.Count <= 0)
            {
                var tempAccYear = await api.Query<CompanyFinanceYearClient>();
                accountYears = tempAccYear.ToList();
            }

            foreach (var year in accountYears)
            {
                if (year._FromDate == editrow._FromDate && year._ToDate == editrow._ToDate)
                    continue;
                else
                {
                    if (year._Current)
                        otherAccountOpen = true;
                }
            }
        }

        private void CheckEdit_OnUnchecked(object sender, RoutedEventArgs e)
        {
            if (otherAccountOpen)
                otherAccountOpen = false;
        }

        bool ValidateState()
        {
            if (cmbPeriodState.SelectedText == AppEnums.GLFinancePeriodeState.Values[3])
#if !SILVERLIGHT
                return UnicontaMessageBox.Show(string.Format("{0}. {1}", Uniconta.ClientTools.Localization.lookup("ConfirmClosing"), Uniconta.ClientTools.Localization.lookup("AreYouSureToContinue")), Uniconta.ClientTools.Localization.lookup("Message"), MessageBoxButton.YesNo) == MessageBoxResult.Yes;
#else
                return UnicontaMessageBox.Show(string.Format("{0}. {1}", Uniconta.ClientTools.Localization.lookup("ConfirmClosing"), Uniconta.ClientTools.Localization.lookup("AreYouSureToContinue")), Uniconta.ClientTools.Localization.lookup("Message"), MessageBoxButton.OKCancel) == MessageBoxResult.OK;
#endif
            else
                return true;
        }
    }
}
