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
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.DataModel;
using UnicontaClient.Models;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
  
    public partial class TMJournalVoucherNumberSerie : FormBasePage
    {
        CompanySettingsClient editrow;
        public override void OnClosePage(object[] RefreshParams)
        {
            globalEvents.OnRefresh(NameOfControl, RefreshParams);
        }
        public override string NameOfControl { get { return TabControls.TMJournalVoucherNumberSerie.ToString(); } }
        public override Type TableType { get { return typeof(CompanySettingsClient); } }
        public override UnicontaBaseEntity ModifiedRow { get { return editrow; } set { editrow = (CompanySettingsClient)value; } }

        public TMJournalVoucherNumberSerie(UnicontaBaseEntity sourceData)
            : base(sourceData, true)
        {
            InitializeComponent();
            cmbProjectOrderVoucherSerie.api = cmbTMJournalVoucherSerie.api= leZeroInvoiceItem.api= api;
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
                    this.saveForm();
                    var company = api.CompanyEntity;
                    break;
                default:
                    frmRibbon_BaseActions(ActionType);
                    break;
            }
        }
    }
}
