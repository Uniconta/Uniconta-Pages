using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
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
using System.Windows;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class CWFrontPage : ChildWindow
    {
        public string FrontPageText2 { get; set; }
        public string FrontPageText { get; set; }
        public string FrontPageReport { get; set; }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.GLAccount))]
        public string FromAccount
        {
            get { return reportDataClient?.FromAccount; }
            set
            {
                if (reportDataClient?.FromAccount != value)
                    reportDataClient.FromAccount = value;
            }
        }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.GLAccount))]
        public string ToAccount
        {
            get { return reportDataClient?.ToAccount; }
            set
            {
                if (reportDataClient?.ToAccount != value)
                    reportDataClient.ToAccount = value;
            }
        }

        public bool SkipEmptyAccount
        {
            get { return reportDataClient?.SkipEmptyAccount ?? false; }
            set
            {
                if (reportDataClient?.SkipEmptyAccount != value)
                    reportDataClient.SkipEmptyAccount = value;
            }
        }

        public string SumAccount
        {
            get { return reportDataClient?.SumAccount; }
            set
            {
                if (reportDataClient?.SumAccount != value)
                    reportDataClient.SumAccount = value;
            }
        }

        public string AccountType
        {
            get { return reportDataClient?.AccountType; }
            set
            {
                if (reportDataClient?.AccountType != value)
                    reportDataClient.AccountType = value;
            }
        }

        public bool UseExternalName
        {
            get { return reportDataClient?.UseExternalName ?? false; }
            set
            {
                if (reportDataClient?.UseExternalName != value)
                    reportDataClient.UseExternalName = value;
            }
        }

        public DateTime FromDate
        {
            get { return reportDataClient?.FromDate ?? DateTime.Now; }
            set
            {
                if (reportDataClient?.FromDate != value)
                    reportDataClient.FromDate = value;
            }
        }

        public DateTime ToDate
        {
            get { return reportDataClient?.ToDate ?? DateTime.Now; }
            set
            {
                if (reportDataClient?.ToDate != value)
                    reportDataClient.ToDate = value;
            }
        }

        public string BalanceMethod
        {
            get { return reportDataClient?.BalanceMethod; }
            set
            {
                if (reportDataClient?.BalanceMethod != value)
                    reportDataClient.BalanceMethod = value;
            }
        }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.GLReportTemplate))]
        public string CompanyAccountTemplate
        {
            get { return reportDataClient?.Template; }
            set
            {
                if (reportDataClient?.Template != value)
                    reportDataClient.Template = value;
            }
        }

        private CrudAPI Api;
        private BalanceFrontPageReportDataClient reportDataClient;
        public CWFrontPage(CrudAPI api, string title, string text, string text2, string report, BalanceFrontPageReportDataClient balanceFrontPageReportData)
        {
            FrontPageText = text;
            FrontPageText2 = text2;
            FrontPageReport = report;
            reportDataClient = balanceFrontPageReportData;
            this.DataContext = this;
            InitializeComponent();
            Api = api;
            this.Title = title;
            cbFromAccount.api = cbToAccount.api = cbTemplate.api = api;
            cmbSumAccount.ItemsSource = new string[] { Uniconta.ClientTools.Localization.lookup("Show"), Uniconta.ClientTools.Localization.lookup("Hide"), Uniconta.ClientTools.Localization.lookup("OnlyShow") }; ;
            cmbSumAccount.IsEditable = false;

            cmbAccountType.ItemsSource = new string[] { Uniconta.ClientTools.Localization.lookup("All"), Uniconta.ClientTools.Localization.lookup("AccountTypePL"), Uniconta.ClientTools.Localization.lookup("AccountTypeBalance") };
            cmbAccountType.IsEditable = false;
            LoadTemplates();
        }

        async private void LoadTemplates()
        {
            var instance = Activator.CreateInstance(typeof(StandardBalanceFrontPageClient)) as UnicontaBaseEntity;
            var source = await Api.Query(instance, null, null) as UserReportDevExpressClient[];
            if (source != null && source.Length > 0)
            {
                var templates = source.Select(p => p.Name).ToList();
                templates.Insert(0, string.Empty);
                cmbCoverPageTemplate.ItemsSource = templates.ToArray();
            }
            else
                cmbCoverPageTemplate.ItemsSource = null;
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
}

