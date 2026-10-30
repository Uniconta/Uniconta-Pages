using UnicontaClient.Models;
using UnicontaClient.Utilities;
using System;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    /// <summary>
    /// Interaction logic for ReportsPage2.xaml
    /// </summary>
    public partial class AppReportsPage2 : FormBasePage
    {
        AppReportDevExpressClient reportDeveExpressClient;
        public override string NameOfControl { get { return TabControls.AppReportsPage2; } }

        public override Type TableType { get { return typeof(AppReportDevExpressClient); } }
        public override UnicontaBaseEntity ModifiedRow { get { return reportDeveExpressClient; } set { reportDeveExpressClient = (AppReportDevExpressClient)value; } }

        public AppReportsPage2(UnicontaBaseEntity sourcedata) : base(sourcedata, true)
        {
            InitializeComponent();
            layoutControl = layoutItems;
            layoutItems.DataContext = reportDeveExpressClient;
            frmRibbon.OnItemClicked += FrmRibbon_OnItemClicked;
            teName.IsEnabled = reportDeveExpressClient.__ReportVersion() > 50 ? false : true;

        }

        private void FrmRibbon_OnItemClicked(string ActionType)
        {
            frmRibbon_BaseActions(ActionType);
        }

        public override void OnClosePage(object[] refreshParams)
        {
            globalEvents.OnRefresh(NameOfControl, refreshParams);
        }

    }
}
