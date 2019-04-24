using Uniconta.API.System;
using UnicontaClient.Models;
using UnicontaClient.Utilities;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
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

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class DebtorInvoicenumberseries : FormBasePage
    {
        CompanySettingsClient editrow;
        public override void OnClosePage(object[] RefreshParams)
        {
            globalEvents.OnRefresh(NameOfControl, RefreshParams);
        }
        public override string NameOfControl { get { return TabControls.DebtorInvoicenumberseries.ToString(); } }
        public override Type TableType { get { return typeof(CompanySettingsClient); } }
        public override UnicontaBaseEntity ModifiedRow { get { return editrow; } set { editrow = (CompanySettingsClient)value; } }
        public DebtorInvoicenumberseries(UnicontaBaseEntity sourceData)
            : base(sourceData, true)
        {
            InitializeComponent();
            cmbDebtorVoucherSerie.api = api;
            layoutControl = layoutItems;
           
            layoutItems.DataContext = editrow;
            frmRibbon.OnItemClicked += frmRibbon_OnItemClicked;
        }

        public static CompanyClient getCompany(Company company)
        {
            CompanyClient companyClient = new CompanyClient();
            StreamingManager.Copy(company, companyClient);
            return companyClient;

        }
        private void frmRibbon_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "Save": this.saveForm();
                    var company = api.CompanyEntity;
                    break;
                default:
                    frmRibbon_BaseActions(ActionType);
                    break;
            }
        }

        private void CheckEditor_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            cmbSalesInvoice.IsEnabled = false;
        }

        private void CheckEditor_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            cmbSalesInvoice.IsEnabled = true;
        }
    }
}
