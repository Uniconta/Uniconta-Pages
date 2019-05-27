using Uniconta.API.System;
using UnicontaClient.Models;
using UnicontaClient.Utilities;
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
    public partial class CreditorTranOpenPage2 : FormBasePage
    {
        CreditorTransOpenClient editrow;
        public override void OnClosePage(object[] RefreshParams)
        {
            globalEvents.OnRefresh(NameOfControl, RefreshParams);

        }

        public override Type TableType { get { return typeof(CreditorTransOpenClient); } }
        public override string NameOfControl { get { return TabControls.CreditorTranOpenPage2; } }
        public override UnicontaBaseEntity ModifiedRow { get { return editrow; } set { editrow = (CreditorTransOpenClient)value; } }
        /*For Edit*/
        public CreditorTranOpenPage2(UnicontaBaseEntity sourcedata)
            : base(sourcedata, true)
        {
            InitializeComponent();
            InitPage(api);
        }
        public CreditorTranOpenPage2(CrudAPI crudApi, string dummy)
            : base(crudApi, dummy)
        {
            InitializeComponent();
            InitPage(crudApi);
#if !SILVERLIGHT
            FocusManager.SetFocusedElement(txtCashDiscount, txtCashDiscount);
#endif
        }
        void InitPage(CrudAPI crudapi)
        {
            Paymentlookupeditior.api = lePaymtFormat.api = Vatlookupeditior.api = api;
            BusyIndicator = busyIndicator;
            layoutControl = layoutItems;
            frmRibbon.DisableButtons("Delete");
            layoutItems.DataContext = editrow;
            frmRibbon.OnItemClicked += frmRibbon_OnItemClicked;
        }

        private void frmRibbon_OnItemClicked(string ActionType)
        {
            frmRibbon_BaseActions(ActionType);
        }
    }
}
