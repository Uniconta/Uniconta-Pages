using DevExpress.XtraSpreadsheet.Model;
using System;
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
using Uniconta.API.Inventory;
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.Util;
using Uniconta.Common;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class CWJoinTwoEmployees : ChildWindow
    {
        SQLCache employeeCache;
        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.Employee))]
        public string FromEmployee { get; set; }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.Employee))]
        public string ToEmployee { get; set; }

        CrudAPI api;

        public CWJoinTwoEmployees(CrudAPI crudapi)
        {
            this.api = crudapi;
            InitializeComponent();
            cmbFromEmployee.api = cmbToEmployee.api = api;
            this.DataContext = this;
            LoadCacheInBackGround();
            this.Title = string.Format(Uniconta.ClientTools.Localization.lookup("JoinTwoOBJ"), Uniconta.ClientTools.Localization.lookup("Employees"));
            this.Loaded += CW_Loaded;
        }

        protected async void LoadCacheInBackGround()
        {
            var api = this.api;
            var Comp = api.CompanyEntity;
            employeeCache = Comp.GetCache(typeof(Uniconta.DataModel.Employee));
            if (employeeCache == null)
                employeeCache = await Comp.LoadCache(typeof(Uniconta.DataModel.Employee), api);

            cmbFromEmployee.ItemsSource = cmbToEmployee.ItemsSource = employeeCache;
        }
        void CW_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => { cmbFromEmployee.Focus(); }));
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
                if (CancelButton.IsFocused)
                {
                    SetDialogResult(false);
                    return;
                }
                OKButton_Click(null, null);
            }
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            var fromEmployee = cmbFromEmployee.SelectedItem as Uniconta.DataModel.Employee;
            var toEmployee = cmbToEmployee.SelectedItem as Uniconta.DataModel.Employee;

            if (fromEmployee == null)
            {
                UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("MandatoryField"), (Uniconta.ClientTools.Localization.lookup("FromEmployee"))), Uniconta.ClientTools.Localization.lookup("Warning"));
                return;
            }
            if (toEmployee == null)
            {
                UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("MandatoryField"), (Uniconta.ClientTools.Localization.lookup("ToEmployee"))), Uniconta.ClientTools.Localization.lookup("Warning"));
                return;
            }

            CWConfirmationBox confirmationDialog = new CWConfirmationBox(Uniconta.ClientTools.Localization.lookup("AreYouSureToContinue"), Uniconta.ClientTools.Localization.lookup("Confirmation"), false);
            confirmationDialog.Closing += delegate
            {
                if (confirmationDialog.ConfirmationResult == CWConfirmationBox.ConfirmationResultEnum.Yes)
                    CallJoinTwoEmployees(fromEmployee, toEmployee);
            };
            confirmationDialog.Show();
        }

        async void CallJoinTwoEmployees(Uniconta.DataModel.Employee fromEmployee, Uniconta.DataModel.Employee toEmployee)
        {
            var compApi = new CompanyAPI(api);
            ErrorCodes res = await compApi.JoinTwoEmployees(fromEmployee, toEmployee);
            if (res == ErrorCodes.Succes)
                SetDialogResult(true);
            UtilDisplay.ShowErrorCode(res);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }
    }
}
