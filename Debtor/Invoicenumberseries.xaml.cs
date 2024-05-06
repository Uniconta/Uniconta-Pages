using UnicontaClient.Models;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.DataModel;
using System;
using Uniconta.ClientTools.Controls;

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
            cmbDebtorVoucherSerie.api = leOneTimeDebtor.api = api;
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
                case "Save":
                    Save();
                    break;
                default:
                    frmRibbon_BaseActions(ActionType);
                    break;
            }
        }

        private void Save()
        {
            MoveFocus();
            var loadedRow = (CompanySettingsClient)LoadedRow;
            var invoicenumber = loadedRow.SalesInvoice;
            var vouchernumber = loadedRow.DebtorVoucherSerie;
            bool save = true;
            if (editrow.SalesInvoice != invoicenumber || (editrow.UseVoucherAsInvoice && editrow._DebtorVoucherSerie != vouchernumber))
            {
                var msg = Uniconta.ClientTools.Localization.lookup("InvoiceNumberChange") + "\n" + Uniconta.ClientTools.Localization.lookup("ProceedConfirmation");
                if (UnicontaMessageBox.Show(msg, Uniconta.ClientTools.Localization.lookup("Warning"), System.Windows.MessageBoxButton.YesNo) == System.Windows.MessageBoxResult.No)
                    save = false;
            }
            if (save)
            {
                saveForm();
                if (api.CompanyEntity.Settings != null)
                    api.CompanyEntity.Settings._GraceDays = editrow._GraceDays;
            }
            else
                frmRibbon_BaseActions("Cancel");
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
