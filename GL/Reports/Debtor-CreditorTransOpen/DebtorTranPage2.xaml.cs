using Uniconta.API.System;
using UnicontaClient.Models;
using UnicontaClient.Utilities;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
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

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class DebtorTranPage2 : FormBasePage
    {
        DebtorTransOpenClient editrow;
        public override void OnClosePage(object[] RefreshParams)
        {
            globalEvents.OnRefresh(NameOfControl, RefreshParams);
        }
        public override string NameOfControl { get { return TabControls.DebtorTranPage2; } }

        public override Type TableType { get { return typeof(DebtorTransOpenClient); } }
        public override UnicontaBaseEntity ModifiedRow { get { return editrow; } set { editrow = (DebtorTransOpenClient)value; } }
        public DebtorTranPage2(UnicontaBaseEntity sourcedata)
            : base(sourcedata, true)
        {
            InitializeComponent();
            InitPage();
        }
        public DebtorTranPage2(CrudAPI crudApi, string dummy)
            : base(crudApi, dummy)
        {
            InitializeComponent();
            InitPage();
        }
        void InitPage()
        {
            Paymentlookupeditior.api= Vatlookupeditior.api = PaymtFormatlookupeditor.api = api;
            Paymentlookupeditior.api = api;
            layoutControl = layoutItems;
            frmRibbon.DisableButtons("Delete");
            layoutItems.DataContext = editrow;
            frmRibbon.OnItemClicked += frmRibbon_OnItemClicked;
            if (!api.CompanyEntity._CollectionLetter)
                grpReminders.Visibility = System.Windows.Visibility.Hidden;
        }

        private void frmRibbon_OnItemClicked(string ActionType)
        {
            frmRibbon_BaseActions(ActionType);
        }
    }
}
