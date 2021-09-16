using Uniconta.API.System;
using UnicontaClient.Models;
using UnicontaClient.Utilities;
using Uniconta.ClientTools;
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
using System.Windows;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.Util;
using DevExpress.Xpf.Editors;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class InvoicePage2 : FormBasePage
    {
        DebtorInvoiceClient editrow;
        public override void OnClosePage(object[] RefreshParams)
        {
            globalEvents.OnRefresh(NameOfControl, RefreshParams);
        }
        public override string NameOfControl { get { return TabControls.InvoicePage2; } }

        public override Type TableType { get { return typeof(DebtorInvoiceClient); } }
        public override UnicontaBaseEntity ModifiedRow { get { return editrow; } set { editrow = (DebtorInvoiceClient)value; } }

        public InvoicePage2(UnicontaBaseEntity sourcedata, bool isEdit = true)
            : base(sourcedata, isEdit)
        {
            InitPage(sourcedata);
        }

        void InitPage(UnicontaBaseEntity sourcedata)
        {
            InitializeComponent();
            ribbonControl = frmRibbon;
            layoutControl = layoutItems;
            layoutItems.DataContext = editrow;
            cmbDelCountry.ItemsSource = Enum.GetValues(typeof(Uniconta.Common.CountryCode));
            leDeliveryTerm.api = lePayment.api = leShipment.api = leInstallation.api = leOrderGroup.api= leLayoutGroup.api= api;
            frmRibbon.OnItemClicked += FrmRibbon_OnItemClicked;
        }

        private void FrmRibbon_OnItemClicked(string ActionType)
        {
            frmRibbon_BaseActions(ActionType);
        }
    }
}
