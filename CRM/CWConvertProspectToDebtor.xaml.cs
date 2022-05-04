using UnicontaClient.Pages;
using UnicontaClient.Utilities;
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
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Util;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class CWConvertProspectToDebtor : ChildWindow
    {
        CrudAPI crudApi;
        CrmProspectClient crmProspect;
        public string Group { get; set; }
        public string AccountNumber { get; set; }
        public DebtorClient DebtorClient;

        public CWConvertProspectToDebtor(CrudAPI api, CrmProspectClient crmProspectClient)
        {
            crudApi = api;
            crmProspect = crmProspectClient;
            this.DataContext = this;
            InitializeComponent();
            grouplookupeditor.api = crudApi;
            SetDebtorGroupSource();
#if !SILVERLIGHT
            this.Title = Uniconta.ClientTools.Localization.lookup("ConvertToDebtor");
#endif
#if SILVERLIGHT
            Utility.SetThemeBehaviorOnChildWindow(this);
#endif
            this.Loaded += CW_Loaded;
        }

        async private void SetDebtorGroupSource()
        {
            var Cache = crudApi.GetCache(typeof(Uniconta.DataModel.DebtorGroup)) ?? await crudApi.LoadCache(typeof(Uniconta.DataModel.DebtorGroup));
            grouplookupeditor.ItemsSource = Cache;
        }

        private void CW_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => 
            {
                if (string.IsNullOrWhiteSpace(txtAccountNumber.Text))
                    txtAccountNumber.Focus();
                else
                    OKButton.Focus();
            }));
        }

        private void ChildWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                SetDialogResult(false);
            }
            else if (e.Key == Key.Enter)
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
            var debtor = crudApi.CompanyEntity.CreateUserType<DebtorClient>();
            debtor.Account = AccountNumber;
            debtor.Group = Group;

            debtor._Name = crmProspect._Name;
            debtor._Address1 = crmProspect._Address1;
            debtor._Address2 = crmProspect._Address2;
            debtor._Address3 = crmProspect._Address3;
            debtor._City = crmProspect._City;
            debtor._ZipCode = crmProspect._ZipCode;
            debtor._Country = crmProspect._Country;
            debtor._LegalIdent = crmProspect._LegalIdent;
            debtor._Currency = crmProspect._Currency;
            debtor._Phone = crmProspect._Phone;
            debtor._VatZone = crmProspect._VatZone;
            debtor._PriceList = crmProspect._PriceList;
            debtor._PriceGroup = crmProspect._PriceGroup;
            debtor._ItemNameGroup = crmProspect._ItemNameGroup;
            debtor._LayoutGroup = crmProspect._LayoutGroup;
            debtor._Blocked = crmProspect._Blocked;
            debtor._Language = crmProspect._Language;
            debtor._ContactEmail = crmProspect._ContactEmail;
            debtor._InvoiceEmail = crmProspect._InvoiceEmail;
            debtor._ContactPerson = crmProspect._ContactPerson;
            debtor._Employee = crmProspect._Employee;
            debtor._EAN = crmProspect._EAN;
            debtor._Interests = crmProspect._Interests;
            debtor._Products = crmProspect._Products;

            DebtorClient = debtor;
            SetDialogResult(true);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }
    }
}
